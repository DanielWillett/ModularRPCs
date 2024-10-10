using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Async;

/// <summary>
/// Extension methods for <see cref="RpcTask"/> and it's deriving classes.
/// </summary>
public static class RpcTaskExtensions
{
    /// <summary>
    /// Try to invoke an RPC, returning the exception if it exists.
    /// </summary>
    /// <returns><see langword="null"/> if the RPC succeeded, otherwise the exception thrown.</returns>
    public static async Task<Exception?> TryInvoke(RpcTask rpcTask)
    {
        if (rpcTask.IsCompleted)
        {
            return rpcTask.GetException();
        }

        try
        {
            await rpcTask;
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Try to invoke an RPC, returning the exception if it exists.
    /// </summary>
    /// <returns>The result and <see langword="null"/> if the RPC succeeded, otherwise <see langword="default"/> and the exception thrown.</returns>
    public static async Task<(T? Result, Exception? Exception)> TryInvoke<T>(RpcTask<T> rpcTask)
    {
        if (rpcTask.IsCompleted)
        {
            Exception? ex = rpcTask.GetException();
            return (ex == null ? rpcTask.ResultIntl : default, ex);
        }

        try
        {
            return (await rpcTask, null);
        }
        catch (Exception ex)
        {
            return (default, ex);
        }
    }

    /// <summary>
    /// Configure this <see cref="RpcTask"/> to not throw an error if no connection is available to send the RPC to. It will instead be ignored.
    /// </summary>
    /// <remarks>To check if a missing connection was ignored, one can use <see cref="FailedDueToNoConnections"/>.</remarks>
    /// <returns>The same <paramref name="task"/> as this method was originally called on.</returns>
    public static T IgnoreNoConnections<T>(this T task) where T : RpcTask
    {
        task.IgnoreNoConnectionsIntl = true;
        return task;
    }

    /// <summary>
    /// Check if this <see cref="RpcTask"/> failed because a connection wasn't found.
    /// </summary>
    /// <remarks>To ignore these errors, use <see cref="IgnoreNoConnections{T}"/>.</remarks>
    /// <returns>The same <paramref name="task"/> as this method was originally called on.</returns>
    public static bool FailedDueToNoConnections(this RpcTask task)
    {
        if (task.Exceptions == null)
            return task.Exception is RpcNoConnectionsException;

        return task.Exceptions.Any(x => x is RpcNoConnectionsException);
    }

    /// <summary>
    /// Add a <see cref="CancellationToken"/> to a task that will throw an <see cref="OperationCanceledException"/> when the token is canceled.
    /// </summary>
    /// <remarks>If the task is a fire-and-forget task, nothing will happen.</remarks>
    public static RpcTask WithToken(this RpcTask task, CancellationToken token)
    {
        if (task is { IsCompleted: false, ConnectionIntl: { } connection })
            task.SetToken(token, connection.Local.Router);

        return task;
    }
    internal static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other)
    {
        if (token.CanBeCanceled)
        {
            if (!other.CanBeCanceled)
                return new CombinedTokenSources(token, null);

            if (token == other)
                return new CombinedTokenSources(token, null);

            CancellationTokenSource src = CancellationTokenSource.CreateLinkedTokenSource(token, other);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }

        if (!other.CanBeCanceled)
            return default;

        token = other;
        return new CombinedTokenSources(other, null);
    }
}
internal readonly struct CombinedTokenSources : IDisposable
{
    private readonly CancellationTokenSource? _tknSrc;
    public readonly CancellationToken Token;
    internal CombinedTokenSources(CancellationToken token, CancellationTokenSource? tknSrc)
    {
        Token = token;
        _tknSrc = tknSrc;
    }
    public void Dispose()
    {
        _tknSrc?.Dispose();
    }
}