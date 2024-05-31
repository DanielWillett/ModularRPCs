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
    /// <remarks>This memory MUST BE COPIED if this method switches contexts and <paramref name="canTakeOwnership"/> is <see langword="false"/>.</remarks>
    /// <param name="canTakeOwnership">If the backing storage for <paramref name="rawData"/> is safe to use outside the current stack frame. If this is <see langword="false"/>, data should be copied before context switching.</param>
    ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, bool canTakeOwnership, CancellationToken token);

    /// <summary>
    /// Send data in the form of a stream to the remote end.
    /// </summary>
    ValueTask SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token);
}