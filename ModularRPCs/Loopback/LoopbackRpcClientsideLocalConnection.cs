using System.Collections.Concurrent;
using System.Collections.Generic;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcClientsideLocalConnection : IModularRpcClientsideConnection, IModularRpcLocalConnection, IRefSafeLoggable
{
    private object? _logger;

    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

    public bool IsClosed { get; internal set; }
    public IRpcRouter Router { get; }
    public IRpcSerializer Serializer { get; }
    public LoopbackRpcClientsideRemoteConnection Remote { get; }
    public IDictionary<string, object> Tags { get; } = new ConcurrentDictionary<string, object>();
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
    internal LoopbackRpcClientsideLocalConnection(LoopbackRpcClientsideRemoteConnection remote, IRpcRouter router, IRpcSerializer serializer)
    {
        Remote = remote;
        Router = router;
        Serializer = serializer;
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
