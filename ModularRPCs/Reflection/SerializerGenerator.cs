using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Data;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using DanielWillett.SpeedBytes;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;
internal sealed class SerializerGenerator
{
    // update source generator when updating this
    private static readonly Type[] AutoInjectedTypes =
    [
        typeof(IRpcInvocationPoint),
        typeof(CancellationToken),
        typeof(RpcOverhead),
        typeof(IRpcRouter),
        typeof(IRpcSerializer),
        typeof(IModularRpcConnection),
        typeof(IEnumerable<IModularRpcConnection>),
        typeof(RpcFlags),
        typeof(IServiceProvider),
        typeof(IEnumerable<IServiceProvider>)
    ];

    private static readonly Type? SpeedBytesWriterType = Type.GetType("DanielWillett.SpeedBytes.ByteWriter, DanielWillett.SpeedBytes");
    private static readonly Type? SpeedBytesReaderType = Type.GetType("DanielWillett.SpeedBytes.ByteReader, DanielWillett.SpeedBytes");

    // ReSharper disable once UseArrayEmptyMethod (no reason to make new instance of static class for GenericTypeParameterBuilder)
    private static readonly GenericTypeParameterBuilder[] EmptyTypeParams = new GenericTypeParameterBuilder[0];
    private readonly ConcurrentDictionary<int, Type> _argBuilders = new ConcurrentDictionary<int, Type>();
    private readonly ConcurrentDictionary<RuntimeMethodHandle, int> _methodSigHashCache = new ConcurrentDictionary<RuntimeMethodHandle, int>();
    private readonly ProxyGenerator _proxyGenerator;

    private static readonly Dictionary<Type, Type> _closureTypes = new Dictionary<Type, Type>(16);

    internal const string GetSizeMethodField = "GetSizeMethod";
    internal const string WriteToStreamMethodField = "WriteStreamMethod";
    internal const string WriteToBytesMethodField = "WriteBytesMethod";
    internal SerializerGenerator(ProxyGenerator proxyGenerator)
    {
        _proxyGenerator = proxyGenerator;
    }
    public Type GetSerializerType(int argCt)
    {
        if (_argBuilders.TryGetValue(argCt, out Type? t))
            return t;
        return _argBuilders.GetOrAdd(argCt, CreateType);
    }
    internal int GetBindingMethodSignatureHash(MethodBase method)
    {
        if (method == null)
            return 0;

        if (_methodSigHashCache.TryGetValue(method.MethodHandle, out int t))
            return t;
        return _methodSigHashCache.GetOrAdd(method.MethodHandle, CreateMethodSignature);
    }
    private Type CreateType(int typeCt)
    {
        string typePrefix = "<" + typeCt.ToString(CultureInfo.InvariantCulture) + ">";

        TypeBuilder typeBuilder = _proxyGenerator.ModuleBuilder.DefineType("SerializerType" + typePrefix, TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public);

        string[] genParamNames = new string[typeCt];
        GenericTypeParameterBuilder[] genParams;
        Type[] currentGenericParameters;
        if (typeCt != 0)
        {
            for (int i = 0; i < typeCt; ++i)
                genParamNames[i] = "T" + i.ToString(CultureInfo.InvariantCulture);

            genParams = typeBuilder.DefineGenericParameters(genParamNames);

            currentGenericParameters = new Type[genParams.Length];
            for (int i = 0; i < genParams.Length; ++i)
                currentGenericParameters[i] = genParams[i].UnderlyingSystemType;
        }
        else
        {
            genParams = EmptyTypeParams;
            currentGenericParameters = Type.EmptyTypes;
        }

        ConstructorBuilder typeInitializer = typeBuilder.DefineTypeInitializer();
        
        MethodInfo getTypeFromHandleMethod = Accessor.GetMethod(Type.GetTypeFromHandle)!;

        IOpCodeEmitter il = typeInitializer.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);
        LocalBuilder lclTypeArray = il.DeclareLocal(typeof(Type[]));

        il.CommentIfDebug("Type[] lclTypeArray = new Type[typeCt];");
        il.Emit(OpCodes.Ldc_I4, typeCt);
        il.Emit(OpCodes.Newarr, typeof(Type));
        il.Emit(OpCodes.Stloc, lclTypeArray);
        for (int i = 0; i < typeCt; ++i)
        {
            il.CommentIfDebug("lclTypeArray[i] = typeof(" + Accessor.Formatter.Format(genParams[i]) + ");");
            il.Emit(OpCodes.Ldloc, lclTypeArray);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldtoken, genParams[i]);
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Stelem_Ref);
        }

        // get size
        Type[] getSizeParamsArray = GetTypeParams(currentGenericParameters, 1, 0, false);
        getSizeParamsArray[0] = typeof(IRpcSerializer);

        // write to byte array
        Type[] writeBytesParamsArray = GetTypeParams(currentGenericParameters, 3, 0, false);
        writeBytesParamsArray[0] = typeof(IRpcSerializer);
        writeBytesParamsArray[1] = typeof(byte*);
        writeBytesParamsArray[2] = typeof(uint);

        // write to stream
        Type[] writeStreamParamsArray = GetTypeParams(currentGenericParameters, 2, 0, false);
        writeStreamParamsArray[0] = typeof(IRpcSerializer);
        writeStreamParamsArray[1] = typeof(Stream);

        Type getSizeDelegateType = DelegateUtility.CreateDelegateType("HandleGetSize" + typePrefix, null, genParamNames, typeof(int), getSizeParamsArray, [ "serializer" ], null, _proxyGenerator);
        Type writeBytesDelegateType = DelegateUtility.CreateDelegateType("HandleWriteBytes" + typePrefix, null, genParamNames, typeof(int), writeBytesParamsArray, [ "serializer", "bytes", "maxSize" ], null, _proxyGenerator);
        Type writeStreamDelegateType = DelegateUtility.CreateDelegateType("HandleWriteStream" + typePrefix, null, genParamNames, typeof(int), writeStreamParamsArray, [ "serializer", "stream" ], null, _proxyGenerator);

        typeBuilder.DefineField(GetSizeMethodField, getSizeDelegateType, FieldAttributes.Static | FieldAttributes.Public);
        typeBuilder.DefineField(WriteToBytesMethodField, writeBytesDelegateType, FieldAttributes.Static | FieldAttributes.Public);
        typeBuilder.DefineField(WriteToStreamMethodField, writeStreamDelegateType, FieldAttributes.Static | FieldAttributes.Public);

        Type thisType = currentGenericParameters.Length > 0
            ? typeBuilder.MakeGenericType(currentGenericParameters)
            : typeBuilder;

        if (Compatibility.IncompatibleWithIgnoresAccessChecksToAttribute)
        {
            LocalBuilder lclObjArray = il.DeclareLocal(typeof(object[]));
            // create array for invoking InitTypeStatic method

            il.CommentIfDebug("object[] lclObjArray = new object[2];");
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, lclObjArray);
            il.Emit(OpCodes.Ldloc, lclObjArray);

            il.CommentIfDebug("lclObjArray[0] = typeof(" + Accessor.Formatter.Format(thisType) + ");");
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldtoken, thisType);
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Stelem_Ref);
            il.Emit(OpCodes.Ldloc, lclObjArray);

            il.CommentIfDebug("lclObjArray[1] = lclTypeArray;");
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldloc, lclTypeArray);
            il.Emit(OpCodes.Stelem_Ref);

            // invoke InitTypeStatic method via reflection since it's private
            il.CommentIfDebug("_ = typeof(SerializerGenerator).GetMethod(\"InitTypeStatic\", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, lclObjArray);");
            il.Emit(OpCodes.Ldtoken, typeof(SerializerGenerator));
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Ldstr, nameof(InitTypeStatic));
            il.Emit(OpCodes.Ldc_I4, (int)(BindingFlags.NonPublic | BindingFlags.Static));
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.TypeGetMethodNameFlags);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, lclObjArray);
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.MethodBaseInvoke);
            il.Emit(OpCodes.Pop);
        }
        else
        {
            // invoke InitTypeStatic
            il.CommentIfDebug("SerializerGenerator.InitTypeStatic(typeof(" + Accessor.Formatter.Format(thisType) + "), lclTypeArray);");
            il.Emit(OpCodes.Ldtoken, thisType);
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Ldloc, lclTypeArray);
            il.Emit(OpCodes.Call, Accessor.GetMethod(InitTypeStatic)!);
        }
        il.Emit(OpCodes.Ret);

#if NETSTANDARD2_0
        return typeBuilder.CreateTypeInfo()!;
#else
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        return typeBuilder.CreateType()!;
#endif
    }
    [UsedImplicitly]
    internal static void InitTypeStatic(Type thisType, Type[] genTypes)
    {
        ProxyGenerator.Instance.SerializerGenerator.InitType(thisType, genTypes);
    }

    internal static bool IsRpcSerializableType(Type type)
    {
        return type.IsDefinedSafe<RpcSerializableAttribute>() && typeof(IRpcSerializable).IsAssignableFrom(type);
    }

    internal static bool IsSerializableArray(Type type, out Type? elementType)
    {
        if (!type.IsArray)
        {
            elementType = null;
            return false;
        }

        elementType = type.GetElementType()!;
        return typeof(IRpcSerializable).IsAssignableFrom(elementType)
               && elementType.IsDefinedSafe<RpcSerializableAttribute>()
               && elementType.MakeArrayType() == type;
    }

    internal static bool IsPrimitiveLikeType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
#if NET5_0_OR_GREATER
               || type == typeof(Half)
#endif
#if NET7_0_OR_GREATER
               || type == typeof(UInt128)
               || type == typeof(Int128)
