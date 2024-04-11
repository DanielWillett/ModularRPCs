using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Invocation;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcCaller
{
    RpcTask InvokeRpc(RpcVirtualInvocationContext context);
    RpcTask<T> InvokeRpc<T>(RpcVirtualInvocationContext context);
}