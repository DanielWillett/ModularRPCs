using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
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
    private readonly ProxyGenerator _generator;

    private readonly IDictionary<RuntimeMethodHandle, Delegate?> _callInfoGetters;
    private readonly IDictionary<RuntimeMethodHandle, Delegate> _invokeStreamMethods;
    private readonly IDictionary<RuntimeMethodHandle, Delegate> _invokeBytesMethods;
    private readonly IDictionary<Type, Func<object, WeakReference?>> _getObjectFunctions;
    private readonly IDictionary<Type, Func<object, bool>> _releaseObjectFunctions;
    private readonly IDictionary<Type, ProxyGenerator.GetOverheadSize?> _overheadSizeFunctions;
    private readonly ConcurrentDictionary<Type, IReadOnlyList<RpcEndpointTarget>> _broadcastMethods;
    internal IDictionary<RuntimeMethodHandle, int>? MethodSignatures;

    internal GeneratedProxyTypeBuilder(ProxyGenerator generator,
        IDictionary<RuntimeMethodHandle, Delegate?> callInfoGetters,
        IDictionary<RuntimeMethodHandle, Delegate> invokeStreamMethods,
        IDictionary<RuntimeMethodHandle, Delegate> invokeBytesMethods,
        IDictionary<Type, Func<object, WeakReference?>> getObjectFunctions,
        IDictionary<Type, Func<object, bool>> releaseObjectFunctions,
        IDictionary<Type, ProxyGenerator.GetOverheadSize?> overheadSizeFunctions,
        ConcurrentDictionary<Type, IReadOnlyList<RpcEndpointTarget>> broadcastMethods)
    {
        _generator = generator;
        _callInfoGetters = callInfoGetters;
        _invokeStreamMethods = invokeStreamMethods;
        _invokeBytesMethods = invokeBytesMethods;
        _getObjectFunctions = getObjectFunctions;
        _releaseObjectFunctions = releaseObjectFunctions;
        _overheadSizeFunctions = overheadSizeFunctions;
        _broadcastMethods = broadcastMethods;
    }

    [UsedImplicitly]
    public void AddBroadcastReceiveMethods(Type type, Action<RpcClassRegistrationBuilder> action)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (type.Assembly == null)
            return;

        _broadcastMethods.AddOrUpdate(
            type,
            _ =>
            {
                RpcClassRegistrationBuilder r = new RpcClassRegistrationBuilder();
                action(r);
                return r.Methods.ToArray();
            },
            (_, value) =>
            {
                RpcClassRegistrationBuilder r = new RpcClassRegistrationBuilder();
                action(r);

                RpcEndpointTarget[] newArray = new RpcEndpointTarget[value.Count + r.Methods.Count];
                switch (value)
                {
                    case RpcEndpointTarget[] t:
                        Array.Copy(t, newArray, t.Length);
                        break;

                    case IList<RpcEndpointTarget> l:
                        l.CopyTo(newArray, 0);
                        break;

                    default:
                        int index = 0;
                        foreach (RpcEndpointTarget target in value)
                        {
                            newArray[index] = target;
                            ++index;
                        }

                        break;
                }

                r.Methods.CopyTo(newArray, value.Count);
                return newArray;
            }
        );
    }

    [UsedImplicitly]
    public void AddGetOverheadSizeFunction(Type type, ProxyGenerator.GetOverheadSize? function)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        _overheadSizeFunctions[type] = function;
    }

    [UsedImplicitly]
    public void AddGetObjectFunction(Type type, Func<object, WeakReference?> function)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        _getObjectFunctions[type] = function;
    }

    [UsedImplicitly]
    public void AddReleaseObjectFunction(Type type, Func<object, bool> function)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        _releaseObjectFunctions[type] = function;
    }


    [UsedImplicitly]
    public void AddMethodSignatureHash(RuntimeMethodHandle handle, int signature)
    {
        if (MethodSignatures == null)
            throw new InvalidOperationException();
        MethodSignatures[handle] = signature;
    }

    [UsedImplicitly]
    public void AddCallGetter(SourceGenerationServices.GetCallInfo getCallInfo)
    {
        if (getCallInfo == null)
            throw new ArgumentNullException(nameof(getCallInfo));
        _callInfoGetters[getCallInfo().MethodHandle] = getCallInfo;
    }

    [UsedImplicitly]
    public void AddReceiveMethod(RuntimeMethodHandle handle, ReceiveMethodInvokerType type, ProxyGenerator.RpcInvokeHandlerBytes invoker)
    {
        if (type != ReceiveMethodInvokerType.Bytes)
            throw new ArgumentOutOfRangeException(nameof(type));

        _invokeBytesMethods[handle] = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }

    [UsedImplicitly]
    public void AddReceiveMethod(RuntimeMethodHandle handle, ReceiveMethodInvokerType type, ProxyGenerator.RpcInvokeHandlerRawBytes invoker)
    {
        if (type != ReceiveMethodInvokerType.BytesRaw)
            throw new ArgumentOutOfRangeException(nameof(type));

        _invokeBytesMethods[handle] = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }

    [UsedImplicitly]
    public void AddReceiveMethod(RuntimeMethodHandle handle, ReceiveMethodInvokerType type, ProxyGenerator.RpcInvokeHandlerStream invoker)
    {
        if (type is not ReceiveMethodInvokerType.Stream and not ReceiveMethodInvokerType.StreamRaw)
            throw new ArgumentOutOfRangeException(nameof(type));

        _invokeStreamMethods[handle] = invoker ?? throw new ArgumentNullException(nameof(invoker));
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