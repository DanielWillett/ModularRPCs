using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel;

namespace DanielWillett.ModularRpcs.Reflection;

[EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
public class RpcClassRegistrationBuilder
{
    internal List<RpcEndpointTarget> Methods { get; } = new List<RpcEndpointTarget>();

    [UsedImplicitly]
    public RpcClassRegistrationBuilder AddMethod(RpcEndpointTarget target)
    {
        Methods.Add(target);
        return this;
    }
}