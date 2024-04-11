using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Represents the remote side of a connection on the local domain.
/// </summary>
public interface IModularRpcRemoteConnection : IModularRpcConnection
{
    /// <summary>
    /// The local side of this connection.
    /// </summary>
    IModularRpcLocalConnection Local { get; }

    /// <summary>
    /// The endpoint this remote connection represents.
    /// </summary>
    IModularRpcRemoteEndPoint EndPoint { get; }

    /// <summary>
    /// Called by the <see cref="IModularRpcRemoteConnection"/> when it receives data.
    /// </summary>
    /// <remarks>Will either supply a stream or raw data.</remarks>
    /// <exception cref="InvalidOperationException">Didn't pass raw binary data or a valid stream.</exception>
    ValueTask SendDataAsync(Stream? streamData, ArraySegment<byte> rawData, CancellationToken token = default);
}
