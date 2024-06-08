using DanielWillett.ModularRpcs.Exceptions;

namespace DanielWillett.ModularRpcs.Async;
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