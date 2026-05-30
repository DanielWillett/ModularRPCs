using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System;
using System.Linq;
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
    /// Used to customize the <see cref="Uri"/> used to reconnect with.
    /// </summary>
    /// <remarks>If multiple handlers are added, the URL of the earliest-registered handler to return a non-null value will be used.</remarks>
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

    internal async Task Disconnect(CancellationToken token = default)
    {
        // expects semaphore locked
        token.ThrowIfCancellationRequested();

        Local.StopReconnectTimer();
        Local.PauseAutoReconnect = true;

        try
        {
            if (WebSocket.State <= WebSocketState.Open)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", token).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignored
        }

        Local.IsClosedIntl = true;

        // reset any partial messages that got cut off during reconnect
        Local.Buffer.Reset();

        WebSocket.Dispose();
    }

    internal async Task Reconnect(CancellationToken token = default)
    {
        // expects semaphore locked
        token.ThrowIfCancellationRequested();

        Local.StopReconnectTimer();
        Local.PauseAutoReconnect = true;

        try
        {
            if (WebSocket.State <= WebSocketState.Open)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Reconnecting", token).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignored
            Local.PauseAutoReconnect = false;
        }

        ClientWebSocket clientWebSocket = new ClientWebSocket();

        Endpoint.ConfigureOptions?.Invoke(clientWebSocket.Options);

        ClientWebSocket oldWebSocket = Interlocked.Exchange(ref WebSocket, clientWebSocket);
        try
        {
            Uri uri = Endpoint.Uri;

            // Invoke OnRequestingReconnect
            Delegate[]? invocations = OnRequestingReconnect?.GetInvocationList();
            if (invocations != null) switch (invocations.Length)
            {
                case 0: break;

                case 1:
                    uri = await ((RequestReconnectHandler)invocations[0])(this) ?? uri;
                    break;

                default:
                    Uri?[] results = await Task.WhenAll(
                        invocations
                            .Cast<RequestReconnectHandler>()
                            .Select(x => x.Invoke(this))
                    ).ConfigureAwait(false);

                    uri = results.FirstOrDefault(x => x != null) ?? uri;
                    break;
            }

            // reset any partial messages that got cut off during reconnect
            Local.Buffer.Reset();

            await WebSocket.ConnectAsync(uri, token).ConfigureAwait(false);
            WebSocketIntl = WebSocket;
            if (WebSocket.State != WebSocketState.Open)
            {
                Local.IsClosedIntl = true;
                return;
            }

            Local.IsClosedIntl = false;
            Local.TryStartListening();

            try
            {
                OnReconnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                Local.LogError(ex, "Exception caught from handler for WebSocketClientsideRemoteRpcConnection.OnReconnected.");
            }
        }
        finally
        {
            oldWebSocket.Dispose();
        }
    }

    /// <inheritdoc />
    public override async ValueTask CloseAsync(CancellationToken token = default)
    {
        Local.PauseAutoReconnect = true;
        await Semaphore.WaitAsync(10000, token);
        bool alreadyDisposed = true;
        try
        {
            if (Interlocked.Exchange(ref _disp, 1) != 0)
                return;
            alreadyDisposed = false;
            Local.DisposeIntl();

            if (WebSocket.State <= WebSocketState.Open)
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

/// <summary>
/// Delegate used to handle when a reconnect is requested.
/// </summary>
/// <param name="connection">The connection instance.</param>
/// <returns>A task that completes when a connection URI is ready.</returns>
public delegate Task<Uri?> RequestReconnectHandler(WebSocketClientsideRemoteRpcConnection connection);

/// <summary>
/// Delegate used to handle when a reconnect occurs, either manually or automatically.
/// </summary>
/// <param name="connection">The connection instance.</param>
public delegate void ReconnectHandler(WebSocketClientsideRemoteRpcConnection connection);