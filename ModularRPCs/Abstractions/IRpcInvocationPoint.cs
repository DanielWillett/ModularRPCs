using DanielWillett.ModularRpcs.Protocol;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; set; }
    bool CanCache { get; }
    int Size { get; }
    bool IsStatic { get; }
    object? Identifier { get; }
    ValueTask Invoke(RpcOverhead overhead, IRpcSerializer serializer, ReadOnlySpan<byte> byteData, CancellationToken token = default);
    ValueTask Invoke(RpcOverhead overhead, IRpcSerializer serializer, Stream stream, CancellationToken token = default);
    IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier);
}