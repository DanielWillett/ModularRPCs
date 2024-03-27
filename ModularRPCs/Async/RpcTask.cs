using System;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask
{
    /// <summary>
    /// An instance of <see cref="RpcTask"/> that instantly completes, skipping any context switching.
    /// </summary>
    public static RpcTask CompletedTask { get; } = new RpcTask();

    private readonly RpcTaskAwaiter _awaiter;
    internal Exception? Exception;
    public bool IsFireAndForget { get; }
    internal RpcTask(bool isFireAndForget)
    {
        _awaiter = new RpcTaskAwaiter(this, false);
        IsFireAndForget = isFireAndForget;
    }
    private RpcTask()
    {
        _awaiter = new RpcTaskAwaiter(this, true);
    }
    public RpcTaskAwaiter GetAwaiter() => _awaiter;
    internal void TriggerComplete(Exception? exception)
    {
        Exception = exception;
        _awaiter.TriggerComplete();
    }
}