using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideLocalConnection : IModularRpcServersideConnection, IModularRpcLocalConnection, IModularRpcAuthoritativeParentConnection
{
    public bool IsClosed { get; internal set; }
    public IRpcRouter Router { get; }
    public LoopbackRpcServersideRemoteConnection Remote { get; private set; }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
    internal LoopbackRpcServersideLocalConnection(LoopbackRpcServersideRemoteConnection remote, IRpcRouter router)
    {
        Remote = remote;
        Router = router;
        IsClosed = true;
    }
    public Task InitializeConnectionAsync(IModularRpcRemoteConnection connection, CancellationToken token = default)
    {
        if (connection is not LoopbackRpcServersideRemoteConnection remote)
            throw new ArgumentException(Properties.Exceptions.ConnectionNotLoopback, nameof(connection));

        Remote = remote;
        remote.Local = this;
        IsClosed = false;
        remote.IsClosed = false;
        remote.Client.IsClosed = false;
        remote.Client.Local.IsClosed = false;
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
