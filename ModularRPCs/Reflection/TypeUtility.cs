using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using MethodInfo = System.Reflection.MethodInfo;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ModularRpcs.Reflection;
internal static class TypeUtility
{
    private static readonly ConcurrentDictionary<Type, Type[]> ServiceInterfaces = new ConcurrentDictionary<Type, Type[]>();
    private static readonly ConcurrentDictionary<Type, Type?> SerializableEnumerableTypes = new ConcurrentDictionary<Type, Type?>();
    private static readonly ConcurrentDictionary<Type, Type?> EnumerableTypes = new ConcurrentDictionary<Type, Type?>();
    public const TypeCode MaxUsedTypeCode = (TypeCode)22;
    [UsedImplicitly]
    public const TypeCode TypeCodeTimeSpan = (TypeCode)17;
    [UsedImplicitly]
    public const TypeCode TypeCodeGuid = (TypeCode)19;
    [UsedImplicitly]
    public const TypeCode TypeCodeDateTimeOffset = (TypeCode)20;
    [UsedImplicitly]
    public const TypeCode TypeCodeIntPtr = (TypeCode)21;
    [UsedImplicitly]
    public const TypeCode TypeCodeUIntPtr = (TypeCode)22;
    [UsedImplicitly]
    public const TypeCode TypeCodeNullable = (TypeCode)30;

    // NOTE: Extension methods are not supported.
    public static bool IsAwaitable(Type valueType, bool useConfigureAwait,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]  
#endif
        out Type awaitReturnType, out MethodInfo? configureAwaitMethod,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]  
#endif
        out MethodInfo getAwaiterMethod,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]  
#endif
        out MethodInfo getResultMethod,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]  
#endif
        out MethodInfo onCompletedMethod,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]  