#endif
            ;
    }
    internal static Type[] GetTypeParams(Type[] genTypes, int countBefore, int countAfter, bool nonPrim, bool allTypesByRef = true)
    {
        Type[] parameterTypes;
        if (!nonPrim)
        {
            parameterTypes = new Type[genTypes.Length + countBefore + countAfter];
            Array.Copy(genTypes, 0, parameterTypes, countBefore, genTypes.Length);
            if (allTypesByRef)
            {
                for (int i = countBefore; i < parameterTypes.Length - countAfter; ++i)
                {
                    ref Type parameterType = ref parameterTypes[i];
                    if (!parameterType.IsByRef)
                        parameterType = parameterType.MakeByRefType();
                }
            }
            return parameterTypes;
        }

        int primitiveTypes = 0;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            if (IsPrimitiveLikeType(genTypes[i]))
                ++primitiveTypes;
        }

        parameterTypes = new Type[genTypes.Length + countBefore + countAfter - primitiveTypes];
        int index = -1;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type parameterType = genTypes[i];
            if (!IsPrimitiveLikeType(parameterType))
            {
                parameterTypes[++index + countBefore] = !parameterType.IsByRef ? parameterType.MakeByRefType() : parameterType;
            }
        }

        return parameterTypes;
    }
    internal static bool CanQuickSerializeType(Type type)
    {
        if (type == typeof(byte) || type == typeof(sbyte))
            return true;

        Type? underlyingType = null;

        if (IntPtr.Size != 8)
        {
            if (type == typeof(nint) || type == typeof(nuint))
                return false;

            if (type.IsEnum)
            {
                underlyingType = type.GetEnumUnderlyingType();
                if (underlyingType == typeof(nint) || underlyingType == typeof(nuint))
                    return false;
            }
        }

        if (type.IsEnum)
        {
            underlyingType ??= type.GetEnumUnderlyingType();
            if (!BitConverter.IsLittleEndian)
                return underlyingType == typeof(byte) || underlyingType == typeof(sbyte);

            return true;
        }

        return BitConverter.IsLittleEndian && type.IsPrimitive;
    }

    internal static bool ShouldBePassedByReference(Type type)
    {
        return type.IsValueType
               && !(IsPrimitiveLikeType(type) || type == typeof(Guid) || type == typeof(decimal))
               && Nullable.GetUnderlyingType(type) == null;
    }

    internal static int GetPrimitiveTypeSize(Type type)
    {
        if (type == typeof(byte) || type == typeof(bool) || type == typeof(sbyte))
        {
            return 1;
        }

        if (type == typeof(short) || type == typeof(ushort) || type == typeof(char))
        {
            return 2;
        }

        if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
        {
            return 4;
        }

        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
        {
            return 8;
        }

        if (type == typeof(decimal))
        {
            return 16;
        }

        if (type == typeof(DateTime) || type == typeof(TimeSpan))
        {
            return 8;
        }

        if (type == typeof(DateTimeOffset))
        {
            return 10;
        }

        if (type == typeof(Guid))
        {
            return 16;
        }

#if NET5_0_OR_GREATER
        if (type == typeof(Half))
        {
            return 2;
        }
#endif

#if NET7_0_OR_GREATER
        if (type == typeof(UInt128) || type == typeof(Int128))
        {
            return 16;
        }
#endif

        if (type == typeof(nint) || type == typeof(nuint))
        {
            return 8;
        }

        if (type.IsEnum)
            return GetPrimitiveTypeSize(type.GetEnumUnderlyingType());

        throw new ArgumentException("Not a primitve type.");
    }
    private void InitType(Type thisType, Type[] genTypes)
    {
        MakeGetSizeMethod(thisType, genTypes);
        MakeWriteMethods(thisType, genTypes);
    }
    private void MakeWriteMethods(Type thisType, Type[] genTypes)
    {
        FieldInfo bytesField = thisType.GetField(WriteToBytesMethodField, BindingFlags.Public | BindingFlags.Static)
                          ?? throw new UnexpectedMemberAccessException(new FieldDefinition(WriteToBytesMethodField)
                              .DeclaredIn(thisType, isStatic: true)
                              .WithFieldType<Delegate>()
                          );

        FieldInfo streamField = thisType.GetField(WriteToStreamMethodField, BindingFlags.Public | BindingFlags.Static)
                          ?? throw new UnexpectedMemberAccessException(new FieldDefinition(WriteToStreamMethodField)
                              .DeclaredIn(thisType, isStatic: true)
                              .WithFieldType<Delegate>()
                          );

        Type[] bytesParamsArray = GetTypeParams(genTypes, 3, 0, false);
        bytesParamsArray[0] = typeof(IRpcSerializer);
        bytesParamsArray[1] = typeof(byte*);
        bytesParamsArray[2] = typeof(uint);

        Type[] streamParamsArray = GetTypeParams(genTypes, 2, 0, false);
        streamParamsArray[0] = typeof(IRpcSerializer);
        streamParamsArray[1] = typeof(Stream);

        /* WRITE TO BYTE PTR */

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);
        DynamicMethod writeBytes = new DynamicMethod("Write", attr, conv, typeof(int), bytesParamsArray, _proxyGenerator.ModuleBuilder, true)
        {
            InitLocals = false
        };

        DefineParams(writeBytes, genTypes, 3);
        writeBytes.DefineParameter(1, ParameterAttributes.None, "serializer");
        writeBytes.DefineParameter(2, ParameterAttributes.None, "bytes");
        writeBytes.DefineParameter(3, ParameterAttributes.None, "maxSize");

        IOpCodeEmitter il = writeBytes.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);
        LocalBuilder lclPreCalc = il.DeclareLocal(typeof(bool));
        LocalBuilder lclSize = il.DeclareLocal(typeof(int));

        il.CommentIfDebug("bool lclPreCalc = serializer.PreCalculatePrimitiveSizes;");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerCanFastReadPrimitives);
        il.Emit(OpCodes.Stloc, lclPreCalc);

        il.CommentIfDebug("int lclSize = 0;");
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, lclSize);

        Label? lblNotPrimitive = null;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
            ushort argIndex = checked ( (ushort)(i + 3) );
            TypeSerializationInfo info = new TypeSerializationInfo(genType);
            il.CommentIfDebug($"== Write {Accessor.Formatter.Format(genType)} ==");
            if (lblNotPrimitive.HasValue)
            {
                il.CommentIfDebug("lblNotPrimitive:");
                il.MarkLabel(lblNotPrimitive.Value);
                lblNotPrimitive = null;
            }

            Label? lblPrimitive;
            if (info.Type == TypeSerializationInfoType.PrimitiveLike)
            {
                il.CommentIfDebug("if (lclPreCalc) goto lblPrimitive;");
                lblPrimitive = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, lclPreCalc);
                il.Emit(OpCodes.Brtrue, lblPrimitive.Value);
            }
            else lblPrimitive = null;

            il.Emit(OpCodes.Ldarg_0);

            // the arg is by ref
            il.Emit(OpCodes.Ldarg, argIndex);

            MethodInfo writeMtd;
            switch (info.Type)
            {
                case TypeSerializationInfoType.PrimitiveLike:
                case TypeSerializationInfoType.Value:
                    il.CommentIfDebug($"size += serializer.WriteObject<{genType}>(arg{i}, bytes + index, maxSize - index);");
                    LoadFromRef(genType, il);
                    writeMtd = CommonReflectionCache.RpcSerializerWriteObjectByValBytes.MakeGenericMethod(genType);
                    break;

                case TypeSerializationInfoType.NullableValue:
                    il.CommentIfDebug($"size += serializer.WriteNullableObject<{genType}>(in arg{i}, bytes + index, maxSize - index);");
                    writeMtd = CommonReflectionCache.RpcSerializerWriteNullableObjectByValBytes.MakeGenericMethod(info.UnderlyingType);
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    il.CommentIfDebug($"size += serializer.WriteSerializableObject<{info.SerializableType}>(in arg{i}, bytes + index, maxSize - index);");
                    writeMtd = CommonReflectionCache.RpcSerializerWriteSerializableObjectBytes.MakeGenericMethod(info.SerializableType);
                    break;

                case TypeSerializationInfoType.NullableSerializableValue:
                    il.CommentIfDebug($"size += serializer.WriteNullableSerializableObject<{info.UnderlyingType}>(in arg{i}, bytes + index, maxSize - index);");
                    writeMtd = CommonReflectionCache.RpcSerializerWriteNullableSerializableObjectBytes.MakeGenericMethod(info.UnderlyingType);
                    break;

                case TypeSerializationInfoType.SerializableCollection:
                    il.CommentIfDebug($"size += serializer.WriteSerializableObjects<{info.SerializableType}>(in arg{i}, bytes + index, maxSize - index);");
                    LoadFromRef(genType, il);
                    if (genType.IsValueType)
                        il.Emit(OpCodes.Box, genType);
                    writeMtd = CommonReflectionCache.RpcSerializerWriteSerializableObjectsBytes.MakeGenericMethod(info.SerializableType);
                    break;

                case TypeSerializationInfoType.NullableSerializableCollection:
                    il.CommentIfDebug($"size += serializer.WriteNullableSerializableObjects<{info.UnderlyingType}>(arg{i}, bytes + index, maxSize - index);");
                    LoadFromRef(genType, il);
                    if (genType.IsValueType)
                        il.Emit(OpCodes.Box, genType);
                    writeMtd = CommonReflectionCache.RpcSerializerWriteNullableSerializableObjectsBytes.MakeGenericMethod(info.SerializableType);
                    break;

                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                    MethodInfo callHasValue = CommonReflectionCache.GetNullableHasValueGetter(genType);

                    MethodInfo callGetValue = CommonReflectionCache.GetNullableValueGetter(genType);

                    if (info.Type == TypeSerializationInfoType.NullableCollectionNullableSerializableCollection)
                        il.CommentIfDebug($"size += serializer.WriteNullableSerializableObjects<{info.UnderlyingType}>(arg{i} ?? null, bytes + index, maxSize - index);");
                    else
                        il.CommentIfDebug($"size += serializer.WriteSerializableObjects<{info.SerializableType}>(arg{i} ?? null, bytes + index, maxSize - index);");

                    il.Emit(OpCodes.Dup);

                    il.Emit(OpCodes.Call, callHasValue);
                    Label hasValue = il.DefineLabel();
                    Label doesntHaveValue = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue_S, hasValue);

                    // if (!arg{i}.HasValue) {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Br, doesntHaveValue);

                    // } else {
                    il.MarkLabel(hasValue);
                    il.Emit(OpCodes.Call, callGetValue);
                    il.Emit(OpCodes.Box, info.UnderlyingType);
                    il.MarkLabel(doesntHaveValue);
                    // }
                    if (info.Type == TypeSerializationInfoType.NullableCollectionSerializableCollection)
                    {
                        writeMtd = CommonReflectionCache.RpcSerializerWriteSerializableObjectsBytes
                            .MakeGenericMethod(info.UnderlyingType);
                    }
                    else
                    {
                        writeMtd = CommonReflectionCache.RpcSerializerWriteNullableSerializableObjectsBytes
                            .MakeGenericMethod(info.SerializableType);
                    }

                    break;

                default:
                    throw new InvalidOperationException($"Invalid type: {info.Type}."); // shouldn't be reached
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Sub_Ovf_Un);

            il.Emit(OpCodes.Callvirt, writeMtd);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclSize);

            if (!lblPrimitive.HasValue)
                continue;

            lblNotPrimitive = il.DefineLabel();
            il.CommentIfDebug("goto lblNotPrimitive;");
            il.Emit(OpCodes.Br, lblNotPrimitive.Value);
            
            il.CommentIfDebug("lblPrimitive:");
            il.MarkLabel(lblPrimitive.Value);
            
            il.CommentIfDebug($"unaligned {{ *({Accessor.Formatter.Format(genType)}*)(bytes + lclSize) = arg{i.ToString(CultureInfo.InvariantCulture)}; }}");
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldarg, checked ( (ushort)(i + 3) ));
            LoadFromRef(genType, il);
            il.Emit(OpCodes.Unaligned, (byte)1);
            SetToRef(genType, il);
            
            il.CommentIfDebug($"lclSize += sizeof({Accessor.Formatter.Format(genType)});");
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Ldc_I4, GetPrimitiveTypeSize(genType));
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclSize);
        }

        if (lblNotPrimitive.HasValue)
        {
            il.CommentIfDebug("lblNotPrimitive:");
            il.MarkLabel(lblNotPrimitive.Value);
        }

        il.CommentIfDebug("return lclSize;");
        il.Emit(OpCodes.Ldloc, lclSize);
        il.Emit(OpCodes.Ret);

        bytesField.SetValue(null, writeBytes.CreateDelegate(bytesField.FieldType));

        /* WRITE TO STREAM */

        DynamicMethod writeStream = new DynamicMethod("Write", attr, conv, typeof(int), streamParamsArray, _proxyGenerator.ModuleBuilder, true)
        {
            InitLocals = false
        };

        DefineParams(writeStream, genTypes, 2);
        writeStream.DefineParameter(1, ParameterAttributes.None, "serializer");
        writeStream.DefineParameter(2, ParameterAttributes.None, "stream");

        il = writeStream.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);
        lclPreCalc = il.DeclareLocal(typeof(bool));
        lclSize = il.DeclareLocal(typeof(int));
        
        il.CommentIfDebug("bool lclPreCalc = serializer.PreCalculatePrimitiveSizes;");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerCanFastReadPrimitives);
        il.Emit(OpCodes.Stloc, lclPreCalc);
        
        il.CommentIfDebug("int lclSize = 0;");
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, lclSize);
        
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
            il.CommentIfDebug($"== Write {Accessor.Formatter.Format(genType)} ==");

            TypeSerializationInfo info = new TypeSerializationInfo(genType);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, checked ( (ushort)(i + 2) ));
        
            MethodInfo writeMtd;
            switch (info.Type)
            {
                case TypeSerializationInfoType.PrimitiveLike:
                case TypeSerializationInfoType.Value:
                    il.CommentIfDebug($"size += serializer.WriteObject<{genType}>(arg{i}, stream);");
                    LoadFromRef(genType, il);
                    writeMtd = CommonReflectionCache.RpcSerializerWriteObjectByValStream.MakeGenericMethod(genType);
                    break;

                case TypeSerializationInfoType.NullableValue:
                    il.CommentIfDebug($"size += serializer.WriteNullableObject<{genType}>(in arg{i}, stream);");
                    writeMtd = CommonReflectionCache.RpcSerializerWriteNullableObjectByValStream.MakeGenericMethod(info.UnderlyingType);
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    il.CommentIfDebug($"size += serializer.WriteSerializableObject<{info.SerializableType}>(in arg{i}, stream);");
                    writeMtd = CommonReflectionCache.RpcSerializerWriteSerializableObjectStream.MakeGenericMethod(info.SerializableType);
                    break;

                case TypeSerializationInfoType.NullableSerializableValue:
                    il.CommentIfDebug($"size += serializer.WriteNullableSerializableObject<{info.UnderlyingType}>(in arg{i}, stream);");
                    writeMtd = CommonReflectionCache.RpcSerializerWriteNullableSerializableObjectStream.MakeGenericMethod(info.UnderlyingType);
                    break;

                case TypeSerializationInfoType.SerializableCollection:
                    il.CommentIfDebug($"size += serializer.WriteSerializableObjects<{info.SerializableType}>(in arg{i}, stream);");
                    LoadFromRef(genType, il);
                    if (genType.IsValueType)
                        il.Emit(OpCodes.Box, genType);
                    writeMtd = CommonReflectionCache.RpcSerializerWriteSerializableObjectsStream.MakeGenericMethod(info.SerializableType);
                    break;

                case TypeSerializationInfoType.NullableSerializableCollection:
                    il.CommentIfDebug($"size += serializer.WriteNullableSerializableObjects<{info.UnderlyingType}>(in arg{i}, stream);");
                    LoadFromRef(genType, il);
                    if (genType.IsValueType)
                        il.Emit(OpCodes.Box, genType);
                    writeMtd = CommonReflectionCache.RpcSerializerWriteNullableSerializableObjectsStream.MakeGenericMethod(info.SerializableType);
                    break;

                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                    MethodInfo callHasValue = CommonReflectionCache.GetNullableHasValueGetter(genType);

                    MethodInfo callGetValue = CommonReflectionCache.GetNullableValueGetter(genType);

                    if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        il.CommentIfDebug($"size += serializer.WriteNullableSerializableObjects<{info.UnderlyingType}>(arg{i} ?? null, stream);");
                    else
                        il.CommentIfDebug($"size += serializer.WriteSerializableObjects<{info.SerializableType}>(arg{i} ?? null, stream);");

                    il.Emit(OpCodes.Dup);

                    il.Emit(OpCodes.Call, callHasValue);
                    Label hasValue = il.DefineLabel();
                    Label doesntHaveValue = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue_S, hasValue);

                    // if (!arg{i}.HasValue) {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Br, doesntHaveValue);

                    // } else {
                    il.MarkLabel(hasValue);
                    il.Emit(OpCodes.Call, callGetValue);
                    il.Emit(OpCodes.Box, info.UnderlyingType);
                    il.MarkLabel(doesntHaveValue);
                    // }
                    if (info.Type == TypeSerializationInfoType.NullableCollectionSerializableCollection)
                    {
                        writeMtd = CommonReflectionCache.RpcSerializerWriteSerializableObjectsStream
                            .MakeGenericMethod(info.UnderlyingType);
                    }
                    else
                    {
                        writeMtd = CommonReflectionCache.RpcSerializerWriteNullableSerializableObjectsStream
                            .MakeGenericMethod(info.SerializableType);
                    }

                    break;

                default:
                    throw new InvalidOperationException($"Invalid type: {info.Type}."); // shouldn't be reached
            }


            il.Emit(OpCodes.Ldarg_1);
        
            il.Emit(OpCodes.Callvirt, writeMtd);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclSize);
        }
        
        il.CommentIfDebug("return lclSize;");
        il.Emit(OpCodes.Ldloc, lclSize);
        il.Emit(OpCodes.Ret);

        streamField.SetValue(null, writeStream.CreateDelegate(streamField.FieldType));
    }

    internal static void LoadFromRef(Type type, IOpCodeEmitter il)
    {
        if (type == typeof(long) || type == typeof(ulong))
            il.Emit(OpCodes.Ldind_I8);
        else if (type == typeof(int))
            il.Emit(OpCodes.Ldind_I4);
        else if (type == typeof(uint))
            il.Emit(OpCodes.Ldind_U4);
        else if (type == typeof(short))
            il.Emit(OpCodes.Ldind_I2);
        else if (type == typeof(ushort) || type == typeof(char))
            il.Emit(OpCodes.Ldind_U2);
        else if (type == typeof(sbyte))
            il.Emit(OpCodes.Ldind_I1);
        else if (type == typeof(byte) || type == typeof(bool))
            il.Emit(OpCodes.Ldind_U1);
        else if (type == typeof(float))
            il.Emit(OpCodes.Ldind_R4);
        else if (type == typeof(double))
            il.Emit(OpCodes.Ldind_R8);
        else if (type == typeof(nint))
            il.Emit(OpCodes.Ldind_I);
        else if (type == typeof(nuint))
            il.Emit(OpCodes.Ldind_I);
        else if (type.IsValueType)
        {
            if (!type.IsEnum)
                il.Emit(OpCodes.Ldobj, type);
            else
                LoadFromRef(type.GetEnumUnderlyingType(), il);
        }
        else
            il.Emit(OpCodes.Ldind_Ref);
    }
    internal static void SetToRef(Type type, IOpCodeEmitter il)
    {
        if (type == typeof(long) || type == typeof(ulong))
            il.Emit(OpCodes.Stind_I8);
        else if (type == typeof(int) || type == typeof(uint))
            il.Emit(OpCodes.Stind_I4);
        else if (type == typeof(short) || type == typeof(ushort))
            il.Emit(OpCodes.Stind_I2);
        else if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(bool))
            il.Emit(OpCodes.Stind_I1);
        else if (type == typeof(float))
            il.Emit(OpCodes.Stind_R4);
        else if (type == typeof(double))
            il.Emit(OpCodes.Stind_R8);
        else if (type == typeof(nint) || type == typeof(nuint))
            il.Emit(OpCodes.Stind_I);
        else if (type.IsValueType)
        {
            if (!type.IsEnum)
                il.Emit(OpCodes.Stobj, type);
            else
                SetToRef(type.GetEnumUnderlyingType(), il);
        }
        else
            il.Emit(OpCodes.Stind_Ref);
    }
    private void MakeGetSizeMethod(Type thisType, Type[] genTypes)
    {
        FieldInfo field = thisType.GetField(GetSizeMethodField, BindingFlags.Public | BindingFlags.Static)
                          ?? throw new UnexpectedMemberAccessException(new FieldDefinition(GetSizeMethodField)
                              .DeclaredIn(thisType, isStatic: true)
                              .WithFieldType<Delegate>()
                          );

        Type[] paramsArray = GetTypeParams(genTypes, 1, 0, false);
        paramsArray[0] = typeof(IRpcSerializer);

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);
        DynamicMethod getSize = new DynamicMethod("GetSize", attr, conv, typeof(int), paramsArray, _proxyGenerator.ModuleBuilder, true);

        DefineParams(getSize, genTypes, 1);
        getSize.DefineParameter(1, ParameterAttributes.None, "serializer");
        IOpCodeEmitter il = getSize.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);

        LocalBuilder lclPreCalc = il.DeclareLocal(typeof(bool));
        LocalBuilder lclSize = il.DeclareLocal(typeof(int));
