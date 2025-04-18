﻿using DanielWillett.ModularRpcs.Abstractions;
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

    /// <inheritdoc />
    public event Action<IRpcConnectionLifetime, IModularRpcRemoteConnection>? ConnectionAdded;

    /// <inheritdoc />
    public event Action<IRpcConnectionLifetime, IModularRpcRemoteConnection>? ConnectionRemoved;

    /// <summary>
    /// Exchange the current connection for a new one.
    /// </summary>
    /// <remarks>This does not close the old connection.</remarks>
    /// <returns>The old connection.</returns>
    public IModularRpcRemoteConnection? ExchangeConnection(IModularRpcRemoteConnection newConnection)
    {
        lock (_sync)
        {
            IModularRpcRemoteConnection? old = Interlocked.Exchange(ref _remoteConnection, newConnection);

            if (!ReferenceEquals(old, newConnection))
            {
                if (old != null)
                    InvokeRemove(old);

                InvokeAdd(newConnection);
            }

            return old;
        }
    }

    /// <inheritdoc />
    public int ForEachRemoteConnection(ForEachRemoteConnectionWhile callback, bool workOnCopy = false, bool openOnly = true)
    {
        lock (_sync)
        {
            IModularRpcRemoteConnection? c = _remoteConnection;
            if (c == null || c.IsClosed)
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
        {
            if (!ReferenceEquals(existing, connection))
                InvokeAdd(connection);
            return true;
        }

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
            
            InvokeRemove(existing);
            InvokeAdd(connection);
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

            InvokeRemove(existing);
        }

        return true;
    }
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    public async ValueTask DisposeAsync()
    {
        IModularRpcRemoteConnection? existing;
        lock (_sync)
        {
            existing = Interlocked.Exchange(ref _remoteConnection, null);
        }

        if (existing == null)
            return;

        try
        {
            await existing.CloseAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            this.LogWarning(ex, "Failed to close connection.");
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

            InvokeRemove(existing);
        }
    }
#endif
    public void Dispose()
    {
        IModularRpcRemoteConnection? existing;
        lock (_sync)
        {
            existing = Interlocked.Exchange(ref _remoteConnection, null);
        }

        if (existing == null)
            return;

        try
        {
            existing.CloseAsync().AsTask().Wait();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            this.LogWarning(ex, "Failed to close connection.");
        }
        finally
        {
            try
            {
                if (existing is IDisposable disp)
                    disp.Dispose();
            }
            catch (Exception ex)
            {
                this.LogWarning(ex, "Failed to dispose removed connection.");
            }

            InvokeRemove(existing);
        }
    }

    private void InvokeRemove(IModularRpcRemoteConnection remote)
    {
        try
        {
            ConnectionRemoved?.Invoke(this, remote);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error invoking ConnectionRemoved event handler from ClientRpcConnectionLifetime.");
        }
    }

    private void InvokeAdd(IModularRpcRemoteConnection remote)
    {
        try
        {
            ConnectionAdded?.Invoke(this, remote);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error invoking ConnectionAdded event handler from ClientRpcConnectionLifetime.");
        }
    }
}
