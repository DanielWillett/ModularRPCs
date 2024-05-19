using DanielWillett.ReflectionTools.Emit;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ModularRpcs.Reflection;
public struct RpcCallMethodInfo
{
    public bool IsFireAndForget;
    public int SignatureHash;
    public RpcEndpointTarget Endpoint;

    private static readonly FieldInfo IsFireAndForgetField = typeof(RpcCallMethodInfo).GetField(nameof(IsFireAndForget), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo SignatureHashField = typeof(RpcCallMethodInfo).GetField(nameof(SignatureHash), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo EndpointField = typeof(RpcCallMethodInfo).GetField(nameof(Endpoint), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Create a <see cref="RpcCallMethodInfo"/> from a call/invoke method.
    /// </summary>
    public static RpcCallMethodInfo FromCallMethod(MethodInfo method, bool isFireAndForget)
    {
        RpcCallMethodInfo info = default;
        info.IsFireAndForget = isFireAndForget;
        info.SignatureHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(method);
        info.Endpoint = RpcEndpointTarget.FromCallMethod(method);
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
        il.Emit(OpCodes.Ldc_I4, SignatureHash);
        il.Emit(OpCodes.Stfld, SignatureHashField);

        // il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldflda, EndpointField);
        Endpoint.EmitToAddress(il);
    }
}
