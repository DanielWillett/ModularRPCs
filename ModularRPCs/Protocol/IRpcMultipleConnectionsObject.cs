using DanielWillett.ModularRpcs.Abstractions;
using System.Collections.Generic;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// Server-side interface defining multiple relevant connections that any containing RPCs will be sent to.
/// </summary>
public interface IRpcMultipleConnectionsObject
{
    /// <summary>
    /// All relevant connections that any containing RPCs will be sent to.
    /// </summary>
    /// <remarks>Invoke methods can pass a <see cref="IModularRpcRemoteConnection"/> or <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/> to override this behavior.</remarks>
    IEnumerable<IModularRpcRemoteConnection> Connections { get; }
}