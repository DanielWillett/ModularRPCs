using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; set; }
    bool CanCache { get; }
    int Size { get; }
    bool IsStatic { get; }
    object? Identifier { get; }

    /// <summary>
    /// Invoke the RPC from a byte buffer. If context switching is needed, the data MUST BE COPIED.
    /// </summary>
    ValueTask Invoke(RpcOverhead overhead, IRpcSerializer serializer, ReadOnlySpan<byte> byteData, CancellationToken token = default);

    /// <summary>
    /// Invoke the RPC from a stream.
    /// </summary>
    ValueTask Invoke(RpcOverhead overhead, IRpcSerializer serializer, Stream stream, CancellationToken token = default);
    IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier);
    string ToString();
}