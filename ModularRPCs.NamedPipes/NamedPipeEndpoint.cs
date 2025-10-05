using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// Endpoint used to create a RPC connection using <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.namedpipeserverstream">Named Pipes</see>.
/// </summary>
public class NamedPipeEndpoint : IModularRpcRemoteEndpoint
{
    private readonly IServiceProvider? _serviceProvider;

    private PlateauingDelay _delaySettings = new PlateauingDelay(amplifier: 6, climb: 2.5, maximum: 300, start: 10);
    private string _serverName = ".";
    private int _maxNumServerInstances = NamedPipeServerStream.MaxAllowedServerInstances;
    private int _inBufferSize;
    private int _outBufferSize;
    private bool _shouldAutoReconnect;
    private int _localBufferSize = 4096;

    /// <summary>
    /// Settings for reconnect delays on client connections. See <see cref="PlateauingDelay"/> for more info.
    /// </summary>
    /// <remarks>Not supported for endpoints created for servers.</remarks>
    /// <exception cref="NotSupportedException">Referenced on an endpoint made for a server.</exception>
    public ref PlateauingDelay DelaySettings
    {
        get
        {
            if (!IsClient)
                throw new NotSupportedException();

            return ref _delaySettings;
        }
    }

    /// <summary>
    /// Should client connections attempt to reconnect if they lose connection? This will be done using the timing defined in <see cref="DelaySettings"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Not supported for endpoints created for servers.</remarks>
    /// <exception cref="NotSupportedException">Referenced on an endpoint made for a server.</exception>
    public bool ShouldAutoReconnect
    {
        get => !IsClient ? throw new NotSupportedException() : _shouldAutoReconnect;
        set
        {
            if (!IsClient)
                throw new NotSupportedException();

            _shouldAutoReconnect = value;
        }
    }

    /// <summary>
    /// The unique name of the pipe server to host or connect to.
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// Whether or not this endpoint is for a <see cref="NamedPipeClientStream"/> instead of a <see cref="NamedPipeServerStream"/>.
    /// </summary>
    public bool IsClient { get; }

