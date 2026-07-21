using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Connections;

/// <summary>
/// Listens for incoming data on an <see cref="IRpcConnection"/>.
/// </summary>
public interface ITransmitter
{
    /// <summary>
    /// Send data in the form of raw binary data to the remote end. The <paramref name="rawData"/> MUST BE COPIED if this method switches contexts.
    /// </summary>
    /// <param name="rawData">Raw binary data to be sent.</param>
    /// <param name="progressReport">Optional progress report sink reporting the total number of bytes sent, including the header.</param>
    /// <param name="token">Cancellation token for the send operation.</param>
    /// <returns>A task that completes when the data has been fully sent (not necessarily received yet).</returns>
    ValueTask SendDataAsync(ReadOnlySpan<byte> rawData, IProgress<long>? progressReport = null, CancellationToken token = default);

    /// <summary>
    /// Send data in the form of raw binary data to the remote end. The <paramref name="rawData"/> MUST BE COPIED if this method switches contexts if <paramref name="canTakeOwnership"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="rawData">Raw binary data to be sent.</param>
    /// <param name="progressReport">Optional progress report sink reporting the total number of bytes sent, including the header.</param>
    /// <param name="canTakeOwnership">Whether or not this method can assume that <paramref name="rawData"/> will not change during the send operation. If this is <see langword="false"/>, the data will be copied to another buffer before returning the task.</param>
    /// <param name="token">Cancellation token for the send operation.</param>
    /// <returns>A task that completes when the data has been fully sent (not necessarily received yet).</returns>
    ValueTask SendDataAsync(ReadOnlyMemory<byte> rawData, bool canTakeOwnership, IProgress<long>? progressReport = null, CancellationToken token = default);

    /// <summary>
    /// Send data in the form of a stream to the remote end.
    /// </summary>
    /// <param name="streamData">Raw stream data to be sent. Will read to the end of the stream.</param>
    /// <param name="progressReport">Optional progress report sink reporting the total number of bytes sent, including the header.</param>
    /// <param name="token">Cancellation token for the send operation.</param>
    /// <returns>A task that completes when the data has been fully sent (not necessarily received yet).</returns>
    ValueTask SendDataAsync(Stream streamData, IProgress<long>? progressReport = null, CancellationToken token = default);
}