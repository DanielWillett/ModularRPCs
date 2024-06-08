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
public abstract class WebSocketLocalRpcConnection : IModularRpcConnection, IContiguousBufferProgressUpdateDispatcher, IRefSafeLoggable
{
    protected readonly CancellationTokenSource CancellationTokenSource;
    protected readonly ContiguousBuffer Buffer;
    protected readonly IRpcSerializer Serializer;
    private PlateauingDelay _delayCalc;
    private Timer? _reconnectTimer;
    private readonly bool _autoReconnect;
    private int _taskRunning;
    private object? _logger;
    internal bool IsClosedIntl;
    /// <inheritdoc />
    public event ContiguousBufferProgressUpdate BufferProgressUpdated
    {
        add => Buffer.BufferProgressUpdated += value;
        remove => Buffer.BufferProgressUpdated -= value;
    }

    public bool IsClosed => WebSocket.State != WebSocketState.Open || IsClosedIntl;
    public IRpcRouter Router { get; }
    public WebSocketEndpoint Endpoint { get; }
    protected internal abstract WebSocket WebSocket { get; }
    protected internal abstract bool CanReconnect { get; }
    protected internal abstract SemaphoreSlim Semaphore { get; }
    public IDictionary<string, object> Tags { get; } = new ConcurrentDictionary<string, object>();
    protected internal WebSocketLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, WebSocketEndpoint endpoint, int bufferSize, bool autoReconnect, PlateauingDelay delaySettings)
    {
        _autoReconnect = autoReconnect;
        Router = router;
        Endpoint = endpoint;
        Serializer = serializer;
        Buffer = new ContiguousBuffer((IModularRpcLocalConnection)this, bufferSize);
        Buffer.SetLogger(this);
        CancellationTokenSource = new CancellationTokenSource();
        // ReSharper disable once VirtualMemberCallInConstructor
        if (autoReconnect && CanReconnect)
            _delayCalc = new PlateauingDelay(ref delaySettings, true);
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
        while (!CancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                if (WebSocket is not { State: WebSocketState.Open })
                {
                    if (CanReconnect && _autoReconnect)
                    {
                        this.LogInformation($"Reconnecting WebSocket because state is {WebSocket?.State.ToString() ?? "null"}.");
                        await Semaphore.WaitAsync();
                        try
                        {
                            if (WebSocket is not { State: WebSocketState.Open })
                            {
                                CancellationTokenSource newSrc = new CancellationTokenSource(TimeSpan.FromSeconds(10d));
                                CancellationTokenSource cmbSrc = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, newSrc.Token);
                                await Reconnect(cmbSrc.Token);
                                newSrc.Dispose();
                                cmbSrc.Dispose();
                                _delayCalc.Reset();
                            }
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
                        await CloseAsync(CancellationToken.None);
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

                Buffer.ProcessBuffer((uint)result.Count, Serializer, RpcBufferParseCallback);
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

        Interlocked.CompareExchange(ref _taskRunning, 0, 1);
    }
    private async Task StartReconnectIntl()
    {
        await Semaphore.WaitAsync();
        try
        {
            IsClosedIntl = true;
            double secLeft = _delayCalc.CalculateNext();
            TimeSpan timeUntilReconnect = TimeSpan.FromSeconds(secLeft);
            this.LogInformation($"Reconnecting in {timeUntilReconnect:g}...");
            Timer? timer = Interlocked.Exchange(ref _reconnectTimer, new Timer(ReconnectCallback, null, timeUntilReconnect, Timeout.InfiniteTimeSpan));
            if (timer == null)
                return;

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
        }
        finally
        {
            Semaphore.Release();
        }
    }
    private void ReconnectCallback(object? state)
    {
        Timer? timer = _reconnectTimer;
        if (timer != null)
        {
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            timer.Dispose();
        }

        Task.Run(async () =>
        {
            double secLeft;
            await Semaphore.WaitAsync();
            try
            {
                CancellationTokenSource newSrc = new CancellationTokenSource(TimeSpan.FromSeconds(10d));
                CancellationTokenSource cmbSrc = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, newSrc.Token);

                await Reconnect(cmbSrc.Token);

                newSrc.Dispose();
                cmbSrc.Dispose();

                if (WebSocket.State == WebSocketState.Open)
                {
                    this.LogInformation($"Reconnected after {_delayCalc.Trials} tries.");
                    _delayCalc.Reset();
                    TryStartListening();
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
            if (timer == null)
                return;
            
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
        });
    }
    private void RpcBufferParseCallback(ReadOnlyMemory<byte> data, bool canTakeOwnership, in PrimitiveRpcOverhead overhead)
    {
        ValueTask vt = Router.ReceiveData(in overhead, ((IModularRpcLocalConnection)this).Remote, Serializer, data, canTakeOwnership, CancellationTokenSource.Token);
        
        if (vt.IsCompleted)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await vt.ConfigureAwait(false);
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
    public virtual Task Reconnect(CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
    internal void DisposeIntl()
    {
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
    public abstract ValueTask DisposeAsync();
    public abstract ValueTask CloseAsync(CancellationToken token = default);
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
}
