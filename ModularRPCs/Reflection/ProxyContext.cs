using System.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Reflection;

#nullable disable
/// <summary>
/// Used by internal code, stores the implementations of various interfaces used by the given proxy type.
/// </summary>
/// <remarks>Not recommended to be used by outside code.</remarks>
public struct ProxyContext
{
    public IRpcSerializer DefaultSerializer;
    public IRpcRouter Router;

    internal static FieldInfo SerializerField = typeof(ProxyContext).GetField(nameof(DefaultSerializer), BindingFlags.Public | BindingFlags.Instance)!;
    internal static FieldInfo RouterField = typeof(ProxyContext).GetField(nameof(Router), BindingFlags.Public | BindingFlags.Instance)!;
}
