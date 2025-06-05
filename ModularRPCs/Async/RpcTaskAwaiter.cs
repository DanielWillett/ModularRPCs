using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Async;
public readonly struct ConfiguredRpcTaskAwaitable : ICriticalNotifyCompletion
{
    private readonly RpcTaskAwaiter _awaiter;
    private readonly bool _continueOnCapturedContext;

    public ConfiguredRpcTaskAwaitable(RpcTaskAwaiter awaiter, bool continueOnCapturedContext)
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

    public void GetResult()
    {
        _awaiter.GetResult();
    }
}

public class RpcTaskAwaiter : ICriticalNotifyCompletion
{
    private Action? _continuation;
    private ExecutionContext? _executionContext;
    private SynchronizationContext? _synchronizationContext;
    private int _hasRanContinuation;
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
        {
            return;
        }

        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(static s =>
            {
                ((RpcTaskAwaiter)s!).Complete();
            }, this);
            _synchronizationContext = null;
        }
        else
        {
            Complete();
        }
    }

    private void Complete()
    {
        if (_executionContext != null)
        {
            ExecutionContext.Run(_executionContext, static s =>
            {
                CompleteIntl((RpcTaskAwaiter)s!);
            }, this);
            _executionContext = null;
        }
        else
        {
            CompleteIntl(this);
        }

        return;
        static void CompleteIntl(RpcTaskAwaiter me)
        {
            me.IsCompleted = true;
            Interlocked.MemoryBarrier();
            Action? continuation = me._continuation;
            if (continuation != null && Interlocked.Exchange(ref me._hasRanContinuation, 1) == 0)
            {
                continuation.Invoke();
            }

            me.Task.DisposeCancellation();
        }
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompletedIntl(continuation, continueOnCapturedContext: true, flowExecutionContext: false);
    }

    public void OnCompleted(Action continuation)
    {
        OnCompletedIntl(continuation, continueOnCapturedContext: true, flowExecutionContext: true);
    }

    internal void OnCompletedIntl(Action continuation, bool continueOnCapturedContext, bool flowExecutionContext)
    {
        _continuation = continuation;
        Interlocked.MemoryBarrier();
        if (IsCompleted)
        {
            if (Interlocked.Exchange(ref _hasRanContinuation, 1) == 0)
            {
                _continuation();
                return;
            }
        }
        if (flowExecutionContext)
            _executionContext = ExecutionContext.Capture();
        if (continueOnCapturedContext)
            _synchronizationContext = SynchronizationContext.Current;
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
