using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketEndpoint : IModularRpcRemoteEndpoint
{
    internal Action<ClientWebSocketOptions>? ConfigureOptions;
    private WebSocket? _webSocket;
    private PlateauingDelay _delaySettings = new PlateauingDelay(amplifier: 6, climb: 2.5, maximum: 300, start: 10);
    private bool _leaveOpen;

    /// <summary>
    /// Size of the buffer used to send data from a stream. Not used for implementations that send data from raw binary data.
    /// </summary>
    public int StreamSendBufferSize { get; set; } = 4096;

    /// <summary>
    /// Size of the buffer used to receive data.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// Sets both <see cref="StreamSendBufferSize"/> and <see cref="ReceiveBufferSize"/>.
    /// </summary>
    public int BufferSize
    {
        get => (StreamSendBufferSize + ReceiveBufferSize) / 2;
        set
        {
            StreamSendBufferSize = value;
            ReceiveBufferSize = value;
        }
    }

    /// <summary>
    /// Settings for reconnect delays on client connections. See <see cref="PlateauingDelay"/> for more info.
    /// </summary>
    /// <remarks>This does nothing for endpoints created with <see cref="AsServer"/>.</remarks>
    public ref PlateauingDelay DelaySettings => ref _delaySettings;

    /// <summary>
    /// Should client connections attempt to reconnect if they lose connection? This will be done using the timing defined in <see cref="DelaySettings"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. This does nothing for endpoints created with <see cref="AsServer"/>.</remarks>
    public bool ShouldAutoReconnect { get; set; } = false;

    /// <summary>
    /// The endpoint the web socket can connect to.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// If this endpoint is for a client connection or a serverside client's connection.
    /// </summary>
    public bool IsClient { get; }
    protected internal WebSocketEndpoint(Uri uri, Action<ClientWebSocketOptions>? configureOptions, bool isClient)
    {
        ConfigureOptions = configureOptions;
        Uri = uri;
        IsClient = isClient;
    }

    /// <summary>
    /// Create a new <see cref="WebSocketEndpoint"/> as a client connecting to a server.
    /// </summary>
    public static WebSocketEndpoint AsClient(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null)
    {
        return new WebSocketEndpoint(uri ?? throw new ArgumentNullException(nameof(uri)), configureOptions, true);
    }

    /// <summary>
    /// Create a new <see cref="WebSocketEndpoint"/> as a client connecting to a server.
    /// </summary>
    /// <param name="serviceProvider">Service provider to use for creating the connections.</param>
    public static WebSocketEndpoint AsClient(IServiceProvider serviceProvider, Uri uri, Action<ClientWebSocketOptions>? configureOptions = null)
    {
        return new DependencyInjectionWebSocketEndpoint(serviceProvider, uri ?? throw new ArgumentNullException(nameof(uri)), configureOptions, true);
    }

    /// <summary>
    /// Create a new <see cref="WebSocketEndpoint"/> as a server receiving a client connection from an existing endpoint.
    /// </summary>
    /// <param name="uri">Address of the endpoint, used for display.</param>
    /// <param name="webSocket">Accepted server-side web socket.</param>
    /// <param name="leaveOpen">Should <paramref name="webSocket"/> be left open (not closed).</param>
    public static WebSocketEndpoint AsServer(Uri uri, WebSocket webSocket, bool leaveOpen)
    {
        if (webSocket == null)
            throw new ArgumentNullException(nameof(webSocket));

        return new WebSocketEndpoint(uri ?? throw new ArgumentNullException(nameof(uri)), null, false)
        {
            _webSocket = webSocket,
            _leaveOpen = leaveOpen
        };
    }

    /// <summary>
    /// Create a new <see cref="WebSocketEndpoint"/> as a server receiving a client connection from an existing endpoint.
    /// </summary>
    /// <param name="serviceProvider">Service provider to use for creating the connections.</param>
    /// <param name="uri">Address of the endpoint, used for display.</param>
    /// <param name="webSocket">Accepted server-side web socket.</param>
    /// <param name="leaveOpen">Should <paramref name="webSocket"/> be left open (not closed).</param>
    public static WebSocketEndpoint AsServer(IServiceProvider serviceProvider, Uri uri, WebSocket webSocket, bool leaveOpen)
    {
        if (webSocket == null)
            throw new ArgumentNullException(nameof(webSocket));

        return new DependencyInjectionWebSocketEndpoint(serviceProvider, uri ?? throw new ArgumentNullException(nameof(uri)), null, false)
        {
            _webSocket = webSocket,
            _leaveOpen = leaveOpen
        };
    }

    /// <summary>
    /// Request connection as a client to a given <see cref="Uri"/>. Requires a service provider or an error will be thrown.
    /// </summary>
    /// <exception cref="InvalidOperationException">When created, this object must have been passed a <see cref="IServiceProvider"/>.</exception>
    public Task<WebSocketClientsideRemoteRpcConnection> RequestConnectionAsync(CancellationToken token = default)
    {
        if (this is not DependencyInjectionWebSocketEndpoint depInj)
            throw new InvalidOperationException(Properties.Exceptions.NoServiceProvider);

        return RequestConnectionAsyncIntl(depInj, token);
    }

    private Task<WebSocketClientsideRemoteRpcConnection> RequestConnectionAsyncIntl(DependencyInjectionWebSocketEndpoint depInj, CancellationToken token)
    {
        IServiceProvider serviceProvider = depInj.ServiceProvider;
        return RequestConnectionAsync(
            serviceProvider.GetRequiredService<IRpcRouter>(),
            serviceProvider.GetRequiredService<IRpcConnectionLifetime>(),
            serviceProvider.GetRequiredService<IRpcSerializer>(),
            token
        );
    }
    private void TryAddLogging(DependencyInjectionWebSocketEndpoint depInj, IRefSafeLoggable loggable)
    {
        ILoggerFactory loggerFactory = (ILoggerFactory)depInj.ServiceProvider.GetService(typeof(ILoggerFactory));
        ILogger logger = loggerFactory.CreateLogger(loggable.GetType());
        loggable.SetLogger(logger);
    }

    /// <summary>
    /// Request connection as a client to a given <see cref="Uri"/>.
    /// </summary>
    public async Task<WebSocketClientsideRemoteRpcConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        ClientWebSocket ws = new ClientWebSocket();

        ConfigureOptions?.Invoke(ws.Options);

        await ws.ConnectAsync(Uri, token).ConfigureAwait(false);
        WebSocketClientsideRemoteRpcConnection remote = new WebSocketClientsideRemoteRpcConnection(this, connectionLifetime, ws, StreamSendBufferSize);
        WebSocketClientsideLocalRpcConnection local = new WebSocketClientsideLocalRpcConnection(router, serializer, remote, ShouldAutoReconnect, _delaySettings, ReceiveBufferSize);

        if (this is DependencyInjectionWebSocketEndpoint depInj)
        {
            TryAddLogging(depInj, local);
        }

        local.TryStartListening();

        await connectionLifetime.TryAddNewConnection(remote, token);

        return remote;
    }


    /// <summary>
    /// Accept a new client's <see cref="WebSocket"/> and create an RPC connection from it. Requires a service provider or an error will be thrown.
    /// </summary>
    /// <exception cref="InvalidOperationException">When created, this object must have been passed a <see cref="IServiceProvider"/>.</exception>
    public ValueTask<WebSocketServersideLocalRpcConnection> AcceptClientConnection(CancellationToken token = default)
    {
        if (this is not DependencyInjectionWebSocketEndpoint depInj)
            throw new InvalidOperationException(Properties.Exceptions.NoServiceProvider);

        return AcceptClientConnectionIntl(depInj, token);
    }

    private ValueTask<WebSocketServersideLocalRpcConnection> AcceptClientConnectionIntl(DependencyInjectionWebSocketEndpoint depInj, CancellationToken token)
    {
        IServiceProvider serviceProvider = depInj.ServiceProvider;
        return AcceptClientConnection(
            serviceProvider.GetRequiredService<IRpcRouter>(),
            serviceProvider.GetRequiredService<IRpcConnectionLifetime>(),
            serviceProvider.GetRequiredService<IRpcSerializer>(),
            token
        );
    }

    /// <summary>
    /// Accept a new client's <see cref="WebSocket"/> and create an RPC connection from it.
    /// </summary>
    public async ValueTask<WebSocketServersideLocalRpcConnection> AcceptClientConnection(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (IsClient || _webSocket == null)
            throw new InvalidOperationException(Properties.Exceptions.WebSocketNotServerEndpoint);

        WebSocketServersideLocalRpcConnection local = new WebSocketServersideLocalRpcConnection(router, serializer, this, ReceiveBufferSize);
        WebSocketServersideRemoteRpcConnection remote = new WebSocketServersideRemoteRpcConnection(_webSocket, local, connectionLifetime, _leaveOpen, StreamSendBufferSize);

        if (this is DependencyInjectionWebSocketEndpoint depInj)
        {
            TryAddLogging(depInj, local);
        }

        await local.InitializeConnectionAsync(token);

        if (await connectionLifetime.TryAddNewConnection(remote, token))
            return local;

        Exception? innerEx = null;
        try
        {
            await local.DisposeAsync();
        }
        catch (Exception ex)
        {
            innerEx = ex;
        }

        throw new RpcException(Properties.Exceptions.RpcExceptionUnableToAddConnectionToLifetime, innerEx!);
    }

    async Task<IModularRpcRemoteConnection> IModularRpcRemoteEndpoint.RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token)
        => await RequestConnectionAsync(router, connectionLifetime, serializer, token).ConfigureAwait(false);

    /// <inheritdoc />
    public override string ToString() => (IsClient ? "Client: \"" : "Server: \"") + Uri.GetLeftPart(UriPartial.Path) + "\"";
}