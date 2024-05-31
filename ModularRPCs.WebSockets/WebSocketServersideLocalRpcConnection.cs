using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketServersideLocalRpcConnection : WebSocketLocalRpcConnection, IModularRpcAuthoritativeParentConnection
{
    public IModularRpcRemoteConnection Remote { get; set; }
    public Task InitializeConnectionAsync(IModularRpcRemoteConnection connection, CancellationToken token = default)
    {
        // todo
        return null;
    }

    internal WebSocketServersideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, int bufferSize) : base(router, serializer, bufferSize)
    {
    }

    public override ValueTask CloseAsync(CancellationToken token = default) => throw new NotImplementedException();

    public override bool IsClosed => throw new NotImplementedException();

    protected internal override WebSocket WebSocket => throw new NotImplementedException();

    protected internal override SemaphoreSlim Semaphore => throw new NotImplementedException();

    protected internal override Task Reconnect(CancellationToken token = default) => throw new NotImplementedException();

    public override ValueTask DisposeAsync() => throw new NotImplementedException();
}
