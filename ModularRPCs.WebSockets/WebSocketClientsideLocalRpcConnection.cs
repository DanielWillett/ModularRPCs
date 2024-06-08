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
    protected internal override WebSocket WebSocket => Remote.WebSocket;
    protected internal override SemaphoreSlim Semaphore => Remote.Semaphore;
    protected internal override bool CanReconnect => true;
    internal WebSocketClientsideRemoteRpcConnection Remote { get; }
    internal WebSocketClientsideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, WebSocketClientsideRemoteRpcConnection remote, bool autoReconnect, PlateauingDelay delaySettings, int bufferSize = 4096)
        : base(router, serializer, remote.Endpoint, bufferSize, autoReconnect, delaySettings)
    {
        Remote = remote;
        Remote.Local = this;
        IsClosedIntl = remote.WebSocket.State != WebSocketState.Open;
    }

    public override Task Reconnect(CancellationToken token = default) => Remote.Reconnect(token);

    public override ValueTask DisposeAsync()
    {
        return Remote.CloseAsync();
    }
    public override ValueTask CloseAsync(CancellationToken token = default)
    {
        return Remote.CloseAsync(token);
    }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
}