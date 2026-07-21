using DanielWillett.ModularRpcs.Protocol;
using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Connections;

/// <summary>
/// Listens for incoming data on an <see cref="IRpcConnection"/>.
/// </summary>
public interface IListener
{
    /// <summary>
    /// The current state of this <see cref="IListener"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    ListenerState State { get; set; }

    /// <summary>
    /// Adds a <see cref="IProgress{T}"/> implementation to listen for progress report updates.
    /// </summary>
    /// <param name="progressSink">Object to report progress to.</param>
    /// <returns>An object that, when disposed, will remove the listener.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="NotSupportedException">This listener doesn't support progress reports.</exception>
    [MustUseReturnValue("Dispose the return value to unsubscribe the progress sink.")]
    IDisposable ReportProgress(IProgress<DownloadProgressReport> progressSink);
}

/// <summary>
/// Represents the state of an <see cref="IListener"/> implementation.
/// </summary>
public enum ListenerState
{
    /// <summary>
    /// Not currently listening for incoming data.
    /// </summary>
    NotListening,

    /// <summary>
    /// Currently listening for incoming data.
    /// </summary>
    Listening
}

/// <summary>
/// Progress reported to <see cref="IProgress{T}"/> when downloading a message.
/// Note that this object may be re-used multiple times within the same message.
/// </summary>
public sealed class DownloadProgressReport
{
    private readonly PrimitiveRpcOverhead _overhead;

    /// <summary>
    /// Number of bytes sent.
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// Total size of message.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Whether or not this is the last progress report to be reported for this message.
    /// </summary>
    public bool IsCompleted => BytesSent == TotalBytes;

    /// <summary>
    /// Progress (0-1) of how much of the message has been downloaded.
    /// </summary>
    public double Progress => (double)BytesSent / TotalBytes;

    /// <summary>
    /// Basic header of the message being sent.
    /// </summary>
    public ref readonly PrimitiveRpcOverhead Overhead => ref _overhead;

    /// <summary>
    /// All data that has been downloaded so far, excluding the header.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; set; }

    /// <summary>
    /// Creates a new progress report for a new download.
    /// </summary>
    /// <param name="totalBytes">Total size of the download including headers.</param>
    /// <param name="overhead">The overhead at the beginning of the message.</param>
    /// <param name="startingData">First section of data.</param>
    public DownloadProgressReport(long totalBytes, PrimitiveRpcOverhead overhead, ReadOnlyMemory<byte> startingData)
    {
        TotalBytes = totalBytes;
        _overhead = overhead;
        Data = startingData;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{_overhead}: {BytesSent:N0}/{TotalBytes:N0} ({Progress:P1})";
    }
}