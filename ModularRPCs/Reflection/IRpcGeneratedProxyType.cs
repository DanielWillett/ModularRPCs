using DanielWillett.ModularRpcs.Routing;
using System;
using System.Collections.Generic;
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

[EditorBrowsable(EditorBrowsableState.Advanced)]
public class GeneratedProxyTypeBuilder
{
    public IDictionary<RuntimeMethodHandle, Delegate?> CallInfoGetters { get; }

    public GeneratedProxyTypeBuilder(IDictionary<RuntimeMethodHandle, Delegate?> callInfoGetters)
    {
        CallInfoGetters = callInfoGetters;
    }

    public void AddCallGetter(SourceGenerationServices.GetCallInfo getCallInfo)
    {
        CallInfoGetters.Add(getCallInfo().MethodHandle, getCallInfo);
    }
    public void AddCallGetter(SourceGenerationServices.GetCallInfoByVal getCallInfo)
    {
        CallInfoGetters.Add(getCallInfo().MethodHandle, getCallInfo);
    }
}