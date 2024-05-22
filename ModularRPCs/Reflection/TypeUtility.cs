using DanielWillett.ReflectionTools;
using System;
using System.Reflection;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ModularRpcs.Reflection;
internal static class TypeUtility
{
    public const TypeCode MaxUsedTypeCode = (TypeCode)20;
    public const TypeCode TypeCodeTimeSpan = (TypeCode)17;
    public const TypeCode TypeCodeGuid = (TypeCode)19;
    public const TypeCode TypeCodeDateTimeOffset = (TypeCode)20;
    public static TypeCode GetTypeCode(Type tc)
    {
        if (tc == typeof(DBNull))
            return TypeCode.DBNull;
        if (tc == typeof(bool))
            return TypeCode.Boolean;
        if (tc == typeof(char))
            return TypeCode.Char;
        if (tc == typeof(sbyte))
            return TypeCode.SByte;
        if (tc == typeof(byte))
            return TypeCode.Byte;
        if (tc == typeof(short))
            return TypeCode.Int16;
        if (tc == typeof(ushort))
            return TypeCode.UInt16;
        if (tc == typeof(int))
            return TypeCode.Int32;
        if (tc == typeof(uint))
            return TypeCode.UInt32;
        if (tc == typeof(long))
            return TypeCode.Int64;
        if (tc == typeof(ulong))
            return TypeCode.UInt64;
        if (tc == typeof(float))
            return TypeCode.Single;
        if (tc == typeof(double))
            return TypeCode.Double;
        if (tc == typeof(decimal))
            return TypeCode.Decimal;
        if (tc == typeof(DateTime))
            return TypeCode.DateTime;
        if (tc == typeof(TimeSpan))
            return TypeCodeTimeSpan;
        if (tc == typeof(string))
            return TypeCode.String;
        if (tc == typeof(Guid))
            return TypeCodeGuid;
        if (tc == typeof(DateTimeOffset))
            return TypeCodeDateTimeOffset;

        return TypeCode.Object;
    }
    public static Type GetType(TypeCode tc)
    {
        return tc switch
        {
            TypeCode.Empty => throw new ArgumentOutOfRangeException(nameof(tc)),
            TypeCode.Object => typeof(object),
            TypeCode.DBNull => typeof(DBNull),
            TypeCode.Boolean => typeof(bool),
            TypeCode.Char => typeof(char),
            TypeCode.SByte => typeof(sbyte),
            TypeCode.Byte => typeof(byte),
            TypeCode.Int16 => typeof(short),
            TypeCode.UInt16 => typeof(ushort),
            TypeCode.Int32 => typeof(int),
            TypeCode.UInt32 => typeof(uint),
            TypeCode.Int64 => typeof(long),
            TypeCode.UInt64 => typeof(ulong),
            TypeCode.Single => typeof(float),
            TypeCode.Double => typeof(double),
            TypeCode.Decimal => typeof(decimal),
            TypeCode.DateTime => typeof(DateTime),
            TypeCodeTimeSpan => typeof(TimeSpan),
            TypeCode.String => typeof(string),
            TypeCodeGuid => typeof(Guid),
            TypeCodeDateTimeOffset => typeof(DateTimeOffset),
            _ => throw new ArgumentOutOfRangeException(nameof(tc))
        };
    }
    public static int GetTypeCodeSize(TypeCode tc)
    {
        return tc switch
        {
            TypeCode.DBNull => 0,
            TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte => 1,
            TypeCode.Char => sizeof(char),
            TypeCode.Int16 => sizeof(short),
            TypeCode.UInt16 => sizeof(ushort),
            TypeCode.Int32 => sizeof(int),
            TypeCode.UInt32 => sizeof(uint),
            TypeCode.Int64 => sizeof(long),
            TypeCode.UInt64 => sizeof(ulong),
            TypeCode.Single => sizeof(float),
            TypeCode.Double => sizeof(double),
            TypeCode.Decimal => sizeof(int) * 4,
            TypeCode.DateTime => sizeof(long),
            TypeCodeDateTimeOffset => sizeof(long) + sizeof(short),
            TypeCodeTimeSpan => sizeof(long),
            TypeCode.String => 2,
            TypeCodeGuid => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(tc), tc, null)
        };
    }
    public static bool ParametersMatchMethod(MethodInfo method, Type[] types, bool bindOnly)
    {
        ParameterInfo[] parameters = method.GetParameters();
        ArraySegment<ParameterInfo> toBind;
        if (bindOnly)
            SerializerGenerator.BindParameters(parameters, out _, out toBind);
        else
            toBind = new ArraySegment<ParameterInfo>(parameters);

        for (int i = 0; i < toBind.Count; ++i)
        {
            ParameterInfo p = toBind.Array![i + toBind.Offset];
            Type type = types[i];
            Type pType = p.ParameterType;

            if (pType == type || pType.IsByRef && pType.GetElementType()! == type)
                continue;

            return false;
        }

        return true;
    }
    public static bool ParametersMatchMethod(MethodInfo method, string[] typeNames, bool bindOnly)
    {
        ParameterInfo[] parameters = method.GetParameters();
        ArraySegment<ParameterInfo> toBind;
        if (bindOnly)
            SerializerGenerator.BindParameters(parameters, out _, out toBind);
        else
            toBind = new ArraySegment<ParameterInfo>(parameters);

        for (int i = 0; i < toBind.Count; ++i)
        {
            ParameterInfo p = toBind.Array![i + toBind.Offset];
            string typeName = typeNames[i];
            Type pType = p.ParameterType;

            if (CompareAssemblyQualifiedNameNoVersion(pType.IsByRef ? pType.GetElementType()! : pType, typeName))
                return false;
        }

        return true;
    }
    public static string GetAssemblyQualifiedNameNoVersion(Type type)
    {
        if (type.Assembly == Accessor.MSCoreLib)
            return type.FullName ?? type.Name;

        return Assembly.CreateQualifiedName(type.Assembly.GetName().Name, type.FullName ?? type.Name);
    }

    public static bool CompareAssemblyQualifiedNameNoVersion(Type type, string asmQualifiedName)
    {
        int asmVerInd = asmQualifiedName.IndexOf(", Version=", StringComparison.OrdinalIgnoreCase);

        // cut off version and following identifiers
        if (asmVerInd != -1)
            return GetAssemblyQualifiedNameNoVersion(type).AsSpan().Equals(asmQualifiedName.AsSpan(0, asmVerInd), StringComparison.Ordinal);
        
        return GetAssemblyQualifiedNameNoVersion(type).Equals(asmQualifiedName, StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Resolve a method from target info.
    /// </summary>
    public static bool TryResolveMethod(
        MethodInfo? decoratingMethod, string? methodName, Type? declaringType, string? declaringTypeName, Type[]? parameterTypes, string[]? parameterTypeNames, bool parametersAreBindedParametersOnly,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out MethodInfo method, out ResolveMethodResult result)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            result = ResolveMethodResult.IsSelfTarget;
            method = decoratingMethod;
            return decoratingMethod != null;
        }

        if (
            decoratingMethod != null
            // is this attribute referencing itself (decoratingMethod).
            && methodName!.Equals(decoratingMethod.Name)
            && (declaringType == null || declaringType == decoratingMethod.DeclaringType)
            && (string.IsNullOrEmpty(declaringTypeName) || decoratingMethod.DeclaringType != null && CompareAssemblyQualifiedNameNoVersion(decoratingMethod.DeclaringType, declaringTypeName))
            && (parameterTypes == null || ParametersMatchMethod(decoratingMethod, parameterTypes, parametersAreBindedParametersOnly))
            && (parameterTypeNames == null || ParametersMatchMethod(decoratingMethod, parameterTypeNames, parametersAreBindedParametersOnly)))
        {
            result = ResolveMethodResult.IsSelfTarget;
            method = decoratingMethod;
            return true;
        }

        method = null;
        Type? type = declaringType;

        if (type == null && string.IsNullOrEmpty(declaringTypeName))
        {
            if (decoratingMethod?.DeclaringType == null)
            {
                result = ResolveMethodResult.DeclaringTypeNotFound;
                return false;
            }

            type = decoratingMethod.DeclaringType;
        }

        if (type == null && !string.IsNullOrEmpty(declaringTypeName))
        {
            type = Type.GetType(declaringTypeName, false, false);
        }

        if (type == null)
        {
            result = ResolveMethodResult.DeclaringTypeNotFound;
            return false;
        }

        try
        {
            method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            result = method == null ? ResolveMethodResult.MethodNotFound : ResolveMethodResult.Success;
            return method != null;
        }
        catch (AmbiguousMatchException)
        {
            if (parameterTypes == null && parameterTypeNames == null)
            {
                result = ResolveMethodResult.AmbiguousMatch;
                return false;
            }
        }

        if (!parametersAreBindedParametersOnly && parameterTypes != null)
        {
            try
            {
                method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, CallingConventions.Any, parameterTypes, null);
                if (method != null)
                {
                    result = ResolveMethodResult.Success;
                    return true;
                }
            }
            catch (AmbiguousMatchException)
            {
                if (parameterTypeNames == null)
                {
                    result = ResolveMethodResult.AmbiguousMatch;
                    return false;
                }
            }
        }

        MethodInfo[] allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        for (int i = 0; i < allMethods.Length; ++i)
        {
            MethodInfo checkMethod = allMethods[i];
            if (!checkMethod.Name.Equals(methodName, StringComparison.Ordinal))
                continue;

            bool match = parameterTypes != null && ParametersMatchMethod(checkMethod, parameterTypes, parametersAreBindedParametersOnly);
            if ((parameterTypes == null || match) && parameterTypeNames != null)
                match = ParametersMatchMethod(checkMethod, parameterTypeNames, parametersAreBindedParametersOnly);

            if (!match)
                continue;

            if (method == null)
                method = checkMethod;
            else
            {
                result = ResolveMethodResult.AmbiguousMatch;
                return false;
            }
        }

        result = method != null ? ResolveMethodResult.Success : ResolveMethodResult.MethodNotFound;
        return result == ResolveMethodResult.Success;
    }
}

public enum ResolveMethodResult
{
    Success = 0,

    /// <summary>
    /// This attribute targets it's decorated method.
    /// </summary>
    IsSelfTarget,

    /// <summary>
    /// The type specified by <see cref="RpcTargetAttribute.TypeName"/> was not found.
    /// </summary>
    DeclaringTypeNotFound,

    /// <summary>
    /// The method with the given name was not found.
    /// </summary>
    MethodNotFound,

    /// <summary>
    /// The name alone was not enough to narrow down the overload to use, or a ref parameter and a non-ref parameter were at the same place and couldn't be distinguished.
    /// </summary>
    AmbiguousMatch
}