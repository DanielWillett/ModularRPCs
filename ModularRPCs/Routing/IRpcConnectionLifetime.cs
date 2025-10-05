using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;

/// <summary>
/// Keeps track of active <see cref="IModularRpcConnection"/> objects.
/// </summary>
/// <remarks>Default implementations: <see cref="ClientRpcConnectionLifetime"/> and <see cref="ServerRpcConnectionLifetime"/>.</remarks>
public interface IRpcConnectionLifetime : IDisposable
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    , IAsyncDisposable
#endif
{
    /// <summary>
    /// Does this lifetime only support one connection (like a client)?
    /// </summary>
    bool IsSingleConnection { get; }

    /// <summary>
    /// Invoked after a connection is added.
    /// </summary>
    event Action<IRpcConnectionLifetime, IModularRpcRemoteConnection>? ConnectionAdded;

    /// <summary>
    /// Invoked after a connection is removed.
    /// </summary>
    event Action<IRpcConnectionLifetime, IModularRpcRemoteConnection>? ConnectionRemoved;

    /// <summary>
    /// Execute a callback for each remote connection, returning <see langword="false"/> to break.
    /// </summary>
    /// <param name="workOnCopy">Works on a copy of the list where applicable so connections can be terminated from within the callback.</param>
    /// <returns>The number of total connections (including connections after breaking).</returns>
    int ForEachRemoteConnection(ForEachRemoteConnectionWhile callback, bool workOnCopy = false, bool openOnly = true);

    /// <summary>
    /// Attempts to add a new connection.
    /// </summary>
    ValueTask<bool> TryAddNewConnection(IModularRpcRemoteConnection connection, CancellationToken token = default);

    /// <summary>
    /// Attempts to remove an existing connection.
    /// </summary>
    ValueTask<bool> TryRemoveConnection(IModularRpcRemoteConnection connection, CancellationToken token = default);

    /// <summary>
    /// Attempts to remove all existing connections.
    /// </summary>
    /// <returns>The number of connections that were present and have now been removed.</returns>
    ValueTask<int> TryRemoveAllConnections(CancellationToken token = default);
}

/// <summary>
/// Execute a callback for each remote connection, returning <see langword="false"/> to break.
/// </summary>
public delegate bool ForEachRemoteConnectionWhile(IModularRpcRemoteConnection connection);

/// <summary>
/// Allows an <see cref="IRpcConnectionLifetime"/> object to provide a custom implementation for <see cref="RpcConnectionLifetimeExtensions.GetLoopbackCount"/>.
/// </summary>
public interface IRpcConnectionLifetimeWithOnlyLoopbackCheck : IRpcConnectionLifetime
{
    /// <summary>
    /// Checks to see how many connections are advertised as loopback connections and if that's all the connections.
    /// </summary>
    /// <remarks>User code should not call this method, but instead should use <see cref="RpcConnectionLifetimeExtensions.GetLoopbackCount"/>.</remarks>
    /// <param name="areAllLoopbacks"><see langword="true"/> if there is at least one connection and all connections are loopback, otherwise <see langword="false"/>.</param>
    /// <returns>The number of loopback connections.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    int GetLoopbackCount(out bool areAllLoopbacks);
}

/// <summary>
/// Extension methods for <see cref="IRpcConnectionLifetime"/>.
/// </summary>
public static class RpcConnectionLifetimeExtensions
{
    /// <summary>
    /// Checks to see how many connections are advertised as loopback connections and if that's all the connections.
    /// </summary>
    /// <param name="lifetime">The connection lifetime to look for connections on.</param>
    /// <param name="areAllLoopbacks"><see langword="true"/> if there is at least one connection and all connections are loopback, otherwise <see langword="false"/>.</param>
    /// <returns>The number of loopback connections.</returns>
    public static int GetLoopbackCount(this IRpcConnectionLifetime lifetime, out bool areAllLoopbacks)
    {
        if (lifetime is IRpcConnectionLifetimeWithOnlyLoopbackCheck l)
            return l.GetLoopbackCount(out areAllLoopbacks);

        int loopbackCount = 0;
        bool all = true;
        lifetime.ForEachRemoteConnection(c =>
        {
            if (c.IsLoopback)
                ++loopbackCount;
            else
                all = false;
            return true;
        });

        if (loopbackCount == 0)
            all = false;
        areAllLoopbacks = all;
        return loopbackCount;
    }

    /// <summary>
    /// Finds the first connection that matches a predicate.
    /// </summary>
    /// <returns>The found connection, or <see langword="null"/> if none are found.</returns>
    public static IModularRpcRemoteConnection? Find(this IRpcConnectionLifetime lifetime, Func<IModularRpcRemoteConnection, bool> predicate, bool openOnly = true)
    {
        IModularRpcRemoteConnection? connection = null;
        lifetime.ForEachRemoteConnection(c =>
        {
            if (!predicate(c))
                return true;

            connection = c;
            return false;

        }, openOnly: openOnly);

        return connection;
    }
}