using System;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; }
    bool CanCache { get; }
    int Size { get; }
    bool IsStatic { get; }
    ValueTask Invoke(object? targetObject, ArraySegment<object> parameters);
}