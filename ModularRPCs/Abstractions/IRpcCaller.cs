using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Invocation;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcCaller
{
    RpcTask InvokeRpc(RpcInvocationContext context);
    RpcTask<T> InvokeRpc<T>(RpcInvocationContext context);
}