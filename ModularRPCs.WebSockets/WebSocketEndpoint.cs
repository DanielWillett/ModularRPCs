using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketEndpoint : IModularRpcRemoteEndpoint
{
    internal Action<ClientWebSocketOptions>? ConfigureOptions;
    private WebSocket? _webSocket;
    public int StreamSendBufferSize { get; set; } = 4096;
    public int ReceiveBufferSize { get; set; } = 4096;
    public Uri Uri { get; }
    public bool IsClient { get; }
    private WebSocketEndpoint(Uri uri, Action<ClientWebSocketOptions>? configureOptions, bool isClient)
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
    /// Create a new <see cref="WebSocketEndpoint"/> as a server receiving a client connection from an existing endpoint.
    /// </summary>
    /// <param name="uri">Address of the endpoint, used for display.</param>
    /// <param name="webSocket">Accepted server-side web socket.</param>
    public static WebSocketEndpoint AsServer(Uri uri, WebSocket webSocket)
    {
        if (webSocket == null)
            throw new ArgumentNullException(nameof(webSocket));

        return new WebSocketEndpoint(uri ?? throw new ArgumentNullException(nameof(uri)), null, false)
        {
            _webSocket = webSocket
        };
    }

    /// <summary>
    /// Request connection as a client to a given <see cref="Uri"/>.
    /// </summary>
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        ClientWebSocket ws = new ClientWebSocket();

        ConfigureOptions?.Invoke(ws.Options);

        await ws.ConnectAsync(Uri, token).ConfigureAwait(false);
        WebSocketClientsideRemoteRpcConnection remote = new WebSocketClientsideRemoteRpcConnection(this, connectionLifetime, ws, StreamSendBufferSize);
        _ = new WebSocketClientsideLocalRpcConnection(router, serializer, remote, ReceiveBufferSize);

        await connectionLifetime.TryAddNewConnection(remote, token);

        return remote;
    }

    /// <summary>
    /// Accept a new client's <see cref="WebSocket"/> and create an RPC connection from it.
    /// </summary>
    public async ValueTask<IModularRpcLocalConnection> AcceptClientConnection(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (IsClient || _webSocket == null)
            throw new InvalidOperationException(Properties.Exceptions.WebSocketNotServerEndpoint);

        WebSocketServersideLocalRpcConnection local = new WebSocketServersideLocalRpcConnection(router, serializer, this, ReceiveBufferSize);
        WebSocketServersideRemoteRpcConnection remote = new WebSocketServersideRemoteRpcConnection(_webSocket, local, connectionLifetime, StreamSendBufferSize);

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

    public override string ToString() => (IsClient ? "Client: \"" : "Server: \"") + Uri + "\"";
}
