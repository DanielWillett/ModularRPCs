using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Threading;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; set; }
    bool CanCache { get; }
    uint Size { get; }
    bool IsStatic { get; }

    /// <remarks>This may be a false positive in some cases.</remarks>
    bool SupportsRemoteCancellation { get; }
    object? Identifier { get; }

    /// <summary>
    /// Invoke the RPC from a byte buffer. If context switching is needed, the data MUST BE COPIED if <paramref name="canTakeOwnership"/> is <see langword="false"/>.
    /// </summary>
    /// <remarks><see cref="IRpcRouter.HandleReturnValue{TReturnType}"/> should be invoked when completed.</remarks>
    void Invoke(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, ReadOnlyMemory<byte> byteData, bool canTakeOwnership, CancellationToken token = default);

    /// <summary>
    /// Invoke the RPC from a stream.
    /// </summary>
    /// <remarks><see cref="IRpcRouter.HandleReturnValue{TReturnType}"/> should be invoked when completed.</remarks>
    void Invoke(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token = default);
    IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier);
    string ToString();
}