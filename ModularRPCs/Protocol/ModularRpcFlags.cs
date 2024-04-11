using System;

namespace DanielWillett.ModularRpcs.Protocol;

[Flags]
public enum ModularRpcFlags : ushort
{
    None = 0,
    HasFullEndpoint = 1 << 0
}
