using DanielWillett.ModularRpcs.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask
{
    private protected RpcTaskAwaiter Awaiter = null!;
    internal Exception? Exception;

    /// <summary>
    /// An instance of <see cref="RpcTask"/> that instantly completes, skipping any context switching.
    /// </summary>
    public static RpcTask CompletedTask { get; } = new RpcTask();

    /// <summary>
    /// This property will always throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <remarks>Mainly used as the default body for RPC callers.</remarks>
    /// <exception cref="NotImplementedException"/>
    public static RpcTask NotImplemented => throw new NotImplementedException(Properties.Exceptions.RpcNotImplemented);

    /// <summary>
    /// Is this task explicitly set to be in fire-and-forget mode due to a <see cref="RpcFireAndForgetAttribute"/>.
    /// </summary>
    public bool IsFireAndForget { get; }
    internal RpcTask(bool isFireAndForget)
    {
        if (GetType() == typeof(RpcTask))
            Awaiter = new RpcTaskAwaiter(this, isFireAndForget);
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