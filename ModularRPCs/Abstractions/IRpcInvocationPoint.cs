using System;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; set; }
    bool CanCache { get; }
    int Size { get; }
    bool IsStatic { get; }
    object? Identifier { get; }
    ValueTask Invoke(ArraySegment<object> parameters);
    IRpcInvocationPoint CloneWithIdentifier(IRpcRouter router, object? identifier);
}