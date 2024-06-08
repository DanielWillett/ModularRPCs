using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Remote side of the connection by the client connected from this machine.
/// </summary>
public class WebSocketClientsideRemoteRpcConnection : WebSocketRemoteRpcConnection<WebSocketClientsideLocalRpcConnection>, IModularRpcClientsideConnection, IModularRpcRemoteConnection
{
    internal ClientWebSocket WebSocket;
    private int _disp;
    public override bool IsClosed => Local.IsClosed;

    /// <summary>
    /// Used to customize the <see cref="Uri"/> used to reconnect with. Not compatible with multiple handlers.
    /// </summary>
    public event ReconnectHandler? OnReconnect;
    internal WebSocketClientsideRemoteRpcConnection(WebSocketEndpoint endpoint, IRpcConnectionLifetime lifetime, ClientWebSocket webSocket, int bufferSize = 4096)
        : base(webSocket, endpoint, lifetime, bufferSize)
    {
        WebSocket = webSocket;
        // ReSharper disable once VirtualMemberCallInConstructor
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

        WebSocket = new ClientWebSocket();

        Task<Uri?>? rec = OnReconnect?.Invoke(this);

        Uri uri = Endpoint.Uri;
        if (rec != null)
        {
            uri = await rec ?? uri;
        }

        await WebSocket.ConnectAsync(uri, token).ConfigureAwait(false);
        WebSocketIntl = WebSocket;
        Local.IsClosedIntl = WebSocket.State != WebSocketState.Open;
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
            if (!alreadyDisposed)
            {
                await Lifetime.TryRemoveConnection(this, CancellationToken.None);
            }
        }
    }
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
}

public delegate Task<Uri?> ReconnectHandler(WebSocketClientsideRemoteRpcConnection connection);