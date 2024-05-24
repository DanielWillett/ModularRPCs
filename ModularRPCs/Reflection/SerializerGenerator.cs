using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;
internal sealed class SerializerGenerator
{
    private static readonly Type[] AutoInjectedTypes =
    [
        typeof(IRpcInvocationPoint),
        typeof(CancellationToken),
        typeof(RpcOverhead),
        typeof(IRpcRouter),
        typeof(IRpcSerializer),
        typeof(IModularRpcConnection),
        typeof(IEnumerable<IModularRpcConnection>),
        typeof(RpcFlags)
    ];

    private readonly ConcurrentDictionary<int, Type> _argBuilders = new ConcurrentDictionary<int, Type>();
    private readonly ConcurrentDictionary<RuntimeMethodHandle, int> _methodSigHashCache = new ConcurrentDictionary<RuntimeMethodHandle, int>();
    private readonly ProxyGenerator _proxyGenerator;
    internal const string GetSizeMethodField = "GetSizeMethod";
    internal const string WriteToStreamMethodField = "WriteStreamMethod";
    internal const string WriteToBytesMethodField = "WriteBytesMethod";
    internal SerializerGenerator(ProxyGenerator proxyGenerator)
    {
        _proxyGenerator = proxyGenerator;
    }
    public Type GetSerializerType(int argCt)
    {
        return _argBuilders.GetOrAdd(argCt, CreateType);
    }
    internal int GetBindingMethodSignatureHash(MethodBase method)
    {
        if (method == null)
            return 0;

        return _methodSigHashCache.GetOrAdd(method.MethodHandle, CreateMethodSignature);
    }
    private Type CreateType(int typeCt)
    {
        string typePrefix = "<" + typeCt.ToString(CultureInfo.InvariantCulture) + ">";

        TypeBuilder typeBuilder = _proxyGenerator.ModuleBuilder.DefineType("SerializerType" + typePrefix, TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public);

        string[] genParamNames = new string[typeCt];
        for (int i = 0; i < typeCt; ++i)
            genParamNames[i] = "T" + i.ToString(CultureInfo.InvariantCulture);

        GenericTypeParameterBuilder[] genParams = typeBuilder.DefineGenericParameters(genParamNames);

        Type[] currentGenericParameters = new Type[genParams.Length];
        for (int i = 0; i < genParams.Length; ++i)
            currentGenericParameters[i] = genParams[i].UnderlyingSystemType;

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

        Type thisType = typeBuilder.MakeGenericType(currentGenericParameters);

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
    private static void InitTypeStatic(Type thisType, Type[] genTypes)
    {
        ProxyGenerator.Instance.SerializerGenerator.InitType(thisType, genTypes);
    }
    internal static bool IsPrimitiveLikeType(Type type)
    {
        return type.IsPrimitive
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid)
#if NET5_0_OR_GREATER
               || type == typeof(Half)
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
        return BitConverter.IsLittleEndian && type.IsPrimitive && (IntPtr.Size == 8 || type != typeof(nint) && type != typeof(nuint));
    }
    internal static bool ShouldBePassedByReference(Type type)
    {
        return type.IsValueType
               && (!IsPrimitiveLikeType(type) || type == typeof(Guid) || type == typeof(decimal))
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

        if (type == typeof(nint) || type == typeof(nuint))
        {
            return 8;
        }

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
            Type? underlyingNullableType = Nullable.GetUnderlyingType(genType);
            il.CommentIfDebug($"== Write {Accessor.Formatter.Format(genType)} ==");
            if (lblNotPrimitive.HasValue)
            {
                il.CommentIfDebug("lblNotPrimitive:");
                il.MarkLabel(lblNotPrimitive.Value);
                lblNotPrimitive = null;
            }

            Label? lblPrimitive;
            if (underlyingNullableType == null && CanQuickSerializeType(genType))
            {
                il.CommentIfDebug("if (lclPreCalc) goto lblPrimitive;");
                lblPrimitive = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, lclPreCalc);
                il.Emit(OpCodes.Brtrue, lblPrimitive.Value);
            }
            else lblPrimitive = null;

