using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Reflection;

#nullable disable
/// <summary>
/// Used by internal code, stores the implementations of various interfaces used by the given proxy type.
/// </summary>
/// <remarks>Not recommended to be used by outside code.</remarks>
[UsedImplicitly]
public struct ProxyContext
{
    [UsedImplicitly]
    public IRpcSerializer DefaultSerializer;

    [UsedImplicitly]
    public IRpcRouter Router;

    [UsedImplicitly]
    public ProxyGenerator Generator;
}