using System;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcInvocationPoint
{
    uint? EndpointId { get; }
    int Size { get; }
    ValueTask Invoke(object? targetObject, ArraySegment<object> parameters);
}
