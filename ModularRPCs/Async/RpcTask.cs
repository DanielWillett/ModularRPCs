using System;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask
{
    /// <summary>
    /// An instance of <see cref="RpcTask"/> that instantly completes, skipping any context switching.
    /// </summary>
    public static RpcTask CompletedTask { get; } = new RpcTask();

    private protected RpcTaskAwaiter Awaiter = null!;
    internal Exception? Exception;
    public bool IsFireAndForget { get; }
    internal RpcTask(bool isFireAndForget)
    {
        if (GetType() == typeof(RpcTask))
            Awaiter = new RpcTaskAwaiter(this, false);
        IsFireAndForget = isFireAndForget;
    }
    private protected RpcTask()
    {
        Awaiter = new RpcTaskAwaiter(this, true);
    }
    public RpcTaskAwaiter GetAwaiter() => Awaiter;
    internal void TriggerComplete(Exception? exception)
    {
        Exception = exception;
        Awaiter.TriggerComplete();
    }

    public static RpcTask<T> FromResult<T>(T value) => new RpcTask<T>(value);
}