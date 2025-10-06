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

    /// <inheritdoc />
    public override bool IsClosed => Local.IsClosed;

    /// <summary>
    /// Used to customize the <see cref="Uri"/> used to reconnect with. Not compatible with multiple handlers.
    /// </summary>
    public event RequestReconnectHandler? OnRequestingReconnect;

    /// <summary>
    /// Invoked after a reconnection. This will not be invoked on the first connection.
    /// </summary>
    public event ReconnectHandler? OnReconnected;

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

        Task<Uri?>? rec = OnRequestingReconnect?.Invoke(this);

        Uri uri = Endpoint.Uri;
        if (rec != null)
        {
            uri = await rec.ConfigureAwait(false) ?? uri;
        }

        await WebSocket.ConnectAsync(uri, token).ConfigureAwait(false);
        WebSocketIntl = WebSocket;
        if (WebSocket.State != WebSocketState.Open)
        {
            Local.IsClosedIntl = true;
            return;
        }

        try
        {
            OnReconnected?.Invoke(this);
        }
        catch (Exception ex)
        {
            Local.LogError(ex, "Exception caught from handler for WebSocketClientsideRemoteRpcConnection.OnReconnected.");
        }
    }

    /// <inheritdoc />
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
            Local.Router.CleanupConnection(this);
            Semaphore.Release();
            if (!alreadyDisposed)
            {
                await Lifetime.TryRemoveConnection(this, CancellationToken.None);
            }
        }
    }
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;

    /// <inheritdoc />
    public override string ToString() => $"WebSocket (Remote, Client): \'{Endpoint.Uri.GetComponents(UriComponents.Host, UriFormat.Unescaped)}\'";
}

public delegate Task<Uri?> RequestReconnectHandler(WebSocketClientsideRemoteRpcConnection connection);
public delegate void ReconnectHandler(WebSocketClientsideRemoteRpcConnection connection);