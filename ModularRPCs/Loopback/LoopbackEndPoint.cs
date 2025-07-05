using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;

/// <summary>
/// Represents a factory for local/loopback RPC connections.
/// </summary>
public class LoopbackEndpoint : IModularRpcRemoteEndpoint
{
    /// <summary>
    /// Should this side act as a server or client?
    /// </summary>
    public bool IsServer { get; }

    /// <summary>
    /// Forces the use of a <see cref="MemoryStream"/> instead of binary data.
    /// </summary>
    public bool UseStreams { get; }

    /// <summary>
    /// Advertises to the library that the loopback connection exhibits loopback behavior, otherwise it behaves as any other remote connection.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool AdvertiseLoopback { get; }

    public LoopbackEndpoint(bool isServer) : this(isServer, false, true) { }
    public LoopbackEndpoint(bool isServer, bool useStreams) : this(isServer, useStreams, true) { }
    public LoopbackEndpoint(bool isServer, bool useStreams, bool advertiseLoopback)
    {
        IsServer = isServer;
        UseStreams = useStreams;
        AdvertiseLoopback = advertiseLoopback;
    }

    /// <summary>
    /// Creates the other side of this endpoint.
    /// </summary>
    public LoopbackEndpoint CreateOtherSide() => new LoopbackEndpoint(!IsServer, UseStreams, AdvertiseLoopback);

    /// <summary>
    /// Get a remote connection with a local and server counterpart.
    /// </summary>
    /// <returns>Either <see cref="LoopbackRpcClientsideLocalConnection"/> or <see cref="LoopbackRpcServersideLocalConnection"/>, depending on the value of <see cref="IsServer"/>.</returns>
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        if (IsServer)
        {
            LoopbackRpcServersideRemoteConnection serverConnection = new LoopbackRpcServersideRemoteConnection(this, router, serializer, connectionLifetime, UseStreams, AdvertiseLoopback);
            _ = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(), router, serializer, lifetime: null, serverConnection, UseStreams, AdvertiseLoopback);

            await serverConnection.Local.InitializeConnectionAsync(token).ConfigureAwait(false);

            await connectionLifetime.TryAddNewConnection(serverConnection, token).ConfigureAwait(false);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), router, serializer, lifetime: null, UseStreams, AdvertiseLoopback);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, router, serializer, connectionLifetime, serverRemote, UseStreams, AdvertiseLoopback);

        await serverRemote.Local.InitializeConnectionAsync(token).ConfigureAwait(false);

        await connectionLifetime.TryAddNewConnection(clientConnection, token).ConfigureAwait(false);
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
                UseStreams, AdvertiseLoopback);
            
            LoopbackRpcClientsideRemoteConnection clientRemote = new LoopbackRpcClientsideRemoteConnection(CreateOtherSide(),
                clientServices.GetRequiredService<IRpcRouter>(),
                clientServices.GetRequiredService<IRpcSerializer>(),
                clientServices.GetRequiredService<IRpcConnectionLifetime>(),
                serverConnection,
                UseStreams, AdvertiseLoopback);

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

            await serverConnection.Local.InitializeConnectionAsync(token).ConfigureAwait(false);

            await serverConnection.Lifetime!.TryAddNewConnection(serverConnection, token).ConfigureAwait(false);
            
            await clientRemote.Lifetime!.TryAddNewConnection(clientRemote, token).ConfigureAwait(false);
            
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(),
            serverServices.GetRequiredService<IRpcRouter>(),
            serverServices.GetRequiredService<IRpcSerializer>(),
            serverServices.GetRequiredService<IRpcConnectionLifetime>(),
            UseStreams, AdvertiseLoopback);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this,
            clientServices.GetRequiredService<IRpcRouter>(),
            clientServices.GetRequiredService<IRpcSerializer>(),
            clientServices.GetRequiredService<IRpcConnectionLifetime>(),
            serverRemote,
            UseStreams, AdvertiseLoopback);
        
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
        
        await serverRemote.Local.InitializeConnectionAsync(token).ConfigureAwait(false);
        
        await serverRemote.Lifetime!.TryAddNewConnection(serverRemote, token).ConfigureAwait(false);
        
        await clientConnection.Lifetime!.TryAddNewConnection(clientConnection, token).ConfigureAwait(false);
        
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

            await serverConnection.Local.InitializeConnectionAsync(token).ConfigureAwait(false);

            await serverConnectionLifetime.TryAddNewConnection(serverConnection, token).ConfigureAwait(false);
            await clientConnectionLifetime.TryAddNewConnection(serverConnection.Client, token).ConfigureAwait(false);
            return serverConnection;
        }

        LoopbackRpcServersideRemoteConnection serverRemote = new LoopbackRpcServersideRemoteConnection(CreateOtherSide(), serverRouter, serverSerializer, lifetime: null, UseStreams);
        LoopbackRpcClientsideRemoteConnection clientConnection = new LoopbackRpcClientsideRemoteConnection(this, clientRouter, clientSerializer, clientConnectionLifetime, serverRemote, UseStreams);

        await serverRemote.Local.InitializeConnectionAsync(token).ConfigureAwait(false);

        await serverConnectionLifetime.TryAddNewConnection(clientConnection.Server, token).ConfigureAwait(false);
        await clientConnectionLifetime.TryAddNewConnection(clientConnection, token).ConfigureAwait(false);
        return clientConnection;
    }

    /// <inheritdoc />
    public override string ToString() => IsServer ? "Loopback (Server)" : "Loopback (Client)";
}