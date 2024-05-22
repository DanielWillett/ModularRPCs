using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Protocol;

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
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, int signatureHash, int byteSize, object? identifier);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    IRpcInvocationPoint ResolveEndpoint(IRpcSerializer serializer, uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, int signatureHash, int byteSize, object? identifier);

    /// <summary>
    /// Invoke an RPC from a 'call' method.
    /// </summary>
    /// <param name="connections">A <see cref="IModularRpcRemoteConnection"/>, <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/>, or <see langword="null"/> for all connections.</param>
    unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, byte* bytesSt, int byteCt, uint dataCt, ref RpcCallMethodInfo callMethodInfo);

    /// <summary>
    /// Pre-calculate the size of the overhead resulting from calling this RPC from a 'call' method.
    /// </summary>
    int GetOverheadSize(RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo);

    /// <summary>
    /// Get the default interface implementations for a proxy class.
    /// </summary>
    void GetDefaultProxyContext(Type proxyType, out ProxyContext context);

    /// <summary>
    /// Invoke an RPC by it's invocation point.
    /// </summary>
    ValueTask InvokeInvocationPoint(IRpcInvocationPoint rpc, RpcOverhead overhead, IRpcSerializer serializer, Stream stream, CancellationToken token);

    /// <summary>
    /// Invoke an RPC by it's invocation point. If context switching would occur, <paramref name="bytes"/> MUST BE COPIED.
    /// </summary>
    ValueTask InvokeInvocationPoint(IRpcInvocationPoint rpc, RpcOverhead overhead, IRpcSerializer serializer, ReadOnlySpan<byte> bytes, CancellationToken token);
}