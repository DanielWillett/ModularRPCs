using System.ComponentModel;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Local side of the connection by the client connected from this machine.
/// </summary>
public class WebSocketClientsideLocalRpcConnection : WebSocketLocalRpcConnection, IModularRpcLocalConnection, IModularRpcClientsideConnection
{
    /// <inheritdoc />
    protected internal override WebSocket WebSocket => Remote.WebSocket;

    /// <inheritdoc />
    protected internal override SemaphoreSlim Semaphore => Remote.Semaphore;

    /// <inheritdoc />
    protected internal override bool CanReconnect => true;

    internal WebSocketClientsideRemoteRpcConnection Remote { get; }
    internal WebSocketClientsideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, WebSocketClientsideRemoteRpcConnection remote, bool autoReconnect, PlateauingDelay delaySettings, int bufferSize = 4096)
        : base(router, serializer, remote.Endpoint, bufferSize, autoReconnect, delaySettings)
    {
        Remote = remote;
        Remote.Local = this;
        IsClosedIntl = remote.WebSocket.State != WebSocketState.Open;
    }

    /// <summary>
    /// Disconnects from the websocket, not trigerring a reconnect.
    /// </summary>
    public virtual async Task Disconnect(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await Semaphore.WaitAsync(token);
        try
        {
            await Remote.Disconnect(token);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Triggers a reconnection to the server. If already disconnected, simply disconnects to the server and cancels any pending reconnection timers.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task that completes when the client reconnects or fails to reconnect (check <see cref="WebSocketClientsideRemoteRpcConnection.IsClosed"/>).</returns>
    public async Task Reconnect(CancellationToken token = default)
    {
        await Semaphore.WaitAsync(token);
        try
        {
            await ReconnectIntl(token);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc cref="WebSocketLocalRpcConnection.StopReconnectTimerIntl"/>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public virtual bool StopReconnectTimer()
    {
        return StopReconnectTimerIntl();
    }

    private protected override Task ReconnectIntl(CancellationToken token = default)
    {
        // expects semaphore locked
        return Remote.Reconnect(token);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        return CloseAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public override ValueTask CloseAsync(CancellationToken token = default)
    {
        Router.CleanupConnection(this);
        return Remote.CloseAsync(token);
    }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;

    /// <inheritdoc />
    public override string ToString() => "WebSocket (Local, Client)";
}