using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;
public interface IRpcConnectionLifetime : IDisposable
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    , IAsyncDisposable
#endif
{
    /// <summary>
    /// Does this lifetime only support one connection (like a client)?
    /// </summary>
    bool IsSingleConnection { get; }

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
}

/// <summary>
/// Execute a callback for each remote connection, returning <see langword="false"/> to break.
/// </summary>
public delegate bool ForEachRemoteConnectionWhile(IModularRpcRemoteConnection connection);