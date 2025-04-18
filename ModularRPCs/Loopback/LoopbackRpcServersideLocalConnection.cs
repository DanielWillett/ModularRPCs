using System.Collections.Concurrent;
using System.Collections.Generic;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideLocalConnection : IModularRpcAuthoritativeParentConnection, IRefSafeLoggable
{
    private object? _logger;

    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

    public bool IsClosed { get; internal set; }
    public IRpcRouter Router { get; }
    public LoopbackRpcServersideRemoteConnection Remote { get;}
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
    public IRpcSerializer Serializer { get; }
    public IDictionary<string, object> Tags { get; } = new ConcurrentDictionary<string, object>();
    internal LoopbackRpcServersideLocalConnection(LoopbackRpcServersideRemoteConnection remote, IRpcRouter router, IRpcSerializer serializer)
    {
        Remote = remote;
        Router = router;
        Serializer = serializer;
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
        Router.CleanupConnection(this);
        return default;
    }

    /// <inheritdoc />
    public override string ToString() => "Loopback (Local, Server)";
}
