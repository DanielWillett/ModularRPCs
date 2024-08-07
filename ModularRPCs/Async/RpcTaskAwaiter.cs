using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTaskAwaiter : ICriticalNotifyCompletion
{
    private Action? _continuation;
    public RpcTask Task { get; }

    /// <summary>
    /// Get the completion state of the <see cref="RpcTask"/>. 
    /// </summary>
    public bool IsCompleted { get; private protected set; }
    public RpcTaskAwaiter(RpcTask task, bool isCompleted)
    {
        Task = task;
        IsCompleted = isCompleted;
    }
    internal void TriggerComplete()
    {
        if (Interlocked.Decrement(ref Task.CompleteCount) != 0)
            return;

        IsCompleted = true;
        _continuation?.Invoke();
        Task.CombinedTokensToDisposeOnComplete.Dispose();
    }

    void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => ((ICriticalNotifyCompletion)this).OnCompleted(continuation);
    void INotifyCompletion.OnCompleted(Action continuation)
    {
        _continuation = continuation;
    }

    /// <summary>
    /// The value of the task. Will throw a <see cref="RpcGetResultUsageException"/> if the task hasn't completed yet.
    /// </summary>
    /// <exception cref="RpcGetResultUsageException">Task has yet to complete.</exception>
    public void GetResult()
    {
        if (!Task.IsFireAndForget && !IsCompleted)
            throw new RpcGetResultUsageException();

        Exception? ex = Task.GetException();

        if (ex == null)
            return;

        // not thrown yet
        if (ex.StackTrace == null)
            throw ex;
        
        if (ex is OperationCanceledException)
            throw ex;
     
        throw new RpcInvocationException(ex);
    }
}
