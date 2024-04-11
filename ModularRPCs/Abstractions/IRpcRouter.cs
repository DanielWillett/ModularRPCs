using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Handles reading and dispatching RPCs.
/// </summary>
public interface IRpcRouter
{
    /// <summary>
    /// Get a saved <see cref="RpcDescriptor"/> from it's Id.
    /// </summary>
    /// <param name="endpointSharedId">Unique shared ID for the rpc endpoint.</param>
    IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId);

    /// <summary>
    /// Intake raw data from a local connection over a stream.
    /// </summary>
    ValueTask HandleReceivedData(IModularRpcLocalConnection connection, Stream streamData, CancellationToken token = default);

    /// <summary>
    /// Intake raw data from a local connection over raw memory.
    /// </summary>
    ValueTask HandleReceivedData(IModularRpcLocalConnection connection, Memory<byte> byteData, CancellationToken token = default);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, int byteSize);
}