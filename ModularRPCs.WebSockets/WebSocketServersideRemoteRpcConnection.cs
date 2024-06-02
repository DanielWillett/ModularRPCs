using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Remote side of a connection to the server hosted on this machine.
/// </summary>
public class WebSocketServersideRemoteRpcConnection : WebSocketRemoteRpcConnection<WebSocketServersideLocalRpcConnection>, IModularRpcClientsideConnection, IModularRpcRemoteConnection
{
    private int _disp;
    public WebSocketServersideRemoteRpcConnection(WebSocket webSocket, WebSocketServersideLocalRpcConnection connection, IRpcConnectionLifetime lifetime, int bufferSize = 4096)
        : base(webSocket, connection.Endpoint, lifetime, bufferSize)
    {
        IsClosed = webSocket.State != WebSocketState.Open;
        connection.Remote = this;
        Local = connection;
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
            if (WebSocketIntl.State == WebSocketState.Open)
            {
                try
                {
                    await WebSocketIntl.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
            WebSocketIntl.Dispose();
        }
        finally
        {
            Semaphore.Release();
            await Lifetime.TryRemoveConnection(this, CancellationToken.None);
        }
    }
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
}