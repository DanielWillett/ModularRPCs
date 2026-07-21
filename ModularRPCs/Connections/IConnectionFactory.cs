using DanielWillett.ModularRpcs.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Connections;

/// <summary>
/// Responsible for activating <see cref="IRpcConnection"/> instances.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// The relationship of clients being created by this <see cref="IConnectionFactory"/>.
    /// </summary>
    RpcConnectionRelationship ConnectionType { get; }

    /// <summary>
    /// Creates a new connection object and establishes the connection this factory is configured for.
    /// </summary>
    /// <param name="token">Cancellation token for the connection task.</param>
    /// <exception cref="RpcConnectionFailedException"/>
    /// <returns>A task that results in the newly-established connection.</returns>
    ValueTask<IRpcConnection> CreateConnectionAsync(CancellationToken token = default);

    /// <summary>
    /// Gets the address to the configured endpoint in a human-readable format.
    /// </summary>
    string ToString();
}