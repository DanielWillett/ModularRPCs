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
    /// Send data in the form of raw binary data to the remote end.
    /// </summary>
    ValueTask SendDataAsync(ReadOnlySpan<byte> rawData, CancellationToken token);

    /// <summary>
    /// Send data in the form of a stream to the remote end.
    /// </summary>
    ValueTask SendDataAsync(Stream streamData, CancellationToken token);
}