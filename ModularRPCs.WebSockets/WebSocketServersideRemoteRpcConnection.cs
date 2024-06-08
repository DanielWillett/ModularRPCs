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
    private readonly bool _leaveOpen;
    public override bool IsClosed => Local.IsClosed;
    public WebSocketServersideRemoteRpcConnection(
        WebSocket webSocket,
        WebSocketServersideLocalRpcConnection connection,
        IRpcConnectionLifetime lifetime,
        bool leaveOpen,
        int bufferSize = 4096)
        : base(webSocket, connection.Endpoint, lifetime, bufferSize)
    {
        _leaveOpen = leaveOpen;
        connection.Remote = this;
        Local = connection;
        Local.IsClosedIntl = webSocket.State != WebSocketState.Open;
    }
    public override async ValueTask CloseAsync(CancellationToken token = default)
    {
        await Semaphore.WaitAsync(10000, token);
        bool alreadyDisposed = true;
        try
        {
            if (Interlocked.Exchange(ref _disp, 1) != 0)
                return;
            alreadyDisposed = false;
            Local.DisposeIntl();
            
            if (_leaveOpen)
                return;

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
            if (!alreadyDisposed)
            {
                await Lifetime.TryRemoveConnection(this, CancellationToken.None);
            }
        }
    }
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
}