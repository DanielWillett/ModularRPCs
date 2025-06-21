using DanielWillett.ModularRpcs.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using DanielWillett.ModularRpcs.Serialization;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Reflection;

[EditorBrowsable(EditorBrowsableState.Advanced), UsedImplicitly]
public interface IRpcGeneratedProxyType
{
    [UsedImplicitly]
    void SetupGeneratedProxyInfo(in GeneratedProxyTypeInfo info);
#if NET7_0_OR_GREATER
    [UsedImplicitly]
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

/// <summary>
/// Used by source generator, should not be used in user code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
public unsafe struct GeneratedSendMethodState
{
    [UsedImplicitly] public byte* Buffer;
    [UsedImplicitly] public uint Size;
    [UsedImplicitly] public uint OverheadSize;
    [UsedImplicitly] public uint PreOverheadSize;
    [UsedImplicitly] public bool HasKnownTypeId;
    [UsedImplicitly] public bool PreCalc;
    [UsedImplicitly] public uint KnownTypeId;
    [UsedImplicitly] public uint IdTypeSize;
    [UsedImplicitly] public uint IdSize;
    [UsedImplicitly] public IRpcSerializer Serializer;
    [UsedImplicitly] public IRpcRouter Router;
}