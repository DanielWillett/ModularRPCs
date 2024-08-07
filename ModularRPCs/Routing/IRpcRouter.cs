using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;

/// <summary>
/// Handles reading and dispatching RPCs.
/// </summary>
public interface IRpcRouter
{
    /// <summary>
    /// Collection of all broadcast receive RPCs in all registered assemblies.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<RpcEndpointTarget>> BroadcastTargets { get; }

    /// <summary>
    /// Get a saved <see cref="RpcDescriptor"/> from it's Id.
    /// </summary>
    /// <param name="endpointSharedId">Unique shared ID for the rpc endpoint.</param>
    [Pure]
    IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId);

    /// <summary>
    /// Gets a new unique ID for the given endpoint and register it. This does not check for duplicate endpoints, just ensures a unique ID is assigned.
    /// </summary>
    uint AddRpcEndpoint(IRpcInvocationPoint endPoint);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    [Pure]
    IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, int byteSize, object? identifier);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    [Pure]
    IRpcInvocationPoint ResolveEndpoint(IRpcSerializer serializer, uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, int byteSize, object? identifier);

    /// <summary>
    /// Invoke an RPC from a 'call' method. Expects that there will be a blank space at the beginning of the buffer for the overhead to be written to. Use <see cref="GetOverheadSize"/> to help calculate that.
    /// </summary>
    /// <param name="connections">A <see cref="IModularRpcRemoteConnection"/>, <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/>, or <see langword="null"/> for all connections.</param>
    unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, CancellationToken token, byte* bytesSt, int byteCt, uint dataCt, ref RpcCallMethodInfo callMethodInfo);

    /// <summary>
    /// Invoke an RPC from a 'call' method. Using this to send data to multiple connections is not recommended over <see cref="InvokeRpc(object?,IRpcSerializer,RuntimeMethodHandle,CancellationToken,byte*,int,uint,ref RpcCallMethodInfo)"/>, as the data will be copied to a buffer anyways.
    /// </summary>
    /// <param name="overheadBuffer">Existing buffer for overhead to be written to. This allows identifiers to be written beforehand.</param>
    /// <param name="dataStream">Stream of the data portion of the message.</param>
    /// <param name="leaveOpen">If this function should not dispose of <paramref name="dataStream"/>, otherwise this method can be trusted to dispose of <paramref name="dataStream"/>.</param>
    /// <param name="dataCt">Number of bytes in the data portion of the message. Usually this would be <c><paramref name="dataStream"/>.Length - <paramref name="dataStream"/>.Position</c>.</param>
    /// <param name="connections">A <see cref="IModularRpcRemoteConnection"/>, <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/>, or <see langword="null"/> for all connections.</param>
    RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, CancellationToken token, ArraySegment<byte> overheadBuffer, Stream dataStream, bool leaveOpen, uint dataCt, ref RpcCallMethodInfo callMethodInfo);

    /// <summary>
    /// Pre-calculate the size of the overhead resulting from calling this RPC from a 'call' method.
    /// </summary>
    [Pure]
    uint GetOverheadSize(RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo);

    /// <summary>
    /// Get the default interface implementations for a proxy class.
    /// </summary>
    void GetDefaultProxyContext(Type proxyType, out ProxyContext context);

    /// <summary>
    /// Invoke an RPC by it's invocation point. If context switching would occur and <paramref name="canTakeOwnership"/> is <see langword="false"/>, <paramref name="rawData"/> MUST BE COPIED.
    /// </summary>
    /// <param name="canTakeOwnership">If the backing storage for <paramref name="rawData"/> is safe to use outside the current stack frame. If this is <see langword="false"/>, data should be copied before context switching.</param>
    ValueTask ReceiveData(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token = default);

    /// <summary>
    /// Invoke an RPC by it's invocation point.
    /// </summary>
    ValueTask ReceiveData(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream, CancellationToken token = default);

    /// <summary>
    /// Invoke an RPC by it's invocation point with the overhead already read. If context switching would occur and <paramref name="canTakeOwnership"/> is <see langword="false"/>, <paramref name="rawData"/> MUST BE COPIED.
    /// </summary>
    /// <param name="canTakeOwnership">If the backing storage for <paramref name="rawData"/> is safe to use outside the current stack frame. If this is <see langword="false"/>, data should be copied before context switching.</param>
    ValueTask ReceiveData(in PrimitiveRpcOverhead overhead, IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token = default);

    /// <summary>
    /// Invoke an RPC by it's invocation point with the overhead already read.
    /// </summary>
    ValueTask ReceiveData(in PrimitiveRpcOverhead overhead, IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream, CancellationToken token = default);
}