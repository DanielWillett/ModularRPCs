using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Connections;

/// <summary>
/// Represents a connection to another client.
/// </summary>
public interface IRpcConnection : IDisposable, IAsyncDisposable, IRefSafeLoggable
{
    /// <summary>
    /// If this connection exhibits a loopback behavior.
    /// </summary>
    /// <remarks>Returning <see langword="true"/> for this property may result in RPCs skipping the serialization/deserialization step and calling the receive method directly.</remarks>
    bool IsLoopback { get; }

    /// <summary>
    /// The current state of this connection.
    /// </summary>
    /// <value>The current state, or <see cref="ConnectionState.Closed"/> after this object is disposed.</value>
    ConnectionState State { get; }

    /// <summary>
    /// Connection module used to listen for incoming data.
    /// </summary>
    IListener Listener { get; }

    /// <summary>
    /// Connection module used to transmit outgoing data.
    /// </summary>
    ITransmitter Transmitter { get; }

    /// <summary>
    /// The factory that created this connection.
    /// </summary>
    IConnectionFactory Factory { get; }

    /// <summary>
    /// The relationship this client has with it's endpoint.
    /// </summary>
    RpcConnectionRelationship EndpointRelationship { get; }

    /// <summary>
    /// Generic string-keyed tags for third party usage.
    /// </summary>
    /// <remarks>Recommended to use a <see cref="ConcurrentDictionary{TKey,TValue}"/>.</remarks>
    IDictionary<string, object> Tags { get; }

    /// <summary>
    /// Temporarily disconnect from the client. Can reconnect later using <see cref="ReconnectAsync"/>.
    /// </summary>
    /// <param name="token">Cancellation token for the disconnect operation.</param>
    /// <returns>A task that completes when the client has fully disconnected.</returns>
    /// <exception cref="ObjectDisposedException"/>
    ValueTask DisconnectAsync(CancellationToken token = default);

    /// <summary>
    /// Connects to the client. If this connection is connected to the client, will disconnect before reconnecting. 
    /// </summary>
    /// <param name="token">Cancellation token for the disconnect operation.</param>
    /// <returns>A task that completes when the client has fully reconnected.</returns>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="RpcConnectionFailedException">Something went wrong with the connection.</exception>
    ValueTask ReconnectAsync(CancellationToken token = default);
    
    // <summary>
    // Disconnect from the client and dispose all resources. Can not be undone without creating a new connection.
    // </summary>
    // <returns>A task that completes when the client is disconnected and all resources have been freed.</returns>
    // ValueTask DisposeAsync();

    /// <summary>
    /// Gets information about the connected endpoint in a human-readable format.
    /// </summary>
    string ToString();
}

/// <summary>
/// Represents the current state of a <see cref="IRpcConnection"/>.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Connected to another client.
    /// </summary>
    Connected,

    /// <summary>
    /// Disconnected, but may reconnect in the future.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Permanently closed.
    /// </summary>
    Closed
}