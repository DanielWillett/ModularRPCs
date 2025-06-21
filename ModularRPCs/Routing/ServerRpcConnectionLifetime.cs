using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;
public class ServerRpcConnectionLifetime : IRpcConnectionLifetimeWithOnlyLoopbackCheck, IRefSafeLoggable
{
    private readonly List<IModularRpcRemoteConnection> _connections = new List<IModularRpcRemoteConnection>();
    public bool IsSingleConnection => false;
    private object? _logger;
    private int? _loopbackCountCached;
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

    /// <inheritdoc />
    public event Action<IRpcConnectionLifetime, IModularRpcRemoteConnection>? ConnectionAdded;

    /// <inheritdoc />
    public event Action<IRpcConnectionLifetime, IModularRpcRemoteConnection>? ConnectionRemoved;

    /// <inheritdoc />
    public int GetLoopbackCount(out bool areAllLoopbacks)
    {
        lock (_connections)
        {
            if (_loopbackCountCached.HasValue)
            {
                int ct = _loopbackCountCached.Value;
                areAllLoopbacks = ct > 0 && _connections.Count == ct;
                return ct;
            }

            areAllLoopbacks = true;
            int count = 0;
            int i = 0;
            for (; i < _connections.Count; ++i)
            {
                IModularRpcRemoteConnection conn = _connections[i];
                if (conn is { IsClosed: false, IsLoopback: true })
                    ++count;
                else
                    areAllLoopbacks = false;
            }

            if (i == 0)
                areAllLoopbacks = false;
            _loopbackCountCached = count;
            return count;
        }
    }

    public int ForEachRemoteConnection(ForEachRemoteConnectionWhile callback, bool workOnCopy = false, bool openOnly = true)
    {
        IModularRpcRemoteConnection[]? copy;
        int i = 0;
        lock (_connections)
        {
            if (workOnCopy)
            {
                if (openOnly)
                {
                    int ct = _connections.Count(x => !x.IsClosed);
                    copy = new IModularRpcRemoteConnection[ct];
                    int index = -1;
                    for (; i < _connections.Count; ++i)
                    {
                        IModularRpcRemoteConnection conn = _connections[i];
                        if (conn.IsClosed)
                            continue;

                        copy[++index] = conn;
                    }
                }
                else
                    copy = _connections.ToArray();
            }
            else
            {
                int ct = 0;
                for (; i < _connections.Count; ++i)
                {
                    IModularRpcRemoteConnection conn = _connections[i];
                    if (conn.IsClosed)
                        continue;
                    ++ct;
                    bool result = callback(conn);
                    if (!result)
                        return _connections.Count(x => !openOnly || !x.IsClosed);
                }

                return ct;
            }
        }

        for (; i < copy.Length; ++i)
        {
            bool result = callback(copy[i]);
            if (!result)
                return copy.Length;
        }

        return i;
    }
    public ValueTask<bool> TryAddNewConnection(IModularRpcRemoteConnection connection, CancellationToken token = default)
    {
        lock (_connections)
        {
            for (int i = 0; i < _connections.Count; ++i)
            {
                if (_connections[i].Equals(connection))
                    return new ValueTask<bool>(false);
            }

            _connections.Add(connection);
            _loopbackCountCached = null;

            InvokeAdd(connection);
        }

        return new ValueTask<bool>(true);
    }

    public async ValueTask<bool> TryRemoveConnection(IModularRpcRemoteConnection connection, CancellationToken token = default)
    {
        IModularRpcRemoteConnection? removed = null;
        lock (_connections)
        {
            for (int i = 0; i < _connections.Count; ++i)
            {
                IModularRpcRemoteConnection conn = _connections[i];
                if (!conn.Equals(connection))
                    continue;

                removed = conn;
                _connections.RemoveAt(i);
                _loopbackCountCached = null;
                break;
            }
        }

        if (removed == null)
            return false;

        try
        {
            await removed.CloseAsync(token).ConfigureAwait(false);
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
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (removed is IAsyncDisposable aDisp)
                    await aDisp.DisposeAsync().ConfigureAwait(false);
                else
#endif
                if (removed is IDisposable disp)
                    disp.Dispose();
            }
            catch (Exception ex)
            {
                this.LogWarning(ex, "Failed to dispose removed connection.");
            }

            InvokeRemove(removed);
        }

        return true;
    }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public async ValueTask DisposeAsync()
    {
        IModularRpcRemoteConnection[] connections;
        lock (_connections)
        {
            connections = _connections.ToArray();
            _connections.Clear();
        }

        Task[] tasks = new Task[connections.Length];
        for (int i = connections.Length - 1; i >= 0; --i)
        {
            IModularRpcRemoteConnection conn = connections[i];
            tasks[i] = conn.CloseAsync().AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        for (int i = connections.Length - 1; i >= 0; --i)
        {
            InvokeRemove(connections[i]);
        }
    }
#endif
    public void Dispose()
    {
        lock (_connections)
        {
            Task[] allTasks = new Task[_connections.Count];
            for (int i = _connections.Count - 1; i >= 0; --i)
            {
                IModularRpcRemoteConnection conn = _connections[i];
                _connections.RemoveAt(i);
                _loopbackCountCached = null;

                allTasks[i] = conn.CloseAsync().AsTask();
            }

            Task.WaitAll(allTasks);

            for (int i = _connections.Count - 1; i >= 0; --i)
            {
                InvokeRemove(_connections[i]);
            }
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