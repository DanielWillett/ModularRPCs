using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;
public class ClientRpcConnectionLifetime : IRpcConnectionLifetime, IRefSafeLoggable
{
    private readonly object _sync = new object();
    private IModularRpcRemoteConnection? _remoteConnection;
    private object? _logger;
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

    /// <inheritdoc />
    public bool IsSingleConnection => true;

    /// <summary>
    /// Exchange the current connection for a new one.
    /// </summary>
    /// <returns>The old connection.</returns>
    public IModularRpcRemoteConnection? ExchangeConnection(IModularRpcRemoteConnection newConnection)
    {
        lock (_sync)
        {
            return Interlocked.Exchange(ref _remoteConnection, newConnection);
        }
    }

    /// <inheritdoc />
    public int ForEachRemoteConnection(ForEachRemoteConnectionWhile callback, bool workOnCopy = false)
    {
        lock (_sync)
        {
            IModularRpcRemoteConnection? c = _remoteConnection;
            if (c == null)
                return 0;

            callback(c);
            return 1;
        }
    }
    public async ValueTask<bool> TryAddNewConnection(IModularRpcRemoteConnection connection, CancellationToken token = default)
    {
        IModularRpcRemoteConnection? existing;
        lock (_sync)
        {
            existing = Interlocked.Exchange(ref _remoteConnection, connection);
        }

        if (existing == null || ReferenceEquals(existing, connection))
            return true;

        try
        {
            await existing.CloseAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            this.LogWarning(ex, "Failed to close old connection.");
        }
        finally
        {
            try
            {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
                if (existing is IAsyncDisposable aDisp)
                    await aDisp.DisposeAsync().ConfigureAwait(false);
                else
#endif
                if (existing is IDisposable disp)
                    disp.Dispose();
            }
            catch (Exception ex)
            {
                this.LogWarning(ex, "Failed to dispose old connection.");
            }
        }

        return true;
    }

    public async ValueTask<bool> TryRemoveConnection(IModularRpcRemoteConnection connection, CancellationToken token = default)
    {
        IModularRpcRemoteConnection? existing;
        lock (_sync)
        {
            existing = Interlocked.CompareExchange(ref _remoteConnection, null, connection);
        }

        if (!ReferenceEquals(existing, connection))
            return false;

        try
        {
            await existing.CloseAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            this.LogWarning(ex, "Failed to close removed connection.");
        }
        finally
        {
            try
            {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
                if (existing is IAsyncDisposable aDisp)
                    await aDisp.DisposeAsync().ConfigureAwait(false);
                else
#endif
                if (existing is IDisposable disp)
                    disp.Dispose();
            }
            catch (Exception ex)
            {
                this.LogWarning(ex, "Failed to dispose removed connection.");
            }
        }

        return true;
    }
}
