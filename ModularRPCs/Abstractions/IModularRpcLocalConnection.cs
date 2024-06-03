using System.Collections.Generic;
using System.Collections.Concurrent;
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

    /// <summary>
    /// Generic string-keyed tags for third party usage. Recommended to use a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    IDictionary<string, object> Tags { get; }
}