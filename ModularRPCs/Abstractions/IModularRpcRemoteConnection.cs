using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

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
    IModularRpcRemoteEndpoint Endpoint { get; }

    /// <summary>
    /// Send data in the form of raw binary data to the remote end.
    /// </summary>
    ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token);

    /// <summary>
    /// Send data in the form of a stream to the remote end.
    /// </summary>
    ValueTask SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token);
}