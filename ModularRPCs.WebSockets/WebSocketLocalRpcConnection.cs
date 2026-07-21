using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Data;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Base class for the listening portion of a <see cref="System.Net.WebSockets.WebSocket"/> connection.
/// </summary>
public abstract class WebSocketLocalRpcConnection : IModularRpcConnection, IContiguousBufferProgressUpdateDispatcher, IRefSafeLoggable, IAsyncDisposable
{
    /// <summary>
    /// Cancellation token source used to cancel listening for incoming messages.
    /// </summary>
    protected readonly CancellationTokenSource CancellationTokenSource;

    /// <summary>
    /// Buffer used to process partical messages.
    /// </summary>
    protected internal readonly ContiguousBuffer Buffer;

    /// <summary>
    /// Serializer used to read header information.
    /// </summary>
    protected readonly IRpcSerializer Serializer;

    private readonly ContiguousBufferCallback _bufferCallback;
    private readonly bool _autoReconnect;
    private PlateauingDelay _delayCalc;
    private Timer? _reconnectTimer;
    private int _taskRunning;
    private object? _logger;
    internal bool IsClosedIntl;
    internal bool PauseAutoReconnect;

    /// <inheritdoc />
    public event ContiguousBufferProgressUpdate BufferProgressUpdated
    {
        add => Buffer.BufferProgressUpdated += value;
        remove => Buffer.BufferProgressUpdated -= value;
    }

    /// <inheritdoc />
    public bool IsClosed => WebSocket.State != WebSocketState.Open || IsClosedIntl;

    /// <inheritdoc cref="IModularRpcLocalConnection.Router" />
    public IRpcRouter Router { get; }

    /// <summary>
    /// Endpoint this connection was created from.
    /// </summary>
    public WebSocketEndpoint Endpoint { get; }

    /// <summary>
    /// Underlying <see cref="System.Net.WebSockets.WebSocket"/> this connection communicates over.
    /// </summary>
    protected internal abstract WebSocket WebSocket { get; }

    /// <summary>
    /// Whether or not this connection is allowed to reconnect automatically after disconnecting.
    /// </summary>
    protected internal abstract bool CanReconnect { get; }

    /// <summary>
    /// Sempahore used to synchronize reads and writes.
    /// </summary>
    protected internal abstract SemaphoreSlim Semaphore { get; }

    /// <inheritdoc cref="IModularRpcLocalConnection.Tags" />
    public IDictionary<string, object> Tags { get; } = new ConcurrentDictionary<string, object>();

