using DanielWillett.ReflectionTools.Emit;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ModularRpcs.Reflection;
public struct RpcCallMethodInfo
{
    public bool IsFireAndForget;
    public int SignatureHash;
    public uint KnownId;
    public RpcEndpointTarget Endpoint;
    public bool HasIdentifier;
    public TimeSpan Timeout;

    /// <remarks>
    /// Only saved for source-generated methods.
    /// </remarks>>
    public RuntimeMethodHandle MethodHandle;

    private static readonly FieldInfo HasIdentifierField = typeof(RpcCallMethodInfo).GetField(nameof(HasIdentifier), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo IsFireAndForgetField = typeof(RpcCallMethodInfo).GetField(nameof(IsFireAndForget), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo SignatureHashField = typeof(RpcCallMethodInfo).GetField(nameof(SignatureHash), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo EndpointField = typeof(RpcCallMethodInfo).GetField(nameof(Endpoint), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo KnownIdField = typeof(RpcCallMethodInfo).GetField(nameof(KnownId), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo TimeoutField = typeof(RpcCallMethodInfo).GetField(nameof(Timeout), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Create a <see cref="RpcCallMethodInfo"/> from a call/invoke method.
    /// </summary>
    public static RpcCallMethodInfo FromCallMethod(ProxyGenerator generator, MethodInfo method, bool isFireAndForget)
    {
        RpcCallMethodInfo info = default;
        info.MethodHandle = method.MethodHandle;
        info.IsFireAndForget = isFireAndForget;
        info.SignatureHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(method);
        info.Endpoint = RpcEndpointTarget.FromCallMethod(method);
        info.HasIdentifier = method is { IsStatic: false, DeclaringType: not null }
                             && method.DeclaringType.GetField(ProxyGenerator.Instance.IdentifierFieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null;
        info.Timeout = TypeUtility.GetTimeoutFromMethod(method, generator.DefaultTimeout);
        return info;
    }

    /// <summary>
    /// Expects an address of type <see cref="RpcCallMethodInfo"/>&amp; on the stack. 
    /// </summary>
    internal readonly void EmitToAddress(IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Initobj, typeof(RpcCallMethodInfo));

        il.Emit(OpCodes.Dup);
        il.Emit(IsFireAndForget ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, IsFireAndForgetField);

        il.Emit(OpCodes.Dup);
        il.Emit(HasIdentifier ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, HasIdentifierField);

        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4, SignatureHash);
        il.Emit(OpCodes.Stfld, SignatureHashField);

        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4, unchecked( (int)KnownId ));
        il.Emit(OpCodes.Stfld, KnownIdField);

        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I8, Timeout.Ticks);
        il.Emit(OpCodes.Newobj, CommonReflectionCache.TimeSpanTicksCtor);
        il.Emit(OpCodes.Stfld, TimeoutField);

        // il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldflda, EndpointField);
        Endpoint.EmitToAddress(il);
    }
}
