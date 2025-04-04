using System;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Exceptions;

namespace DanielWillett.ModularRpcs.Async;
public readonly struct ConfiguredRpcTaskAwaitable<T> : ICriticalNotifyCompletion
{
    private readonly RpcTaskAwaiter<T> _awaiter;
    private readonly bool _continueOnCapturedContext;

    public ConfiguredRpcTaskAwaitable(RpcTaskAwaiter<T> awaiter, bool continueOnCapturedContext)
    {
        _awaiter = awaiter;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public bool IsCompleted => _awaiter.IsCompleted;

    public void UnsafeOnCompleted(Action continuation)
    {
        _awaiter.OnCompletedIntl(continuation, continueOnCapturedContext: _continueOnCapturedContext, flowExecutionContext: false);
    }

    public void OnCompleted(Action continuation)
    {
        _awaiter.OnCompletedIntl(continuation, continueOnCapturedContext: _continueOnCapturedContext, flowExecutionContext: true);
    }

    public T GetResult()
    {
        return _awaiter.GetResult();
    }
}

public class RpcTaskAwaiter<T> : RpcTaskAwaiter
{
    public RpcTaskAwaiter(RpcTask<T> task, bool isCompleted) : base(task, isCompleted) { }

    /// <summary>
    /// The value of the task. Will throw a <see cref="RpcGetResultUsageException"/> if the task hasn't completed yet.
    /// </summary>
    /// <exception cref="RpcGetResultUsageException">Task has yet to complete.</exception>
    public new T GetResult()
    {
        base.GetResult();
        return ((RpcTask<T>)Task).ResultIntl!;
    }
}