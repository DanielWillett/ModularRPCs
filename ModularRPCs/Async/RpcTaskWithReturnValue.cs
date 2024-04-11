using System;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask<T> : RpcTask
{
    internal T? ResultIntl;
    public T Result => ((RpcTaskAwaiter<T>)Awaiter).GetResult();
    internal RpcTask(bool isFireAndForget) : base(isFireAndForget)
    {
        Awaiter = new RpcTaskAwaiter<T>(this, false);
    }
    internal RpcTask(T value) : base(false)
    {
        Awaiter = new RpcTaskAwaiter<T>(this, true);
        ResultIntl = value;
    }
    public new RpcTaskAwaiter<T> GetAwaiter() => (RpcTaskAwaiter<T>)Awaiter;
    internal void TriggerComplete(Exception? exception, T value)
    {
        Exception = exception;
        Awaiter.TriggerComplete();
    }
}