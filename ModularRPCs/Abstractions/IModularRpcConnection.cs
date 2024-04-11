using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Represents a connection with another client or server.
/// </summary>
public interface IModularRpcConnection
{
    /// <summary>
    /// Terminate the connection if it's open.
    /// </summary>
    ValueTask CloseAsync(CancellationToken token = default);

    /// <summary>
    /// Is the current connection closed/disposed.
    /// </summary>
    bool IsClosed { get; }
}