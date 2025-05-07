using DanielWillett.ModularRpcs.Routing;
using System.ComponentModel;

namespace DanielWillett.ModularRpcs.Reflection;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IRpcGeneratedProxyType
{
    void SetupGeneratedProxyInfo(GeneratedProxyTypeInfo info);
}

[EditorBrowsable(EditorBrowsableState.Advanced)]
public readonly struct GeneratedProxyTypeInfo
{
    public IRpcRouter Router { get; }
    public GeneratedProxyTypeInfo(IRpcRouter router)
    {
        Router = router;
    }
}