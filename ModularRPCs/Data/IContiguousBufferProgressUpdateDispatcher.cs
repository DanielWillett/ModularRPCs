using DanielWillett.ModularRpcs.Abstractions;

namespace DanielWillett.ModularRpcs.Data;

/// <summary>
/// This interface will usually be added to local connections and can be used to see if a connection provider supports progress updates on single messages.
/// </summary>
public interface IContiguousBufferProgressUpdateDispatcher
{
    /// <summary>
    /// Invoked when a large download has a buffer progress update. Used to track large downloads.
    /// </summary>
    /// <remarks>Not all implementations of <see cref="IModularRpcConnection"/> will support this.</remarks>
    public event ContiguousBufferProgressUpdate BufferProgressUpdated;
}