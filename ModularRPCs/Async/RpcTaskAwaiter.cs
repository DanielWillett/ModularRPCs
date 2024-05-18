using System;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Exceptions;

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
        IsCompleted = true;
        try
        {
            _continuation?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            // todo
        }
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
        if (Task.IsFireAndForget)
            return;
        if (!IsCompleted)
            throw new RpcGetResultUsageException();

        if (Task.Exception != null)
            throw Task.Exception;
    }
}
