using System;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ModularRpcs.Reflection;
public struct RpcEndpointTarget
{
    /// <summary>
    /// Normally, the send method specifies the receive method, which must be marked with a <see cref="RpcReceiveAttribute"/>.
    /// In some cases, however, the receive method can specify the send method, acting as a listener.
    /// </summary>
    public bool IsDeclaringSendMethod;

    public string? MethodName;

    /// <summary>
    /// Create a <see cref="RpcEndpointTarget"/> from a call/invoke method's attributes.
    /// </summary>
    public static RpcEndpointTarget FromCallMethod(MethodInfo method)
    {
        bool forceSignatureCheck = method.IsDefinedSafe<RpcForceSignatureCheckAttribute>();

        RpcSendAttribute? sendAttribute = method.GetAttributeSafe<RpcSendAttribute>();
        if (sendAttribute == null)
            throw new ArgumentException(string.Format(Properties.Exceptions.MethodNotCallMethod, Accessor.ExceptionFormatter.Format(method)), nameof(method));

        bool isDeclaringSendMethod = string.IsNullOrEmpty(sendAttribute.MethodName);

        if (!isDeclaringSendMethod && sendAttribute.MethodName!.Equals(method.Name))
        {
            if (sendAttribute.Type == null && sendAttribute.TypeName == null) // todo check parameters
            {
                isDeclaringSendMethod = true;
            }
        }

        RpcEndpointTarget target = default;

        FromAttribute(ref target, sendAttribute);

        return target;
    }

    private static bool ParametersMatchMethod(MethodInfo method, Type[] types, bool bindOnly)
    {
        ParameterInfo[] parameters = method.GetParameters();
        ArraySegment<ParameterInfo> toBind;
        if (bindOnly)
            SerializerGenerator.BindParameters(parameters, out _, out toBind);
        else
            toBind = new ArraySegment<ParameterInfo>(parameters);

        return false; // todo
    }
    private static bool ParametersMatchMethod(MethodInfo method, string[] typeNames, bool bindOnly)
    {
        ParameterInfo[] parameters = method.GetParameters();
        ArraySegment<ParameterInfo> toBind;
        if (bindOnly)
            SerializerGenerator.BindParameters(parameters, out _, out toBind);
        else
            toBind = new ArraySegment<ParameterInfo>(parameters);

        return false; // todo
    }


    private static void FromAttribute(ref RpcEndpointTarget target, RpcTargetAttribute targetAttribute)
    {

    }

    private static void InitMethodInfo(ref RpcEndpointTarget target)
    {

    }

    /// <summary>
    /// Expects an address of type <see cref="RpcEndpointTarget"/>&amp; on the stack. 
    /// </summary>
    internal readonly void EmitToAddress(IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Initobj, typeof(RpcEndpointTarget));
        // todo
    }
}
