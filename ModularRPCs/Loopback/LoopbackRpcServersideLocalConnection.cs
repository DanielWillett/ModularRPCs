using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideLocalConnection : IModularRpcAuthoritativeParentConnection
{
    public bool IsClosed { get; internal set; }
    public IRpcRouter Router { get; }
    public LoopbackRpcServersideRemoteConnection Remote { get;}
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
    internal LoopbackRpcServersideLocalConnection(LoopbackRpcServersideRemoteConnection remote, IRpcRouter router)
    {
        Remote = remote;
        Router = router;
        IsClosed = true;
    }
    public Task InitializeConnectionAsync(CancellationToken token = default)
    {
        IsClosed = false;
        Remote.IsClosed = false;
        Remote.Client.IsClosed = false;
        Remote.Client.Local.IsClosed = false;
        return Task.CompletedTask;
    }
    public ValueTask DisposeAsync() => CloseAsync();
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        Remote.IsClosed = true;
        IsClosed = true;
        return default;
    }
}
