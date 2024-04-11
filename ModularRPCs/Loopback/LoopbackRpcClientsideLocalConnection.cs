using DanielWillett.ModularRpcs.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcClientsideLocalConnection : IModularRpcClientsideConnection, IModularRpcLocalConnection
{
    public bool IsClosed { get; internal set; }
    public IRpcRouter Router { get; }
    public LoopbackRpcClientsideRemoteConnection Remote { get; }
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
        return default;
    }
}
