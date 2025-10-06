using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Threading;

namespace DanielWillett.ModularRpcs;

public static class ConnectionExtensions
{
    /// <summary>
    /// Pings a remote <paramref name="connection"/> and waits for a response.
    /// </summary>
    /// <param name="connection">The connection to ping.</param>
    /// <param name="token">A token that cancels the ping request.</param>
    /// <param name="timeout">The maximum time to wait before throwing a <see cref="RpcTimeoutException"/>.</param>
    /// <exception cref="RpcTimeoutException">Thrown when the ping doesn't succeed.</exception>
    public static RpcTask<TimeSpan> PingAsync(this IModularRpcRemoteConnection connection, TimeSpan timeout = default, CancellationToken token = default)
    {
        return connection.Local.Router.PingAsync(connection, timeout, token);
    }
}