#if DEBUG
        LocalBuilder lclSizeSingle = il.DeclareLocal(typeof(int));
#endif

        il.CommentIfDebug("bool lclPreCalc = serializer.PreCalculatePrimitiveSizes;");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerCanFastReadPrimitives);
        il.Emit(OpCodes.Stloc, lclPreCalc);
        int ttl = 0;
        for (int i = 0; i < genTypes.Length; i++)
        {
            if (IsPrimitiveLikeType(genTypes[i]))
                ttl += GetPrimitiveTypeSize(genTypes[i]);
        }

        il.CommentIfDebug($"int lclSize = {ttl.ToString(CultureInfo.InvariantCulture)} * (int)lclPreCalc;");
        il.Emit(OpCodes.Ldc_I4, ttl);
        il.Emit(OpCodes.Ldloc, lclPreCalc);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Stloc, lclSize);

        Label? lblPrimitive = null;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            ushort argIndex = checked ( (ushort)(i + 1) );
            Type genType = genTypes[i];
            il.CommentIfDebug($"== Get size of {Accessor.Formatter.Format(genType)} ==");
            if (lblPrimitive.HasValue)
            {
                il.CommentIfDebug("lblPrimitive:");
                il.MarkLabel(lblPrimitive.Value);
            }
            TypeSerializationInfo info = new TypeSerializationInfo(genType);
            if (info.Type == TypeSerializationInfoType.PrimitiveLike)
            {
                il.CommentIfDebug("if (lclPreCalc) goto lblPrimitive;");
                lblPrimitive = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, lclPreCalc);
                il.Emit(OpCodes.Brtrue, lblPrimitive.Value);
            }
            else lblPrimitive = null;

            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, argIndex);

            switch (info.Type)
            {
                case TypeSerializationInfoType.Value:
                case TypeSerializationInfoType.PrimitiveLike:
                    il.CommentIfDebug($"size += serializer.GetSize<{genType}>(arg{i});");

                    LoadFromRef(genType, il);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeByVal
                        .MakeGenericMethod(genType));
                    break;

                case TypeSerializationInfoType.NullableValue:
                    il.CommentIfDebug($"size += serializer.GetSize<{info.UnderlyingType}>(in arg{i});");

                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeNullableByVal
                        .MakeGenericMethod(info.UnderlyingType));
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    il.CommentIfDebug($"size += serializer.GetSerializableSize<{info.SerializableType}>(in arg{i});");

                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSerializableObjectSize
                        .MakeGenericMethod(info.SerializableType));
                    break;

                case TypeSerializationInfoType.NullableSerializableValue:
                    il.CommentIfDebug($"size += serializer.GetNullableSerializableSize<{info.SerializableType}>(in arg{i});");

                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetNullableSerializableObjectSize
                        .MakeGenericMethod(info.SerializableType));
                    break;

                case TypeSerializationInfoType.SerializableCollection:
                case TypeSerializationInfoType.NullableSerializableCollection:
                    if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        il.CommentIfDebug($"size += serializer.GetNullableSerializablesSize<{info.UnderlyingType}>(arg{i});");
                    else
                        il.CommentIfDebug($"size += serializer.GetSerializablesSize<{info.SerializableType}>(arg{i});");

                    LoadFromRef(genType, il);
                    if (genType.IsValueType)
                        il.Emit(OpCodes.Box, genType);
                    if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                    {
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetNullableSerializableObjectsSize
                            .MakeGenericMethod(info.UnderlyingType));
                    }
                    else
                    {
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSerializableObjectsSize
                            .MakeGenericMethod(info.SerializableType));
                    }
                    break;

                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                    MethodInfo callHasValue = CommonReflectionCache.GetNullableHasValueGetter(genType);

                    MethodInfo callGetValue = CommonReflectionCache.GetNullableValueGetter(genType);

                    if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        il.CommentIfDebug($"size += serializer.GetNullableSerializablesSize<{info.UnderlyingType}>(arg{i} ?? null);");
                    else
                        il.CommentIfDebug($"size += serializer.GetSerializablesSize<{info.SerializableType}>(arg{i} ?? null);");

                    il.Emit(OpCodes.Dup);

                    il.Emit(OpCodes.Call, callHasValue);
                    Label hasValue = il.DefineLabel();
                    Label doesntHaveValue = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue_S, hasValue);

                    // if (!arg{i}.HasValue) {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Br, doesntHaveValue);

                    // } else {
                    il.MarkLabel(hasValue);
                    il.Emit(OpCodes.Call, callGetValue);
                    il.Emit(OpCodes.Box, info.UnderlyingType);
                    il.MarkLabel(doesntHaveValue);
                    // }
                    if (info.Type == TypeSerializationInfoType.NullableCollectionSerializableCollection)
                    {
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSerializableObjectsSize
                            .MakeGenericMethod(info.UnderlyingType));
                    }
                    else
                    {
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetNullableSerializableObjectsSize
                            .MakeGenericMethod(info.SerializableType));
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Invalid type: {info.Type}."); // shouldn't be reached
            }

#if DEBUG
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, lclSizeSingle);
#endif

            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclSize);

#if DEBUG
            il.Emit(OpCodes.Ldstr, genType.Name);
            il.Emit(OpCodes.Call, new Action<string>(Console.WriteLine).Method);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Call, new Action<int>(Console.WriteLine).Method);
            il.Emit(OpCodes.Ldloc, lclSizeSingle);
            il.Emit(OpCodes.Call, new Action<int>(Console.WriteLine).Method);
