using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Reflection;

namespace DanielWillett.ModularRpcs.Routing;

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
    /// Gets a new unique ID for the given endpoint and register it. This does not check for duplicate endpoints, just ensures a unique ID is assigned.
    /// </summary>
    uint AddRpcEndpoint(IRpcInvocationPoint endPoint);

    /// <summary>
    /// Intake raw data from a local connection over a stream.
    /// </summary>
    ValueTask HandleReceivedData(RpcOverhead overhead, Stream streamData, CancellationToken token = default);

    /// <summary>
    /// Intake raw data from a local connection over raw memory.
    /// </summary>
    ValueTask HandleReceivedData(RpcOverhead overhead, ReadOnlySpan<byte> byteData, CancellationToken token = default);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, bool isStatic, string[] args, int byteSize, object? identifier);

    /// <summary>
    /// Read an identifier starting at the given byte pointer with <paramref name="maxCt"/> bytes left.
    /// </summary>
    /// <param name="bytes">Pointer to the start of the identifier.</param>
    /// <param name="maxCt">Number of bytes available for reading.</param>
    /// <param name="bytesRead">Number of bytes consumed.</param>
    /// <returns>The identifier.</returns>
    /// <exception cref="RpcOverheadParseException">Unable to parse identifier or not enough bytes in buffer.</exception>
    unsafe object ReadIdentifierFromBytes(byte* bytes, uint maxCt, out int bytesRead);

    /// <summary>
    /// Read an identifier from a stream.
    /// </summary>
    /// <param name="bytesRead">Number of bytes consumed.</param>
    /// <returns>The identifier.</returns>
    /// <exception cref="RpcOverheadParseException">Unable to parse identifier or not enough bytes left in the stream.</exception>
    object ReadIdentifierFromStream(Stream stream, out int bytesRead);

    /// <summary>
    /// Calculates the size of an identifier in bytes.
    /// </summary>
    /// <exception cref="ArgumentException">The given identifier can not be serialized or deserialized.</exception>
    int CalculateIdentifierSize(object identifier);

    /// <summary>
    /// Get the default interface implementations for a proxy class.
    /// </summary>
    void GetDefaultProxyContext(Type proxyType, out ProxyContext context);
}