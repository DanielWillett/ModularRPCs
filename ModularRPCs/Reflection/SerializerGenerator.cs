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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using MethodDefinition = DanielWillett.ReflectionTools.Formatting.MethodDefinition;

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
        typeof(RpcFlags)
    ];

    private readonly ConcurrentDictionary<int, Type> _argBuilders = new ConcurrentDictionary<int, Type>();
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
        il.Emit(OpCodes.Stloc_S, lclTypeArray);
        for (int i = 0; i < typeCt; ++i)
        {
            il.CommentIfDebug("lclTypeArray[i] = typeof(" + Accessor.Formatter.Format(genParams[i]) + ");");
            il.Emit(OpCodes.Ldloc_S, lclTypeArray);
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
            MethodInfo getMethodMethod = typeof(Type).GetMethod(nameof(Type.GetMethod), BindingFlags.Public | BindingFlags.Instance, null, [ typeof(string), typeof(BindingFlags) ], null)
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Type.GetMethod))
                                             .DeclaredIn<Type>(isStatic: false)
                                             .WithParameter<string>("name")
                                             .WithParameter<BindingFlags>("bindingAttr")
                                             .Returning<MethodInfo>()
                                         )}");

            MethodInfo invokeMethod = typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), BindingFlags.Public | BindingFlags.Instance, null, [ typeof(object), typeof(object[]) ], null)
                                      ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Type.GetMethod))
                                          .DeclaredIn<MethodBase>(isStatic: false)
                                          .WithParameter<object>("obj")
                                          .WithParameter<object[]>("parameters")
                                          .Returning<object>()
                                      )}");

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
            il.Emit(OpCodes.Ldloc_S, lclTypeArray);
            il.Emit(OpCodes.Stelem_Ref);

            // invoke InitTypeStatic method via reflection since it's private
            il.CommentIfDebug("_ = typeof(SerializerGenerator).GetMethod(\"InitTypeStatic\", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, lclObjArray);");
            il.Emit(OpCodes.Ldtoken, typeof(SerializerGenerator));
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Ldstr, nameof(InitTypeStatic));
            il.Emit(OpCodes.Ldc_I4, (int)(BindingFlags.NonPublic | BindingFlags.Static));
            il.Emit(OpCodes.Callvirt, getMethodMethod);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, lclObjArray);
            il.Emit(OpCodes.Callvirt, invokeMethod);
            il.Emit(OpCodes.Pop);
        }
        else
        {
            // invoke InitTypeStatic
            il.CommentIfDebug("SerializerGenerator.InitTypeStatic(typeof(" + Accessor.Formatter.Format(thisType) + "), lclTypeArray);");
            il.Emit(OpCodes.Ldtoken, thisType);
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Ldloc_S, lclTypeArray);
            il.Emit(OpCodes.Call, Accessor.GetMethod(InitTypeStatic)!);
        }
        il.Emit(OpCodes.Ret);

#if NETSTANDARD2_0
        return typeBuilder.CreateTypeInfo()!;
