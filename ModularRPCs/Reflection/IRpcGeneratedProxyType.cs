using DanielWillett.ModularRpcs.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DanielWillett.ModularRpcs.Reflection;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IRpcGeneratedProxyType
{
    void SetupGeneratedProxyInfo(in GeneratedProxyTypeInfo info);
#if NET7_0_OR_GREATER
    static abstract void __ModularRpcsGeneratedSetupStaticGeneratedProxy(GeneratedProxyTypeBuilder state);
#endif
}

[EditorBrowsable(EditorBrowsableState.Advanced)]
public readonly struct GeneratedProxyTypeInfo
{
    public IRpcRouter Router { get; }
    public ProxyGenerator Generator { get; }
    public GeneratedProxyTypeInfo(IRpcRouter router, ProxyGenerator generator)
    {
        Router = router;
        Generator = generator;
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