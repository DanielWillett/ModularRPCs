using System.Collections.Concurrent;
using System.Collections.Generic;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcClientsideLocalConnection : IModularRpcClientsideConnection, IModularRpcLocalConnection
{
    public bool IsClosed { get; internal set; }
    public IRpcRouter Router { get; }
    public LoopbackRpcClientsideRemoteConnection Remote { get; }
    public IDictionary<string, object> Tags { get; } = new ConcurrentDictionary<string, object>();
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
    internal LoopbackRpcClientsideLocalConnection(LoopbackRpcClientsideRemoteConnection remote, IRpcRouter router)
    {
        Remote = remote;
        Router = router;
        Remote.Local = this;
        IsClosed = true;
    }
    public ValueTask DisposeAsync() => CloseAsync();
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        Remote.IsClosed = true;
        IsClosed = true;
        Router.CleanupConnection(this);
        return default;
    }

    /// <inheritdoc />
    public override string ToString() => "Loopback (Local, Client)";
}
