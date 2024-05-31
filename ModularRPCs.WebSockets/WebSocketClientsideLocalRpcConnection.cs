using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketClientsideLocalRpcConnection : WebSocketLocalRpcConnection, IModularRpcLocalConnection, IModularRpcClientsideConnection
{
    protected internal override WebSocket WebSocket => Remote.WebSocket;
    protected internal override SemaphoreSlim Semaphore => Remote.Semaphore;
    public override bool IsClosed => Remote.IsClosed;
    internal WebSocketClientsideRemoteRpcConnection Remote { get; }
    internal WebSocketClientsideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, WebSocketClientsideRemoteRpcConnection remote, int bufferSize = 4096) : base(router, serializer, bufferSize)
    {
        Remote = remote;
        Remote.Local = this;
    }
    protected internal override Task Reconnect(CancellationToken token = default) => Remote.Reconnect(token);

    public override ValueTask DisposeAsync()
    {
        return Remote.DisposeAsync();
    }
    public override ValueTask CloseAsync(CancellationToken token = default)
    {
        return Remote.CloseAsync(token);
    }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
}