using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;
public class ServerRpcConnectionLifetime : IRpcConnectionLifetime, IRefSafeLoggable
{
    private readonly List<IModularRpcRemoteConnection> _connections = new List<IModularRpcRemoteConnection>();
    public bool IsSingleConnection => false;
    private object? _logger;
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
    public int ForEachRemoteConnection(ForEachRemoteConnectionWhile callback, bool workOnCopy = false)
    {
        IModularRpcRemoteConnection[]? copy;
        int i = 0;
        lock (_connections)
        {
            if (workOnCopy)
                copy = _connections.ToArray();
            else
            {
                for (; i < _connections.Count; ++i)
                {
                    bool result = callback(_connections[i]);
                    if (!result)
                        return _connections.Count;
                }

                return i;
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
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
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
        }

        return true;
    }
}
