using DanielWillett.ModularRpcs.Protocol;

namespace DanielWillett.ModularRpcs.Invocation;
public readonly struct RpcInvocationContext
{
    public RpcOverhead Overhead { get; }
    private RpcInvocationContext(RpcOverhead overhead)
    {
        Overhead = overhead;
    }
}