#endif
        }

        if (lblPrimitive.HasValue)
        {
            il.CommentIfDebug("lblPrimitive:");
            il.MarkLabel(lblPrimitive.Value);
        }

        il.CommentIfDebug("return lclSize;");
        il.Emit(OpCodes.Ldloc, lclSize);
        il.Emit(OpCodes.Ret);

        field.SetValue(null, getSize.CreateDelegate(field.FieldType));
    }
    private static void DefineParams(object methodBuilder, Type[] genTypes, int countBefore, bool valueTypesByRef = true)
    {
        int pInd = countBefore;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];

            string name = "arg" + i.ToString(CultureInfo.InvariantCulture);

            ParameterAttributes attr = valueTypesByRef && ShouldBePassedByReference(genType) ? ParameterAttributes.In : ParameterAttributes.None;

            if (methodBuilder is MethodBuilder methodBuilder2)
                methodBuilder2.DefineParameter(++pInd, attr, name);
            else if (methodBuilder is DynamicMethod dynamicMethod)
                dynamicMethod.DefineParameter(++pInd, attr, name);
        }
    }

    /// <summary>
    /// Splits parameters into a segment of injected parameters and a segment of binded parameters.
    /// </summary>
    internal static void BindParameters(ParameterInfo[] parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind)
    {
        if (parameters.Length == 0)
        {
            toInject = default;
            toBind = default;
            return;
        }

        BitArray injectionMask = new BitArray(parameters.Length);
        int injectionParams = 0;
        int bindParams = 0;
        int numBindToInjTransitions = 0;
        int numInjToBindTransitions = 0;
        for (int i = 0; i < parameters.Length; ++i)
        {
            ParameterInfo parameterInfo = parameters[i];

            bool isInjected = Array.Exists(AutoInjectedTypes, x => x != typeof(object) && x != typeof(ValueType) && x.IsAssignableFrom(parameterInfo.ParameterType))
                              || parameterInfo.IsDefinedSafe<RpcInjectAttribute>();

            injectionMask[i] = isInjected;
            injectionParams += isInjected ? 1 : 0;
            bindParams += isInjected ? 0 : 1;

            if (i == 0)
                continue;

            if (isInjected && !injectionMask[i - 1])
                ++numBindToInjTransitions;
            else if (!isInjected && injectionMask[i - 1])
                ++numInjToBindTransitions;
        }

        if (bindParams == 0)
        {
            toBind = default;
            toInject = new ArraySegment<ParameterInfo>(parameters);
            return;
        }
        if (injectionParams == 0)
        {
            toInject = default;
            toBind = new ArraySegment<ParameterInfo>(parameters);
            return;
        }

        if (numBindToInjTransitions + numInjToBindTransitions == 1)
        {
            bool isInjFirst = injectionMask[0];
            int swapIndex = -1;
            for (int i = 1; i < parameters.Length; ++i)
            {
                if (injectionMask[i] == isInjFirst)
                    continue;

                swapIndex = i;
                break;
            }

            if (isInjFirst)
            {

                toBind = new ArraySegment<ParameterInfo>(parameters, swapIndex, parameters.Length - swapIndex);
                toInject = new ArraySegment<ParameterInfo>(parameters, 0, swapIndex);
            }
            else
            {
                toInject = new ArraySegment<ParameterInfo>(parameters, swapIndex, parameters.Length - swapIndex);
                toBind = new ArraySegment<ParameterInfo>(parameters, 0, swapIndex);
            }

            return;
        }

        if (numBindToInjTransitions + numInjToBindTransitions == 2)
        {
            bool isInjPart = injectionMask[0];
            int lenPart = isInjPart ? injectionParams : bindParams;
            int lenWhole = parameters.Length - lenPart;
            ParameterInfo[] halfArr = new ParameterInfo[lenPart];
            int swapIndex = -1;
            for (int i = 1; i < parameters.Length; ++i)
            {
                if (injectionMask[i] == isInjPart)
                    continue;

                swapIndex = i;
                break;
            }

            Array.Copy(parameters, 0, halfArr, 0, swapIndex);
            Array.Copy(parameters, swapIndex + lenWhole, halfArr, halfArr.Length - swapIndex, swapIndex);
            if (isInjPart)
            {
                toInject = new ArraySegment<ParameterInfo>(halfArr);
                toBind = new ArraySegment<ParameterInfo>(parameters, swapIndex, lenWhole);
            }
            else
            {
                toBind = new ArraySegment<ParameterInfo>(halfArr);
                toInject = new ArraySegment<ParameterInfo>(parameters, swapIndex, lenWhole);
            }

            return;
        }

        ParameterInfo[] toInj = new ParameterInfo[injectionParams];
        ParameterInfo[] toBnd = new ParameterInfo[bindParams];
        int injInd = -1, bndInd = -1;
        for (int i = 0; i < parameters.Length; ++i)
        {
            if (injectionMask[i])
                toInj[++injInd] = parameters[i];
            else
                toBnd[++bndInd] = parameters[i];
        }

        toBind = new ArraySegment<ParameterInfo>(toBnd);
        toInject = new ArraySegment<ParameterInfo>(toInj);
    }
    private static void HandleInjections(MethodBase method, IOpCodeEmitter il, ArraySegment<ParameterInfo> toInject, LocalBuilder[] injectionLcls, bool isBytes)
    {
        Type[] paramArray = isBytes ? ProxyGenerator.RpcInvokeHandlerBytesParams : ProxyGenerator.RpcInvokeHandlerStreamParams;
        for (int i = 0; i < toInject.Count; ++i)
        {
            ParameterInfo param = toInject.Array![i + toInject.Offset];
            Type injectionType = param.ParameterType;

            LocalBuilder lcl = il.DeclareLocal(injectionType);
            injectionLcls[i] = lcl;

            if (injectionType == typeof(object) || injectionType == typeof(ValueType))
            {
                throw new RpcInjectionException(param, method);
            }

            if (injectionType == typeof(CancellationToken))
            {
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(CancellationToken)) ));
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (injectionType.CouldBeAssignedTo<RpcOverhead>())
            {
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));

                if (!injectionType.IsAssignableFrom(typeof(RpcOverhead)))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IRpcInvocationPoint).IsAssignableFrom(injectionType))
            {
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));

                il.Emit(CommonReflectionCache.RpcOverheadGetRpc.GetCallRuntime(), CommonReflectionCache.RpcOverheadGetRpc);

                if (injectionType != typeof(IRpcInvocationPoint))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (injectionType == typeof(RpcFlags))
            {
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));

                il.Emit(CommonReflectionCache.RpcOverheadGetFlags.GetCallRuntime(), CommonReflectionCache.RpcOverheadGetFlags);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IRpcSerializer).IsAssignableFrom(injectionType))
            {
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(IRpcSerializer)) ));

                if (injectionType != typeof(IRpcSerializer))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IRpcRouter).IsAssignableFrom(injectionType))
            {
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(IRpcRouter)) ));

                if (injectionType != typeof(IRpcRouter))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IModularRpcConnection).IsAssignableFrom(injectionType))
            {
                if (typeof(IModularRpcLocalConnection).IsAssignableFrom(injectionType))
                {
                    il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));

                    il.Emit(CommonReflectionCache.RpcOverheadGetReceivingConnection.GetCallRuntime(), CommonReflectionCache.RpcOverheadGetReceivingConnection);

                    if (!injectionType.IsAssignableFrom(typeof(IModularRpcLocalConnection)))
                        il.Emit(OpCodes.Castclass, injectionType);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));

                    il.Emit(CommonReflectionCache.RpcOverheadGetSendingConnection.GetCallRuntime(), CommonReflectionCache.RpcOverheadGetSendingConnection);

                    if (!injectionType.IsAssignableFrom(typeof(IModularRpcRemoteConnection)))
                        il.Emit(OpCodes.Castclass, injectionType);
                }
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IEnumerable<IModularRpcConnection>).IsAssignableFrom(injectionType))
            {
                if (typeof(IEnumerable<IModularRpcLocalConnection>).IsAssignableFrom(injectionType))
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Newarr, typeof(IModularRpcLocalConnection));
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));
                    il.Emit(CommonReflectionCache.RpcOverheadGetReceivingConnection.GetCallRuntime(), CommonReflectionCache.RpcOverheadGetReceivingConnection);
                    il.Emit(OpCodes.Stelem_Ref);

                    if (!injectionType.IsAssignableFrom(typeof(IModularRpcLocalConnection[])))
                        il.Emit(OpCodes.Castclass, injectionType);
                }
                else
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Newarr, typeof(IModularRpcRemoteConnection));
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(paramArray, typeof(RpcOverhead)) ));
                    il.Emit(CommonReflectionCache.RpcOverheadGetSendingConnection.GetCallRuntime(), CommonReflectionCache.RpcOverheadGetSendingConnection);
                    il.Emit(OpCodes.Stelem_Ref);

                    if (!injectionType.IsAssignableFrom(typeof(IModularRpcRemoteConnection[])))
                        il.Emit(OpCodes.Castclass, injectionType);
                }
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (injectionType == typeof(IServiceProvider))
            {
                Label next = il.DefineLabel();
                Label notEither = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_0); // service provider (maybe)
                il.Emit(OpCodes.Isinst, injectionType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lcl);
                il.Emit(OpCodes.Brtrue, next);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Brfalse, notEither);
                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionMultipleServiceProviders);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(notEither);
                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInfo);
                il.Emit(OpCodes.Ldstr, param.Name ?? string.Empty);
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(injectionType));
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                il.Emit(OpCodes.Call, CommonReflectionCache.StringFormat3);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(next);
            }
            else if (injectionType == typeof(IEnumerable<IServiceProvider>))
            {
                Label next = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_0); // service provider (maybe)
                il.Emit(OpCodes.Isinst, injectionType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lcl);
                il.Emit(OpCodes.Brtrue, next);

                Label throwError = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Isinst, typeof(IServiceProvider));
                il.Emit(OpCodes.Brfalse, throwError);

                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Newarr, typeof(IServiceProvider));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stelem_Ref);
                il.Emit(OpCodes.Stloc, lcl);
                il.Emit(OpCodes.Br, next);

                il.MarkLabel(throwError);

                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInfo);
                il.Emit(OpCodes.Ldstr, param.Name ?? string.Empty);
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(injectionType));
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                il.Emit(OpCodes.Call, CommonReflectionCache.StringFormat3);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(next);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0); // service provider (maybe)
                il.Emit(OpCodes.Ldtoken, injectionType);
                il.Emit(OpCodes.Call, Accessor.GetMethod(Type.GetTypeFromHandle)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(TypeUtility.GetServiceFromUnknownProviderType)!);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lcl);
                Label next = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, next);

                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInfo);
                il.Emit(OpCodes.Ldstr, param.Name ?? string.Empty);
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(injectionType));
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                il.Emit(OpCodes.Call, CommonReflectionCache.StringFormat3);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(next);
            }
        }
    }

    /// <summary>
    /// Used as a sanity check to usually catch argument mismatches.
    /// </summary>
    internal int CreateMethodSignature(RuntimeMethodHandle handle)
    {
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        MethodBase method = MethodBase.GetMethodFromHandle(handle)!;

        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out _, out ArraySegment<ParameterInfo> toBind);

        if (toBind.Count == 0)
        {
            return 0;
        }

        Type[] types = new Type[toBind.Count];

        int len = 0;
        for (int i = 0; i < types.Length; ++i)
        {
            Type type = toBind.Array![toBind.Offset + i].ParameterType;

            try
            {
                if (type.IsByRef)
                {
                    type = type.GetElementType()!;
                }
                else if (type is { IsArray: false, IsValueType: false } && type != typeof(string))
                {
                    // IEnumerable's are turned into arrays of their elements for matching purposes
                    Type? intxType;
                    if (type is { IsInterface: true, IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        intxType = type;
                    }
                    else
                    {
                        intxType = type
                            .GetInterfaces()
                            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                    }
                    if (intxType != null)
                        type = intxType.GetGenericArguments()[0].MakeArrayType();
                }
            }
            catch (NotSupportedException) { }

            types[i] = type;

            len += Encoding.UTF8.GetByteCount(type.Name);
        }

        byte[] toHash = new byte[len];
        int index = 0;
        for (int i = 0; i < types.Length; ++i)
        {
            Type type = types[i];
            string typeName = type.Name;
            index += Encoding.UTF8.GetBytes(typeName, 0, typeName.Length, toHash, index);
        }

#if NETFRAMEWORK || NETSTANDARD || !NET5_0_OR_GREATER
        byte[] hash;
        using (SHA1 sha1 = SHA1.Create())
            hash = sha1.ComputeHash(toHash);
#else
        byte[] hash = SHA1.HashData(toHash);
#endif

        uint h1, h2, h3, h4, h5;
        if (BitConverter.IsLittleEndian)
        {
            h1 = Unsafe.ReadUnaligned<uint>(ref hash[00]);
            h2 = Unsafe.ReadUnaligned<uint>(ref hash[04]);
            h3 = Unsafe.ReadUnaligned<uint>(ref hash[08]);
            h4 = Unsafe.ReadUnaligned<uint>(ref hash[12]);
            h5 = Unsafe.ReadUnaligned<uint>(ref hash[16]);
        }
        else
        {
            h1 = (uint)(hash[00] << 24 | hash[01] << 16 | hash[02] << 8 | hash[03]);
            h2 = (uint)(hash[04] << 24 | hash[05] << 16 | hash[06] << 8 | hash[07]);
            h3 = (uint)(hash[08] << 24 | hash[09] << 16 | hash[10] << 8 | hash[11]);
            h4 = (uint)(hash[12] << 24 | hash[13] << 16 | hash[14] << 8 | hash[15]);
            h5 = (uint)(hash[16] << 24 | hash[17] << 16 | hash[18] << 8 | hash[19]);
        }

        return unchecked ( (int)(h1 + ((h2 >> 6) | (h2 << 26)) + ((h3 >> 12) | (h3 << 20)) + ((h4 >> 18) | (h4 << 14)) + ((h5 >> 24) | (h5 << 8))) );
    }
    private void HandleInvocation(MethodInfo method, ParameterInfo[] parameters, LocalBuilder[] injectionLcls, LocalBuilder[] bindLcls, IOpCodeEmitter il, ArraySegment<ParameterInfo> toInject, ArraySegment<ParameterInfo> toBind, Type[] methodParameters)
    {
        if (!method.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_1);
            if (method.DeclaringType != null)
            {
                if (method.DeclaringType.IsClass)
                {
                    il.CommentIfDebug("if (targetObject == null) throw new RpcInjectionException(..)");
                    il.Emit(OpCodes.Castclass, method.DeclaringType);
                    il.Emit(OpCodes.Dup);
                    Label lblDontThrowNullRef = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue, lblDontThrowNullRef);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInstanceNull);
                    il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method.DeclaringType));
                    il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                    il.Emit(OpCodes.Call, CommonReflectionCache.StringFormat2);
                    il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                    il.Emit(OpCodes.Throw);
                    il.MarkLabel(lblDontThrowNullRef);
                }
                else
                {
                    il.CommentIfDebug($"targetObject = ({Accessor.Formatter.Format(method.DeclaringType)})targetObejct");
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }
        }
        
        for (int i = 0; i < parameters.Length; ++i)
        {
            LocalBuilder? lcl = null;
            ParameterInfo param = parameters[i];
            if (toBind.Count != 0)
            {
                int ind = Array.IndexOf(toBind.Array!, param, toBind.Offset, toBind.Count);
                if (ind != -1)
                    lcl = bindLcls[ind - toBind.Offset];
            }

            if (lcl == null)
            {
                int ind = Array.IndexOf(toInject.Array!, param, toInject.Offset, toInject.Count);
                lcl = injectionLcls[ind - toInject.Offset];
            }

            il.Emit(param.ParameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc, lcl);
        }
        
        il.Emit(method.GetCallRuntime(), method);

        Label? lblReturnType = null;

        Label lblRtn = il.DefineLabel();
        Type rtnType = method.ReturnType;

        LocalBuilder? lclResult;
        if (TypeUtility.IsAwaitable(rtnType,
                _proxyGenerator.UseConfigureAwaitWhenAwaitingRpcInvocations,
                out Type? awaitReturnType,
                out MethodInfo? configureAwaitMethod,
                out MethodInfo? getAwaiterMethod,
                out MethodInfo? getResultMethod,
                out MethodInfo? onCompletedMethod,
                out PropertyInfo? getIsCompletedProperty))
        {
            LocalBuilder? lclAwaitable = null;
            if (rtnType.IsValueType)
            {
                lclAwaitable = il.DeclareLocal(rtnType);
                il.Emit(OpCodes.Stloc, lclAwaitable);
                il.Emit(OpCodes.Ldloca, lclAwaitable);
            }
            lblReturnType = il.DefineLabel();
            LocalBuilder lclAwaiter = il.DeclareLocal(getAwaiterMethod.ReturnType);
            if (configureAwaitMethod != null)
            {
                if (configureAwaitMethod.ReturnType == typeof(void))
                {
                    il.CommentIfDebug("task.ConfigureAwait(false)  // outputs nothing");
                    if (lclAwaitable == null)
                    {
                        il.Emit(OpCodes.Dup);
                    }
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(configureAwaitMethod.DeclaringType is { IsValueType: true } ? OpCodes.Call : OpCodes.Callvirt, configureAwaitMethod);
                    il.CommentIfDebug("awaiter = task.GetResult()");
                    if (lclAwaitable != null)
                    {
                        il.Emit(OpCodes.Ldloca, lclAwaitable);
                    }
                    il.Emit(getAwaiterMethod.GetCallRuntime(), getAwaiterMethod);
                }
                else if (configureAwaitMethod.ReturnType.IsByRef
                         && configureAwaitMethod.ReturnType.GetElementType()!.IsValueType)
                {
                    il.CommentIfDebug("awaiter = task.ConfigureAwait(false).GetAwaiter() // outputs ref value type");
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(configureAwaitMethod.DeclaringType is { IsValueType: true } ? OpCodes.Call : OpCodes.Callvirt, configureAwaitMethod);
                    il.Emit(OpCodes.Call, getAwaiterMethod);
                }
                else if (configureAwaitMethod.ReturnType.IsValueType)
                {
                    il.CommentIfDebug("awaiter = task.ConfigureAwait(false).GetAwaiter() // outputs value type");
                    LocalBuilder lclTask = lclAwaitable != null
                                           && lclAwaitable.LocalType == configureAwaitMethod.ReturnType
                        ? lclAwaitable
                        : il.DeclareLocal(configureAwaitMethod.ReturnType);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(configureAwaitMethod.DeclaringType is { IsValueType: true } ? OpCodes.Call : OpCodes.Callvirt, configureAwaitMethod);
                    il.Emit(OpCodes.Stloc, lclTask);
                    il.Emit(OpCodes.Ldloca, lclTask);
                    il.Emit(OpCodes.Call, getAwaiterMethod);
                }
                else
                {
                    il.CommentIfDebug("awaiter = task.ConfigureAwait(false).GetAwaiter() // outputs reference type");
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(configureAwaitMethod.DeclaringType is { IsValueType: true } ? OpCodes.Call : OpCodes.Callvirt, configureAwaitMethod);
                    if (configureAwaitMethod.ReturnType.IsByRef)
                        il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Callvirt, getAwaiterMethod);
                }
            }
            else
            {
                il.CommentIfDebug("awaiter = task.GetAwaiter()");
                il.Emit(getAwaiterMethod.DeclaringType is { IsValueType: true } ? OpCodes.Call : OpCodes.Callvirt, getAwaiterMethod);
            }

            if (!lclAwaiter.LocalType!.IsValueType)
                il.Emit(OpCodes.Dup, lclAwaiter);
            il.Emit(OpCodes.Stloc, lclAwaiter);

            if (lclAwaiter.LocalType.IsValueType)
                il.Emit(OpCodes.Ldloca, lclAwaiter);

            il.CommentIfDebug("bool isCompleted = awaiter.IsCompleted");
            il.Emit(getIsCompletedProperty.DeclaringType is { IsValueType: true } ? OpCodes.Call : OpCodes.Callvirt, getIsCompletedProperty.GetMethod!);

            Label lblQueueContinuation = il.DefineLabel();

            il.CommentIfDebug("if (isCompleted) {");
            il.Emit(OpCodes.Brfalse, lblQueueContinuation);


            lclResult = getResultMethod.ReturnType == typeof(void) ? null : il.DeclareLocal(getResultMethod.ReturnType);

            // try {
            il.BeginExceptionBlock();

            il.CommentIfDebug($"    {Accessor.Formatter.Format(awaitReturnType)} rtnValue = awaiter.GetResult()");

            if (lclAwaiter.LocalType.IsValueType)
                il.Emit(OpCodes.Ldloca, lclAwaiter);
            else
                il.Emit(OpCodes.Ldloc, lclAwaiter);

            il.Emit(getResultMethod.GetCallRuntime(), getResultMethod);
            if (lclResult != null)
                il.Emit(OpCodes.Stloc, lclResult);
            il.Emit(OpCodes.Leave, lblReturnType.Value);
            // } catch (Exception ex) {
            il.BeginCatchBlock(typeof(Exception));

            LocalBuilder lclEx = il.DeclareLocal(typeof(Exception));

            il.Emit(OpCodes.Stloc, lclEx);

            il.CommentIfDebug("  router.HandleInvokeException(ex, overhead, serializer)");
            il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(IRpcRouter)) ));
            il.Emit(OpCodes.Ldloc, lclEx);
            il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(RpcOverhead)) ));
            il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(IRpcSerializer)) ));
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterHandleInvokeException);

            il.CommentIfDebug("  return");
            il.Emit(OpCodes.Leave, lblRtn);
            il.EndExceptionBlock();
            // }
            
            il.CommentIfDebug("} else {");
            il.MarkLabel(lblQueueContinuation);

            Type? closureType;
            Type closureLookupType = getAwaiterMethod.ReturnType;
            lock (_closureTypes)
            {
                if (!_closureTypes.TryGetValue(closureLookupType, out closureType))
                {
                    // create a closure type to capture necessary information for continuation
                    TypeBuilder tb = ProxyGenerator.Instance.ModuleBuilder.DefineType("Closure_"
                        + (closureLookupType.FullName ?? closureLookupType.Name).Replace('+', '_').Replace('.', '_'),
                        TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.SpecialName);

                    FieldBuilder overheadField = tb.DefineField("_overhead", typeof(RpcOverhead), FieldAttributes.Public | FieldAttributes.InitOnly);
                    FieldBuilder routerField = tb.DefineField("_router", typeof(IRpcRouter), FieldAttributes.Public | FieldAttributes.InitOnly);
                    FieldBuilder serializerField = tb.DefineField("_serializer", typeof(IRpcSerializer), FieldAttributes.Public | FieldAttributes.InitOnly);
                    FieldBuilder awaiterField = tb.DefineField("_awaiter", closureLookupType, FieldAttributes.Public);

                    // Continuation action
                    MethodBuilder continuation = tb.DefineMethod(
                        "Continuation",
                        MethodAttributes.Public | MethodAttributes.HideBySig,
                        CallingConventions.Standard,
                        typeof(void),
                        Type.EmptyTypes
                    );

                    IOpCodeEmitter contIl = continuation.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);

                    LocalBuilder? lclResult2 = getResultMethod.ReturnType == typeof(void) ? null : contIl.DeclareLocal(getResultMethod.ReturnType);
                    
                    Label lblContRtn = contIl.DefineLabel();
                    Label lblExitTry = contIl.DefineLabel();
                    
                    // try {
                    contIl.BeginExceptionBlock();

#if DEBUG
                    contIl.EmitWriteLine($"Closure invoking for {Accessor.Formatter.Format(method)}.");
#endif
                    contIl.CommentIfDebug("[var rtnValue = ]awaiter.GetResult()");
                    contIl.Emit(OpCodes.Ldarg_0);
                    if (awaiterField.FieldType.IsValueType)
                        contIl.Emit(OpCodes.Ldflda, awaiterField);
                    else
                        contIl.Emit(OpCodes.Ldfld, awaiterField);
                    
                    contIl.Emit(getResultMethod.GetCallRuntime(), getResultMethod);
#if DEBUG
                    contIl.EmitWriteLine($"Got result for {Accessor.Formatter.Format(method)}.");
#endif
                    if (lclResult2 != null)
                        contIl.Emit(OpCodes.Stloc, lclResult2);

                    contIl.Emit(OpCodes.Leave, lblExitTry);
                    
                    // } catch (Exception ex) {
                    contIl.BeginCatchBlock(typeof(Exception));
                    
                    lclEx = contIl.DeclareLocal(typeof(Exception));

                    contIl.Emit(OpCodes.Stloc, lclEx);

#if DEBUG
                    contIl.EmitWriteLine($"Got exception for {Accessor.Formatter.Format(method)}.");
#endif
                    contIl.CommentIfDebug("  router.HandleInvokeException(ex, overhead, serializer)");
                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, routerField);
                    contIl.Emit(OpCodes.Ldloc, lclEx);
                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, overheadField);
                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, serializerField);
                    contIl.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterHandleInvokeException);
                    
                    contIl.CommentIfDebug("  return");
                    contIl.Emit(OpCodes.Leave, lblContRtn);
                    contIl.EndExceptionBlock();
                    // }
                    
                    contIl.MarkLabel(lblExitTry);

                    contIl.CommentIfDebug("if ((_overhead.Flags & RpcFlags.FireAndForget) != 0)");
                    contIl.CommentIfDebug("    return;");
                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, overheadField);
                    contIl.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcOverheadGetFlags);
                    contIl.Emit(OpCodes.Ldc_I4, (int)RpcFlags.FireAndForget);
                    contIl.Emit(OpCodes.And);
                    contIl.Emit(OpCodes.Ldc_I4_0);
                    contIl.Emit(OpCodes.Bgt_Un, lblContRtn);

                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, routerField);
                    if (lclResult2 != null)
                    {
                        contIl.Emit(OpCodes.Ldloc, lclResult2);
                    }
                    
                    MethodInfo processMethod = ProcessReturnValue(contIl, awaitReturnType);
                    
                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, overheadField);
                    contIl.Emit(OpCodes.Ldarg_0);
                    contIl.Emit(OpCodes.Ldfld, serializerField);
                    contIl.Emit(OpCodes.Callvirt, processMethod);
                    
                    contIl.MarkLabel(lblContRtn);
                    contIl.Emit(OpCodes.Ret);
                    
                    Type[] ctorArgs = [ typeof(RpcOverhead), typeof(IRpcRouter), typeof(IRpcSerializer), closureLookupType ];
                    // Constructor
                    ConstructorBuilder ctor = tb.DefineConstructor(
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                        CallingConventions.Standard,
                        ctorArgs);

                    ctor.DefineParameter(1, ParameterAttributes.None, "overhead");
                    ctor.DefineParameter(2, ParameterAttributes.None, "router");
                    ctor.DefineParameter(3, ParameterAttributes.None, "serializer");
                    ctor.DefineParameter(4, ParameterAttributes.None, "awaiter");

                    IOpCodeEmitter ctorIl = ctor.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);
                    ctorIl.CommentIfDebug("this._overhead = overhead;");
                    ctorIl.CommentIfDebug("this._router = router;");
                    ctorIl.CommentIfDebug("this._serializer = serializer;");
                    ctorIl.CommentIfDebug("this._awaiter = awaiter;");
                    ctorIl.Emit(OpCodes.Ldarg_0);
                    ctorIl.Emit(OpCodes.Ldarg_1);
                    ctorIl.Emit(OpCodes.Stfld, overheadField);
                    ctorIl.Emit(OpCodes.Ldarg_0);
                    ctorIl.Emit(OpCodes.Ldarg_2);
                    ctorIl.Emit(OpCodes.Stfld, routerField);
                    ctorIl.Emit(OpCodes.Ldarg_0);
                    ctorIl.Emit(OpCodes.Ldarg_3);
                    ctorIl.Emit(OpCodes.Stfld, serializerField);
                    ctorIl.Emit(OpCodes.Ldarg_0);
                    ctorIl.Emit(OpCodes.Ldarg_S, (byte)4);
                    ctorIl.Emit(OpCodes.Stfld, awaiterField);

                    if (lclAwaiter.LocalType.IsValueType)
                    {
                        ctorIl.Emit(OpCodes.Ldarg_0);
                        ctorIl.Emit(OpCodes.Ldflda, awaiterField);
                    }
                    else
                        ctorIl.Emit(OpCodes.Ldarg_S, (byte)4);
                    
                    ctorIl.Emit(OpCodes.Ldarg_0);

                    ctorIl.Emit(OpCodes.Ldftn, continuation);

                    ctorIl.Emit(OpCodes.Newobj, CommonReflectionCache.ActionConstructor);
                    
                    ctorIl.Emit(onCompletedMethod.GetCallRuntime(), onCompletedMethod);