#endif
        out PropertyInfo getIsCompletedProperty)
    {
        awaitReturnType = null!;
        getAwaiterMethod = null!;
        getResultMethod = null!;
        getIsCompletedProperty = null!;
        onCompletedMethod = null!;

        Type? originalAwaiterType = null;

        configureAwaitMethod = useConfigureAwait ? GetMostRelevantMethodByName("ConfigureAwait",
            valueType,
            [ typeof(bool) ],
            x => !x.ContainsGenericParameters && x.GetParameters() is { Length: 1 } p && p[0].ParameterType == typeof(bool))
                : null;
        if (configureAwaitMethod != null && configureAwaitMethod.ReturnType != typeof(void))
        {
            originalAwaiterType = valueType;
            valueType = configureAwaitMethod.ReturnType;
        }

        runWithoutConfigureAwait:
        getAwaiterMethod = GetMostRelevantMethodByName("GetAwaiter", valueType, Type.EmptyTypes,
            x => !x.ContainsGenericParameters && x.ReturnType != typeof(void));

        if (getAwaiterMethod == null)
        {
            if (originalAwaiterType == null)
                return false;
            valueType = originalAwaiterType;
            originalAwaiterType = null;
            goto runWithoutConfigureAwait;
        }

        Type awaiterType = getAwaiterMethod.ReturnType;
        if (!typeof(INotifyCompletion).IsAssignableFrom(awaiterType))
        {
            if (originalAwaiterType == null)
                return false;
            valueType = originalAwaiterType;
            originalAwaiterType = null;
            goto runWithoutConfigureAwait;
        }

        getIsCompletedProperty = GetMostRelevantPropertyByName("IsCompleted", awaiterType, typeof(bool), x => x.GetGetMethod(false) != null);

        if (getIsCompletedProperty == null)
        {
            if (originalAwaiterType == null)
                return false;
            valueType = originalAwaiterType;
            originalAwaiterType = null;
            goto runWithoutConfigureAwait;
        }

        onCompletedMethod = null;
        if (typeof(ICriticalNotifyCompletion).IsAssignableFrom(awaiterType))
        {
            onCompletedMethod = Accessor.GetImplementedMethod(awaiterType, CommonReflectionCache.CriticalNotifyCompletionOnCompleted);
        }

        if (onCompletedMethod == null)
        {
            onCompletedMethod = Accessor.GetImplementedMethod(awaiterType, CommonReflectionCache.NotifyCompletionOnCompleted);
        }

        if (onCompletedMethod == null || getIsCompletedProperty.GetGetMethod(false) == null)
        {
            if (originalAwaiterType == null)
                return false;
            valueType = originalAwaiterType;
            originalAwaiterType = null;
            goto runWithoutConfigureAwait;
        }

        getResultMethod = GetMostRelevantMethodByName("GetResult", awaiterType, Type.EmptyTypes, x => !x.ContainsGenericParameters);
        if (getResultMethod == null)
        {
            if (originalAwaiterType == null)
                return false;
            valueType = originalAwaiterType;
            originalAwaiterType = null;
            goto runWithoutConfigureAwait;
        }

        awaitReturnType = getResultMethod.ReturnType;
        return true;
    }

    private static MethodInfo? GetMostRelevantMethodByName(string methodName, Type type, Type[] parameters, Func<MethodInfo, bool> verify, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        if ((flags & BindingFlags.Instance) != 0)
            flags |= BindingFlags.FlattenHierarchy;

        MethodInfo? method = null;
        try
        {
            method = type.GetMethod(methodName, flags, null, parameters, null);
            if (method == null || !verify(method))
                return null;
        }
        catch (AmbiguousMatchException)
        {
            MethodInfo[] allMethods = type.GetMethods(flags);
            foreach (MethodInfo iter in allMethods)
            {
                // find matching method highest in the type hierarchy
                if (!string.Equals(iter.Name, methodName, StringComparison.Ordinal))
                    continue;

                ParameterInfo[] checkParameters = iter.GetParameters();
                if (parameters.Length != checkParameters.Length)
                    continue;

                bool bindSuccess = true;
                for (int i = 0; i < checkParameters.Length; ++i)
                {
                    if (checkParameters[i].ParameterType == parameters[i])
                        continue;

                    bindSuccess = false;
                    break;
                }

                if (!bindSuccess || !verify(iter))
                    continue;

                if (method == null || method.DeclaringType!.IsAssignableFrom(iter.DeclaringType))
                    method = iter;
            }
        }

        return method;
    }
    private static PropertyInfo? GetMostRelevantPropertyByName(string propertyName, Type type, Type returnType, Func<PropertyInfo, bool> verify, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        if ((flags & BindingFlags.Instance) != 0)
            flags |= BindingFlags.FlattenHierarchy;

        PropertyInfo? property = null;
        try
        {
            property = type.GetProperty(propertyName, flags, null, returnType, Type.EmptyTypes, null);
            if (property == null || !verify(property))
                return null;
        }
        catch (AmbiguousMatchException)
        {
            PropertyInfo[] allProperties = type.GetProperties(flags);
            foreach (PropertyInfo iter in allProperties)
            {
                // find matching method highest in the type hierarchy
                if (!string.Equals(iter.Name, propertyName, StringComparison.Ordinal))
                    continue;

                if (iter.GetIndexParameters().Length > 0 || iter.PropertyType != returnType || !verify(iter))
                    continue;

                if (property == null || property.DeclaringType!.IsAssignableFrom(iter.DeclaringType))
                    property = iter;
            }
        }

        return property;
    }

    public static Type? GetSerializableEnumerableType(Type type, out bool isNullable)
    {
        if (type == typeof(string))
        {
            isNullable = false;
            return null;
        }

        if (SerializableEnumerableTypes.TryGetValue(type, out Type? t))
        {
            isNullable = t != null && Nullable.GetUnderlyingType(t) != null;
            return t;
        }

        if (type.IsArray)
        {
            Type arrayElementType = type.GetElementType()!;
            if (Nullable.GetUnderlyingType(arrayElementType) is { } nullableElementType)
            {
                if (SerializerGenerator.IsRpcSerializableType(nullableElementType))
                {
                    SerializableEnumerableTypes.TryAdd(type, arrayElementType);
                    isNullable = true;
                    return arrayElementType;
                }
            }

            isNullable = false;
            if (SerializerGenerator.IsRpcSerializableType(arrayElementType))
            {
                SerializableEnumerableTypes.TryAdd(type, arrayElementType);
                return arrayElementType;
            }

            SerializableEnumerableTypes.TryAdd(type, null);
            return null;
        }

        if (type.IsInterface)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                if (Nullable.GetUnderlyingType(elementType) is { } nullableElementType)
                {
                    if (SerializerGenerator.IsRpcSerializableType(nullableElementType))
                    {
                        SerializableEnumerableTypes.TryAdd(type, elementType);
                        isNullable = true;
                        return elementType;
                    }
                }

                if (SerializerGenerator.IsRpcSerializableType(elementType))
                {
                    SerializableEnumerableTypes.TryAdd(type, elementType);
                    isNullable = false;
                    return elementType;
                }
            }
        }

        if (type is { IsValueType: true, IsConstructedGenericType: true })
        {
            Type genTypeDef = type.GetGenericTypeDefinition();
            if (genTypeDef == typeof(Memory<>) || genTypeDef == typeof(ReadOnlyMemory<>) || genTypeDef == typeof(ArraySegment<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                Type checkType = elementType;
                if (Nullable.GetUnderlyingType(elementType) is { } nullableElementType)
                {
                    checkType = nullableElementType;
                    isNullable = true;
                }
                else
                {
                    isNullable = false;
                }

                if (SerializerGenerator.IsRpcSerializableType(checkType))
                {
                    SerializableEnumerableTypes.TryAdd(type, elementType);
                    return elementType;
                }
            }
        }

        bool isIListNonGeneric = false;
        foreach (Type intx in type.GetInterfaces())
        {
            if (intx == typeof(IList))
                isIListNonGeneric = true;

            if (!intx.IsConstructedGenericType)
                continue;

            if (intx.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            Type elementType = intx.GetGenericArguments()[0];
            if (Nullable.GetUnderlyingType(elementType) is { } nullableElementType)
            {
                if (SerializerGenerator.IsRpcSerializableType(nullableElementType))
                {
                    SerializableEnumerableTypes.TryAdd(type, elementType);
                    isNullable = true;
                    return elementType;
                }
            }

            if (!SerializerGenerator.IsRpcSerializableType(elementType))
                continue;

            SerializableEnumerableTypes.TryAdd(type, elementType);
            isNullable = false;
            return elementType;
        }

        if (isIListNonGeneric && type.IsConstructedGenericType)
        {
            Type elementType = type.GetGenericArguments()[0];
            if (Nullable.GetUnderlyingType(elementType) is { } nullableElementType)
            {
                if (SerializerGenerator.IsRpcSerializableType(nullableElementType))
                {
                    SerializableEnumerableTypes.TryAdd(type, elementType);
                    isNullable = true;
                    return elementType;
                }
            }

            if (SerializerGenerator.IsRpcSerializableType(elementType))
            {
                SerializableEnumerableTypes.TryAdd(type, elementType);
                isNullable = false;
                return elementType;
            }
        }

        SerializableEnumerableTypes.TryAdd(type, null);
        isNullable = false;
        return null;
    }

    public static Type? GetEnumerableType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
            return type.GetElementType();

        if (EnumerableTypes.TryGetValue(type, out Type? t))
            return t;

        if (type.IsInterface)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                EnumerableTypes.TryAdd(type, elementType);
                return elementType;
            }
        }

        foreach (Type intx in type.GetInterfaces())
        {
            if (!intx.IsConstructedGenericType)
                continue;

            if (EnumerableTypes.TryGetValue(intx, out Type? elementType))
            {
                EnumerableTypes.TryAdd(type, elementType);
                return elementType;
            }

            if (intx.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            elementType = intx.GetGenericArguments()[0];
            EnumerableTypes.TryAdd(intx, elementType);
            EnumerableTypes.TryAdd(type, elementType);
            return elementType;
        }

        EnumerableTypes.TryAdd(type, null);
        return null;
    }

    public static TypeCode GetTypeCode(Type tc)
    {
        if (tc.IsValueType)
        {
            if (tc.IsEnum)
            {
                return GetTypeCode(tc.GetEnumUnderlyingType());
            }

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
            if (tc == typeof(Guid))
                return TypeCodeGuid;
            if (tc == typeof(DateTimeOffset))
                return TypeCodeDateTimeOffset;
            if (tc == typeof(nint))
                return TypeCodeIntPtr;
            if (tc == typeof(nuint))
                return TypeCodeUIntPtr;
        }
        else if (tc == typeof(string))
            return TypeCode.String;
        else if (tc == typeof(DBNull))
            return TypeCode.DBNull;

        return TypeCode.Object;
    }

    public static TypeCode GetTypeCode<TValue>()
    {
        return TypeCodeCache<TValue>.Value;
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
            TypeCodeIntPtr => typeof(nint),
            TypeCodeUIntPtr => typeof(nuint),
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
            TypeCodeIntPtr or TypeCodeUIntPtr => 8,
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

        if (types.Length != toBind.Count)
        {
            return false;
        }

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
        if (type.IsConstructedGenericType || type.IsNested)
        {
            return CreateAssemblyQualifiedNameSlow(type);
        }

        if (type.Assembly == Accessor.MSCoreLib)
            return type.FullName ?? type.Name;

        return Assembly.CreateQualifiedName(type.Assembly.GetName().Name, type.FullName ?? type.Name);
    }

    private static string CreateAssemblyQualifiedNameSlow(Type type)
    {
        Type[] args = type.GetGenericArguments();
        StringBuilder bldr = new StringBuilder((args.Length + 1) * 32);
        CreateAssemblyQualifiedNameSlow(type, bldr, args);
        return bldr.ToString();
    }

    private static void CreateAssemblyQualifiedNameSlow(Type type, StringBuilder bldr, Type[]? args, bool nested = false)
    {
        Type elementType = type;
        while (true)
        {
            if (elementType.HasElementType)
                elementType = elementType.GetElementType()!;
            else
                break;
        }

        if (!nested && !string.IsNullOrEmpty(elementType.Namespace))
        {
            bldr.Append(EscapeAssemblyQualifiedName(elementType.Namespace))
                .Append('.');
        }

        if (elementType.DeclaringType != null)
        {
            CreateAssemblyQualifiedNameSlow(elementType.DeclaringType, bldr, null, nested: true);
            bldr.Append('+');
        }

        bldr.Append(EscapeAssemblyQualifiedName(elementType.Name));

        if (nested)
            return;

        if (elementType.IsConstructedGenericType)
        {
            args ??= elementType.GetGenericArguments();
            bldr.Append('[');

            for (int i = 0; i < args.Length; ++i)
            {
                if (i != 0)
                    bldr.Append(',');

                bldr.Append('[');

                CreateAssemblyQualifiedNameSlow(args[i], bldr, null);

                bldr.Append(']');
            }

            bldr.Append(']');
        }

        elementType = type;
        while (true)
        {
            if (elementType.IsArray)
            {
                int rank = elementType.GetArrayRank();
                switch (rank)
                {
                    case 0:
                        break;

                    case 1:
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        bldr.Append(elementType.IsSZArray ? "[]" : "[*]");
#else
                        bldr.Append(elementType == elementType.GetElementType()!.MakeArrayType() ? "[]" : "[*]");
#endif
                        break;

                    default:
                        bldr.Append('[').Append(',', rank - 1).Append(']');
                        break;
                }

                elementType = elementType.GetElementType()!;
            }
            else if (elementType.IsPointer)
            {
                bldr.Append('*');
                elementType = elementType.GetElementType()!;
            }
            else if (elementType.IsByRef)
            {
                bldr.Append('&');
                elementType = elementType.GetElementType()!;
            }
            else
                break;
        }


        Assembly? asm = type.Assembly;
        if (asm == Accessor.MSCoreLib || asm == null)
        {
            return;
        }

        bldr.Append(", ")
            .Append(EscapeAssemblyQualifiedName(asm.GetName().Name));
    }

    public static bool CompareAssemblyQualifiedNameNoVersion(Type type, string asmQualifiedName)
    {
        int asmVerInd = asmQualifiedName.IndexOf(", Version=", StringComparison.OrdinalIgnoreCase);

        // cut off version and following identifiers
        if (asmVerInd != -1)
            return GetAssemblyQualifiedNameNoVersion(type).AsSpan().Equals(asmQualifiedName.AsSpan(0, asmVerInd), StringComparison.Ordinal);
        
        return GetAssemblyQualifiedNameNoVersion(type).Equals(asmQualifiedName, StringComparison.Ordinal);
    }

    private static readonly char[] AssemblyQualifiedEscapables = ['\n', '\r', '\t', '\v', '\\', ',', '+', '[', ']', '*', '&'];

    private static string EscapeAssemblyQualifiedName(string? value)
    {
        if (value == null)
            return null!;

        int c = 0;
        string s = value;
        for (int i = 0; i < s.Length; ++i)
        {
            if (s[i] is <= '\r' and ('\n' or '\r' or '\t' or '\v') or '\\' or ',' or '+' or '[' or ']' or '*' or '&')
                ++c;
        }

        if (c <= 0)
        {
            return s;
        }

        unsafe
        {
            char* newValue = stackalloc char[s.Length + c];

            int prevIndex = -1;
            int writeIndex = 0;
            while (true)
            {
                int index = s.IndexOfAny(AssemblyQualifiedEscapables, prevIndex + 1);
                if (index == -1)
                {
                    for (int i = prevIndex + 1; i < s.Length; ++i)
                    {
                        newValue[writeIndex] = s[i];
                        ++writeIndex;
                    }
                    break;
                }

                for (int i = prevIndex + 1; i < index; ++i)
                {
                    newValue[writeIndex] = s[i];
                    ++writeIndex;
                }

                char self = s[index];
                newValue[writeIndex] = '\\';
                newValue[writeIndex + 1] = self switch
                {
                    '\n' => 'n',
                    '\r' => 'r',
                    '\t' => 't',
                    '\v' => 'v',
                    _ => self
                };

                writeIndex += 2;

                prevIndex = index;
            }

            return new string(newValue, 0, writeIndex);
        }
    }

    public static unsafe void WriteTypeCode(TypeCode tc, IRpcSerializer serializer, object value, byte* ptr, ref uint index, uint size)
    {
        bool canFastRead = serializer.CanFastReadPrimitives;
        if (canFastRead && tc != TypeCode.String && size - index < GetTypeCodeSize(tc))
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        switch (tc)
        {
            case TypeCode.DBNull:
                break;

            case TypeCode.String:
                index += (uint)serializer.WriteObject((string)value, ptr + index, size - index);
                break;

            case TypeCode.SByte:
                if (canFastRead)
                {
                    ptr[index] = unchecked( (byte)(sbyte)value );
                    ++index;
                }
                else
                    index += (uint)serializer.WriteObject((sbyte)value, ptr + index, size - index);
                break;

            case TypeCode.Byte:
                if (canFastRead)
                {
                    ptr[index] = (byte)value;
                    ++index;
                }
                else
                    index += (uint)serializer.WriteObject((byte)value, ptr + index, size - index);
                break;

            case TypeCode.Boolean:
                if (canFastRead)
                {
                    ptr[index] = (bool)value ? (byte)1 : (byte)0;
                    ++index;
                }
                else
                    index += (uint)serializer.WriteObject((bool)value, ptr + index, size - index);
                break;

            case TypeCode.Int16:
                if (canFastRead)
                {
                    short i16 = (short)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, i16);
                    else
                    {
                        ptr[index + 1] = unchecked( (byte) i16 );
                        ptr[index]     = unchecked( (byte)(i16 >>> 8) );
                    }

                    index += 2;
                }
                else
                    index += (uint)serializer.WriteObject((short)value, ptr + index, size - index);
                break;

            case TypeCode.UInt16:
                if (canFastRead)
                {
                    ushort ui16 = (ushort)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, ui16);
                    else
                    {
                        ptr[index + 1] = unchecked( (byte) ui16 );
                        ptr[index]     = unchecked( (byte)(ui16 >>> 8) );
                    }

                    index += 2;
                }
                else
                    index += (uint)serializer.WriteObject((ushort)value, ptr + index, size - index);
                break;

            case TypeCode.Char:
                if (canFastRead)
                {
                    char c = (char)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, c);
                    else
                    {
                        ptr[index + 1] = unchecked( (byte) c );
                        ptr[index]     = unchecked( (byte)(c >>> 8) );
                    }

                    index += 2;
                }
                else
                    index += (uint)serializer.WriteObject((char)value, ptr + index, size - index);
                break;

            case TypeCode.Int32:
                if (canFastRead)
                {
                    int i32 = (int)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, i32);
                    else
                    {
                        ptr[index + 3] = unchecked( (byte) i32 );
                        ptr[index + 2] = unchecked( (byte)(i32 >>> 8) );
                        ptr[index + 1] = unchecked( (byte)(i32 >>> 16) );
                        ptr[index]     = unchecked( (byte)(i32 >>> 24) );
                    }

                    index += 4;
                }
                else
                    index += (uint)serializer.WriteObject((int)value, ptr + index, size - index);
                break;

            case TypeCode.UInt32:
                if (canFastRead)
                {
                    uint ui32 = (uint)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, ui32);
                    else
                    {
                        ptr[index + 3] = unchecked( (byte) ui32 );
                        ptr[index + 2] = unchecked( (byte)(ui32 >>> 8) );
                        ptr[index + 1] = unchecked( (byte)(ui32 >>> 16) );
                        ptr[index]     = unchecked( (byte)(ui32 >>> 24) );
                    }

                    index += 4;
                }
                else
                    index += (uint)serializer.WriteObject((uint)value, ptr + index, size - index);

                break;

            case TypeCode.Int64:
                if (canFastRead)
                {
                    long i64 = (long)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, i64);
                    else
                    {
                        ptr[index + 7] = unchecked( (byte) i64 );
                        ptr[index + 6] = unchecked( (byte)(i64 >>> 8) );
                        ptr[index + 5] = unchecked( (byte)(i64 >>> 16) );
                        ptr[index + 4] = unchecked( (byte)(i64 >>> 24) );
                        ptr[index + 3] = unchecked( (byte)(i64 >>> 32) );
                        ptr[index + 2] = unchecked( (byte)(i64 >>> 40) );
                        ptr[index + 1] = unchecked( (byte)(i64 >>> 48) );
                        ptr[index]     = unchecked( (byte)(i64 >>> 56) );
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((long)value, ptr + index, size - index);
                break;

            case TypeCode.UInt64:
                if (canFastRead)
                {
                    ulong ui64 = (ulong)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, ui64);
                    else
                    {
                        ptr[index + 7] = unchecked( (byte) ui64 );
                        ptr[index + 6] = unchecked( (byte)(ui64 >>> 8) );
                        ptr[index + 5] = unchecked( (byte)(ui64 >>> 16) );
                        ptr[index + 4] = unchecked( (byte)(ui64 >>> 24) );
                        ptr[index + 3] = unchecked( (byte)(ui64 >>> 32) );
                        ptr[index + 2] = unchecked( (byte)(ui64 >>> 40) );
                        ptr[index + 1] = unchecked( (byte)(ui64 >>> 48) );
                        ptr[index]     = unchecked( (byte)(ui64 >>> 56) );
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((ulong)value, ptr + index, size - index);

                break;

            case TypeCode.Single:
                if (canFastRead)
                {
                    float fl = (float)value;
                    int i32 = *(int*)&fl;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, i32);
                    else
                    {
                        ptr[index + 3] = unchecked( (byte) i32 );
                        ptr[index + 2] = unchecked( (byte)(i32 >>> 8) );
                        ptr[index + 1] = unchecked( (byte)(i32 >>> 16) );
                        ptr[index]     = unchecked( (byte)(i32 >>> 24) );
                    }

                    index += 4;
                }
                else
                    index += (uint)serializer.WriteObject((float)value, ptr + index, size - index);

                break;

            case TypeCode.Double:
                if (canFastRead)
                {
                    double dl = (double)value;
                    long i64 = *(long*)&dl;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, i64);
                    else
                    {
                        ptr[index + 7] = unchecked( (byte) i64);
                        ptr[index + 6] = unchecked( (byte)(i64 >>> 8) );
                        ptr[index + 5] = unchecked( (byte)(i64 >>> 16) );
                        ptr[index + 4] = unchecked( (byte)(i64 >>> 24) );
                        ptr[index + 3] = unchecked( (byte)(i64 >>> 32) );
                        ptr[index + 2] = unchecked( (byte)(i64 >>> 40) );
                        ptr[index + 1] = unchecked( (byte)(i64 >>> 48) );
                        ptr[index]     = unchecked( (byte)(i64 >>> 56) );
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((double)value, ptr + index, size - index);
                break;

            case TypeCode.Decimal:
                if (canFastRead)
                {
#if NET5_0_OR_GREATER
                    Span<int> bits = stackalloc int[4];
                    decimal.GetBits((decimal)value, bits);
#else
                    int[] bits = decimal.GetBits((decimal)value);
#endif

                    uint ind = index;
                    for (int i = 0; i < 4; ++i)
                    {
                        int bit = bits[i];
                        if (BitConverter.IsLittleEndian)
                        {
                            Unsafe.WriteUnaligned(ptr + ind, bit);
                        }
                        else
                        {
                            ptr[ind + 3] = unchecked( (byte) bit );
                            ptr[ind + 2] = unchecked( (byte)(bit >>> 8) );
                            ptr[ind + 1] = unchecked( (byte)(bit >>> 16) );
                            ptr[ind]     = unchecked( (byte)(bit >>> 24) );
                        }

                        ind += 4;
                    }

                    index += 16;
                }
                else
                    index += (uint)serializer.WriteObject((decimal)value, ptr + index, size - index);
                break;

            case TypeCode.DateTime:
                if (canFastRead)
                {
                    DateTime dateTime = (DateTime)value;
                    long dt = (long)dateTime.Kind << 62 | dateTime.Ticks;
                    
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, dt);
                    else
                    {
                        ptr[index + 7] = unchecked( (byte) dt );
                        ptr[index + 6] = unchecked( (byte)(dt >>> 8) );
                        ptr[index + 5] = unchecked( (byte)(dt >>> 16) );
                        ptr[index + 4] = unchecked( (byte)(dt >>> 24) );
                        ptr[index + 3] = unchecked( (byte)(dt >>> 32) );
                        ptr[index + 2] = unchecked( (byte)(dt >>> 40) );
                        ptr[index + 1] = unchecked( (byte)(dt >>> 48) );
                        ptr[index]     = unchecked( (byte)(dt >>> 56) );
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((DateTime)value, ptr + index, size - index);
                break;

            case TypeCodeTimeSpan:
                if (canFastRead)
                {
                    TimeSpan timeSpan = (TimeSpan)value;
                    long ticks = timeSpan.Ticks;

                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, ticks);
                    else
                    {
                        ptr[index + 7] = unchecked( (byte) ticks );
                        ptr[index + 6] = unchecked( (byte)(ticks >>> 8) );
                        ptr[index + 4] = unchecked( (byte)(ticks >>> 24) );
                        ptr[index + 5] = unchecked( (byte)(ticks >>> 16) );
                        ptr[index + 3] = unchecked( (byte)(ticks >>> 32) );
                        ptr[index + 2] = unchecked( (byte)(ticks >>> 40) );
                        ptr[index + 1] = unchecked( (byte)(ticks >>> 48) );
                        ptr[index]     = unchecked( (byte)(ticks >>> 56) );
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((TimeSpan)value, ptr + index, size - index);
                break;

            case TypeCodeGuid:
                if (canFastRead)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    ((Guid)value).TryWriteBytes(new Span<byte>(ptr + index, 16));
#else
                    byte[] data = ((Guid)value).ToByteArray();
                    Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(ptr + index), ref data[0], 16u);
#endif
                    index += 16;
                }
                else
                    index += (uint)serializer.WriteObject((Guid)value, ptr + index, size - index);
                break;

            case TypeCodeDateTimeOffset:
                if (canFastRead)
                {
                    DateTimeOffset dateTime = (DateTimeOffset)value;
                    long ticks = dateTime.Ticks;
                    short offset = (short)Math.Round(dateTime.Offset.TotalMinutes);
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(ptr + index, ticks);
                        Unsafe.WriteUnaligned(ptr + index + 8, offset);
                    }
                    else
                    {
                        ptr[index + 7] = unchecked( (byte) ticks );
                        ptr[index + 6] = unchecked( (byte)(ticks >>> 8) );
                        ptr[index + 5] = unchecked( (byte)(ticks >>> 16) );
                        ptr[index + 4] = unchecked( (byte)(ticks >>> 24) );
                        ptr[index + 3] = unchecked( (byte)(ticks >>> 32) );
                        ptr[index + 2] = unchecked( (byte)(ticks >>> 40) );
                        ptr[index + 1] = unchecked( (byte)(ticks >>> 48) );
                        ptr[index]     = unchecked( (byte)(ticks >>> 56) );

                        ptr[index + 9] = unchecked( (byte) offset);
                        ptr[index + 8] = unchecked( (byte)(offset >>> 8) );
                    }
                }
                else
                    index += (uint)serializer.WriteObject((DateTimeOffset)value, ptr + index, size - index);
                break;

            case TypeCodeIntPtr:
                if (canFastRead)
                {
                    long ui64 = (nint)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, ui64);
                    else
                    {
                        ptr[index + 7] = unchecked((byte)ui64);
                        ptr[index + 6] = unchecked((byte)(ui64 >>> 8));
                        ptr[index + 5] = unchecked((byte)(ui64 >>> 16));
                        ptr[index + 4] = unchecked((byte)(ui64 >>> 24));
                        ptr[index + 3] = unchecked((byte)(ui64 >>> 32));
                        ptr[index + 2] = unchecked((byte)(ui64 >>> 40));
                        ptr[index + 1] = unchecked((byte)(ui64 >>> 48));
                        ptr[index] = unchecked((byte)(ui64 >>> 56));
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((nint)value, ptr + index, size - index);

                break;

            case TypeCodeUIntPtr:
                if (canFastRead)
                {
                    ulong ui64 = (nuint)value;
                    if (BitConverter.IsLittleEndian)
                        Unsafe.WriteUnaligned(ptr + index, ui64);
                    else
                    {
                        ptr[index + 7] = unchecked((byte)ui64);
                        ptr[index + 6] = unchecked((byte)(ui64 >>> 8));
                        ptr[index + 5] = unchecked((byte)(ui64 >>> 16));
                        ptr[index + 4] = unchecked((byte)(ui64 >>> 24));
                        ptr[index + 3] = unchecked((byte)(ui64 >>> 32));
                        ptr[index + 2] = unchecked((byte)(ui64 >>> 40));
                        ptr[index + 1] = unchecked((byte)(ui64 >>> 48));
                        ptr[index] = unchecked((byte)(ui64 >>> 56));
                    }

                    index += 8;
                }
                else
                    index += (uint)serializer.WriteObject((nuint)value, ptr + index, size - index);

                break;

        }
    }

    public static unsafe object? ReadTypeCode(TypeCode tc, IRpcSerializer serializer, Stream stream, out int bytesRead)
    {
        if (tc == TypeCode.String)
        {
            return serializer.ReadObject<string?>(stream, out bytesRead);
        }

        bool canFastRead = serializer.CanFastReadPrimitives;
        if (!canFastRead)
        {
            switch (tc)
            {
                case TypeCode.DBNull:
                    bytesRead = 0;
                    return DBNull.Value;

                case TypeCode.SByte:
                    return serializer.ReadObject<sbyte>(stream, out bytesRead);

                case TypeCode.Byte:
                    return serializer.ReadObject<byte>(stream, out bytesRead);

                case TypeCode.Boolean:
                    return serializer.ReadObject<bool>(stream, out bytesRead);

                case TypeCode.Int16:
                    return serializer.ReadObject<short>(stream, out bytesRead);

                case TypeCode.UInt16:
                    return serializer.ReadObject<ushort>(stream, out bytesRead);

                case TypeCode.Char:
                    return serializer.ReadObject<char>(stream, out bytesRead);

                case TypeCode.Int32:
                    return serializer.ReadObject<int>(stream, out bytesRead);

                case TypeCode.UInt32:
                    return serializer.ReadObject<uint>(stream, out bytesRead);

                case TypeCode.Int64:
                    return serializer.ReadObject<long>(stream, out bytesRead);

                case TypeCode.UInt64:
                    return serializer.ReadObject<ulong>(stream, out bytesRead);

                case TypeCode.Single:
                    return serializer.ReadObject<float>(stream, out bytesRead);

                case TypeCode.Double:
                    return serializer.ReadObject<double>(stream, out bytesRead);

                case TypeCode.Decimal:
                    return serializer.ReadObject<decimal>(stream, out bytesRead);

                case TypeCode.DateTime:
                    return serializer.ReadObject<DateTime>(stream, out bytesRead);

                case TypeCodeTimeSpan:
                    return serializer.ReadObject<TimeSpan>(stream, out bytesRead);

                case TypeCodeGuid:
                    return serializer.ReadObject<Guid>(stream, out bytesRead);

                case TypeCodeDateTimeOffset:
                    return serializer.ReadObject<DateTimeOffset>(stream, out bytesRead);

                case TypeCodeIntPtr:
                    return serializer.ReadObject<nint>(stream, out bytesRead);

                case TypeCodeUIntPtr:
                    return serializer.ReadObject<nuint>(stream, out bytesRead);

                default:
                    bytesRead = 0;
                    return null!;
            }
        }

        int sz = GetTypeCodeSize(tc);
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        byte* ptr = stackalloc byte[sz];
        int rdCt = stream.Read(new Span<byte>(ptr, sz));
        bytesRead = rdCt;
        if (rdCt != sz)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        uint index = 0;
        return ReadTypeCode(tc, serializer, ptr, sz, ref index, out bytesRead);
#else
        byte[] buffer = DefaultSerializer.ArrayPool.Rent(sz);
        try
        {
            int rdCt = stream.Read(buffer, 0, sz);
            bytesRead = rdCt;
            if (rdCt != sz)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            object? rtnValue;
            switch (tc)
            {
                case TypeCode.DBNull:
                    bytesRead = 0;
                    rtnValue = DBNull.Value;
                    break;

                case TypeCode.SByte:
                    rtnValue = unchecked((sbyte)buffer[0]);
                    bytesRead = 1;
                    break;

                case TypeCode.Byte:
                    rtnValue = buffer[0];
                    bytesRead = 1;
                    break;

                case TypeCode.Boolean:
                    rtnValue = buffer[0] != 0;
                    bytesRead = 1;
                    break;

                case TypeCode.Int16:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<short>(ref buffer[0])
                        : unchecked((short)(buffer[0] << 8 | buffer[1]));
                    bytesRead = 2;
                    break;

                case TypeCode.UInt16:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ushort>(ref buffer[0])
                        : unchecked((ushort)(buffer[0] << 8 | buffer[1]));
                    bytesRead = 2;
                    break;

                case TypeCode.Char:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<char>(ref buffer[0])
                        : unchecked((char)(buffer[0] << 8 | buffer[1]));
                    bytesRead = 2;
                    break;

                case TypeCode.Int32:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(ref buffer[0])
                        : buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
                    bytesRead = 4;
                    break;

                case TypeCode.UInt32:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(ref buffer[0])
                        : (uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3];
                    bytesRead = 4;
                    break;

                case TypeCode.Int64:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref buffer[0])
                        : ((long)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);
                    bytesRead = 8;
                    break;

                case TypeCode.UInt64:
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ulong>(ref buffer[0])
                        : ((ulong)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);
                    bytesRead = 8;
                    break;

                case TypeCode.Single:
                    int i32 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(ref buffer[0])
                        : buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
                    rtnValue = *(float*)&i32;
                    bytesRead = 4;
                    break;

                case TypeCode.Double:
                    long i64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref buffer[0])
                        : ((long)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);
                    rtnValue = *(double*)&i64;
                    bytesRead = 8;
                    break;

                case TypeCode.Decimal:
                    int[] bits = new int[4];
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref bits[0]), ref buffer[0], 16);
                    }
                    else
                    {
                        bits[0] = buffer[00] << 24 | buffer[01] << 16 | buffer[02] << 8 | buffer[03];
                        bits[1] = buffer[04] << 24 | buffer[05] << 16 | buffer[06] << 8 | buffer[07];
                        bits[2] = buffer[08] << 24 | buffer[09] << 16 | buffer[10] << 8 | buffer[11];
                        bits[3] = buffer[12] << 24 | buffer[13] << 16 | buffer[14] << 8 | buffer[15];
                    }

                    rtnValue = new decimal(bits);
                    bytesRead = 16;
                    break;

                case TypeCode.DateTime:
                    long z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref buffer[0])
                        : ((long)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);
                    DateTimeKind kind = (DateTimeKind)((z64 >> 62) & 0b11);
                    z64 &= ~(0b11L << 62);
                    rtnValue = new DateTime(z64, kind);
                    bytesRead = 8;
                    break;

                case TypeCodeTimeSpan:
                    z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref buffer[0])
                        : ((long)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);
                    rtnValue = new TimeSpan(z64);
                    bytesRead = 8;
                    break;

                case TypeCodeGuid:
                    byte[] span = DefaultSerializer.ArrayPool.Rent(16);
                    try
                    {
                        Unsafe.CopyBlockUnaligned(ref span[0], ref buffer[0], 16u);
                        rtnValue = new Guid(span);
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(span);
                    }
                    bytesRead = 16;
                    break;

                case TypeCodeDateTimeOffset:
                    z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref buffer[0])
                        : ((long)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);

                    short offset = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<short>(ref buffer[8])
                        : unchecked((short)(buffer[8] << 8 | buffer[9]));

                    rtnValue = new DateTimeOffset(z64, TimeSpan.FromMinutes(offset));
                    bytesRead = 10;
                    break;

                case TypeCodeIntPtr:
                    z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref buffer[0])
                        : ((long)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);

                    if (IntPtr.Size < 8)
                    {
                        if (z64 is > int.MaxValue or < int.MinValue)
                            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, "UIntPtr"));
                    }
                    rtnValue = (nint)z64;
                    bytesRead = 8;
                    break;

                case TypeCodeUIntPtr:
                    ulong u64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ulong>(ref buffer[0])
                        : ((ulong)((uint)buffer[0] << 24 | (uint)buffer[1] << 16 | (uint)buffer[2] << 8 | buffer[3]) << 32) | ((uint)buffer[4] << 24 | (uint)buffer[5] << 16 | (uint)buffer[6] << 8 | buffer[7]);

                    if (IntPtr.Size < 8)
                    {
                        if (u64 > uint.MaxValue)
                            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, "UIntPtr"));
                    }
                    rtnValue = (nuint)u64;

                    bytesRead = 8;
                    break;

                default:
                    rtnValue = null!;
                    bytesRead = 0;
                    break;
            }

            return rtnValue;
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(buffer);
        }
