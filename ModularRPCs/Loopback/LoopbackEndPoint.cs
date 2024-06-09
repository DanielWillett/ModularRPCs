using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;

/// <summary>
/// Represents a factory for local/loopback RPC connections.
/// </summary>
public class LoopbackEndpoint(bool isServer, bool useStreams = false) : IModularRpcRemoteEndpoint
{
    /// <summary>
    /// Should this side act as a server or client?
    /// </summary>
    public bool IsServer { get; } = isServer;

    /// <summary>
    /// Forces the use of a <see cref="MemoryStream"/> instead of binary data.
    /// </summary>
    public bool UseStreams { get; } = useStreams;

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
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, router, connectionLifetime, UseStreams);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), router, lifetime: null, serverConnection, UseStreams);

            await serverConnection.Local.InitializeConnectionAsync(token);

            await connectionLifetime.TryAddNewConnection(serverConnection, token);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), router, lifetime: null, UseStreams);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, router, connectionLifetime, serverRemote, UseStreams);

        await serverRemote.Local.InitializeConnectionAsync(token);

        await connectionLifetime.TryAddNewConnection(clientConnection, token);
        return clientConnection;
    }

    /// <summary>
    /// Get a remote connection with a local and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(
        IRpcRouter clientRouter, IRpcRouter serverRouter,
        IRpcConnectionLifetime clientConnectionLifetime, IRpcConnectionLifetime serverConnectionLifetime,
        CancellationToken token = default
    )
    {
        if (IsServer)
        {
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, serverRouter, serverConnectionLifetime, UseStreams);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), clientRouter, lifetime: null, serverConnection, UseStreams);

            await serverConnection.Local.InitializeConnectionAsync(token);

            await serverConnectionLifetime.TryAddNewConnection(serverConnection, token);
            await clientConnectionLifetime.TryAddNewConnection(serverConnection.Client, token);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), serverRouter, lifetime: null, UseStreams);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, clientRouter, clientConnectionLifetime, serverRemote, UseStreams);

        await serverRemote.Local.InitializeConnectionAsync(token);

        await serverConnectionLifetime.TryAddNewConnection(clientConnection.Server, token);
        await clientConnectionLifetime.TryAddNewConnection(clientConnection, token);
        return clientConnection;
    }
}