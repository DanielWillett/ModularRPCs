using System;

namespace DanielWillett.ModularRpcs.Protocol;

[Flags]
public enum RpcFlags : ushort
{
    None = 0,

    /// <summary>
    /// The entire endpoint is declared instead of just the known ID.
    /// </summary>
    HasFullEndpoint = 1 << 0,

    /// <summary>
    /// The endpoint is declared using the known ID but also has an identifier.
    /// </summary>
    EndpointCodeIncludesIdentifier = 1 << 1,

    /// <summary>
    /// There is no listener waiting for a response from this RPC.
    /// </summary>
    FireAndForget = 1 << 2,
}
