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
    /// If this connection exhibits a loopback behavior.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    /// <remarks>Setting this to <see langword="true"/> may result in RPCs skipping the serialization/deserialization step and calling the receive method directly.</remarks>
    bool IsLoopback { get; }

    /// <summary>
    /// The local side of this connection.
    /// </summary>
    IModularRpcLocalConnection Local { get; }

    /// <summary>
    /// The endpoint this remote connection represents.
    /// </summary>
    IModularRpcRemoteEndpoint Endpoint { get; }

    /// <summary>
    /// Send data in the form of raw binary data to the remote end. The <paramref name="rawData"/> MUST BE COPIED if this method switches contexts.
    /// </summary>
    ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token);

    /// <summary>
    /// Send data in the form of raw binary data to the remote end. The <paramref name="rawData"/> MUST BE COPIED if this method switches contexts if <paramref name="canTakeOwnership"/> is <see langword="false"/>.
    /// </summary>
    ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token);

    /// <summary>
    /// Send data in the form of a stream to the remote end.
    /// </summary>
    ValueTask SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token);
}