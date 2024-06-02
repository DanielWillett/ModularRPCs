using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Remote side of the connection by the client connected from this machine.
/// </summary>
public class WebSocketClientsideRemoteRpcConnection : WebSocketRemoteRpcConnection<WebSocketClientsideLocalRpcConnection>, IModularRpcClientsideConnection, IModularRpcRemoteConnection
{
    internal readonly ClientWebSocket WebSocket;
    private int _disp;
    internal WebSocketClientsideRemoteRpcConnection(WebSocketEndpoint endpoint, IRpcConnectionLifetime lifetime, ClientWebSocket webSocket, int bufferSize = 4096)
        : base(webSocket, endpoint, lifetime, bufferSize)
    {
        WebSocket = webSocket;
        // ReSharper disable once VirtualMemberCallInConstructor
        IsClosed = webSocket.State != WebSocketState.Open;
    }

    internal async Task Reconnect(CancellationToken token = default)
    {
        try
        {
            if (WebSocket.State is not WebSocketState.Closed and not WebSocketState.CloseReceived and not WebSocketState.CloseSent)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Reconnecting", token).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignored
        }

        await WebSocket.ConnectAsync(Endpoint.Uri, token).ConfigureAwait(false);
    }
    public override async ValueTask CloseAsync(CancellationToken token = default)
    {
        await Semaphore.WaitAsync(10000, token);
        try
        {
            if (Interlocked.Exchange(ref _disp, 1) != 0)
                return;
            Local.DisposeIntl();
            IsClosed = true;
            if (WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
            WebSocket.Dispose();
        }
        finally
        {
            Semaphore.Release();
            await Lifetime.TryRemoveConnection(this, CancellationToken.None);
        }
    }
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
}
