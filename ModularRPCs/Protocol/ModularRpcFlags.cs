using System;

namespace DanielWillett.ModularRpcs.Protocol;

[Flags]
public enum ModularRpcFlags : ushort
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
    /// Number of arguments goes above 255.
    /// </summary>
    ArgCt16 = 1 << 2
}
