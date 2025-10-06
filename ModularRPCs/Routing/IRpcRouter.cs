using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
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
    /// The connection lifetime used by this router.
    /// </summary>
    [UsedImplicitly]
    IRpcConnectionLifetime ConnectionLifetime { get; }

    /// <summary>
    /// The serializer used by this router.
    /// </summary>
    [UsedImplicitly]
    IRpcSerializer Serializer { get; }

    /// <summary>
    /// Get a saved <see cref="IRpcInvocationPoint"/> from it's Id.
    /// </summary>
    /// <param name="endpointSharedId">Unique shared ID for the rpc endpoint.</param>
    [System.Diagnostics.Contracts.Pure]
    IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId);

    /// <summary>
    /// Gets a new unique ID for the given endpoint and register it. This does not check for duplicate endpoints, just ensures a unique ID is assigned.
    /// </summary>
    uint AddRpcEndpoint(IRpcInvocationPoint endPoint);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    [System.Diagnostics.Contracts.Pure]
    IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation, int byteSize, object? identifier);

    /// <summary>
    /// Resolve an endpoint from the read information.
    /// </summary>
    /// <param name="knownRpcShortcutId">Unique known RPC ID from the server. 0 means unknown.</param>
    [System.Diagnostics.Contracts.Pure]
    IRpcInvocationPoint ResolveEndpoint(IRpcSerializer serializer, uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation, int byteSize, object? identifier);

    /// <summary>
    /// Invoke an RPC from a 'call' method. Expects that there will be a blank space at the beginning of the buffer for the overhead to be written to. Use <see cref="GetOverheadSize"/> to help calculate that.
    /// </summary>
    /// <param name="connections">A <see cref="IModularRpcRemoteConnection"/>, <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/>, or <see langword="null"/> for all connections.</param>
    unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, CancellationToken token, byte* bytesSt, int byteCt, uint dataCt, ref RpcCallMethodInfo callMethodInfo, RpcInvokeOptions options = RpcInvokeOptions.Default);

    /// <summary>
    /// Invoke an RPC from a 'call' method. Using this to send data to multiple connections is not recommended over <see cref="InvokeRpc(object?,IRpcSerializer,RuntimeMethodHandle,CancellationToken,byte*,int,uint,ref RpcCallMethodInfo,RpcInvokeOptions)"/>, as the data will be copied to a buffer anyways.
    /// </summary>
    /// <param name="overheadBuffer">Existing buffer for overhead to be written to. This allows identifiers to be written beforehand.</param>
    /// <param name="dataStream">Stream of the data portion of the message.</param>
    /// <param name="leaveOpen">If this function should not dispose of <paramref name="dataStream"/>, otherwise this method can be trusted to dispose of <paramref name="dataStream"/>.</param>
    /// <param name="dataCt">Number of bytes in the data portion of the message. Usually this would be <c><paramref name="dataStream"/>.Length - <paramref name="dataStream"/>.Position</c>.</param>
    /// <param name="connections">A <see cref="IModularRpcRemoteConnection"/>, <see cref="IEnumerable{T}"/> of <see cref="IModularRpcRemoteConnection"/>, or <see langword="null"/> for all connections.</param>
    RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, CancellationToken token, ArraySegment<byte> overheadBuffer, Stream dataStream, bool leaveOpen, uint dataCt, ref RpcCallMethodInfo callMethodInfo, RpcInvokeOptions options = RpcInvokeOptions.Default);

    /// <summary>
    /// Pre-calculate the size of the overhead resulting from calling this RPC from a 'call' method.
    /// </summary>
    [System.Diagnostics.Contracts.Pure, UsedImplicitly]
    uint GetOverheadSize(RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo);

    /// <summary>
    /// Get the default interface implementations for a proxy class.
    /// </summary>
    [UsedImplicitly]
    void GetDefaultProxyContext(Type proxyType, out ProxyContext context);

    /// <summary>
    /// Sends a cancellation of a message if supported, otherwise does nothing.
    /// </summary>
    void InvokeCancellation(RpcTask task);

    /// <summary>
    /// Called when a connection is closed.
    /// </summary>
    void CleanupConnection(IModularRpcConnection connection);

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

    /// <summary>
    /// Pings a remote <paramref name="connection"/> and waits for a response.
    /// </summary>
    /// <param name="connection">The connection to ping.</param>
    /// <param name="token">A token that cancels the ping request.</param>
    /// <param name="timeout">The maximum time to wait before throwing a <see cref="RpcTimeoutException"/>.</param>
    /// <exception cref="RpcTimeoutException">Thrown when the ping doesn't succeed.</exception>
    RpcPingTask PingAsync(IModularRpcRemoteConnection connection, TimeSpan timeout = default, CancellationToken token = default);

    /// <summary>
    /// Send a message to the remote connection to gracefully disconnect.
    /// </summary>
    /// <returns>A task that completes when the message has been sent, not when the connection fully disconnects.</returns>
    ValueTask GracefullyDisconnectAsync(IModularRpcRemoteConnection connection, CancellationToken token = default);

    /*
     *  The following methods are invoked by SerializerGenerator after an RPC finishes running.
     *
     *  This can't be done internally because a continuation has to be used for methods returning awaitable objects
     */

    /// <summary>
    /// Invoked after a <see cref="RpcReceiveAttribute"/> method returns that has a <see langword="void"/> return type.
    /// </summary>
    /// <param name="overhead">Overhead of the RPC being invoked.</param>
    /// <param name="serializer">The serializer being used by the RPC.</param>
    void HandleInvokeVoidReturn(RpcOverhead overhead, IRpcSerializer serializer);

    /// <summary>
    /// Invoked after a <see cref="RpcReceiveAttribute"/> method throws an exception.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="overhead">Overhead of the RPC being invoked.</param>
    /// <param name="serializer">The serializer being used by the RPC.</param>
    void HandleInvokeException(Exception exception, RpcOverhead overhead, IRpcSerializer serializer);

    /// <summary>
    /// Invoked after a <see cref="RpcReceiveAttribute"/> method returns that has a <see cref="IRpcSerializable"/> return type.
    /// </summary>
    /// <param name="value">The value to be serialized. This is ignored if <paramref name="collection"/> is not null.</param>
    /// <param name="collection">The collection to be serialized. To use a null collection, pass <see cref="DBNull.Value"/>.</param>
    /// <param name="overhead">Overhead of the RPC being invoked.</param>
    /// <param name="serializer">The serializer being used by the RPC.</param>
    void HandleInvokeSerializableReturnValue<TSerializable>(TSerializable value, object? collection, RpcOverhead overhead, IRpcSerializer serializer) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Invoked after a <see cref="RpcReceiveAttribute"/> method returns that has a non-<see langword="void"/> return type.
    /// </summary>
    /// <param name="value">The value to be serialized.</param>
    /// <param name="overhead">Overhead of the RPC being invoked.</param>
    /// <param name="serializer">The serializer being used by the RPC.</param>
    void HandleInvokeReturnValue<TReturnType>(TReturnType value, RpcOverhead overhead, IRpcSerializer serializer);

    /// <summary>
    /// Invoked after a <see cref="RpcReceiveAttribute"/> method returns that has a nullable value return type.
    /// </summary>
    /// <param name="value">The value to be serialized.</param>
    /// <param name="overhead">Overhead of the RPC being invoked.</param>
    /// <param name="serializer">The serializer being used by the RPC.</param>
    void HandleInvokeNullableReturnValue<TReturnType>(TReturnType? value, RpcOverhead overhead, IRpcSerializer serializer) where TReturnType : struct;

    /// <summary>
    /// Invoked after a <see cref="RpcReceiveAttribute"/> method returns that has a nullable <see cref="IRpcSerializable"/> return type or collection of them.
    /// </summary>
    /// <param name="value">The value to be serialized. This is ignored if <paramref name="collection"/> is not null.</param>
    /// <param name="collection">The collection to be serialized. To use a null collection, pass <see cref="DBNull.Value"/>.</param>
    /// <param name="overhead">Overhead of the RPC being invoked.</param>
    /// <param name="serializer">The serializer being used by the RPC.</param>
    void HandleInvokeNullableSerializableReturnValue<TSerializable>(TSerializable? value, object? collection, RpcOverhead overhead, IRpcSerializer serializer) where TSerializable : struct, IRpcSerializable;
}