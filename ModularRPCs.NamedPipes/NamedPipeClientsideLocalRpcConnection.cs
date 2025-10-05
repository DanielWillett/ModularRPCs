using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.IO.Pipes;
using System.Threading;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// The local side of a named pipe client-side connection using a <see cref="NamedPipeClientStream"/>.
/// </summary>
public sealed class NamedPipeClientsideLocalRpcConnection : NamedPipeLocalRpcConnection<NamedPipeClientsideLocalRpcConnection, NamedPipeClientStream>
{
    internal NamedPipeClientsideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, NamedPipeClientsideRemoteRpcConnection remote, CancellationTokenSource cts)
        : base(router, serializer, remote, cts) { }

    /// <inheritdoc />
    private protected override void TryStartAutoReconnecting()
    {
        ((NamedPipeClientsideRemoteRpcConnection)Remote).TryStartAutoReconnecting();
    }
}
