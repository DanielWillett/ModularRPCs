using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Loopback;

/// <summary>
/// Represents a factory for local/loopback RPC connections.
/// </summary>
public class LoopbackEndpoint(bool isServer) : IModularRpcRemoteEndpoint
{
    /// <summary>
    /// Should this side act as a server or client?
    /// </summary>
    public bool IsServer { get; } = isServer;

    /// <summary>
    /// Creates the other side of this endpoint.
    /// </summary>
    public LoopbackEndpoint CreateOtherSide() => new LoopbackEndpoint(!IsServer);

    /// <summary>
    /// Get a local connection with a remote and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public Task<IModularRpcLocalConnection> RequestConnectionAsync(IRpcRouter router, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (IsServer)
        {
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, router, serializer);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), router, serverConnection);
            return Task.FromResult<IModularRpcLocalConnection>(serverConnection.Local);
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), router, serializer);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, router, serverRemote);
        return Task.FromResult<IModularRpcLocalConnection>(clientConnection.Local);
    }
}