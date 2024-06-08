using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ModularRpcs.Reflection;
public struct RpcEndpointTarget
{
    /// <summary>
    /// Normally, the send method specifies the receive method, which must be marked with a <see cref="RpcReceiveAttribute"/>.
    /// In some cases, however, the receive method can specify the send method, acting as a listener.
    /// </summary>
    public bool IsBroadcast;

    private RpcEndpoint? _endpoint;

    public string MethodName;
    public string DeclaringTypeName;
    public string[]? ParameterTypes;
    public bool ParameterTypesAreBindOnly;
    public bool IgnoreSignatureHash;
    public int SignatureHash;
    internal MethodInfo? OwnerMethodInfo;

    private static readonly FieldInfo IsDeclaringSendMethodField = typeof(RpcEndpointTarget).GetField(nameof(IsBroadcast), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo MethodNameField = typeof(RpcEndpointTarget).GetField(nameof(MethodName), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo DeclaringTypeNameField = typeof(RpcEndpointTarget).GetField(nameof(DeclaringTypeName), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ParameterTypesField = typeof(RpcEndpointTarget).GetField(nameof(ParameterTypes), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ParameterTypesAreBindOnlyField = typeof(RpcEndpointTarget).GetField(nameof(ParameterTypesAreBindOnly), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo SignatureHashField = typeof(RpcEndpointTarget).GetField(nameof(SignatureHash), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo IgnoreSignatureHashField = typeof(RpcEndpointTarget).GetField(nameof(IgnoreSignatureHash), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    public RpcEndpoint GetEndpoint()
    {
        return _endpoint ??= new RpcEndpoint(DeclaringTypeName, MethodName, ParameterTypes, ParameterTypesAreBindOnly, IsBroadcast, SignatureHash, IgnoreSignatureHash);
    }

    /// <summary>
    /// Create a <see cref="RpcEndpointTarget"/> from a call/invoke method's attributes.
    /// </summary>
    public static RpcEndpointTarget FromCallMethod(MethodInfo method)
    {
        bool forceSignatureCheck = method.IsDefinedSafe<RpcForceSignatureCheckAttribute>();

        RpcSendAttribute? sendAttribute = method.GetAttributeSafe<RpcSendAttribute>();
        if (sendAttribute == null)
            throw new ArgumentException(string.Format(Properties.Exceptions.MethodNotCallMethod, Accessor.ExceptionFormatter.Format(method)), nameof(method));

        RpcEndpointTarget target = default;

        target.SignatureHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(method);
        if (sendAttribute.TryResolveMethod(method, out MethodInfo? resolvedMethod, out ResolveMethodResult result))
        {
            target.IsBroadcast = result == ResolveMethodResult.IsSelfTarget;
            FromMethod(ref target, resolvedMethod, forceSignatureCheck, sendAttribute.ParametersAreBindedParametersOnly);
            return target;
        }

        FromAttribute(ref target, method, forceSignatureCheck, sendAttribute);
        return target;
    }

    public static RpcEndpointTarget FromReceiveMethod(MethodInfo method)
    {
        bool forceSignatureCheck = method.IsDefinedSafe<RpcForceSignatureCheckAttribute>();

        RpcReceiveAttribute? receiveAttribute = method.GetAttributeSafe<RpcReceiveAttribute>();
        if (receiveAttribute == null)
            throw new ArgumentException(string.Format(Properties.Exceptions.MethodNotReceiveMethod, Accessor.ExceptionFormatter.Format(method)), nameof(method));

        RpcEndpointTarget target = default;

        if (receiveAttribute.TryResolveMethod(method, out MethodInfo? resolvedMethod, out ResolveMethodResult result))
        {
            target.IsBroadcast = result == ResolveMethodResult.IsSelfTarget;
            FromMethod(ref target, resolvedMethod, forceSignatureCheck, receiveAttribute.ParametersAreBindedParametersOnly);
            return target;
        }

        FromAttribute(ref target, method, forceSignatureCheck, receiveAttribute);
        return target;
    }

    private static void FromMethod(ref RpcEndpointTarget target, MethodInfo method, bool forceSignatureCheck, bool bindOnlyParameters)
    {
        target.MethodName = method.Name ?? string.Empty;
        target.DeclaringTypeName = method.DeclaringType == null ? string.Empty : TypeUtility.GetAssemblyQualifiedNameNoVersion(method.DeclaringType);
        
        if (!(forceSignatureCheck || 
            method.DeclaringType != null && method.DeclaringType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Count(x => x.Name.Equals(method.Name, StringComparison.Ordinal)) > 1
            ))
        {
            target.ParameterTypesAreBindOnly = false;
            target.ParameterTypes = null;
            return;
        }

        ParameterInfo[] parameters = method.GetParameters();
        ArraySegment<ParameterInfo> toBind;
        if (bindOnlyParameters)
            SerializerGenerator.BindParameters(parameters, out _, out toBind);
        else
            toBind = new ArraySegment<ParameterInfo>(parameters);

        string[] typeNames = new string[toBind.Count];
        for (int i = 0; i < toBind.Count; ++i)
        {
            typeNames[i] = TypeUtility.GetAssemblyQualifiedNameNoVersion(toBind.Array![i + toBind.Offset].ParameterType);
        }

        target.ParameterTypesAreBindOnly = bindOnlyParameters;
        target.ParameterTypes = typeNames;
    }
    private static void FromAttribute(ref RpcEndpointTarget target, MethodInfo decoratingMethod, bool forceSignatureCheck, RpcTargetAttribute targetAttribute)
    {
        target.MethodName = targetAttribute.MethodName!;
        Type? declaringType;
        if (targetAttribute.Type != null)
        {
            declaringType = targetAttribute.Type;
            target.DeclaringTypeName = TypeUtility.GetAssemblyQualifiedNameNoVersion(targetAttribute.Type);
        }
        else if (!string.IsNullOrEmpty(targetAttribute.TypeName))
        {
            declaringType = Type.GetType(targetAttribute.TypeName!, false, false);
            target.DeclaringTypeName = targetAttribute.TypeName!;
        }
        else if (decoratingMethod.DeclaringType is { } methodDeclaringType
                 && methodDeclaringType.TryGetAttributeSafe(out RpcClassAttribute classAttribute)
                 && (classAttribute.DefaultType != null || !string.IsNullOrEmpty(classAttribute.DefaultTypeName)))
        {
            target.DeclaringTypeName = classAttribute.DefaultType != null
                ? TypeUtility.GetAssemblyQualifiedNameNoVersion(classAttribute.DefaultType)
                : classAttribute.DefaultTypeName!;
            declaringType = Type.GetType(target.DeclaringTypeName, false, false);
        }
        else
        {
            declaringType = decoratingMethod.DeclaringType;
            target.DeclaringTypeName = declaringType != null
                ? TypeUtility.GetAssemblyQualifiedNameNoVersion(declaringType)
                : decoratingMethod.Module.FullyQualifiedName;
        }

        bool needsSigCheck = forceSignatureCheck;
        if (!needsSigCheck && declaringType != null)
        {
            try
            {
                _ = declaringType.GetMethod(target.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }
            catch (AmbiguousMatchException)
            {
                needsSigCheck = true;
            }
        }

        if (!needsSigCheck)
            return;

        target.ParameterTypesAreBindOnly = targetAttribute.ParametersAreBindedParametersOnly;

        if (targetAttribute.ParameterTypes != null)
        {
            Type[] types = targetAttribute.ParameterTypes;
            string[] names = new string[types.Length];

            for (int i = 0; i < types.Length; ++i)
                names[i] = TypeUtility.GetAssemblyQualifiedNameNoVersion(types[i]);

            target.ParameterTypes = names;
        }
        else
        {
            target.ParameterTypes = targetAttribute.ParameterTypeNames;
        }
    }

    /// <summary>
    /// Expects an address of type <see cref="RpcEndpointTarget"/>&amp; on the stack. 
    /// </summary>
    internal readonly void EmitToAddress(IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Initobj, typeof(RpcEndpointTarget));

        il.Emit(OpCodes.Dup);
        il.Emit(IsBroadcast ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, IsDeclaringSendMethodField);

        il.Emit(OpCodes.Dup);
        il.Emit(ParameterTypesAreBindOnly ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, ParameterTypesAreBindOnlyField);

        il.Emit(OpCodes.Dup);
        il.Emit(IgnoreSignatureHash ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stfld, IgnoreSignatureHashField);

        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4, SignatureHash);
        il.Emit(OpCodes.Stfld, SignatureHashField);

        il.Emit(OpCodes.Dup);
        if (DeclaringTypeName == null)
            il.Emit(OpCodes.Ldnull);
        else
            il.Emit(OpCodes.Ldstr, MethodName);
        il.Emit(OpCodes.Stfld, MethodNameField);

        il.Emit(OpCodes.Dup);
        if (DeclaringTypeName == null)
            il.Emit(OpCodes.Ldnull);
        else
            il.Emit(OpCodes.Ldstr, DeclaringTypeName);
        il.Emit(OpCodes.Stfld, DeclaringTypeNameField);

        // il.Emit(OpCodes.Dup);
        if (ParameterTypes == null)
        {
            il.Emit(OpCodes.Ldnull);
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4, ParameterTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(string));

            for (int i = 0; i < ParameterTypes.Length; ++i)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                if (ParameterTypes[i] == null)
                    il.Emit(OpCodes.Ldnull);
                else
                    il.Emit(OpCodes.Ldstr, ParameterTypes[i]);
                il.Emit(OpCodes.Stelem_Ref);
            }
        }
        il.Emit(OpCodes.Stfld, ParameterTypesField);
    }
}
