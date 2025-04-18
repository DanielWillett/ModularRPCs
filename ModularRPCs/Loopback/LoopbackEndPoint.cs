using System;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, router, serializer, connectionLifetime, UseStreams);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), router, serializer, lifetime: null, serverConnection, UseStreams);

            await serverConnection.Local.InitializeConnectionAsync(token);

            await connectionLifetime.TryAddNewConnection(serverConnection, token);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), router, serializer, lifetime: null, UseStreams);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, router, serializer, connectionLifetime, serverRemote, UseStreams);

        await serverRemote.Local.InitializeConnectionAsync(token);

        await connectionLifetime.TryAddNewConnection(clientConnection, token);
        return clientConnection;
    }

    /// <summary>
    /// Get a remote connection with a local and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public Task<IModularRpcRemoteConnection> RequestConnectionAsync(IServiceProvider services, CancellationToken token = default)
    {
        return RequestConnectionAsync(services, services, token);
    }

    /// <summary>
    /// Get a remote connection with a local and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(IServiceProvider clientServices, IServiceProvider serverServices, CancellationToken token = default)
    {
        ILoggerFactory? clientLoggerFactory = (ILoggerFactory?)clientServices.GetService(typeof(ILoggerFactory));
        ILoggerFactory? serverLoggerFactory = (ILoggerFactory?)serverServices.GetService(typeof(ILoggerFactory));

        if (IsServer)
        {
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this,
                serverServices.GetRequiredService<IRpcRouter>(),
                serverServices.GetRequiredService<IRpcSerializer>(),
                serverServices.GetRequiredService<IRpcConnectionLifetime>(),
                UseStreams);
            LoopbackRpcClientsideRemoteConnection clientRemote = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(),
                clientServices.GetRequiredService<IRpcRouter>(),
                clientServices.GetRequiredService<IRpcSerializer>(),
                clientServices.GetRequiredService<IRpcConnectionLifetime>(),
                serverConnection, UseStreams);

            if (serverLoggerFactory != null)
            {
                serverConnection.SetLogger(serverLoggerFactory.CreateLogger<LoopbackRpcServersideRemoteConnection>());
                serverConnection.Local.SetLogger(serverLoggerFactory.CreateLogger<LoopbackRpcServersideLocalConnection>());
            }
            if (clientLoggerFactory != null)
            {
                clientRemote.SetLogger(clientLoggerFactory.CreateLogger<LoopbackRpcClientsideRemoteConnection>());
                clientRemote.Local.SetLogger(clientLoggerFactory.CreateLogger<LoopbackRpcClientsideLocalConnection>());
            }

            await serverConnection.Local.InitializeConnectionAsync(token);

            await serverConnection.Lifetime!.TryAddNewConnection(serverConnection, token);
            await clientRemote.Lifetime!.TryAddNewConnection(clientRemote, token);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(),
            serverServices.GetRequiredService<IRpcRouter>(),
            serverServices.GetRequiredService<IRpcSerializer>(),
            serverServices.GetRequiredService<IRpcConnectionLifetime>(),
            UseStreams);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this,
            clientServices.GetRequiredService<IRpcRouter>(),
            clientServices.GetRequiredService<IRpcSerializer>(),
            clientServices.GetRequiredService<IRpcConnectionLifetime>(),
            serverRemote, UseStreams);

        if (serverLoggerFactory != null)
        {
            serverRemote.SetLogger(serverLoggerFactory.CreateLogger<LoopbackRpcServersideRemoteConnection>());
            serverRemote.Local.SetLogger(serverLoggerFactory.CreateLogger<LoopbackRpcServersideLocalConnection>());
        }
        if (clientLoggerFactory != null)
        {
            clientConnection.SetLogger(clientLoggerFactory.CreateLogger<LoopbackRpcClientsideRemoteConnection>());
            clientConnection.Local.SetLogger(clientLoggerFactory.CreateLogger<LoopbackRpcClientsideLocalConnection>());
        }

        await serverRemote.Local.InitializeConnectionAsync(token);

        await serverRemote.Lifetime!.TryAddNewConnection(serverRemote, token);
        await clientConnection.Lifetime!.TryAddNewConnection(clientConnection, token);
        return clientConnection;
    }

    /// <summary>
    /// Get a remote connection with a local and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(
        IRpcRouter clientRouter, IRpcSerializer clientSerializer, IRpcRouter serverRouter, IRpcSerializer serverSerializer, 
        IRpcConnectionLifetime clientConnectionLifetime, IRpcConnectionLifetime serverConnectionLifetime,
        CancellationToken token = default
    )
    {
        if (IsServer)
        {
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, serverRouter, serverSerializer, serverConnectionLifetime, UseStreams);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), clientRouter, clientSerializer, lifetime: null, serverConnection, UseStreams);

            await serverConnection.Local.InitializeConnectionAsync(token);

            await serverConnectionLifetime.TryAddNewConnection(serverConnection, token);
            await clientConnectionLifetime.TryAddNewConnection(serverConnection.Client, token);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), serverRouter, serverSerializer, lifetime: null, UseStreams);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, clientRouter, clientSerializer, clientConnectionLifetime, serverRemote, UseStreams);

        await serverRemote.Local.InitializeConnectionAsync(token);

        await serverConnectionLifetime.TryAddNewConnection(clientConnection.Server, token);
        await clientConnectionLifetime.TryAddNewConnection(clientConnection, token);
        return clientConnection;
    }

    /// <inheritdoc />
    public override string ToString() => IsServer ? "Loopback (Server)" : "Loopback (Client)";
}