using DanielWillett.ModularRpcs.Abstractions;

namespace DanielWillett.ModularRpcs.Data;
public interface IContiguousBufferProgressUpdateDispatcher
{
    /// <summary>
    /// Invoked when a large download has a buffer progress update. Used to track large downloads.
    /// </summary>
    /// <remarks>Not all implementations of <see cref="IModularRpcConnection"/> will support this.</remarks>
    public event ContiguousBufferProgressUpdate BufferProgressUpdated;
}