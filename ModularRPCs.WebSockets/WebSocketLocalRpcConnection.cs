using System;
using DanielWillett.ModularRpcs.Abstractions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public abstract class WebSocketLocalRpcConnection : IModularRpcConnection
{
    protected readonly CancellationTokenSource CancellationTokenSource;
    public abstract bool IsClosed { get; }
    protected abstract WebSocket WebSocket { get; }
    public IRpcRouter Router { get; }
    protected WebSocketLocalRpcConnection(IRpcRouter router)
    {
        Router = router;
        CancellationTokenSource = new CancellationTokenSource();
    }
    internal void StartListening()
    {
        Task.Run(ListenTask, CancellationTokenSource.Token);
    }
    private async Task ListenTask()
    {
        while (!CancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                if (WebSocket is not { State: WebSocketState.Open })
                {
                    Logging.LogInfo($"Reconnecting WebSocket because state is {WebSocket?.State.ToString() ?? "null"}.");
                    await _semaphore.WaitAsync(_cts.Token);
                    try
                    {
                        if (WebSocket is not { State: WebSocketState.Open })
                            await Connect();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    if (WebSocket is not { State: WebSocketState.Open })
                    {
                        await Task.Delay(10000);
                        continue;
                    }
                }

                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(_networkBuffer.Buffer, _cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Logging.LogWarning($"Received close: {result.CloseStatus?.ToString() ?? "No closing status"} ({result.CloseStatusDescription ?? "<unknown reason>"}).");
                    Close();
                    return;
                }
                _networkBuffer.ProcessBuffer(result.Count);
            }
            catch (Exception ex)
            {
                if (!CheckCommonErrors(ex))
                {
                    Logging.LogWarning("Error listening for message.");
                    Logging.LogException(ex);
                }
            }
        }
    }
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
}