#if DEBUG
                    ctorIl.EmitWriteLine($"Added onCompleted: {Accessor.Formatter.Format(onCompletedMethod)}.");
#endif

                    ctorIl.Emit(OpCodes.Ret);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    closureType = tb.CreateTypeInfo()!;
#else
                    closureType = tb.CreateType()!;
#endif

                    _closureTypes.Add(closureLookupType, closureType);
                }
            }
            
            ConstructorInfo fullCtor = closureType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];

#if DEBUG
            il.EmitWriteLine($"Creating closure for : {Accessor.Formatter.Format(method)}.");
#endif
            il.CommentIfDebug("  _ = new Closure(overhead, router, serializer, awaiter)");
            il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(RpcOverhead)) ));
            il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(IRpcRouter)) ));
            il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(IRpcSerializer)) ));
            il.Emit(OpCodes.Ldloc, lclAwaiter);
            il.Emit(OpCodes.Newobj, fullCtor);
            il.Emit(OpCodes.Pop);
            // skip async stuff: awaitReturnType = typeof(void);
            // skip async stuff: lblReturnType ??= il.DefineLabel();
            // skip async stuff: lclResult = null;
            // skip async stuff: il.Emit(OpCodes.Br, lblReturnType.Value);
            il.CommentIfDebug("  return");
            il.Emit(OpCodes.Br, lblRtn);
        }
        else
        {
            awaitReturnType = rtnType;
            lclResult = awaitReturnType == typeof(void) ? null : il.DeclareLocal(awaitReturnType);
            if (lclResult != null)
                il.Emit(OpCodes.Stloc, lclResult);
        }
        
        il.TryMarkLabel(lblReturnType);

        il.CommentIfDebug("if ((_overhead.Flags & RpcFlags.FireAndForget) != 0)");
        il.CommentIfDebug("    return;");
        il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(RpcOverhead)) ));
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcOverheadGetFlags);
        il.Emit(OpCodes.Ldc_I4, (int)RpcFlags.FireAndForget);
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Bgt_Un, lblRtn);

        il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(IRpcRouter)) ));
        if (lclResult != null)
            il.Emit(OpCodes.Ldloc, lclResult);

        MethodInfo processMethod2 = ProcessReturnValue(il, awaitReturnType);
        il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(RpcOverhead)) ));
        il.Emit(OpCodes.Ldarg, checked( (ushort)Array.IndexOf(methodParameters, typeof(IRpcSerializer)) ));
        il.Emit(OpCodes.Callvirt, processMethod2);
        il.MarkLabel(lblRtn);
    }

    private static MethodInfo ProcessReturnValue(IOpCodeEmitter il, Type awaitReturnType)
    {
        TypeSerializationInfo info = new TypeSerializationInfo(awaitReturnType);
        switch (info.Type)
        {
            case TypeSerializationInfoType.Void:
                il.CommentIfDebug("  router.HandleInvokeVoidReturn(overhead, serializer)");
                return CommonReflectionCache.RpcRouterHandleInvokeVoidReturn;

            case TypeSerializationInfoType.PrimitiveLike:
            case TypeSerializationInfoType.Value:
                il.CommentIfDebug($"  router.HandleInvokeReturnValue<{awaitReturnType}>(rtnValue, overhead, serializer)");
                return CommonReflectionCache.RpcRouterHandleInvokeReturnValue.MakeGenericMethod(awaitReturnType);

            case TypeSerializationInfoType.NullableValue:
                il.CommentIfDebug($"  router.HandleInvokeNullableReturnValue<{awaitReturnType}>(rtnValue, overhead, serializer)");
                return CommonReflectionCache.RpcRouterHandleInvokeNullableReturnValue.MakeGenericMethod(info.UnderlyingType);

            case TypeSerializationInfoType.SerializableValue:
                il.CommentIfDebug($"  router.HandleInvokeSerializableReturnValue<{info.SerializableType}>(rtnValue, null, overhead, serializer)");
                il.Emit(OpCodes.Ldnull);
                return CommonReflectionCache.RpcRouterHandleInvokeSerializableReturnValue.MakeGenericMethod(info.SerializableType);

            case TypeSerializationInfoType.NullableSerializableValue:
                il.CommentIfDebug($"  router.HandleSerializableReturnValue<{info.UnderlyingType}>(rtnValue, null, overhead, serializer)");
                il.Emit(OpCodes.Ldnull);
                return CommonReflectionCache.RpcRouterHandleInvokeNullableSerializableReturnValue.MakeGenericMethod(info.UnderlyingType);

            case TypeSerializationInfoType.SerializableCollection:
            case TypeSerializationInfoType.NullableSerializableCollection:
                bool nullable = info.Type == TypeSerializationInfoType.NullableSerializableCollection;
                if (!nullable)
                    il.CommentIfDebug($"  router.HandleSerializableReturnValue<{info.SerializableType}>(default, rtnValue ?? DBNull.Value, overhead, serializer)");
                else
                    il.CommentIfDebug($"  router.HandleNullableSerializableReturnValue<{info.UnderlyingType}>(null, rtnValue ?? DBNull.Value, overhead, serializer)");
                LocalBuilder lclRtnValue = il.DeclareLocal(typeof(object));
                if (awaitReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, awaitReturnType);
                }
                else
                {
                    il.Emit(OpCodes.Dup);

                    Label lblNotNull = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue, lblNotNull);
                    il.Emit(OpCodes.Pop);

                    il.Emit(OpCodes.Ldsfld, CommonReflectionCache.GetDbNullValue);

                    il.MarkLabel(lblNotNull);
                }
                il.Emit(OpCodes.Stloc, lclRtnValue);
                if (nullable || info.SerializableType.IsValueType)
                {
                    LocalBuilder lclDefault = il.DeclareLocal(info.SerializableType);
                    il.Emit(OpCodes.Ldloca, lclDefault);
                    il.Emit(OpCodes.Initobj, info.SerializableType);
                    il.Emit(OpCodes.Ldloc, lclDefault);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
                il.Emit(OpCodes.Ldloc, lclRtnValue);
                if (lclRtnValue.LocalType!.IsValueType)
                    il.Emit(OpCodes.Box, lclRtnValue.LocalType);
                return !nullable
                    ? CommonReflectionCache.RpcRouterHandleInvokeSerializableReturnValue.MakeGenericMethod(info.SerializableType)
                    : CommonReflectionCache.RpcRouterHandleInvokeNullableSerializableReturnValue.MakeGenericMethod(info.UnderlyingType);

            case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:
            case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                nullable = info.Type == TypeSerializationInfoType.NullableCollectionNullableSerializableCollection;
                MethodInfo callHasValue = CommonReflectionCache.GetNullableHasValueGetter(awaitReturnType);

                MethodInfo callGetValue = CommonReflectionCache.GetNullableValueGetter(awaitReturnType);

                lclRtnValue = il.DeclareLocal(awaitReturnType);
                il.Emit(OpCodes.Stloc, lclRtnValue);
                if (nullable || info.SerializableType.IsValueType)
                {
                    LocalBuilder lclDefault = il.DeclareLocal(info.SerializableType);
                    il.Emit(OpCodes.Ldloca, lclDefault);
                    il.Emit(OpCodes.Initobj, info.SerializableType);
                    il.Emit(OpCodes.Ldloc, lclDefault);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }

                il.Emit(OpCodes.Ldloca, lclRtnValue);

                il.CommentIfDebug("lclRtnValue = arg.HasValue ? arg.Value : null");

                il.Emit(OpCodes.Dup);

                il.Emit(OpCodes.Call, callHasValue);
                Label hasValue = il.DefineLabel();
                Label doesntHaveValue = il.DefineLabel();
                il.Emit(OpCodes.Brtrue_S, hasValue);

                // if (!arg{i}.HasValue) {
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldsfld, CommonReflectionCache.GetDbNullValue);
                il.Emit(OpCodes.Br, doesntHaveValue);

                // } else {
                il.MarkLabel(hasValue);
                il.Emit(OpCodes.Call, callGetValue);
                il.Emit(OpCodes.Box, info.UnderlyingType);
                il.MarkLabel(doesntHaveValue);

                return !nullable
                    ? CommonReflectionCache.RpcRouterHandleInvokeSerializableReturnValue.MakeGenericMethod(info.SerializableType)
                    : CommonReflectionCache.RpcRouterHandleInvokeNullableSerializableReturnValue.MakeGenericMethod(info.UnderlyingType);


            default:
                throw new InvalidOperationException($"Invalid type: {info.Type}."); // shouldn't be reached
        }
    }

    internal void GenerateInvokeBytes(MethodInfo method, DynamicMethod dynMethod, IOpCodeEmitter il)
    {
        dynMethod.InitLocals = false;
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder lclPreCalc = il.DeclareLocal(typeof(bool));
        LocalBuilder lclReadInd = il.DeclareLocal(typeof(int));
        LocalBuilder lclTempByteCt = il.DeclareLocal(typeof(int));
        
        il.CommentIfDebug("bool lclPreCalc = serializer.PreCalculatePrimitiveSizes;");
        il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer)) ));
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerCanFastReadPrimitives);
        il.Emit(OpCodes.Stloc, lclPreCalc);

        il.CommentIfDebug("int lclReadInd = 0;");
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, lclReadInd);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        HandleInjections(method, il, toInject, injectionLcls, true);

        for (int i = 0; i < toBind.Count; ++i)
        {
            ParameterInfo parameter = toBind.Array![i + toBind.Offset];
            Type type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

            TypeSerializationInfo info = new TypeSerializationInfo(type);

            bindLcls[i] = il.DeclareLocal(type);

            Label lblDoPrimitiveRead = default;
            Label lblDontPrimitiveRead = default;

            il.CommentIfDebug($"== Read {Accessor.Formatter.Format(type)} ==");
            if (info.Type == TypeSerializationInfoType.PrimitiveLike)
            {
                lblDoPrimitiveRead = il.DefineLabel();
                lblDontPrimitiveRead = il.DefineLabel();
                il.CommentIfDebug("if (lclPreCalc) goto lblDoPrimitiveRead;");
                il.Emit(OpCodes.Ldloc, lclPreCalc);
                il.Emit(OpCodes.Brtrue, lblDoPrimitiveRead);
            }

            Label continueRead = il.DefineLabel();
            il.CommentIfDebug($"if (maxCount - lclReadInd <= 0) throw new RpcParseException({Properties.Exceptions.RpcParseExceptionBufferRunOut}) {{ ErrorCode = 1 }};");
            il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint)) ));
            il.Emit(OpCodes.Ldloc, lclReadInd);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Bgt, continueRead);
            il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcParseExceptionBufferRunOut);
            il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcParseExceptionCtorMessage);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(CommonReflectionCache.SetRpcParseExceptionErrorCode.GetCallRuntime(), CommonReflectionCache.SetRpcParseExceptionErrorCode);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(continueRead);

            switch (info.Type)
            {
                case TypeSerializationInfoType.PrimitiveLike:
                case TypeSerializationInfoType.Value:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadObject<{Accessor.Formatter.Format(type)}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadObjectByValBytes.MakeGenericMethod(type));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.NullableValue:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadNullableObject<{Accessor.Formatter.Format(info.UnderlyingType)}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableObjectByValBytes.MakeGenericMethod(info.UnderlyingType));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObject<{Accessor.Formatter.Format(type)}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadSerializableObjectBytes.MakeGenericMethod(type));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;
                
                case TypeSerializationInfoType.NullableSerializableValue:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObject<{Accessor.Formatter.Format(info.SerializableType)}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableSerializableObjectBytes.MakeGenericMethod(info.SerializableType));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;
                
                case TypeSerializationInfoType.SerializableCollection:
                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                    if (info.CollectionType == info.SerializableType.MakeArrayType())
                    {
                        il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObjects<{info.SerializableType}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                        il.Emit(OpCodes.Ldloc, lclReadInd);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                        il.Emit(OpCodes.Ldloc, lclReadInd);
                        il.Emit(OpCodes.Sub);
                        il.Emit(OpCodes.Ldloca, lclTempByteCt);
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadSerializableObjectsArrayBytes.MakeGenericMethod(info.SerializableType));
                    }
                    else
                    {
                        il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObjects<{info.SerializableType}, {info.CollectionType}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                        il.Emit(OpCodes.Ldloc, lclReadInd);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                        il.Emit(OpCodes.Ldloc, lclReadInd);
                        il.Emit(OpCodes.Sub);
                        il.Emit(OpCodes.Ldloca, lclTempByteCt);
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadSerializableObjectsAnyBytes.MakeGenericMethod(info.SerializableType, info.CollectionType));
                    }

                    if (info.Type != TypeSerializationInfoType.SerializableCollection)
                    {
                        il.Emit(OpCodes.Newobj, CommonReflectionCache.GetNullableConstructor(type));
                    }

                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.NullableSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                    Type collectionType = info.Type == TypeSerializationInfoType.NullableSerializableCollection
                        ? info.CollectionType
                        : Nullable.GetUnderlyingType(info.CollectionType)!;

                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadNullableSerializableObjects<{info.UnderlyingType}, {collectionType}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint))));
                    il.Emit(OpCodes.Ldloc, lclReadInd);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableSerializableObjectsAnyBytes.MakeGenericMethod(info.UnderlyingType, collectionType));

                    if (info.Type != TypeSerializationInfoType.NullableSerializableCollection)
                    {
                        il.Emit(OpCodes.Newobj, CommonReflectionCache.GetNullableConstructor(type));
                    }

                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;
            }


            il.CommentIfDebug("lclReadInd += lclTempByteCt;");
            il.Emit(OpCodes.Ldloc, lclReadInd);
            il.Emit(OpCodes.Ldloc, lclTempByteCt);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclReadInd);

            if (info.Type != TypeSerializationInfoType.PrimitiveLike)
                continue;

            il.CommentIfDebug("goto lblDontPrimitiveRead;");
            il.Emit(OpCodes.Br, lblDontPrimitiveRead);

            il.CommentIfDebug("lblDoPrimitiveRead:");
            il.MarkLabel(lblDoPrimitiveRead);

            continueRead = il.DefineLabel();
            int primSize = GetPrimitiveTypeSize(type);
            il.CommentIfDebug($"if (maxCount - lclReadInd < {primSize}) throw new RpcParseException({Properties.Exceptions.RpcParseExceptionBufferRunOut}) {{ ErrorCode = 1 }};");
            il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint)) ));
            il.Emit(OpCodes.Ldloc, lclReadInd);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ldc_I4, primSize);
            il.Emit(OpCodes.Bge, continueRead);
            il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcParseExceptionBufferRunOut);
            il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcParseExceptionCtorMessage);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(CommonReflectionCache.SetRpcParseExceptionErrorCode.GetCallRuntime(), CommonReflectionCache.SetRpcParseExceptionErrorCode);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(continueRead);

            il.CommentIfDebug($"bindLcl{i} = *({Accessor.Formatter.Format(type)}*)(bytes + lclReadInd);");
            il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*)) ));
            il.Emit(OpCodes.Ldloc, lclReadInd);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Unaligned, (byte)1);
            LoadFromRef(type, il);
            il.Emit(OpCodes.Stloc, bindLcls[i]);

            il.CommentIfDebug($"lclReadInd += {primSize};");
            il.Emit(OpCodes.Ldloc, lclReadInd);
            il.Emit(OpCodes.Ldc_I4, primSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclReadInd);

            il.CommentIfDebug("lblDontPrimitiveRead:");
            il.MarkLabel(lblDontPrimitiveRead);
        }

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind, ProxyGenerator.RpcInvokeHandlerBytesParams);
        il.Emit(OpCodes.Ret);
    }
    internal void GenerateInvokeStream(MethodInfo method, IOpCodeEmitter il)
    {
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        HandleInjections(method, il, toInject, injectionLcls, false);

        LocalBuilder lclTempByteCt = il.DeclareLocal(typeof(int));

        for (int i = 0; i < toBind.Count; ++i)
        {
            ParameterInfo parameter = toBind.Array![i + toBind.Offset];

            Type type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
            TypeSerializationInfo info = new TypeSerializationInfo(type);
            bindLcls[i] = il.DeclareLocal(type);

            il.CommentIfDebug($"== Read {Accessor.Formatter.Format(type)} ==");

            switch (info.Type)
            {
                case TypeSerializationInfoType.PrimitiveLike:
                case TypeSerializationInfoType.Value:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadObject<{Accessor.Formatter.Format(type)}>(stream, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadObjectByValStream.MakeGenericMethod(type));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.NullableValue:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadNullable<{Accessor.Formatter.Format(info.UnderlyingType)}>(__makeref(bindLcl{i}), stream, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldloca, bindLcls[i]);
                    il.Emit(OpCodes.Mkrefany, type);
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableObjectByTRefStream.MakeGenericMethod(info.UnderlyingType));
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObject<{Accessor.Formatter.Format(type)}>(stream, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadSerializableObjectStream.MakeGenericMethod(type));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.NullableSerializableValue:
                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadNullableSerializableObject<{Accessor.Formatter.Format(type)}>(stream, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableSerializableObjectStream.MakeGenericMethod(type));
                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.SerializableCollection:
                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                    if (info.CollectionType == info.SerializableType.MakeArrayType())
                    {
                        il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObjects<{info.SerializableType}>(stream, out lclTempByteCt);");
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                        il.Emit(OpCodes.Ldloca, lclTempByteCt);
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadSerializableObjectsArrayStream.MakeGenericMethod(info.SerializableType!));
                    }
                    else
                    {
                        il.CommentIfDebug($"bindLcl{i} = serializer.ReadSerializableObjects<{info.SerializableType}, {info.CollectionType}>(stream, out lclTempByteCt);");
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                        il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                        il.Emit(OpCodes.Ldloca, lclTempByteCt);
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadSerializableObjectsAnyStream.MakeGenericMethod(info.SerializableType, info.CollectionType));
                    }

                    if (info.Type != TypeSerializationInfoType.SerializableCollection)
                    {
                        il.Emit(OpCodes.Newobj, CommonReflectionCache.GetNullableConstructor(type));
                    }

                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;

                case TypeSerializationInfoType.NullableSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                    Type collectionType = info.Type == TypeSerializationInfoType.NullableSerializableCollection
                        ? info.CollectionType
                        : Nullable.GetUnderlyingType(info.CollectionType)!;

                    il.CommentIfDebug($"bindLcl{i} = serializer.ReadNullableSerializableObjects<{info.UnderlyingType}, {collectionType}>(stream, out lclTempByteCt);");
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(IRpcSerializer))));
                    il.Emit(OpCodes.Ldarg, checked((ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream))));
                    il.Emit(OpCodes.Ldloca, lclTempByteCt);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableSerializableObjectsAnyStream.MakeGenericMethod(info.UnderlyingType, collectionType));

                    if (info.Type != TypeSerializationInfoType.NullableSerializableCollection)
                    {
                        il.Emit(OpCodes.Newobj, CommonReflectionCache.GetNullableConstructor(type));
                    }

                    il.Emit(OpCodes.Stloc, bindLcls[i]);
                    break;
            }
        }

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind, ProxyGenerator.RpcInvokeHandlerStreamParams);
        il.Emit(OpCodes.Ret);
    }
    internal void GenerateRawInvokeBytes(MethodInfo method, DynamicMethod dynMethod, IOpCodeEmitter il)
    {
        dynMethod.InitLocals = false;
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        int? canTakeOwnershipIndex = null,
             dataIndex = null,
             byteCountIndex = null;

        Type? dataType = null, countType = null;

        for (int i = 0; i < toBind.Count; ++i)
        {
            ParameterInfo parameter = toBind.Array![i + toBind.Offset];
            Type type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
            Type actualType = Nullable.GetUnderlyingType(type) ?? type;
            bindLcls[i] = il.DeclareLocal(type);
            FindLocalType(actualType, il, i, ref canTakeOwnershipIndex, ref dataIndex, ref byteCountIndex, ref dataType, ref countType);
        }

        HandleInjections(method, il, toInject, injectionLcls, true);

        int memArgInd = Array.IndexOf(ProxyGenerator.RpcInvokeHandlerRawBytesParams, typeof(ReadOnlyMemory<byte>));

        if (byteCountIndex.HasValue)
        {
            il.Emit(OpCodes.Ldarga_S, (ushort)memArgInd);
            il.Emit(OpCodes.Call, CommonReflectionCache.GetReadOnlyMemoryLength);
            if (countType != typeof(int))
            {
                if (countType == typeof(byte))
                    il.Emit(OpCodes.Conv_Ovf_U1);
                else if (countType == typeof(sbyte))
                    il.Emit(OpCodes.Conv_Ovf_I1);
                else if (countType == typeof(uint))
                    il.Emit(OpCodes.Conv_U4);
                else if (countType == typeof(ushort))
                    il.Emit(OpCodes.Conv_Ovf_U2);
                else if (countType == typeof(short))
                    il.Emit(OpCodes.Conv_Ovf_I2);
                else if (countType == typeof(long))
                    il.Emit(OpCodes.Conv_I8);
                else if (countType == typeof(ulong))
                    il.Emit(OpCodes.Conv_U8);
                else if (countType == typeof(nint))
                    il.Emit(OpCodes.Conv_I);
                else if (countType == typeof(nuint))
                    il.Emit(OpCodes.Conv_U);
            }
            il.Emit(OpCodes.Stloc, bindLcls[byteCountIndex.Value]);
        }

        bool canTakeOwnership = true;
        bool skipCanTakeOwnership = !canTakeOwnershipIndex.HasValue;
        int canTakeOwnrshpInd = Array.IndexOf(ProxyGenerator.RpcInvokeHandlerRawBytesParams, typeof(bool));

        if (dataIndex.HasValue)
        {
            if (dataType == typeof(ReadOnlyMemory<byte>))
            {
                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
            }
            else if (dataType == typeof(Memory<byte>))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new Func<ArraySegment<byte>, Memory<byte>>(MemoryExtensions.AsMemory))!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                skipCanTakeOwnership = true;
            }
            else if (dataType == typeof(ReadOnlySpan<byte>))
            {
                il.Emit(OpCodes.Ldarga_S, (ushort)memArgInd);
                il.Emit(OpCodes.Call, CommonReflectionCache.GetReadOnlyMemorySpan);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType == typeof(Span<byte>))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new AsSpanHandle(MemoryExtensions.AsSpan))!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType == typeof(ArraySegment<byte>))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvArraySeg)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                skipCanTakeOwnership = true;
            }
            else if (dataType!.IsAssignableFrom(typeof(byte[])) || dataType == typeof(Array))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvArray)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                skipCanTakeOwnership = true;
            }
            else if (dataType == typeof(List<byte>))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvList)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                skipCanTakeOwnership = true;
            }
            else if (dataType == typeof(ArrayList))
            {
                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvArrayList)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                if (canTakeOwnershipIndex.HasValue)
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stloc, bindLcls[canTakeOwnershipIndex.Value]);
                    skipCanTakeOwnership = true;
                }
            }
            else if (dataType == typeof(byte*))
            {
                LocalBuilder pin = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
                LocalBuilder span = il.DeclareLocal(typeof(ReadOnlySpan<byte>));
                il.Emit(OpCodes.Ldarga_S, (ushort)memArgInd);
                il.Emit(OpCodes.Call, CommonReflectionCache.GetReadOnlyMemorySpan);
                il.Emit(OpCodes.Stloc, span);
                il.Emit(OpCodes.Ldloca, span);
                il.Emit(OpCodes.Call, CommonReflectionCache.PinReadOnlySpan);
                il.Emit(OpCodes.Stloc, pin);
                il.Emit(OpCodes.Ldloc, pin);
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType.IsAssignableFrom(typeof(MemoryStream)))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];
                LocalBuilder seg = il.DeclareLocal(typeof(ArraySegment<byte>));

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvArraySeg)!);
                il.Emit(OpCodes.Stloc, seg);
                il.Emit(OpCodes.Ldloca, seg);
                il.Emit(OpCodes.Call, CommonReflectionCache.ByteArraySegmentArray);
                il.Emit(OpCodes.Ldloca, seg);
                il.Emit(OpCodes.Call, CommonReflectionCache.ByteArraySegmentOffset);
                il.Emit(OpCodes.Ldloca, seg);
                il.Emit(OpCodes.Call, CommonReflectionCache.ByteArraySegmentCount);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.CtorFullMemoryStream);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
            }
            else if (dataType.AssemblyQualifiedName != null && dataType.AssemblyQualifiedName.StartsWith("DanielWillett.SpeedBytes.ByteReader, DanielWillett.SpeedBytes", StringComparison.Ordinal))
            {
                LocalBuilder lcl = skipCanTakeOwnership ? il.DeclareLocal(typeof(bool)) : bindLcls[canTakeOwnershipIndex!.Value];

                il.Emit(OpCodes.Ldarg_S, (ushort)memArgInd);
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
                il.Emit(OpCodes.Ldloca, lcl);
                il.Emit(OpCodes.Call, Accessor.GetMethod(GetByteReader)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                skipCanTakeOwnership = true;
            }
            else
            {
                canTakeOwnership = false;
            }
        }

        if (!skipCanTakeOwnership)
        {
            if (canTakeOwnership)
            {
                il.Emit(OpCodes.Ldarg_S, (ushort)canTakeOwnrshpInd);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4_0);
            }
            il.Emit(OpCodes.Stloc, bindLcls[canTakeOwnershipIndex!.Value]);
        }

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind, ProxyGenerator.RpcInvokeHandlerRawBytesParams);
        il.Emit(OpCodes.Ret);
    }
    internal void GenerateRawInvokeStream(MethodInfo method, DynamicMethod dynMethod, IOpCodeEmitter il)
    {
        dynMethod.InitLocals = false;
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        int? canTakeOwnershipIndex = null,
             dataIndex = null,
             byteCountIndex = null;

        Type? dataType = null, countType = null;

        for (int i = 0; i < toBind.Count; ++i)
        {
            ParameterInfo parameter = toBind.Array![i + toBind.Offset];
            Type type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
            Type actualType = Nullable.GetUnderlyingType(type) ?? type;
            bindLcls[i] = il.DeclareLocal(type);
            FindLocalType(actualType, il, i, ref canTakeOwnershipIndex, ref dataIndex, ref byteCountIndex, ref dataType, ref countType);
        }

        HandleInjections(method, il, toInject, injectionLcls, false);

        int streamInd = Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(Stream));
        int ovhInd = Array.IndexOf(ProxyGenerator.RpcInvokeHandlerStreamParams, typeof(RpcOverhead));

        if (byteCountIndex.HasValue)
        {
            LoadSize(il, ovhInd, false);
            if (countType != typeof(uint))
            {
                if (countType == typeof(byte))
                    il.Emit(OpCodes.Conv_Ovf_U1);
                else if (countType == typeof(sbyte))
                    il.Emit(OpCodes.Conv_Ovf_I1);
                else if (countType == typeof(int))
                    il.Emit(OpCodes.Conv_Ovf_I4);
                else if (countType == typeof(ushort))
                    il.Emit(OpCodes.Conv_Ovf_U2);
                else if (countType == typeof(short))
                    il.Emit(OpCodes.Conv_Ovf_I2);
                else if (countType == typeof(long))
                    il.Emit(OpCodes.Conv_I8);
                else if (countType == typeof(ulong))
                    il.Emit(OpCodes.Conv_U8);
                else if (countType == typeof(nint))
                    il.Emit(OpCodes.Conv_Ovf_I);
                else if (countType == typeof(nuint))
                    il.Emit(OpCodes.Conv_U);
            }

            il.Emit(OpCodes.Stloc, bindLcls[byteCountIndex.Value]);
        }

        bool canTakeOwnership = true;

        if (dataIndex.HasValue)
        {
            if (dataType == typeof(ReadOnlyMemory<byte>))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new Func<ArraySegment<byte>, Memory<byte>>(MemoryExtensions.AsMemory))!);
                il.Emit(OpCodes.Call, CommonReflectionCache.MemoryToReadOnlyMemory);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = true;
            }
            else if (dataType == typeof(Memory<byte>))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new Func<ArraySegment<byte>, Memory<byte>>(MemoryExtensions.AsMemory))!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = true;
            }
            else if (dataType == typeof(ReadOnlySpan<byte>))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new AsSpanHandle(MemoryExtensions.AsSpan))!);
                il.Emit(OpCodes.Call, CommonReflectionCache.SpanToReadOnlySpan);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType == typeof(Span<byte>))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new AsSpanHandle(MemoryExtensions.AsSpan))!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType == typeof(ArraySegment<byte>))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArraySeg)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = true;
            }
            else if (dataType!.IsAssignableFrom(typeof(byte[])) || dataType == typeof(Array))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArray)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = true;
            }
            else if (dataType == typeof(List<byte>))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamList)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = true;
            }
            else if (dataType == typeof(ArrayList))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArrayList)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = true;
            }
            else if (dataType == typeof(byte*))
            {
                LocalBuilder pin = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
                LocalBuilder span = il.DeclareLocal(typeof(ReadOnlySpan<byte>));
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamArraySeg)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(new AsSpanHandle(MemoryExtensions.AsSpan))!);
                il.Emit(OpCodes.Call, CommonReflectionCache.SpanToReadOnlySpan);
                il.Emit(OpCodes.Stloc, span);
                il.Emit(OpCodes.Ldloca, span);
                il.Emit(OpCodes.Call, CommonReflectionCache.PinReadOnlySpan);
                il.Emit(OpCodes.Stloc, pin);
                il.Emit(OpCodes.Ldloc, pin);
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType == typeof(Stream))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(ConvStreamStream)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else if (dataType.AssemblyQualifiedName != null && dataType.AssemblyQualifiedName.StartsWith("DanielWillett.SpeedBytes.ByteReader, DanielWillett.SpeedBytes", StringComparison.Ordinal))
            {
                LoadSize(il, ovhInd, false);
                il.Emit(OpCodes.Ldarg_S, (ushort)streamInd);
                il.Emit(OpCodes.Call, Accessor.GetMethod(GetStreamByteReader)!);
                il.Emit(OpCodes.Stloc, bindLcls[dataIndex.Value]);
                canTakeOwnership = false;
            }
            else
            {
                canTakeOwnership = false;
            }
        }

        if (canTakeOwnershipIndex.HasValue)
        {
            il.Emit(canTakeOwnership ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, bindLcls[canTakeOwnershipIndex.Value]);
        }

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind, ProxyGenerator.RpcInvokeHandlerStreamParams);
        il.Emit(OpCodes.Ret);
    }
    private static byte[] ConvStreamArray(uint size, Stream stream)
    {
        ArraySegment<byte> seg = ConvStreamArraySeg(size, stream);
        if (seg.Offset == 0 && seg.Count == seg.Array!.Length)
            return seg.Array;

        return seg.ToArray();
    }
    private static ArrayList ConvStreamArrayList(uint size, Stream stream)
    {
        ArrayList arrayList = new ArrayList(checked ( (int)size ));
        byte[] fullArray = ConvStreamArray(size, stream);
        arrayList.AddRange(fullArray);
        return arrayList;
    }
    private static List<byte> ConvStreamList(uint size, Stream stream)
    {
        List<byte> list = new List<byte>(0);
        if (size == 0u)
            return list;

        ArraySegment<byte> seg = ConvStreamArraySeg(size, stream);
        if (seg.Offset == 0 && seg.Count == seg.Array!.Length && list.TrySetUnderlyingArray(seg.Array, seg.Count))
            return list;

        list.AddRange(seg);
        return list;
    }
    private static Stream ConvStreamStream(uint size, Stream stream)
    {
        return new PassthroughReadStream(stream, size);
    }
    private static object GetStreamByteReader(uint size, Stream stream)
    {
        ByteReader reader = new ByteReader();
        
        reader.LoadNew(new PassthroughReadStream(stream, size));
        return reader;
    }
    private static ArraySegment<byte> ConvStreamArraySeg(uint size, Stream stream)
    {
        if (size == 0)
            return new ArraySegment<byte>(Array.Empty<byte>());

        byte[] newArray = new byte[size];
        int sizeActuallyRead = stream.Read(newArray, 0, newArray.Length);

        if ((uint)sizeActuallyRead < size)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
        
        return new ArraySegment<byte>(newArray, 0, sizeActuallyRead);
    }
    private static void LoadSize(IOpCodeEmitter il, int ovhInd, bool asInt)
    {
        il.Emit(OpCodes.Ldarg_S, (ushort)ovhInd);
        il.Emit(OpCodes.Call, CommonReflectionCache.RpcOverheadGetMessageSize);
        if (asInt)
        {
            il.Emit(OpCodes.Conv_Ovf_I4);
        }
    }

    private delegate Span<byte> AsSpanHandle(ArraySegment<byte> arrSeg);
    private static object GetByteReader(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        ArraySegment<byte> arr = ConvArraySeg(mem, couldTakeOwnership, out canTakeOwnership);
        ByteReader reader = new ByteReader();

        reader.LoadNew(arr);
        return reader;
    }
    private static ArraySegment<byte> ConvArraySeg(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        if (MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> arr))
        {
            canTakeOwnership = couldTakeOwnership;
        }
        else
        {
            arr = new ArraySegment<byte>(mem.ToArray());
            canTakeOwnership = true;
        }

        return arr.Array == null ? new ArraySegment<byte>(Array.Empty<byte>()) : arr;
    }
    private static byte[] ConvArray(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        if (mem.Length == 0)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        if (!MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> arr))
        {
            canTakeOwnership = true;
            return mem.ToArray();
        }

        if (arr.Count == 0 || arr.Array == null)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        if (arr.Offset == 0 && arr.Count == arr.Array.Length)
        {
            canTakeOwnership = couldTakeOwnership;
            return arr.Array;
        }

        byte[] newArray = new byte[arr.Count];
        Buffer.BlockCopy(arr.Array, arr.Offset, newArray, 0, newArray.Length);
        canTakeOwnership = true;
        return newArray;
    }
    private static List<byte> ConvList(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        ArraySegment<byte> arr = ConvArraySeg(mem, couldTakeOwnership, out canTakeOwnership);
        List<byte> list = new List<byte>(0);
        if (arr.Count == 0 || arr.Array == null)
        {
            canTakeOwnership = true;
            return list;
        }

        if (arr.Offset == 0 && arr.Count == arr.Array.Length)
        {
            if (list.TrySetUnderlyingArray(arr.Array, arr.Array.Length))
                return list;

            list.AddRange(arr.Array);
            canTakeOwnership = true;

            return list;
        }

        byte[] newArray = new byte[arr.Count];
        Buffer.BlockCopy(arr.Array, arr.Offset, newArray, 0, newArray.Length);

        if (!list.TrySetUnderlyingArray(newArray, newArray.Length))
        {
            list.AddRange(newArray);
        }
        canTakeOwnership = true;
        return list;
    }
    private static ArrayList ConvArrayList(ReadOnlyMemory<byte> mem)
    {
        ArrayList arrayList = new ArrayList(mem.Length);
        byte[] fullArray = ConvArray(mem, false, out _);
        arrayList.AddRange(fullArray);
        return arrayList;
    }
    private static void FindLocalType(Type actualType, IOpCodeEmitter il, int index, ref int? canTakeOwnershipIndex, ref int? dataIndex, ref int? byteCountIndex, ref Type? dataType, ref Type? countType)
    {
        if (actualType == typeof(bool))
        {
            if (canTakeOwnershipIndex.HasValue)
            {
                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionMultipleCanTakeOwnership);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);
            }
            else
            {
                canTakeOwnershipIndex = index;
            }
        }
        else if (actualType == typeof(int)
                 || actualType == typeof(uint)
                 || actualType == typeof(long)
                 || actualType == typeof(ulong)
                 || actualType == typeof(ushort)
                 || actualType == typeof(short)
                 || actualType == typeof(byte)
                 || actualType == typeof(sbyte)
                 || actualType == typeof(nint)
                 || actualType == typeof(nuint)
                )
        {
            if (byteCountIndex.HasValue)
            {
                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionMultipleByteCount);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);
            }
            else
            {
                byteCountIndex = index;
                countType = actualType;
            }
            
        }
        else if (actualType == typeof(byte*)
                 || actualType == typeof(byte[])
                 || actualType == typeof(Stream)
                 || actualType == typeof(Memory<byte>)
                 || actualType == typeof(ReadOnlyMemory<byte>)
                 || actualType == typeof(Span<byte>)
                 || actualType == typeof(ReadOnlySpan<byte>)
                 || actualType == typeof(ArraySegment<byte>)
                 || actualType == typeof(IList<byte>)
                 || actualType == typeof(IReadOnlyList<byte>)
                 || actualType == typeof(ICollection<byte>)
                 || actualType == typeof(IEnumerable<byte>)
                 || actualType == typeof(IReadOnlyCollection<byte>)
                 || actualType == typeof(List<byte>)
                 || actualType == typeof(ArrayList)
                 || actualType == typeof(ReadOnlyCollection<byte>)
                 || actualType == typeof(byte).MakeByRefType()
                 || actualType == SpeedBytesWriterType
                 || actualType == SpeedBytesReaderType
                 )
        {
            if (dataIndex.HasValue)
            {
                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionMultipleByteData);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);
            }
            else
            {
                dataIndex = index;
                dataType = actualType;
            }
        }
    }
}