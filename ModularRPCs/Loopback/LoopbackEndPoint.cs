using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
    /// Get a remote connection with a local and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (IsServer)
        {
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, router, serializer);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), router, serverConnection);

            serverConnection.IsClosed = false;
            serverConnection.Local.IsClosed = false;
            serverConnection.Client.IsClosed = false;
            serverConnection.Client.Local.IsClosed = false;

            await connectionLifetime.TryAddNewConnection(serverConnection, token);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), router, serializer);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, router, serverRemote);

        clientConnection.IsClosed = false;
        clientConnection.Local.IsClosed = false;
        clientConnection.Server.IsClosed = false;
        clientConnection.Server.Local.IsClosed = false;

        await connectionLifetime.TryAddNewConnection(clientConnection, token);
        return clientConnection;
    }
}