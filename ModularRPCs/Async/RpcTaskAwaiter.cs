using System;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Exceptions;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTaskAwaiter : ICriticalNotifyCompletion
{
    private Action? _continuation;
    public RpcTask Task { get; }
    public bool IsCompleted { get; private set; }
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
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    public void OnCompleted(Action continuation)
    {
        _continuation = continuation;
    }
    public void GetResult()
    {
        if (Task.IsFireAndForget)
            throw new RpcFireAndForgetException();
        if (!IsCompleted)
            throw new RpcGetResultUsageException();

        if (Task.Exception != null)
            throw Task.Exception;
    }
}
