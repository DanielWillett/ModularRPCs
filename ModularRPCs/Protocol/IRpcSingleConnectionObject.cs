using DanielWillett.ModularRpcs.Abstractions;
using System.Collections.Generic;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// Server-side interface defining one relevant connection that any containing RPCs will be sent to.
/// </summary>
public interface IRpcSingleConnectionObject
{
    /// <summary>
    /// The relevant connection that any containing RPCs will be sent to.
    /// </summary>
    /// <remarks>Invoke methods can pass a <see cref="IModularRpcRemoteConnection"/> or <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/> to override this behavior.</remarks>
    IModularRpcRemoteConnection Connection { get; }
}