    /// <summary>
    /// The size of the buffer used to read from the pipe.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value was not a positive number.</exception>
    public int LocalBufferSize
    {
        get => _localBufferSize;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _localBufferSize = value;
        }
    }

    /// <summary>
    /// The server name for clients connecting to a named pipe server.
    /// </summary>
    /// <remarks>Defaults to <c>"."</c>. This is not supported for endpoints created for servers.</remarks>
    /// <exception cref="NotSupportedException">Accessed on an endpoint made for a server.</exception>
    public string ServerName
    {
        get => !IsClient ? throw new NotSupportedException() : _serverName;
        set
        {
            if (!IsClient)
                throw new NotSupportedException();

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(Properties.Resources.MissingOrInvalidPipeServerName, nameof(value));

            _serverName = value;
        }
    }

    /// <summary>
    /// Maximum number of server instances. Specify a fixed value between 1 and 254, or use <see cref="NamedPipeServerStream.MaxAllowedServerInstances"/> to use the
    /// maximum amount allowed by system resources.
    /// </summary>
    /// <remarks>Defaults to <see cref="NamedPipeServerStream.MaxAllowedServerInstances"/>. This is not supported for endpoints created for clients.</remarks>
    /// <exception cref="NotSupportedException">Accessed on an endpoint made for a client.</exception>
    public int MaxNumberOfServerInstances
    {
        get => IsClient ? throw new NotSupportedException() : _maxNumServerInstances;
        set
        {
            if (IsClient)
                throw new NotSupportedException();

            AssertServerPropertyCanBeChanged();

            if (value is < 1 or > 254
                && value != NamedPipeServerStream.MaxAllowedServerInstances)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _maxNumServerInstances = value;
        }
    }

    /// <summary>
    /// Incoming buffer size, 0 or higher. Note: this size is always advisory; OS uses a suggestion.
    /// </summary>
    /// <remarks>Defaults to <c>0</c>. This is not supported for endpoints created for clients.</remarks>
    /// <exception cref="NotSupportedException">Accessed on an endpoint made for a client.</exception>
    public int InBufferSize
    {
        get => IsClient ? throw new NotSupportedException() : _inBufferSize;
        set
        {
            if (IsClient)
                throw new NotSupportedException();

            AssertServerPropertyCanBeChanged();

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _inBufferSize = value;
        }
    }

    /// <summary>
    /// Outgoing buffer size, 0 or higher. Note: this size is always advisory; OS uses a suggestion.
    /// </summary>
    /// <remarks>Defaults to <c>0</c>. This is not supported for endpoints created for clients.</remarks>
    /// <exception cref="NotSupportedException">Accessed on an endpoint made for a client.</exception>
    public int OutBufferSize
    {
        get => IsClient ? throw new NotSupportedException() : _outBufferSize;
        set
        {
            if (IsClient)
                throw new NotSupportedException();

            AssertServerPropertyCanBeChanged();

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _outBufferSize = value;
        }
    }

    private protected virtual void AssertServerPropertyCanBeChanged() { }

    /// <summary>
    /// Create a new <see cref="NamedPipeEndpoint"/> for a server or client with the given unique <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="serviceProvider">Service provider to use for creating the connections.</param>
    /// <param name="pipeName">The unique name of the pipe server to host or connect to.</param>
    /// <param name="isClient">Whether or not this endpoint is for a <see cref="NamedPipeClientStream"/> instead of a <see cref="NamedPipeServerStream"/>.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">Pipe name is invalid or reserved ('anonymous').</exception>
    private protected NamedPipeEndpoint(IServiceProvider? serviceProvider, string pipeName, bool isClient)
    {
        if (pipeName == null)
            throw new ArgumentNullException(nameof(pipeName));

        if (string.IsNullOrWhiteSpace(pipeName) || string.Equals(pipeName, "anonymous", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(Properties.Resources.MissingOrInvalidPipeName, nameof(pipeName));

        if (this is NamedPipeServer != !isClient)
            throw new InvalidOperationException("Unexpected server type not matching up with isClient. This shouldn't happen.");

        _serviceProvider = serviceProvider;
        PipeName = pipeName;
        IsClient = isClient;
    }

    /// <summary>
    /// Create a new <see cref="NamedPipeEndpoint"/> as a client connecting to a server.
    /// </summary>
    /// <param name="pipeName">The unique name of the pipe server to host or connect to.</param>
    /// <param name="serverName">The name of the remote computer to connect to, or <see langword="null"/> to specify the local computer. Note that cross-machine pipes are not supported on Unix.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">Pipe name is invalid or reserved ('anonymous').</exception>
    public static NamedPipeEndpoint AsClient(string pipeName, string? serverName = null)
    {
        NamedPipeEndpoint ep = new NamedPipeEndpoint(null, pipeName, isClient: true);
        if (!string.IsNullOrWhiteSpace(serverName) && !string.Equals(serverName, ".", StringComparison.Ordinal))
        {
            ep.ServerName = serverName!;
        }

        return ep;
    }

    /// <summary>
    /// Create a new <see cref="NamedPipeEndpoint"/> as a client connecting to a server.
    /// </summary>
    /// <param name="serviceProvider">Service provider to use for creating the connections.</param>
    /// <param name="pipeName">The unique name of the pipe server to host or connect to.</param>
    /// <param name="serverName">The name of the remote computer to connect to, or <see langword="null"/> to specify the local computer. Note that cross-machine pipes are not supported on Unix.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">Pipe name is invalid or reserved ('anonymous').</exception>
    public static NamedPipeEndpoint AsClient(IServiceProvider serviceProvider, string pipeName, string? serverName = null)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        NamedPipeEndpoint ep = new NamedPipeEndpoint(serviceProvider, pipeName, isClient: true);
        if (!string.IsNullOrWhiteSpace(serverName) && !string.Equals(serverName, ".", StringComparison.Ordinal))
        {
            ep.ServerName = serverName!;
        }

        return ep;
    }

    /// <summary>
    /// Create a new <see cref="NamedPipeEndpoint"/> as a server receiving a client connection from an existing endpoint.
    /// </summary>
    /// <param name="pipeName">The unique name of the pipe server to host or connect to.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">Pipe name is invalid or reserved ('anonymous').</exception>
    public static NamedPipeServer AsServer(string pipeName)
    {
        return new NamedPipeServer(null, pipeName);
    }

    /// <summary>
    /// Create a new <see cref="NamedPipeEndpoint"/> as a server receiving a client connection from an existing endpoint.
    /// </summary>
    /// <param name="serviceProvider">Service provider to use for creating the connections.</param>
    /// <param name="pipeName">The unique name of the pipe server to host or connect to.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">Pipe name is invalid or reserved ('anonymous').</exception>
    public static NamedPipeServer AsServer(IServiceProvider serviceProvider, string pipeName)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        return new NamedPipeServer(serviceProvider, pipeName);
    }

    /// <summary>
    /// Request connection as a client to the named pipe. Requires a service provider or an error will be thrown.
    /// </summary>
    /// <exception cref="InvalidOperationException">No service provider was passed on creation of this endpoint. Use the other overload instead.</exception>
    /// <exception cref="PlatformNotSupportedException"/>
    /// <exception cref="IOException"/>
    public Task<NamedPipeClientsideRemoteRpcConnection> RequestConnectionAsync(CancellationToken token = default)
    {
        return RequestConnectionAsync(Timeout.InfiniteTimeSpan, token);
    }

    /// <summary>
    /// Request connection as a client to the named pipe. Requires a service provider or an error will be thrown.
    /// </summary>
    /// <exception cref="InvalidOperationException">No service provider was passed on creation of this endpoint. Use the other overload instead.</exception>
    /// <exception cref="PlatformNotSupportedException"/>
    /// <exception cref="IOException"/>
    public Task<NamedPipeClientsideRemoteRpcConnection> RequestConnectionAsync(TimeSpan timeout, CancellationToken token = default)
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException(DanielWillett.ModularRpcs.Properties.Exceptions.NoServiceProvider);

        return RequestConnectionAsyncIntl(_serviceProvider, timeout, token);
    }

    private Task<NamedPipeClientsideRemoteRpcConnection> RequestConnectionAsyncIntl(IServiceProvider serviceProvider, TimeSpan timeout, CancellationToken token)
    {
        return RequestConnectionAsync(
            serviceProvider.GetRequiredService<IRpcRouter>(),
            serviceProvider.GetRequiredService<IRpcConnectionLifetime>(),
            serviceProvider.GetRequiredService<IRpcSerializer>(),
            timeout,
            token
        );
    }

    private protected void TryAddLogging(IRefSafeLoggable loggable)
    {
        if (_serviceProvider == null)
            return;

        ILoggerFactory loggerFactory = (ILoggerFactory)_serviceProvider.GetService(typeof(ILoggerFactory));
        if (loggerFactory == null)
            return;

        ILogger logger = loggerFactory.CreateLogger(loggable.GetType());
        loggable.SetLogger(logger);
    }

    /// <summary>
    /// Request connection as a client to the named pipe.
    /// </summary>
    /// <exception cref="NotSupportedException">Ran on a server endpoint.</exception>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="PlatformNotSupportedException"/>
    /// <exception cref="IOException"/>
    public Task<NamedPipeClientsideRemoteRpcConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        return RequestConnectionAsync(router, connectionLifetime, serializer, Timeout.InfiniteTimeSpan, token);
    }

    /// <summary>
    /// Request connection as a client to the named pipe.
    /// </summary>
    /// <exception cref="NotSupportedException">Ran on a server endpoint.</exception>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="PlatformNotSupportedException"/>
    /// <exception cref="IOException"/>
    public async Task<NamedPipeClientsideRemoteRpcConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, TimeSpan timeout, CancellationToken token = default)
    {
        if (router == null) throw new ArgumentNullException(nameof(router));
        if (connectionLifetime == null) throw new ArgumentNullException(nameof(connectionLifetime));
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));

        if (!IsClient)
            throw new NotSupportedException();

        token.ThrowIfCancellationRequested();
        
        NamedPipeClientStream client = new NamedPipeClientStream(
            ServerName,
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous,
            TokenImpersonationLevel.None,
            HandleInheritability.None
        );

        try
        {
            int timeoutInt = timeout.Ticks <= 0 ? Timeout.Infinite : (int)(long)timeout.TotalMilliseconds;

            await client.ConnectAsync(timeoutInt, token).ConfigureAwait(false);

            CancellationTokenSource cts = new CancellationTokenSource();
            NamedPipeClientsideRemoteRpcConnection remote = new NamedPipeClientsideRemoteRpcConnection(this, client, cts, ownsCts: true);
            NamedPipeClientsideLocalRpcConnection local = new NamedPipeClientsideLocalRpcConnection(router, serializer, remote, cts);

            TryAddLogging(local);

            local.StartListening();

            await connectionLifetime.TryAddNewConnection(remote, CancellationToken.None);

            return remote;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private ValueTask CreateServerAsyncIntl(IServiceProvider serviceProvider, CancellationToken token)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        return CreateServerAsync(
            serviceProvider.GetRequiredService<IRpcRouter>(),
            serviceProvider.GetRequiredService<IRpcConnectionLifetime>(),
            serviceProvider.GetRequiredService<IRpcSerializer>(),
            token
        );
    }

    /// <summary>
    /// Starts hosting a server for this named pipe using a <see cref="NamedPipeServerStream"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Ran on a client endpoint.</exception>
    /// <exception cref="InvalidOperationException">
    /// No service provider was passed on creation of this endpoint. Use the other overload instead.
    /// -- OR --
    /// Called more than once on an endpoint.
    /// </exception>
    /// <exception cref="InvalidOperationException"></exception>
    public ValueTask CreateServerAsync(CancellationToken token = default)
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException(DanielWillett.ModularRpcs.Properties.Exceptions.NoServiceProvider);

        token.ThrowIfCancellationRequested();
        return CreateServerAsyncIntl(_serviceProvider, token);
    }

    /// <summary>
    /// Starts hosting a server for this named pipe using a <see cref="NamedPipeServerStream"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Ran on a client endpoint.</exception>
    /// <exception cref="InvalidOperationException">Called more than once on an endpoint.</exception>
    /// <exception cref="PlatformNotSupportedException"/>
    /// <exception cref="IOException"/>
    public ValueTask CreateServerAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (IsClient)
            throw new NotSupportedException();

        token.ThrowIfCancellationRequested();
        NamedPipeServer server = (NamedPipeServer)this;
        return server.HostServer(router, connectionLifetime, serializer, token);
    }

    /// <summary>
    /// Stops hosting a server for this named pipe using a <see cref="NamedPipeServerStream"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Ran on a client endpoint.</exception>
    /// <exception cref="InvalidOperationException"><see cref="CreateServerAsync(CancellationToken)"/> needs to be called first.</exception>
    public ValueTask CloseServerAsync(CancellationToken token = default)
    {
        if (IsClient)
            throw new NotSupportedException();

        token.ThrowIfCancellationRequested();
        NamedPipeServer server = (NamedPipeServer)this;
        
        if (!server.HasStarted)
            throw new InvalidOperationException(Properties.Resources.NamedPipeServerNotStarted);

        return server.DisposeAsync();
    }

    async Task<IModularRpcRemoteConnection> IModularRpcRemoteEndpoint.RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token)
        => await RequestConnectionAsync(router, connectionLifetime, serializer, token).ConfigureAwait(false);

    /// <inheritdoc />
    public override string ToString()
    {
        return IsClient
            ? $"""
               Client: "\\{_serverName}\pipe\{PipeName}"
               """
            : $"""
               Server: "\\.\pipe\{PipeName}"
               """;
    }
}