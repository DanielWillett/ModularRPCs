using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Represents an endpoint that could become a connection.
/// </summary>
public interface IModularRpcRemoteEndPoint
{
    /// <summary>
    /// Attempt to start a connection with this endpoint.
    /// </summary>
    /// <returns>That connection that was started.</returns>
    Task<IModularRpcLocalConnection> RequestConnectionAsync(IRpcRouter router, CancellationToken token = default);
}