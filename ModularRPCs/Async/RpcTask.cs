using DanielWillett.ModularRpcs.Annotations;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask
{
    private protected RpcTaskAwaiter Awaiter = null!;
    internal Exception? Exception;
    internal ConcurrentBag<Exception>? Exceptions;
    internal int CompleteCount = 1;

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

    /// <summary>
    /// The unique (to the sender) id of this message.
    /// </summary>
    public ulong MessageId { get; internal set; }
    
    /// <summary>
    /// The sub-message id used to differentiate between the original message and it's responses.
    /// </summary>
    public byte SubMessageId { get; internal set; }
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
        if (exception == null)
        {
            Awaiter.TriggerComplete();
            return;
        }

        ConcurrentBag<Exception>? bag = null;
        if (Exception == null)
        {
            Exception? alreadyThereException = Interlocked.CompareExchange(ref Exception, exception, null);
            if (alreadyThereException != null)
            {
                if (Exceptions == null)
                    bag = Interlocked.CompareExchange(ref Exceptions, new ConcurrentBag<Exception>(), null) ?? Exceptions;
                else
                    bag = Exceptions;
            }
        }
        else if (Exceptions == null)
            bag = Interlocked.CompareExchange(ref Exceptions, new ConcurrentBag<Exception>(), null) ?? Exceptions;
        else
            bag = Exceptions;

        bag?.Add(exception);
        Awaiter.TriggerComplete();
    }

    public static RpcTask<T> FromResult<T>(T value) => new RpcTask<T>(value);
}