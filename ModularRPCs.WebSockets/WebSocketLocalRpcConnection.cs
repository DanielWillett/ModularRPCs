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
    private readonly bool _autoReconnect;
    private int _taskRunning;
    private object? _logger;
    /// <inheritdoc />
    public event ContiguousBufferProgressUpdate BufferProgressUpdated
    {
        add => Buffer.BufferProgressUpdated += value;
        remove => Buffer.BufferProgressUpdated -= value;
    }
    public abstract bool IsClosed { get; }
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
            _delayCalc = new PlateauingDelay(delaySettings.Plateau, delaySettings.Amplifier, delaySettings.StartingTrials);
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
                                await Reconnect(CancellationTokenSource.Token);
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
                            await Task.Delay(10000);
                            continue;
                        }
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
            catch (Exception ex)
            {
                this.LogWarning(ex, "Error listening for message.");
            }
        }

        Interlocked.CompareExchange(ref _taskRunning, 0, 1);
    }
    private void RpcBufferParseCallback(ReadOnlySpan<byte> data, bool canTakeOwnership, in PrimitiveRpcOverhead overhead)
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
    public abstract Task Reconnect(CancellationToken token = default);
    internal void DisposeIntl()
    {
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
