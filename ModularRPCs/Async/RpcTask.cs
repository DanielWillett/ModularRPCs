using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DanielWillett.ModularRpcs.Async;

/// <summary>
/// Represents a pending remote RPC invocation with no return value (or a fire-and-forget task).
/// </summary>
public class RpcTask
{
    private protected RpcTaskAwaiter Awaiter = null!;
    private TokenRegistration? _token;
    internal IModularRpcRemoteConnection? ConnectionIntl;
    internal Exception? Exception;
    internal ConcurrentBag<Exception>? Exceptions;
    internal Timer? Timer;
    internal TimeSpan Timeout;
    internal int CompleteCount = 1;
    internal bool IgnoreNoConnectionsIntl;
    internal CombinedTokenSources CombinedTokensToDisposeOnComplete;

    /// <summary>
    /// The type of value stored in this task, or <see langword="void"/>.
    /// </summary>
    public virtual Type ValueType => typeof(void);

    /// <summary>
    /// If the RPC has completed or errored.
    /// </summary>
    public bool IsCompleted => Awaiter.IsCompleted;

    /// <summary>
    /// If the RPC has errored.
    /// </summary>
    public bool IsErrored => Exception != null || Exceptions != null;

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

    /// <summary>
    /// The endpoint this rpc was meant to invoke.
    /// </summary>
    public IRpcInvocationPoint? Endpoint { get; internal set; }

    /// <summary>
    /// The connection this task was sent to. May not be available in cases where the task was already completed, such as <see cref="CompletedTask"/>.
    /// </summary>
    public IModularRpcRemoteConnection? Connection => ConnectionIntl;

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

    ~RpcTask()
    {
        TokenRegistration? tkn = Interlocked.Exchange(ref _token, null);
        tkn?.Registration.Dispose();
    }

    /// <summary>
    /// Get the awaiter object for this task used by <see langword="async"/> method builders to queue continuations.
    /// </summary>
    [Pure]
    public RpcTaskAwaiter GetAwaiter() => Awaiter;

    /// <summary>
    /// Configures this task to not continue the current <see langword="async"/> method on the current <see cref="SynchronizationContext"/>, if supported by the runtime..
    /// </summary>
    /// <param name="continueOnCapturedContext">Whether or not the current <see langword="async"/> method will continue on the current <see cref="SynchronizationContext"/>, if supported by the runtime.</param>
    /// <returns>A configured <see cref="RpcTask{T}"/>.</returns>
    [Pure]
    public ConfiguredRpcTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
    {
        return new ConfiguredRpcTaskAwaitable(Awaiter, continueOnCapturedContext);
    }

    internal void SetToken(CancellationToken token, IRpcRouter router)
    {
        if (IsFireAndForget)
            return;

        TokenRegistration? old;
        if (!token.CanBeCanceled)
        {
            old = Interlocked.Exchange(ref _token, null);
            if (old == null)
                return;

            try
            {
                old.Registration.Dispose();
            }
            catch
            {
                // ignored
            }

            return;
        }

        TokenRegistration reg = new TokenRegistration
        {
            Token = token,
            Router = router,
            Task = this
        };
        old = Interlocked.Exchange(ref _token, reg);
        if (old != null)
        {
            try
            {
                old.Registration.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        if (token.IsCancellationRequested)
        {
            router.InvokeCancellation(this);
            TriggerComplete(new OperationCanceledException(Properties.Exceptions.RpcTaskCancelled));
            Interlocked.CompareExchange(ref _token, null, reg);
            return;
        }

        reg.Registration = token.Register(reg.RegistrationMethod);

        if (token.IsCancellationRequested)
        {
            router.InvokeCancellation(this);
            TriggerComplete(new OperationCanceledException(Properties.Exceptions.RpcTaskCancelled));
            Interlocked.CompareExchange(ref _token, null, reg);
            reg.Registration.Dispose();
            return;
        }

        if (ReferenceEquals(_token, reg))
            return;

        try
        {
            reg.Registration.Dispose();
        }
        catch
        {
            // ignored
        }
    }
    private class TokenRegistration
    {
        public CancellationToken Token;
        public CancellationTokenRegistration Registration;
        public IRpcRouter Router;
        public RpcTask Task;

        public void RegistrationMethod()
        {
            Registration.Dispose();
            Interlocked.CompareExchange(ref Task._token, null, this);
            Router.InvokeCancellation(Task);
            Task.TriggerComplete(new OperationCanceledException(Properties.Exceptions.RpcTaskCancelled));
        }
    }

    public Exception? GetException()
    {
        if (Exceptions == null)
        {
            Exception? x = Exception;
            if (!IgnoreNoConnectionsIntl || (x is not RpcNoConnectionsException && (ConnectionIntl is not { IsClosed: true } || x is not RpcTimeoutException)))
                return x;

            return null;
        }

        Exception[] newExceptions = IgnoreNoConnectionsIntl
            ? Exceptions.Where(x => x is not RpcNoConnectionsException && (ConnectionIntl is not { IsClosed: true } || x is not RpcTimeoutException)).ToArray()
            : Exceptions.ToArray();

        return newExceptions.Length switch
        {
            0 => null,
            1 => newExceptions[0],
            _ => new AggregateException(newExceptions)
        };
    }

    protected internal virtual bool TrySetResult(object? value)
    {
        return false;
    }
    internal void TriggerComplete(Exception? exception)
    {
        try
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
        finally
        {
            if (Awaiter.IsCompleted)
            {
                DisposeCancellation();
            }
        }
    }

    public static RpcTask<T> FromResult<T>(T value) => new RpcTask<T>(value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void DisposeCancellation()
    {
        TokenRegistration? tkn = Interlocked.Exchange(ref _token, null);
        if (tkn == null)
            return;

        tkn.Registration.Dispose();
        if (tkn.Token.IsCancellationRequested)
        {
            TriggerComplete(new OperationCanceledException(Properties.Exceptions.RpcTaskCancelled));
        }
    }
}