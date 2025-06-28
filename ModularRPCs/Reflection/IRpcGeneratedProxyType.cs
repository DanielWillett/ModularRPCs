using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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

[EditorBrowsable(EditorBrowsableState.Advanced), UsedImplicitly]
public class GeneratedProxyTypeBuilder
{
    private readonly IDictionary<RuntimeMethodHandle, Delegate?> _callInfoGetters;
    private readonly IDictionary<RuntimeMethodHandle, Delegate> _invokeStreamMethods;
    private readonly IDictionary<RuntimeMethodHandle, Delegate> _invokeBytesMethods;
    internal IDictionary<RuntimeMethodHandle, int>? MethodSignatures;

    public GeneratedProxyTypeBuilder(IDictionary<RuntimeMethodHandle, Delegate?> callInfoGetters, IDictionary<RuntimeMethodHandle, Delegate> invokeStreamMethods, IDictionary<RuntimeMethodHandle, Delegate> invokeBytesMethods)
    {
        _callInfoGetters = callInfoGetters;
        _invokeStreamMethods = invokeStreamMethods;
        _invokeBytesMethods = invokeBytesMethods;
    }


    [UsedImplicitly]
    public void AddMethodSignatureHash(RuntimeMethodHandle handle, int signature)
    {
        if (MethodSignatures == null)
            throw new InvalidOperationException();
        MethodSignatures.Add(handle, signature);
    }

    [UsedImplicitly]
    public void AddCallGetter(SourceGenerationServices.GetCallInfo getCallInfo)
    {
        if (getCallInfo == null)
            throw new ArgumentNullException(nameof(getCallInfo));
        _callInfoGetters.Add(getCallInfo().MethodHandle, getCallInfo);
    }

    [UsedImplicitly]
    public void AddCallGetter(SourceGenerationServices.GetCallInfoByVal getCallInfo)
    {
        if (getCallInfo == null)
            throw new ArgumentNullException(nameof(getCallInfo));
        _callInfoGetters.Add(getCallInfo().MethodHandle, getCallInfo);
    }

    [UsedImplicitly]
    public void AddReceiveMethod(RuntimeMethodHandle handle, ReceiveMethodInvokerType type, ProxyGenerator.RpcInvokeHandlerBytes invoker)
    {
        if (invoker == null)
            throw new ArgumentNullException(nameof(invoker));
        if (type != ReceiveMethodInvokerType.Bytes)
            throw new ArgumentOutOfRangeException(nameof(type));

        _invokeBytesMethods.Add(handle, invoker);
    }

    [UsedImplicitly]
    public void AddReceiveMethod(RuntimeMethodHandle handle, ReceiveMethodInvokerType type, ProxyGenerator.RpcInvokeHandlerRawBytes invoker)
    {
        if (invoker == null)
            throw new ArgumentNullException(nameof(invoker));
        if (type != ReceiveMethodInvokerType.BytesRaw)
            throw new ArgumentOutOfRangeException(nameof(type));

        _invokeBytesMethods.Add(handle, invoker);
    }

    [UsedImplicitly]
    public void AddReceiveMethod(RuntimeMethodHandle handle, ReceiveMethodInvokerType type, ProxyGenerator.RpcInvokeHandlerStream invoker)
    {
        if (invoker == null)
            throw new ArgumentNullException(nameof(invoker));
        if (type is not ReceiveMethodInvokerType.Stream and not ReceiveMethodInvokerType.StreamRaw)
            throw new ArgumentOutOfRangeException(nameof(type));

        _invokeStreamMethods.Add(handle, invoker);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced), UsedImplicitly]
    public enum ReceiveMethodInvokerType
    {
        [UsedImplicitly] Stream,
        [UsedImplicitly] Bytes,
        [UsedImplicitly] StreamRaw,
        [UsedImplicitly] BytesRaw
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