#else
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
               && (!IsPrimitiveLikeType(type) || type == typeof(Guid) || type == typeof(decimal));
    }
    private static int GetPrimitiveTypeSize(Type type)
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
        MethodInfo[] methods = typeof(IRpcSerializer).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        MakeGetSizeMethod(thisType, genTypes, methods);
        MakeWriteMethods(thisType, genTypes, methods);
    }
    private void MakeWriteMethods(Type thisType, Type[] genTypes, MethodInfo[] iRpcSerializerMethods)
    {
        MethodInfo writeRefMethodBytes = iRpcSerializerMethods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject) && !x.IsGenericMethod && x.GetParameters() is { Length: 3 } p && p[0].ParameterType == typeof(TypedReference))
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                             .DeclaredIn<IRpcSerializer>(isStatic: false)
                                             .WithParameter(typeof(TypedReference), "value")
                                             .WithParameter(typeof(byte*), "bytes")
                                             .WithParameter<uint>("maxSize")
                                             .Returning<int>()
                                         )}");

        MethodInfo writeRefMethodStream = iRpcSerializerMethods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject) && !x.IsGenericMethod && x.GetParameters() is { Length: 3 } p && p[0].ParameterType == typeof(TypedReference))
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                             .DeclaredIn<IRpcSerializer>(isStatic: false)
                                             .WithParameter(typeof(TypedReference), "value")
                                             .WithParameter<Stream>("stream")
                                             .Returning<int>()
                                         )}");

        MethodInfo writeMethodBytes = iRpcSerializerMethods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject) && x.IsGenericMethod && x.GetParameters() is { Length: 3 } p && p[0].ParameterType == x.GetGenericArguments()[0])
                                      ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                          .DeclaredIn<IRpcSerializer>(isStatic: false)
                                          .WithGenericParameterDefinition("T")
                                          .WithParameterUsingGeneric(0, "value")
                                          .WithParameter(typeof(byte*), "bytes")
                                          .WithParameter<uint>("maxSize")
                                          .Returning<int>()
                                      )}");

        MethodInfo writeMethodStream = iRpcSerializerMethods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject) && x.IsGenericMethod && x.GetParameters() is { Length: 3 } p && p[0].ParameterType == x.GetGenericArguments()[0])
                                      ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                          .DeclaredIn<IRpcSerializer>(isStatic: false)
                                          .WithGenericParameterDefinition("T")
                                          .WithParameterUsingGeneric(0, "value")
                                          .WithParameter<Stream>("stream")
                                          .Returning<int>()
                                      )}");

        FieldInfo bytesField = thisType.GetField(WriteToBytesMethodField, BindingFlags.Public | BindingFlags.Static)
                          ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new FieldDefinition(WriteToBytesMethodField)
                              .DeclaredIn(thisType, isStatic: true)
                              .WithFieldType<Delegate>()
                          )}");

        FieldInfo streamField = thisType.GetField(WriteToStreamMethodField, BindingFlags.Public | BindingFlags.Static)
                          ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new FieldDefinition(WriteToStreamMethodField)
                              .DeclaredIn(thisType, isStatic: true)
                              .WithFieldType<Delegate>()
                          )}");

        MethodInfo getCanPreCalcPrimitives = typeof(IRpcSerializer).GetProperty(nameof(IRpcSerializer.CanFastReadPrimitives), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                             ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(IRpcSerializer.CanFastReadPrimitives))
                                                 .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                 .WithPropertyType<bool>()
                                                 .WithNoSetter()
                                             )}");

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
        il.Emit(OpCodes.Callvirt, getCanPreCalcPrimitives);
        il.Emit(OpCodes.Stloc, lclPreCalc);

        il.CommentIfDebug("int lclSize = 0;");
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, lclSize);

        Label? lblNotPrimitive = null;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
            if (lblNotPrimitive.HasValue)
            {
                il.CommentIfDebug("lblNotPrimitive:");
                il.MarkLabel(lblNotPrimitive.Value);
                lblNotPrimitive = null;
            }

            Label? lblPrimitive;
            if (CanQuickSerializeType(genType))
            {
                il.CommentIfDebug("if (lclPreCalc) goto lblPrimitive;");
                lblPrimitive = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, lclPreCalc);
                il.Emit(OpCodes.Brtrue, lblPrimitive.Value);
            }
            else lblPrimitive = null;

            bool isByRef = ShouldBePassedByReference(genType);
            il.CommentIfDebug($"size += serializer.WriteObject{(!isByRef ? "<" + Accessor.Formatter.Format(genType) + ">" : string.Empty)}(" +
                                $"arg{i.ToString(CultureInfo.InvariantCulture)}, bytes + lclSize, maxSize - lclSize);");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, i + 3);

            MethodInfo writeMtd;
            if (isByRef)
            {
                il.Emit(OpCodes.Mkrefany, genType);
                writeMtd = writeRefMethodBytes;
            }
            else
            {
                LoadFromRef(genType, il);
                writeMtd = writeMethodBytes.MakeGenericMethod(genType);
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
            il.Emit(OpCodes.Ldarg, i + 3);
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
        il.Emit(OpCodes.Ldloc_S, lclSize);
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
        il.Emit(OpCodes.Callvirt, getCanPreCalcPrimitives);
        il.Emit(OpCodes.Stloc, lclPreCalc);
        
        il.CommentIfDebug("int lclSize = 0;");
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, lclSize);
        
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
        
            bool isByRef = ShouldBePassedByReference(genType);
            il.CommentIfDebug($"size += serializer.WriteObject{(!isByRef ? "<" + Accessor.Formatter.Format(genType) + ">" : string.Empty)}(" +
                              $"arg{i.ToString(CultureInfo.InvariantCulture)}, stream);");
        
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, i + 2);
        
            MethodInfo writeMtd;
            if (isByRef)
            {
                il.Emit(OpCodes.Mkrefany, genType);
                writeMtd = writeRefMethodStream;
            }
            else
            {
                LoadFromRef(genType, il);
                writeMtd = writeMethodStream.MakeGenericMethod(genType);
            }
        
            il.Emit(OpCodes.Ldarg_1);
        
            il.Emit(OpCodes.Callvirt, writeMtd);
            il.Emit(OpCodes.Ldloc, lclSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclSize);
        }
        
        il.CommentIfDebug("return lclSize;");
        il.Emit(OpCodes.Ldloc_S, lclSize);
        il.Emit(OpCodes.Ret);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);

        streamField.SetValue(null, writeStream.CreateDelegate(streamField.FieldType));
    }
    private static void LoadFromRef(Type type, IOpCodeEmitter il)
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
    private static void SetToRef(Type type, IOpCodeEmitter il)
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
    private void MakeGetSizeMethod(Type thisType, Type[] genTypes, MethodInfo[] iRpcSerializerMethods)
    {
        MethodInfo getSizeTypeRefMethod = iRpcSerializerMethods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize) && !x.IsGenericMethod && x.GetParameters() is { Length: 1 } p && p[0].ParameterType == typeof(TypedReference))
                ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.GetSize))
                    .DeclaredIn<IRpcSerializer>(isStatic: false)
                    .WithParameter(typeof(TypedReference), "value")
                    .Returning<int>()
                )}");

        MethodInfo getSizeTypeMethod = iRpcSerializerMethods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize) && x.IsGenericMethod && x.GetParameters() is { Length: 1 } p && p[0].ParameterType == x.GetGenericArguments()[0])
                ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.GetSize))
                    .DeclaredIn<IRpcSerializer>(isStatic: false)
                    .WithGenericParameterDefinition("T")
                    .WithParameterUsingGeneric(0, "value")
                    .Returning<int>()
                )}");

        FieldInfo field = thisType.GetField(GetSizeMethodField, BindingFlags.Public | BindingFlags.Static)
                          ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new FieldDefinition(GetSizeMethodField)
                              .DeclaredIn(thisType, isStatic: true)
                              .WithFieldType<Delegate>()
                          )}");

        MethodInfo getCanPreCalcPrimitives = typeof(IRpcSerializer).GetProperty(nameof(IRpcSerializer.CanFastReadPrimitives), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                             ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(IRpcSerializer.CanFastReadPrimitives))
                                                 .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                 .WithPropertyType<bool>()
                                                 .WithNoSetter()
                                             )}");

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
        il.Emit(OpCodes.Callvirt, getCanPreCalcPrimitives);
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
        il.Emit(OpCodes.Stloc_S, lclSize);

        Label? lblPrimitive = null;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
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

            bool isByRef = ShouldBePassedByReference(genType);

            il.CommentIfDebug($"size += serializer.GetSize{(!isByRef ? "<" + Accessor.Formatter.Format(genType) + ">" : string.Empty)}(" +
                              $"arg{i.ToString(CultureInfo.InvariantCulture)});");

            il.Emit(OpCodes.Ldloc_S, lclSize);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, i + 1);

            if (isByRef)
            {
                il.Emit(OpCodes.Mkrefany, genType);
                il.Emit(OpCodes.Callvirt, getSizeTypeRefMethod);
            }
            else
            {
                LoadFromRef(genType, il);
                il.Emit(OpCodes.Callvirt, getSizeTypeMethod.MakeGenericMethod(genType));
            }

            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_S, lclSize);
        }

        if (lblPrimitive.HasValue)
        {
            il.CommentIfDebug("lblPrimitive:");
            il.MarkLabel(lblPrimitive.Value);
        }

        il.CommentIfDebug("return lclSize;");
        il.Emit(OpCodes.Ldloc_S, lclSize);
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
                il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(CancellationToken)));
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (injectionType.CouldBeAssignedTo<RpcOverhead>())
            {
                il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(RpcOverhead)));

                if (!injectionType.IsAssignableFrom(typeof(RpcOverhead)))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IRpcInvocationPoint).IsAssignableFrom(injectionType))
            {
                MethodInfo getRpcInvPt = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.Rpc), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(RpcOverhead.Rpc))
                                             .DeclaredIn<RpcOverhead>(isStatic: false)
                                             .WithPropertyType<IRpcInvocationPoint>()
                                             .WithNoSetter()
                                         )}.");

                il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(RpcOverhead)));

                il.Emit(getRpcInvPt.GetCallRuntime(), getRpcInvPt);

                if (injectionType != typeof(IRpcInvocationPoint))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (injectionType == typeof(RpcFlags))
            {
                MethodInfo getRpcInvPt = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.Flags), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(RpcOverhead.Flags))
                                             .DeclaredIn<RpcOverhead>(isStatic: false)
                                             .WithPropertyType<RpcFlags>()
                                             .WithNoSetter()
                                         )}.");

                il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(RpcOverhead)));

                il.Emit(getRpcInvPt.GetCallRuntime(), getRpcInvPt);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IRpcSerializer).IsAssignableFrom(injectionType))
            {
                il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(IRpcSerializer)));

                if (injectionType != typeof(IRpcSerializer))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IRpcRouter).IsAssignableFrom(injectionType))
            {
                il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(IRpcRouter)));

                if (injectionType != typeof(IRpcRouter))
                    il.Emit(OpCodes.Castclass, injectionType);
                il.Emit(OpCodes.Stloc, lcl);
            }
            else if (typeof(IModularRpcConnection).IsAssignableFrom(injectionType))
            {
                if (typeof(IModularRpcLocalConnection).IsAssignableFrom(injectionType))
                {
                    MethodInfo getConn = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.ReceivingConnection), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(RpcOverhead.ReceivingConnection))
                                             .DeclaredIn<RpcOverhead>(isStatic: false)
                                             .WithPropertyType<IModularRpcLocalConnection>()
                                             .WithNoSetter()
                                         )}.");

                    il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(RpcOverhead)));

                    il.Emit(getConn.GetCallRuntime(), getConn);

                    if (!injectionType.IsAssignableFrom(typeof(IModularRpcLocalConnection)))
                        il.Emit(OpCodes.Castclass, injectionType);
                }
                else
                {
                    MethodInfo getConn = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.SendingConnection), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(RpcOverhead.SendingConnection))
                                             .DeclaredIn<RpcOverhead>(isStatic: false)
                                             .WithPropertyType<IModularRpcRemoteConnection>()
                                             .WithNoSetter()
                                         )}.");

                    il.Emit(OpCodes.Ldarg, Array.IndexOf(paramArray, typeof(RpcOverhead)));

                    il.Emit(getConn.GetCallRuntime(), getConn);

                    if (!injectionType.IsAssignableFrom(typeof(IModularRpcRemoteConnection)))
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

                MethodInfo stringFormat3 = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, [ typeof(string), typeof(object), typeof(object), typeof(object) ], null)
                                           ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(string.Format))
                                               .WithParameter<string>("format")
                                               .WithParameter<object>("arg0")
                                               .WithParameter<object>("arg1")
                                               .WithParameter<object>("arg2")
                                               .Returning<string>()
                                           )}");

                ConstructorInfo typeNotFoundCtor = typeof(RpcInjectionException).GetConstructor([ typeof(string) ])
                                                   ?? throw new MemberAccessException($"Failed to find {Accessor.Formatter.Format(new MethodDefinition(typeof(RpcInjectionException))
                                                       .WithParameter<string>("message")
                                                   )}.");

                il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInfo);
                il.Emit(OpCodes.Ldstr, param.Name);
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(injectionType));
                il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                il.Emit(OpCodes.Call, stringFormat3);
                il.Emit(OpCodes.Newobj, typeNotFoundCtor);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(next);
            }
        }
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
                    MethodInfo stringFormat2 = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, [ typeof(string), typeof(object), typeof(object) ], null)
                                               ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(string.Format))
                                                   .WithParameter<string>("format")
                                                   .WithParameter<object>("arg0")
                                                   .WithParameter<object>("arg1")
                                                   .Returning<string>()
                                               )}");

                    ConstructorInfo typeNotFoundCtor = typeof(RpcInjectionException).GetConstructor([ typeof(string) ])
                                                       ?? throw new MemberAccessException($"Failed to find {Accessor.Formatter.Format(new MethodDefinition(typeof(RpcInjectionException))
                                                           .WithParameter<string>("message")
                                                       )}.");

                    il.Emit(OpCodes.Castclass, method.DeclaringType);
                    il.Emit(OpCodes.Dup);
                    Label lblDontThrowNullRef = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue, lblDontThrowNullRef);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldstr, Properties.Exceptions.RpcInjectionExceptionInstanceNull);
                    il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method.DeclaringType));
                    il.Emit(OpCodes.Ldstr, Accessor.ExceptionFormatter.Format(method));
                    il.Emit(OpCodes.Call, stringFormat2);
                    il.Emit(OpCodes.Newobj, typeNotFoundCtor);
                    il.Emit(OpCodes.Throw);
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
            if (toBind.Count != 0)
            {
                int ind = Array.IndexOf(toBind.Array!, parameters[i], toBind.Offset, toBind.Count);
                if (ind != -1)
                    lcl = bindLcls[ind];
            }

            if (lcl == null)
            {
                int ind = Array.IndexOf(toInject.Array!, parameters[i], toInject.Offset, toInject.Count);
                lcl = injectionLcls[ind];
            }

            il.Emit(OpCodes.Ldloc, lcl);
        }

        il.Emit(method.GetCallRuntime(), method);
    }
    internal void GenerateInvokeBytes(MethodInfo method, DynamicMethod dynMethod, IOpCodeEmitter il)
    {
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        HandleInjections(method, il, toInject, injectionLcls, true);

        for (int i = 0; i < toBind.Count; ++i)
        {
            bindLcls[i] = il.DeclareLocal(toBind.Array![i + toBind.Offset].ParameterType);

        }

        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind);
        il.Emit(OpCodes.Ret);
    }
    
    internal void GenerateInvokeStream(MethodInfo method, DynamicMethod dynMethod, IOpCodeEmitter il)
    {
        ParameterInfo[] parameters = method.GetParameters();
        BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

        LocalBuilder[] injectionLcls = new LocalBuilder[toInject.Count];
        LocalBuilder[] bindLcls = new LocalBuilder[toBind.Count];

        HandleInjections(method, il, toInject, injectionLcls, false);

        for (int i = 0; i < toBind.Count; ++i)
            bindLcls[i] = il.DeclareLocal(toBind.Array![i + toBind.Offset].ParameterType);


        HandleInvocation(method, parameters, injectionLcls, bindLcls, il, toInject, toBind);
        il.Emit(OpCodes.Ret);
    }
}
