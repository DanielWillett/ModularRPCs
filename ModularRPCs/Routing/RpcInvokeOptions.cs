using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Routing;

[Flags, UsedImplicitly]
public enum RpcInvokeOptions
{
    Default,

    /// <summary>
    /// Skips all connections advertising themselves as loopback connections.
    /// </summary>
    [UsedImplicitly]
    SkipLoopback
}
