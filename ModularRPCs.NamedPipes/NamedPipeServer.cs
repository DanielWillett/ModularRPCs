using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// A child of <see cref="NamedPipeEndpoint"/> that also handles keeping track of active pipes.
/// </summary>
public sealed class NamedPipeServer : NamedPipeEndpoint, IDisposable, IRefSafeLoggable
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    , IAsyncDisposable
#endif
{
    private IRpcConnectionLifetime? _connectionLifetime;
    private IRpcRouter? _router;
    private IRpcSerializer? _serializer;
    private CancellationTokenSource? _unhostCancellationTokenSource;
    private IAsyncResult? _waitingServerTask;
    
    private int _hasStarted;
    private int _isDisposed;

    private NamedPipeServerStream? _waitingServer;
    private AsyncCallback? _connectDelegate;
    private object? _logger;

    internal bool HasStarted => _hasStarted != 0;

    /// <inheritdoc />
    internal NamedPipeServer(IServiceProvider? serviceProvider, string pipeName)
        : base(serviceProvider, pipeName, false)
    {

    }

    private protected override void AssertServerPropertyCanBeChanged()
    {
        if (_hasStarted != 0)
            throw new InvalidOperationException(Properties.Resources.NamedPipeServerAlreadyStartedImmutableProperties);
    }

    private NamedPipeServerStream CreateNewServer()
    {
        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            MaximumConnections,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            InBufferSize,
            OutBufferSize
        );
    }

    internal ValueTask HostServer(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (Interlocked.Exchange(ref _hasStarted, 1) != 0)
            throw new InvalidOperationException(Properties.Resources.NamedPipeServerAlreadyStarted);

        TryAddLogging(this);

        _router = router;
        _serializer = serializer;
        _unhostCancellationTokenSource = new CancellationTokenSource();

        _connectionLifetime = connectionLifetime ?? throw new ArgumentNullException(nameof(connectionLifetime));
        _connectionLifetime.ConnectionRemoved += ConnectionRemoved;

        _connectDelegate = EndConnect;

        _waitingServer = CreateNewServer();
        _waitingServerTask = _waitingServer.BeginWaitForConnection(_connectDelegate, _waitingServer);

        return default;
    }

    private void ConnectionRemoved(IRpcConnectionLifetime arg1, IModularRpcRemoteConnection arg2)
    {
        // if the server is full and a connection is removed then we can add another server
        if (_waitingServer != null)
            return;

        NamedPipeServerStream newServer;
        try
        {
            newServer = CreateNewServer();
        }
        catch (IOException)
        {
            // server is most likely full
            return;
        }

        if (Interlocked.CompareExchange(ref _waitingServer, newServer, null) != null)
        {
            newServer.Dispose();
            return;
        }

        try
        {
            _waitingServerTask = newServer.BeginWaitForConnection(_connectDelegate!, _waitingServer);
        }
        catch (ObjectDisposedException)
        {
            Interlocked.CompareExchange(ref _waitingServer, null, newServer);
        }
        catch (Exception ex)
        {
            this.LogError(ex, Properties.Resources.LogErrorConnectingClientToServer);
        }
    }

    private void EndConnect(IAsyncResult result)
    {
        NamedPipeServerStream connectingStream = (NamedPipeServerStream)result.AsyncState;

        if (_isDisposed != 0)
        {
            NamedPipeServerStream? str = Interlocked.Exchange(ref _waitingServer, null);
            try
            {
                connectingStream.EndWaitForConnection(result);
            }
            catch (ObjectDisposedException)
            {
                if (ReferenceEquals(str, connectingStream))
                    return;
            }
            catch (Exception ex)
            {
                this.LogError(ex, null);
            }
            str?.Dispose();
            return;
        }

        NamedPipeServerStream? newServer;
        try
        {
            newServer = CreateNewServer();
        }
        catch (IOException)
        {
            // server is most likely full
            newServer = null;
        }

        // _waitingServer = CreateNewServer();
        if (!ReferenceEquals(Interlocked.CompareExchange(ref _waitingServer, newServer, connectingStream), connectingStream))
        {
            // should never happen
            try
            {
                connectingStream.EndWaitForConnection(result);
            }
            catch (Exception ex)
            {
                this.LogError(ex, null);
            }

            try
            {
                connectingStream.Dispose();
            }
            finally
            {
                newServer?.Dispose();
            }
            return;
        }

        try
        {
            connectingStream.EndWaitForConnection(result);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception ex)
        {
            this.LogError(ex, Properties.Resources.LogErrorConnectingClientToServer);
        }

        ValueTask addTask = AddConnectionForStream(connectingStream);

        if (newServer == null)
        {
            _waitingServerTask = null;
        }
        else
        {
            try
            {
                _waitingServerTask = newServer.BeginWaitForConnection(_connectDelegate!, _waitingServer);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                this.LogError(ex, Properties.Resources.LogErrorConnectingClientToServer);
            }
        }

        if (addTask.IsCompleted)
            return;

        ValueTask vt2 = addTask;
        Task.Run(async () =>
        {
            try
            {
                await vt2.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.LogError(ex, Properties.Resources.LogErrorConnectingClientToServer);
            }
        });
    }

    private ValueTask AddConnectionForStream(NamedPipeServerStream connectingStream)
    {
        NamedPipeServersideRemoteRpcConnection remote = new NamedPipeServersideRemoteRpcConnection(this, connectingStream);
        NamedPipeServersideLocalRpcConnection local = new NamedPipeServersideLocalRpcConnection(_router!, _serializer!, remote, _unhostCancellationTokenSource!);

        TryAddLogging(local);

        ValueTask<bool> task;
        try
        {
            task = _connectionLifetime!.TryAddNewConnection(remote, _unhostCancellationTokenSource!.Token);
        }
        catch (Exception ex)
        {
            task = new ValueTask<bool>(false);
            remote.Local.LogError(ex, DanielWillett.ModularRpcs.Properties.Exceptions.RpcExceptionUnableToAddConnectionToLifetime);
        }

        if (!task.IsCompleted)
        {
            return new ValueTask(Core(task, remote));
        }

        if (!task.Result)
        {
            Exception? innerEx = null;
            try
            {
                remote.Dispose();
            }
            catch (Exception ex)
            {
                innerEx = ex;
            }

            throw new RpcException(DanielWillett.ModularRpcs.Properties.Exceptions.RpcExceptionUnableToAddConnectionToLifetime, innerEx!);
        }

        local.StartListening();

        return new ValueTask(Core(task, remote));
        
        static async Task Core(ValueTask<bool> task, NamedPipeServersideRemoteRpcConnection remote)
        {
            bool result;
            try
            {
                result = await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result = false;
                remote.Local.LogError(ex, DanielWillett.ModularRpcs.Properties.Exceptions.RpcExceptionUnableToAddConnectionToLifetime);
            }

            if (result)
            {
                remote.Local.StartListening();
                return;
            }

            Exception? innerEx = null;
            try
            {
                remote.Dispose();
            }
            catch (Exception ex)
            {
                innerEx = ex;
            }

            throw new RpcException(DanielWillett.ModularRpcs.Properties.Exceptions.RpcExceptionUnableToAddConnectionToLifetime, innerEx!);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // also add to DisposeAsync
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        NamedPipeServerStream? waitingServer = Interlocked.Exchange(ref _waitingServer, null);
        if (_connectionLifetime != null)
        {
            _connectionLifetime.ConnectionRemoved -= ConnectionRemoved;
            _connectionLifetime.TryRemoveAllConnections().AsTask().Wait();
        }
        IAsyncResult? result = _waitingServerTask;
        if (waitingServer != null)
        {
            result?.AsyncWaitHandle.Dispose();
            waitingServer.Dispose();
        }

    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        NamedPipeServerStream? waitingServer = Interlocked.Exchange(ref _waitingServer, null);
        if (_connectionLifetime != null)
        {
            _connectionLifetime.ConnectionRemoved -= ConnectionRemoved;
            await _connectionLifetime.TryRemoveAllConnections().ConfigureAwait(false);
        }

        IAsyncResult? result = _waitingServerTask;
        if (waitingServer != null)
        {
            result?.AsyncWaitHandle.Dispose();
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await waitingServer.DisposeAsync().ConfigureAwait(false);
#else
            waitingServer.Dispose();
#endif
        }
    }

    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
}