#endif
    }

    public static unsafe object? ReadTypeCode(TypeCode tc, IRpcSerializer serializer, byte* data, int maxSize, ref uint index, out int bytesRead)
    {
        if (tc == TypeCode.String)
        {
            return serializer.ReadObject<string?>(data + index, (uint)maxSize - index, out bytesRead);
        }

        bool canFastRead = serializer.CanFastReadPrimitives;
        int sz = GetTypeCodeSize(tc);
        if (canFastRead && tc != TypeCode.String && maxSize - index < sz)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

        object? rtnValue;
        switch (tc)
        {
            case TypeCode.DBNull:
                bytesRead = 0;
                rtnValue = DBNull.Value;
                break;

            case TypeCode.SByte:
                if (canFastRead)
                {
                    rtnValue = unchecked( (sbyte)data[index] );
                    bytesRead = 1;
                }
                else
                    rtnValue = serializer.ReadObject<sbyte>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.Byte:
                if (canFastRead)
                {
                    rtnValue = data[index];
                    bytesRead = 1;
                }
                else
                    rtnValue = serializer.ReadObject<byte>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.Boolean:
                if (canFastRead)
                {
                    rtnValue = data[index] != 0;
                    bytesRead = 1;
                }
                else
                    rtnValue = serializer.ReadObject<bool>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.Int16:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<short>(data + index)
                        : unchecked( (short)(data[index] << 8 | data[index + 1]) );
                    bytesRead = 2;
                }
                else
                    rtnValue = serializer.ReadObject<short>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.UInt16:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ushort>(data + index)
                        : unchecked( (ushort)(data[index] << 8 | data[index + 1]) );
                    bytesRead = 2;
                }
                else
                    rtnValue = serializer.ReadObject<ushort>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.Char:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<char>(data + index)
                        : unchecked( (char)(data[index] << 8 | data[index + 1]) );
                    bytesRead = 2;
                }
                else
                    rtnValue = serializer.ReadObject<char>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.Int32:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(data + index)
                        : data[index] << 24 | data[index + 1] << 16 | data[index + 2] << 8 | data[index + 3];
                    bytesRead = 4;
                }
                else
                    rtnValue = serializer.ReadObject<int>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.UInt32:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(data + index)
                        : (uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3];
                    bytesRead = 4;
                }
                else
                    rtnValue = serializer.ReadObject<uint>(data + index, (uint)maxSize - index, out bytesRead);

                break;

            case TypeCode.Int64:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(data + index)
                        : unchecked( ((long)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                    bytesRead = 8;
                }
                else
                    rtnValue = serializer.ReadObject<long>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.UInt64:
                if (canFastRead)
                {
                    rtnValue = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ulong>(data + index)
                        : unchecked( ((ulong)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                    bytesRead = 8;
                }
                else
                    rtnValue = serializer.ReadObject<ulong>(data + index, (uint)maxSize - index, out bytesRead);

                break;

            case TypeCode.Single:
                if (canFastRead)
                {
                    int i32 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(data + index)
                        : data[index] << 24 | data[index + 1] << 16 | data[index + 2] << 8 | data[index + 3];
                    rtnValue = *(float*)&i32;
                    bytesRead = 4;
                }
                else
                    rtnValue = serializer.ReadObject<float>(data + index, (uint)maxSize - index, out bytesRead);

                break;

            case TypeCode.Double:
                if (canFastRead)
                {
                    long i64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(data + index)
                        : unchecked( ((long)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                    rtnValue = *(double*)&i64;
                    bytesRead = 8;
                }
                else
                    rtnValue = serializer.ReadObject<double>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCode.Decimal:
                if (!canFastRead)
                {
                    rtnValue = serializer.ReadObject<decimal>(data + index, (uint)maxSize - index, out bytesRead);
                    break;
                }

#if NET5_0_OR_GREATER
                int* bits = stackalloc int[4];
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.CopyBlockUnaligned(bits, data + index, 16);
                }
#else
                int[] bits = new int[4];
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref bits[0]), ref Unsafe.AsRef<byte>(data + index), 16);
                }
#endif
                else
                {
                    bits[0] = data[index + 00] << 24 | data[index + 01] << 16 | data[index + 02] << 8 | data[index + 03];
                    bits[1] = data[index + 04] << 24 | data[index + 05] << 16 | data[index + 06] << 8 | data[index + 07];
                    bits[2] = data[index + 08] << 24 | data[index + 09] << 16 | data[index + 10] << 8 | data[index + 11];
                    bits[3] = data[index + 12] << 24 | data[index + 13] << 16 | data[index + 14] << 8 | data[index + 15];
                }

#if NET5_0_OR_GREATER
                rtnValue = new decimal(new ReadOnlySpan<int>(bits, 4));
#else
                rtnValue = new decimal(bits);
#endif
                bytesRead = 16;
                break;

            case TypeCode.DateTime:
                if (!canFastRead)
                {
                    rtnValue = serializer.ReadObject<DateTime>(data + index, (uint)maxSize - index, out bytesRead);
                    break;
                }

                long z64 = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<long>(data + index)
                    : unchecked( ((long)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                DateTimeKind kind = (DateTimeKind)((z64 >> 62) & 0b11);
                z64 &= ~(0b11L << 62);
                rtnValue = new DateTime(z64, kind);
                bytesRead = 8;
                break;

            case TypeCodeTimeSpan:
                if (!canFastRead)
                {
                    rtnValue = serializer.ReadObject<TimeSpan>(data + index, (uint)maxSize - index, out bytesRead);
                    break;
                }

                z64 = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<long>(data + index)
                    : unchecked( ((long)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                rtnValue = new TimeSpan(z64);
                bytesRead = 8;
                break;

            case TypeCodeGuid:
                if (!canFastRead)
                {
                    rtnValue = serializer.ReadObject<Guid>(data + index, (uint)maxSize - index, out bytesRead);
                    break;
                }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                rtnValue = new Guid(new ReadOnlySpan<byte>(data + index, 16));
#else
                byte[] span = DefaultSerializer.ArrayPool.Rent(16);
                try
                {
                    Unsafe.CopyBlockUnaligned(ref span[0], ref Unsafe.AsRef<byte>(data + index), 16u);
                    rtnValue = new Guid(span);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(span);
                }
#endif
                bytesRead = 16;
                break;

            case TypeCodeDateTimeOffset:
                if (!canFastRead)
                {
                    rtnValue = serializer.ReadObject<DateTimeOffset>(data + index, (uint)maxSize - index, out bytesRead);
                    break;
                }

                z64 = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<long>(data + index)
                    : unchecked( ((long)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );

                short offset = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<short>(data + index + 8)
                    : unchecked( (short)(data[index + 8] << 8 | data[index + 9]) );

                rtnValue = new DateTimeOffset(z64, TimeSpan.FromMinutes(offset));
                bytesRead = 10;
                break;

            case TypeCodeIntPtr:
                if (canFastRead)
                {
                    z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(data + index)
                        : unchecked( ((long)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                    bytesRead = 8;
                    if (IntPtr.Size < 8)
                    {
                        if (z64 is > int.MaxValue or < int.MinValue)
                            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, "UIntPtr"));
                    }

                    rtnValue = (nint)z64;
                }
                else
                    rtnValue = serializer.ReadObject<nint>(data + index, (uint)maxSize - index, out bytesRead);
                break;

            case TypeCodeUIntPtr:
                if (canFastRead)
                {
                    ulong u64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ulong>(data + index)
                        : unchecked( ((ulong)((uint)data[index] << 24 | (uint)data[index + 1] << 16 | (uint)data[index + 2] << 8 | data[index + 3]) << 32) | ((uint)data[index + 4] << 24 | (uint)data[index + 5] << 16 | (uint)data[index + 6] << 8 | data[index + 7]) );
                    bytesRead = 8;
                    if (IntPtr.Size < 8)
                    {
                        if (u64 > uint.MaxValue)
                            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, "UIntPtr"));
                    }

                    rtnValue = (nuint)u64;
                }
                else
                    rtnValue = serializer.ReadObject<nuint>(data + index, (uint)maxSize - index, out bytesRead);

                break;

            default:
                rtnValue = null!;
                bytesRead = 0;
                break;
        }

        index += (uint)bytesRead;
        return rtnValue;
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
            method = decoratingMethod!;
            return decoratingMethod != null;
        }

        if (declaringType == null
            && declaringTypeName == null
            && decoratingMethod?.DeclaringType is { } methodDeclaringType
            && methodDeclaringType.TryGetAttributeSafe(out RpcClassAttribute classAttribute)
            && (classAttribute.DefaultType != null || !string.IsNullOrEmpty(classAttribute.DefaultTypeName)))
        {
            declaringType = classAttribute.DefaultType;
            declaringTypeName = classAttribute.DefaultTypeName;
        }

        if (
            decoratingMethod != null
            // is this attribute referencing itself (decoratingMethod).
            && methodName!.Equals(decoratingMethod.Name)
            && (declaringType == null || declaringType == decoratingMethod.DeclaringType)
            && (string.IsNullOrEmpty(declaringTypeName) || decoratingMethod.DeclaringType != null && CompareAssemblyQualifiedNameNoVersion(decoratingMethod.DeclaringType, declaringTypeName!))
            && (parameterTypes == null || ParametersMatchMethod(decoratingMethod, parameterTypes, parametersAreBindedParametersOnly))
            && (parameterTypeNames == null || ParametersMatchMethod(decoratingMethod, parameterTypeNames, parametersAreBindedParametersOnly)))
        {
            result = ResolveMethodResult.IsSelfTarget;
            method = decoratingMethod;
            return true;
        }

        method = null!;
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
            method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
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
                method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, CallingConventions.Any, parameterTypes, null)!;
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

    public static TimeSpan GetTimeoutFromMethod(MethodBase method, TimeSpan defaultTimeout)
    {
        RpcTimeoutAttribute? attribue = method.GetAttributeSafe<RpcTimeoutAttribute>();
        if (attribue == null)
        {
            if (method.DeclaringType == null)
                return defaultTimeout;

            attribue = method.DeclaringType.GetAttributeSafe<RpcTimeoutAttribute>();
            if (attribue == null)
                return defaultTimeout;
        }

        return attribue.Timeout < 0
            ? Timeout.InfiniteTimeSpan
            : TimeSpan.FromMilliseconds(attribue.Timeout);
    }

    internal static object? GetServiceFromUnknownProviderType(object serviceProvider, Type declaringType)
    {
        if (serviceProvider is not IEnumerable<IServiceProvider> multiple)
        {
            return serviceProvider is IServiceProvider sp ? GetService(sp, declaringType) : null;
        }

        bool isIntx = declaringType.IsInterface;

        object? intxMatch = null;
        foreach (IServiceProvider provider in multiple)
        {
            object? service = GetService(provider, declaringType, checkInterfaces: false);
            if (service != null)
                return service;

            if (!isIntx)
                intxMatch ??= GetService(provider, declaringType, checkDeclaringType: false);
        }

        return intxMatch;
    }

    internal static object? GetService(IServiceProvider serviceProvider, Type declaringType, bool checkDeclaringType = true, bool checkInterfaces = true)
    {
        if (checkDeclaringType)
        {
            object? service = serviceProvider.GetService(declaringType);
            if (service != null)
                return service;
        }

        if (!checkInterfaces)
            return null;

        if (!ServiceInterfaces.TryGetValue(declaringType, out Type[] intx))
        {
            // try to get service by it's interfaces (cached in ServiceInterfaces).
            intx = ServiceInterfaces.GetOrAdd(declaringType, static declaringType =>
            {
                RpcServiceTypeAttribute[] attributes = declaringType.GetAttributesSafe<RpcServiceTypeAttribute>();
                if (attributes.Length == 0)
                    return declaringType.GetInterfaces();

                int ct = attributes.Count(attribute =>
                    attribute.ServiceType != null
                    && attribute.ServiceType != declaringType
                    && attribute.ServiceType.IsAssignableFrom(declaringType)
                );

                if (ct == 0)
                    return Type.EmptyTypes;

                Type[] attrArray = new Type[ct];
                for (int i = attributes.Length - 1; i >= 0; --i)
                {
                    RpcServiceTypeAttribute attribute = attributes[i];

                    if (attribute.ServiceType != null
                        && attribute.ServiceType != declaringType
                        && attribute.ServiceType.IsAssignableFrom(declaringType))
                    {
                        attrArray[--ct] = attribute.ServiceType;
                    }
                }

                return attrArray;
            });
        }

        for (int i = 0; i < intx.Length; ++i)
        {
            object? service = serviceProvider.GetService(intx[i]);
            if (service != null)
                return service;
        }

        return null;
    }

    public static bool ParametersMatchParameters(Type?[]? testParamTypes, string[]? testParamTypeNames, bool testParametersAreBindedParametersOnly, string[] actualParamTypeNames, bool actualParametersAreBindedParametersOnly)
    {
        bool matchExact = false;
        bool anyNull = testParamTypes == null;
        if (!anyNull)
        {
            for (int i = 0; i < testParamTypes!.Length; ++i)
            {
                if (testParamTypes[i] != null)
                    continue;

                anyNull = true;
                break;
            }
        }

        int len = anyNull ? testParamTypeNames!.Length : testParamTypes!.Length;
        if (testParametersAreBindedParametersOnly == actualParametersAreBindedParametersOnly)
        {
            if (len != actualParamTypeNames.Length)
                return false;

            matchExact = true;
        }

        if (matchExact)
        {
            if (anyNull)
            {
                for (int i = 0; i < len; ++i)
                {
                    if (!string.Equals(testParamTypeNames![i], actualParamTypeNames[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }
            else
            {
                for (int i = 0; i < len; ++i)
                {
                    Type testParamType = testParamTypes![i];
                    if (!CompareAssemblyQualifiedNameNoVersion(testParamType.IsByRef ? testParamType.GetElementType()! : testParamType, actualParamTypeNames[i]))
                    {
                        return false;
                    }
                }
            }
        }
        else if (actualParametersAreBindedParametersOnly)
        {
            len = actualParamTypeNames.Length;
            int ind = -1;
            if (anyNull)
            {
                for (int i = 0; i < len; ++i)
                {
                    do
                    {
                        ++ind;
                        if (ind >= testParamTypeNames!.Length)
                            return false;
                    }
                    while (!string.Equals(testParamTypeNames[ind], actualParamTypeNames[i], StringComparison.Ordinal));
                }
            }
            else
            {
                for (int i = 0; i < len; ++i)
                {
                    do
                    {
                        ++ind;
                        if (ind >= testParamTypes!.Length)
                            return false;
                    }
                    while (!CompareAssemblyQualifiedNameNoVersion(testParamTypes[i].IsByRef ? testParamTypes[i].GetElementType()! : testParamTypes[i], actualParamTypeNames[i]));
                }
            }
        }
        else // if (actualParametersAreBindedParametersOnly)
        {
            int ind = -1;
            if (anyNull)
            {
                for (int i = 0; i < len; ++i)
                {
                    do
                    {
                        ++ind;
                        if (ind >= actualParamTypeNames.Length)
                            return false;
                    }
                    while (!string.Equals(testParamTypeNames![i], actualParamTypeNames[ind], StringComparison.Ordinal));
                }
            }
            else
            {
                for (int i = 0; i < len; ++i)
                {
                    Type testParamType = testParamTypes![i];
                    do
                    {
                        ++ind;
                        if (ind >= testParamTypes!.Length)
                            return false;
                    }
                    while (!CompareAssemblyQualifiedNameNoVersion(testParamType.IsByRef ? testParamType.GetElementType()! : testParamType, actualParamTypeNames[ind]));
                }
            }
        }

        return true;
    }

    public static PropertyInfo? GetImplementedProperty(Type type, PropertyInfo interfaceProperty)
    {
        MethodInfo? accessor = interfaceProperty.GetMethod;
        bool isSet;
        if (accessor == null)
        {
            accessor = interfaceProperty.SetMethod;
            isSet = true;

            if (accessor == null)
            {
                return null;
            }
        }
        else
        {
            isSet = false;
        }

        MethodInfo? declaredAccessor = Accessor.GetImplementedMethod(type, accessor);
        if (declaredAccessor == null)
        {
            return accessor.IsVirtual && (isSet || interfaceProperty.SetMethod is not { IsVirtual: false }) ? interfaceProperty : null;
        }

        if (declaredAccessor.Name.StartsWith(isSet ? "set_" : "get_"))
        {
            string likelyPropertyName = declaredAccessor.Name.Substring(4);
            if (likelyPropertyName.Length != 0)
            {
                Type[] indexProperties = Type.EmptyTypes;
                ParameterInfo[] parameters = interfaceProperty.GetIndexParameters();
                if (parameters.Length > 0)
                {
                    indexProperties = new Type[parameters.Length];
                    for (int i = 0; i < indexProperties.Length; ++i)
                        indexProperties[i] = parameters[i].ParameterType;
                }

                PropertyInfo? property = type.GetProperty(
                    likelyPropertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    interfaceProperty.PropertyType,
                    indexProperties,
                    null
                );
                if (property != null && (isSet ? property.SetMethod : property.GetMethod) == declaredAccessor)
                {
                    return property;
                }
            }
        }

        PropertyInfo[] allProperties = type.GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        for (int i = 0; i < allProperties.Length; ++i)
        {
            PropertyInfo property = allProperties[i];
            if ((isSet ? property.SetMethod : property.GetMethod) == declaredAccessor)
            {
                return property;
            }
        }

        return null;
    }


    private static class TypeCodeCache<TValue>
    {
        public static readonly TypeCode Value = GetTypeCode(typeof(TValue));
    }

    public static MethodInfo? FindDeclaredMethodByName(Type declaringType, string methodName, Type[]? parameters, BindingFlags flags)
    {
        MethodInfo? refMethod;
        try
        {
            refMethod = declaringType.GetMethod(
                methodName,
                flags | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
            );
        }
        catch (AmbiguousMatchException)
        {
            if (parameters == null)
                return null;

            try
            {
                refMethod = declaringType.GetMethod(
                    methodName,
                    flags | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null,
                    CallingConventions.Any,
                    parameters,
                    null
                );
            }
            catch (AmbiguousMatchException)
            {
                refMethod = null;
            }
        }

        return refMethod;
    }

    /// <summary>
    /// Compare two assembly-qualified type names.
    /// </summary>
    /// <remarks>Stolen from <see href="https://github.com/DanielWillett/unturned-asset-file-vscode/blob/master/UnturnedAssetSpec/QualifiedType.cs"/>.</remarks>
    public static bool TypesEqual(ReadOnlySpan<char> left, ReadOnlySpan<char> right, bool caseInsensitive = false)
    {
        StringComparison c = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (left.Equals(right, c))
            return true;

        if (left.IsEmpty || right.IsEmpty)
            return false;

        if (!ExtractParts(left, out ReadOnlySpan<char> typeNameLeft, out ReadOnlySpan<char> assemblyNameLeft)
            || !ExtractParts(right, out ReadOnlySpan<char> typeNameRight, out ReadOnlySpan<char> assemblyNameRight))
        {
            return false;
        }

        return typeNameLeft.Equals(typeNameRight, c) && assemblyNameLeft.Equals(assemblyNameRight, c);
    }


    /// <summary>
    /// Extract the type name and assembly name from an assembly-qualified full type name.
    /// </summary>
    public static bool ExtractParts(ReadOnlySpan<char> assemblyQualifiedTypeName, out ReadOnlySpan<char> fullTypeName, out ReadOnlySpan<char> assemblyName)
    {
        bool isLookingForAssemblyName = false;
        if (assemblyQualifiedTypeName.IndexOfAny('[', ']') != -1)
        {
            fullTypeName = default;
            assemblyName = default;

            int genericDepth = 0;
            int escapeDepth = 0;

            // more advanced check taking generic types into consideration
            // ex: System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]], mscorlib
            for (int i = 0; i < assemblyQualifiedTypeName.Length; ++i)
            {
                char c = assemblyQualifiedTypeName[i];

                if (c is '[' or ']')
                {
                    if (escapeDepth > 0 && escapeDepth % 2 == 1)
                    {
                        escapeDepth = 0;
                        continue;
                    }

                    escapeDepth = 0;
                    if (!fullTypeName.IsEmpty)
                    {
                        fullTypeName = default;
                        return false;
                    }

                    if (c == '[')
                    {
                        ++genericDepth;
                        continue;
                    }

                    --genericDepth;
                    if (genericDepth < 0)
                    {
                        fullTypeName = default;
                        assemblyName = default;
                        return false;
                    }
                }

                if (c == '\\')
                {
                    ++escapeDepth;
                    continue;
                }

                if (genericDepth != 0 || c != ',')
                {
                    escapeDepth = 0;
                    continue;
                }

                if (escapeDepth > 0 && escapeDepth % 2 == 1)
                {
                    escapeDepth = 0;
                    continue;
                }

                escapeDepth = 0;

                if (isLookingForAssemblyName)
                {
                    assemblyName = assemblyName.Slice(0, i).TrimEnd();
                    return !assemblyName.IsEmpty;
                }

                fullTypeName = assemblyQualifiedTypeName.Slice(0, i).Trim();
                if (fullTypeName.IsEmpty)
                    return false;
                assemblyQualifiedTypeName = assemblyQualifiedTypeName.Slice(i + 1).Trim();
                assemblyName = assemblyQualifiedTypeName;
                isLookingForAssemblyName = true;
            }

            if (genericDepth > 0)
            {
                fullTypeName = default;
                assemblyName = default;
            }
        }
        else
        {
            fullTypeName = default;
            assemblyName = default;
            if (assemblyQualifiedTypeName.Length == 0)
                return false;

            int lastIndex = -1;
            while (true)
            {
                ++lastIndex;
                if (lastIndex >= assemblyQualifiedTypeName.Length)
                    break;
                int nextInd = assemblyQualifiedTypeName.Slice(lastIndex).IndexOf(',');
                if (nextInd <= 0)
                    break;

                nextInd += lastIndex;

                if (IsEscaped(assemblyQualifiedTypeName, nextInd))
                {
                    lastIndex = nextInd;
                    continue;
                }

                if (isLookingForAssemblyName)
                {
                    assemblyName = assemblyName.Slice(0, nextInd).TrimEnd();
                    return !assemblyName.IsEmpty;
                }

                fullTypeName = assemblyQualifiedTypeName.Slice(0, nextInd).Trim();
                if (fullTypeName.IsEmpty)
                    return false;
                assemblyQualifiedTypeName = assemblyQualifiedTypeName.Slice(nextInd + 1).Trim();
                assemblyName = assemblyQualifiedTypeName;
                isLookingForAssemblyName = true;
                lastIndex = 0;
            }
        }

        return !assemblyName.IsEmpty;
    }

    private static bool IsEscaped(ReadOnlySpan<char> text, int index)
    {
        if (index == 0)
            return false;

        int slashCt = 0;
        while (index > 0 && text[index - 1] == '\\')
        {
            ++slashCt;
            --index;
        }

        return slashCt % 2 == 1;
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