using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Represents an endpoint that could become a connection.
/// </summary>
public interface IModularRpcRemoteEndpoint
{
    /// <summary>
    /// Attempt to start a connection with this endpoint.
    /// </summary>
    /// <returns>That connection that was started.</returns>
    Task<IModularRpcRemoteConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default);
}