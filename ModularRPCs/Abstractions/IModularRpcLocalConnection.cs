using DanielWillett.ModularRpcs.Routing;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Represents the local side of a connection.
/// </summary>
public interface IModularRpcLocalConnection : IModularRpcConnection
{
    /// <summary>
    /// The router this connection uses.
    /// </summary>
    IRpcRouter Router { get; }

    /// <summary>
    /// The remote side of this connection.
    /// </summary>
    IModularRpcRemoteConnection Remote { get; }
}