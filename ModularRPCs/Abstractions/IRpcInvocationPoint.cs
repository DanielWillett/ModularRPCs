using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Routing;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; set; }
    bool CanCache { get; }
    uint Size { get; }
    bool IsStatic { get; }
    object? Identifier { get; }

    /// <summary>
    /// Invoke the RPC from a byte buffer. If context switching is needed, the data MUST BE COPIED.
    /// </summary>
    ValueTask Invoke(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, ReadOnlySpan<byte> byteData, CancellationToken token = default);

    /// <summary>
    /// Invoke the RPC from a stream.
    /// </summary>
    ValueTask Invoke(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token = default);
    IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier);
    string ToString();
}