            bool isByRef = underlyingNullableType == null && ShouldBePassedByReference(genType);
            il.CommentIfDebug($"size += serializer.WriteObject{(!isByRef ? "<" + Accessor.Formatter.Format(underlyingNullableType ?? genType) + ">" : string.Empty)}(" + (underlyingNullableType != null ? "in " : string.Empty) +
                              $"arg{i.ToString(CultureInfo.InvariantCulture)}, bytes + lclSize, maxSize - lclSize);");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, checked ( (ushort)(i + 3) ));

            MethodInfo writeMtd;
            if (isByRef)
            {
                il.Emit(OpCodes.Mkrefany, genType);
                writeMtd = CommonReflectionCache.RpcSerializerWriteObjectByTRefBytes;
            }
            else if (underlyingNullableType == null)
            {
                LoadFromRef(genType, il);
                writeMtd = CommonReflectionCache.RpcSerializerWriteObjectByValBytes.MakeGenericMethod(genType);
            }
            else
            {
                writeMtd = CommonReflectionCache.RpcSerializerWriteNullableObjectByValBytes.MakeGenericMethod(underlyingNullableType);
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
            Type? underlyingNullableType = Nullable.GetUnderlyingType(genType);
            il.CommentIfDebug($"== Write {Accessor.Formatter.Format(genType)} ==");

            bool isByRef = underlyingNullableType == null && ShouldBePassedByReference(genType);
            il.CommentIfDebug($"size += serializer.WriteObject{(!isByRef ? "<" + Accessor.Formatter.Format(underlyingNullableType ?? genType) + ">" : string.Empty)}(" + (underlyingNullableType != null ? "in " : string.Empty) +
                              $"arg{i.ToString(CultureInfo.InvariantCulture)}, stream);");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, checked ( (ushort)(i + 2) ));
        
            MethodInfo writeMtd;
            if (isByRef)
            {
                il.Emit(OpCodes.Mkrefany, genType);
                writeMtd = CommonReflectionCache.RpcSerializerWriteObjectByTRefStream;
            }
            else if (underlyingNullableType == null)
            {
                LoadFromRef(genType, il);
                writeMtd = CommonReflectionCache.RpcSerializerWriteObjectByValStream.MakeGenericMethod(genType);
            }
            else
            {
                writeMtd = CommonReflectionCache.RpcSerializerWriteNullableObjectByValStream.MakeGenericMethod(underlyingNullableType);
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
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);

        streamField.SetValue(null, writeStream.CreateDelegate(streamField.FieldType));
    }
    internal static void LoadFromRef(Type type, IOpCodeEmitter il)
    {
        if (type == typeof(long))
            il.Emit(OpCodes.Ldind_I8);
        else if (type == typeof(int))
            il.Emit(OpCodes.Ldind_I4);
        else if (type == typeof(uint))
            il.Emit(OpCodes.Ldind_U4);
        else if (type == typeof(short))
            il.Emit(OpCodes.Ldind_I2);
        else if (type == typeof(ushort))
            il.Emit(OpCodes.Ldind_U2);
        else if (type == typeof(sbyte))
            il.Emit(OpCodes.Ldind_I1);
        else if (type == typeof(byte))
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
            il.Emit(OpCodes.Ldobj, type);
        else
            il.Emit(OpCodes.Ldind_Ref);
    }
    internal static void SetToRef(Type type, IOpCodeEmitter il)
    {
        if (type == typeof(long))
            il.Emit(OpCodes.Stind_I8);
        else if (type == typeof(int))
            il.Emit(OpCodes.Stind_I4);
        else if (type == typeof(uint))
            il.Emit(OpCodes.Stind_I4);
        else if (type == typeof(short))
            il.Emit(OpCodes.Stind_I2);
        else if (type == typeof(ushort))
            il.Emit(OpCodes.Stind_I2);
        else if (type == typeof(sbyte))
            il.Emit(OpCodes.Stind_I1);
        else if (type == typeof(byte))
            il.Emit(OpCodes.Stind_I1);
        else if (type == typeof(float))
            il.Emit(OpCodes.Stind_R4);
        else if (type == typeof(double))
            il.Emit(OpCodes.Stind_R8);
        else if (type == typeof(nint))
            il.Emit(OpCodes.Stind_I);
        else if (type == typeof(nuint))
            il.Emit(OpCodes.Stind_I);
        else if (type.IsValueType)
            il.Emit(OpCodes.Stobj, type);
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
            Type genType = genTypes[i];
            Type? underlyingNullableType = Nullable.GetUnderlyingType(genType);
            il.CommentIfDebug($"== Get size of {Accessor.Formatter.Format(genType)} ==");
            if (lblPrimitive.HasValue)
            {
                il.CommentIfDebug("lblPrimitive:");
                il.MarkLabel(lblPrimitive.Value);
            }
            if (IsPrimitiveLikeType(genType))
            {
                il.CommentIfDebug("if (lclPreCalc) goto lblPrimitive;");
                lblPrimitive = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, lclPreCalc);
                il.Emit(OpCodes.Brtrue, lblPrimitive.Value);
            }
            else lblPrimitive = null;

            bool isByRef = underlyingNullableType == null && ShouldBePassedByReference(genType);

            il.CommentIfDebug($"size += serializer.GetSize{(!isByRef ? "<" + Accessor.Formatter.Format(underlyingNullableType ?? genType) + ">" : string.Empty)}(" + (underlyingNullableType != null ? "in " : string.Empty) +
                              $"arg{i.ToString(CultureInfo.InvariantCulture)});");

            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, checked ( (ushort)(i + 1) ));

            if (isByRef)
            {
                il.Emit(OpCodes.Mkrefany, genType);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeByTRef);
            }
            else if (underlyingNullableType == null)
            {
                LoadFromRef(genType, il);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeByVal.MakeGenericMethod(genType));
            }
            else
            {
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeNullableByVal.MakeGenericMethod(underlyingNullableType));
            }

            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclSize);
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
    private static object? InjectFromServiceProvider(object? serviceProvider, Type type)
    {
        return serviceProvider != null ? InjectFromServiceProviderIntl(serviceProvider, type) : null;
    }
    private static object InjectFromServiceProviderIntl(object serviceProvider, Type type)
    {
        return ((IServiceProvider)serviceProvider).GetRequiredService(type);
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

            bool isInjected = Array.Exists(AutoInjectedTypes, x => x.IsAssignableFrom(parameterInfo.ParameterType))
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
            else
            {
                il.Emit(OpCodes.Ldarg_0); // service provider (maybe)
                il.Emit(OpCodes.Ldtoken, injectionType);
                il.Emit(OpCodes.Call, Accessor.GetMethod(Type.GetTypeFromHandle)!);
                il.Emit(OpCodes.Call, Accessor.GetMethod(InjectFromServiceProvider)!);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lcl);
                Label next = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, next);

                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInfo);
                il.Emit(OpCodes.Ldstr, param.Name);
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(injectionType));
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                il.Emit(OpCodes.Call, CommonReflectionCache.StringFormat3);
                il.Emit(OpCodes.Newobj, CommonReflectionCache.RpcInjectionExceptionCtorMessage);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(next);
            }
        }
    }

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
                    type = type.GetElementType()!;
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
    private static void HandleInvocation(MethodInfo method, ParameterInfo[] parameters, LocalBuilder[] injectionLcls, LocalBuilder[] bindLcls, IOpCodeEmitter il, ArraySegment<ParameterInfo> toInject, ArraySegment<ParameterInfo> toBind)
    {
        if (!method.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_1);
            if (method.DeclaringType != null)
            {
                if (method.DeclaringType.IsClass)
                {
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
        if (method.ReturnType == typeof(void))
            il.Emit(OpCodes.Ldnull);
        else if (method.ReturnType.IsValueType)
            il.Emit(OpCodes.Box, method.ReturnType);

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
            Type? underlyingNullableType = Nullable.GetUnderlyingType(type);
            bindLcls[i] = il.DeclareLocal(type);

            bool isPrimitive = CanQuickSerializeType(type);
            bool shouldPassByRef = underlyingNullableType == null && ShouldBePassedByReference(type);
            Label lblDoPrimitiveRead = default;
            Label lblDontPrimitiveRead = default;

            il.CommentIfDebug($"== Read {Accessor.Formatter.Format(type)} ==");
            if (isPrimitive)
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
            if (shouldPassByRef)
            {
                il.CommentIfDebug($"serializer.ReadObject(__makeref(bindLcl{i}), bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer)) ));
                il.Emit(OpCodes.Ldloca, bindLcls[i]);
                il.Emit(OpCodes.Mkrefany, type);
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*)) ));
                il.Emit(OpCodes.Ldloc, lclReadInd);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint)) ));
                il.Emit(OpCodes.Ldloc, lclReadInd);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Ldloca, lclTempByteCt);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadObjectByTRefBytes);
            }
            else if (underlyingNullableType == null)
            {
                il.CommentIfDebug($"bindLcl{i} = serializer.ReadObject<{Accessor.Formatter.Format(type)}>(bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer)) ));
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*)) ));
                il.Emit(OpCodes.Ldloc, lclReadInd);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint)) ));
                il.Emit(OpCodes.Ldloc, lclReadInd);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Ldloca, lclTempByteCt);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadObjectByValBytes.MakeGenericMethod(type));
                il.Emit(OpCodes.Stloc, bindLcls[i]);
            }
            else
            {
                il.CommentIfDebug($"bindLcl{i} = serializer.ReadNullable<{Accessor.Formatter.Format(underlyingNullableType)}>(__makeref(bindLcl{i}), bytes + lclReadInd, maxCount - lclReadInd, out lclTempByteCt);");
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(IRpcSerializer)) ));
                il.Emit(OpCodes.Ldloca, bindLcls[i]);
                il.Emit(OpCodes.Mkrefany, type);
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(byte*)) ));
                il.Emit(OpCodes.Ldloc, lclReadInd);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldarg, checked ( (ushort)Array.IndexOf(ProxyGenerator.RpcInvokeHandlerBytesParams, typeof(uint)) ));
                il.Emit(OpCodes.Ldloc, lclReadInd);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Ldloca, lclTempByteCt);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerReadNullableObjectByTRefBytes.MakeGenericMethod(underlyingNullableType));
            }

            il.CommentIfDebug("lclReadInd += lclTempByteCt;");
            il.Emit(OpCodes.Ldloc, lclReadInd);
            il.Emit(OpCodes.Ldloc, lclTempByteCt);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclReadInd);

            if (!isPrimitive)
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

        il.EmitWriteLine("Read bytes:");
        il.EmitWriteLine(lclReadInd);

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind);
        il.Emit(OpCodes.Ret);
    }
    
    internal void GenerateInvokeStream(MethodInfo method, IOpCodeEmitter il)
    {
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        HandleInjections(method, il, toInject, injectionLcls, false);

        for (int i = 0; i < toBind.Count; ++i)
        {
            bindLcls[i] = il.DeclareLocal(toBind.Array![i + toBind.Offset].ParameterType);
            // todo
        }

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind);
        il.Emit(OpCodes.Ret);
    }
}
