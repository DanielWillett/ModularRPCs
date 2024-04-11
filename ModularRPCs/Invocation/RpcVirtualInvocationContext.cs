using DanielWillett.ModularRpcs.Protocol;
using System.Reflection;

namespace DanielWillett.ModularRpcs.Invocation;
public readonly struct RpcVirtualInvocationContext
{
    public MethodInfo? CallerMethod { get; }
    public RpcOverhead Overhead { get; }
}