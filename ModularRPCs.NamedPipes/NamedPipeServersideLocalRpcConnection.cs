using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System.IO.Pipes;
using System.Threading;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// The local side of a named pipe server-side connection using a <see cref="NamedPipeServerStream"/>.
/// </summary>
public sealed class NamedPipeServersideLocalRpcConnection : NamedPipeLocalRpcConnection<NamedPipeServersideLocalRpcConnection, NamedPipeServerStream>
{
    internal NamedPipeServersideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, NamedPipeServersideRemoteRpcConnection remote, CancellationTokenSource cts)
        : base(router, serializer, remote, cts) { }

    /// <inheritdoc />
    public override string ToString() => "Named Pipes (Local, Server)";
}
