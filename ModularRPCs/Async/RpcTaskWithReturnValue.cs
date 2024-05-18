using DanielWillett.ModularRpcs.Exceptions;
using System;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask<T> : RpcTask
{
    internal T? ResultIntl;

    /// <summary>
    /// This property will always throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <remarks>Mainly used as the default body for RPC callers.</remarks>
    /// <exception cref="NotImplementedException"/>
    public new static RpcTask<T> NotImplemented => throw new NotImplementedException(Properties.Exceptions.RpcNotImplemented);

    /// <summary>
    /// The value of the task. Will throw a <see cref="RpcGetResultUsageException"/> if the task hasn't completed yet.
    /// </summary>
    /// <exception cref="RpcGetResultUsageException">Task has yet to complete.</exception>
    public T Result => ((RpcTaskAwaiter<T>)Awaiter).GetResult();
    internal RpcTask() : base(false)
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