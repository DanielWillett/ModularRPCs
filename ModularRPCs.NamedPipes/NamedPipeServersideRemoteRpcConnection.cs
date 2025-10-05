using DanielWillett.ModularRpcs.Abstractions;
using System.IO.Pipes;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// The remote side of a named pipe server-side connection using a <see cref="NamedPipeServerStream"/>.
/// </summary>
public sealed class NamedPipeServersideRemoteRpcConnection
    : NamedPipeRemoteRpcConnection<NamedPipeServersideLocalRpcConnection, NamedPipeServerStream>,
        IModularRpcServersideConnection
{
    internal NamedPipeServersideRemoteRpcConnection(NamedPipeEndpoint endpoint, NamedPipeServerStream server) : base(endpoint)
    {
        PipeStream = server;
    }
}