    /// <summary>
    /// Create a new <see cref="WebSocketLocalRpcConnection"/> instance.
    /// </summary>
    protected internal WebSocketLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, WebSocketEndpoint endpoint, int bufferSize, bool autoReconnect, PlateauingDelay delaySettings)
    {
        _autoReconnect = autoReconnect;
        Router = router;
        Endpoint = endpoint;
        Serializer = serializer;
        Buffer = new ContiguousBuffer((IModularRpcLocalConnection)this, bufferSize);

        // ReSharper disable once InvokeAsExtensionMethod
        LoggingExtensions.SetLogger(Buffer, this);

        CancellationTokenSource = new CancellationTokenSource();
        // ReSharper disable once VirtualMemberCallInConstructor
        if (autoReconnect && CanReconnect)
            _delayCalc = new PlateauingDelay(ref delaySettings, true);

        _bufferCallback = RpcBufferParseCallback;
    }

    internal bool TryStartListening()
    {
        if (Interlocked.CompareExchange(ref _taskRunning, 1, 0) != 0)
            return false;

        Task.Run(ListenTask, CancellationTokenSource.Token);
        return true;
    }

    private async Task ListenTask()
    {
        bool doClose = false;
        while (!CancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                if (WebSocket is not { State: WebSocketState.Open })
                {
                    if (PauseAutoReconnect)
                    {
                        break;
                    }
                    if (CanReconnect && _autoReconnect)
                    {
                        this.LogInformation($"Reconnecting WebSocket because state is {WebSocket?.State.ToString() ?? "null"}.");
                        await Semaphore.WaitAsync();
                        try
                        {
                            if (WebSocket is not { State: WebSocketState.Open })
                            {
                                using CancellationTokenSource newSrc = new CancellationTokenSource(TimeSpan.FromSeconds(10d));
                                using CancellationTokenSource cmbSrc = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, newSrc.Token);
                                await ReconnectIntl(cmbSrc.Token);
                                _delayCalc.Reset();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            this.LogDebug("Closing - WebSocket disposed.");
                            doClose = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            this.LogDebug(ex, "Failed to reconnect");
                        }
                        finally
                        {
                            Semaphore.Release();
                        }

                        if (WebSocket is not { State: WebSocketState.Open })
                        {
                            await StartReconnectIntl();
                            break;
                        }

                        this.LogInformation("Reconnected.");
                    }
                    else
                    {
                        doClose = true;
                        break;
                    }
                }

                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(Buffer.Buffer), CancellationTokenSource.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    this.LogWarning($"Received close: {result.CloseStatus?.ToString() ?? "No closing status"} ({result.CloseStatusDescription ?? "<unknown reason>"}).");
                    await CloseAsync(CancellationToken.None);
                    Interlocked.CompareExchange(ref _taskRunning, 0, 1);
                    return;
                }

                Buffer.ProcessBuffer((uint)result.Count, Serializer, _bufferCallback);
            }
            catch (ObjectDisposedException)
            {
                if (PauseAutoReconnect)
                    break;
                this.LogDebug("Closing - WebSocket disposed.");
                await CloseAsync(CancellationToken.None);
                break;
            }
            catch (WebSocketException ex)
            {
                this.LogWarning(ex, "WebSocket error listening for message.");
                if (!CanReconnect)
                {
                    await CloseAsync();
                    break;
                }

                await StartReconnectIntl();
                break;
            }
            catch (Exception ex)
            {
                this.LogWarning(ex, "Error listening for message.");
            }
        }

        // avoids Semaphore deadlock
        if (doClose)
        {
            await CloseAsync(CancellationToken.None);
        }

        Interlocked.CompareExchange(ref _taskRunning, 0, 1);
    }

    /// <summary>
    /// Stops the reconnection timer that gets started after disconnecting if this connection was configured to auto-reconnect.
    /// </summary>
    /// <remarks>Call <see cref="WebSocketClientsideLocalRpcConnection.Reconnect"/> later to reconnect manually.</remarks>
    /// <returns>If <see langword="false"/>, the timer was already stopped before this function was invoked. If <see langword="true"/>, the timer was stopped and disposed.</returns>
    protected bool StopReconnectTimerIntl()
    {
        Timer? timer = Interlocked.Exchange(ref _reconnectTimer, null);
        if (timer == null)
            return false;

        DisposeTimer(timer);
        return true;
    }

    private static void DisposeTimer(Timer timer)
    {
        try
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        timer.Dispose();
    }

    private async Task StartReconnectIntl()
    {
        bool doClose = false;
        await Semaphore.WaitAsync();
        try
        {
            IsClosedIntl = true;
            double secLeft = _delayCalc.CalculateNext();
            TimeSpan timeUntilReconnect = TimeSpan.FromSeconds(secLeft);
            this.LogInformation($"Reconnecting in {timeUntilReconnect:g}...");
            PauseAutoReconnect = false;
            Timer? timer = Interlocked.Exchange(ref _reconnectTimer, new Timer(ReconnectCallback, null, timeUntilReconnect, Timeout.InfiniteTimeSpan));
            if (timer != null)
            {
                DisposeTimer(timer);
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error setting reconnect timer, closing.");
            doClose = true;
        }
        finally
        {
            Semaphore.Release();
        }

        // avoid Semaphore deadlock
        if (doClose)
        {
            await CloseAsync();
        }
    }

    private void ReconnectCallback(object? state)
    {
        StopReconnectTimerIntl();
        if (PauseAutoReconnect)
        {
            this.LogWarning("Auto-reconnect timer invoked while auto-reconnect was paused. This shouldn't happen.");
            return;
        }

        Task.Run(async () =>
        {
            double secLeft;
            await Semaphore.WaitAsync();
            try
            {
                using (CancellationTokenSource newSrc = new CancellationTokenSource(TimeSpan.FromSeconds(10d)))
                using (CancellationTokenSource cmbSrc = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, newSrc.Token))
                {
                    await ReconnectIntl(cmbSrc.Token);
                }

                if (WebSocket.State == WebSocketState.Open)
                {
                    this.LogInformation($"Reconnected after {_delayCalc.Trials} tries.");
                    _delayCalc.Reset();
                    return;
                }

                secLeft = _delayCalc.CalculateNext();
            }
            catch (Exception ex)
            {
                this.LogDebug(ex, "Failed to reconnect");
                secLeft = _delayCalc.CalculateNext();
            }
            finally
            {
                Semaphore.Release();
            }

            TimeSpan timeUntilReconnect = TimeSpan.FromSeconds(secLeft);
            this.LogInformation($"Reconnecting in {timeUntilReconnect:g}...");
            Timer? timer = Interlocked.Exchange(ref _reconnectTimer, new Timer(ReconnectCallback, null, timeUntilReconnect, Timeout.InfiniteTimeSpan));
            if (timer != null)
            {
                // stop old timer
                DisposeTimer(timer);
            }
        });
    }

    private void RpcBufferParseCallback(ReadOnlyMemory<byte> data, bool canTakeOwnership, in PrimitiveRpcOverhead overhead)
    {
        ValueTask vt = Router.ReceiveData(in overhead, ((IModularRpcLocalConnection)this).Remote, Serializer, data, canTakeOwnership, CancellationTokenSource.Token);
        
        if (vt.IsCompleted)
            return;

        ValueTask vt2 = vt;
        Task.Run(async () =>
        {
            try
            {
                await vt2.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Failed to execute rpc read callback.");
            }
        });
    }

    /// <summary>
    /// Force the underlying connection to reconnect.
    /// </summary>
    /// <exception cref="NotSupportedException">Not supported server-side.</exception>
    private protected virtual Task ReconnectIntl(CancellationToken token = default)
    {
        throw new NotSupportedException();
    }

    internal void DisposeIntl()
    {
        Router.CleanupConnection(this);
        StopReconnectTimerIntl();
        IsClosedIntl = true;
        try
        {
            CancellationTokenSource.Cancel();
        }
        catch
        {
            // ignored
        }

        CancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Dispose resources owned by this object.
    /// </summary>
    public abstract ValueTask DisposeAsync();

    /// <inheritdoc />
    public abstract ValueTask CloseAsync(CancellationToken token = default);
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
}
