using JetBrains.Annotations;
using System;
using System.ComponentModel;

namespace DanielWillett.ModularRpcs.Routing;

[Flags, UsedImplicitly]
public enum RpcInvokeOptions
{
    Default,

    /// <summary>
    /// Indicates that this method is being invoked by a source-generated send method.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    Generated = 1,

    /// <summary>
    /// Skips all connections advertising themselves as loopback connections.
    /// </summary>
    [UsedImplicitly]
    SkipLoopback = 2
}
