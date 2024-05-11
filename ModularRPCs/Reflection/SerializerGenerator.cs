using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Reflection;
internal sealed class SerializerGenerator
{
    private ConcurrentDictionary<int, Type> _argBuilders = new ConcurrentDictionary<int, Type>();
    private readonly ProxyGenerator _proxyGenerator;
    internal const string GetSizeMethodField = "GetSizeMethod";
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
        TypeBuilder typeBuilder = _proxyGenerator.ModuleBuilder.DefineType($"SerializerType<{typeCt}>", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public);

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
        il.Emit(OpCodes.Ldc_I4, typeCt);
        il.Emit(OpCodes.Newarr, typeof(Type));
        il.Emit(OpCodes.Stloc_S, lclTypeArray);
        for (int i = 0; i < typeCt; ++i)
        {
            il.Emit(OpCodes.Ldloc_S, lclTypeArray);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldtoken, genParams[i]);
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Stelem_Ref);
        }

        // ReSharper disable once CoVariantArrayConversion
        Type[] paramsArray = GetTypeParams(currentGenericParameters, 1, 0, false);
        paramsArray[0] = typeof(IRpcSerializer);

        Type deleType = DelegateUtility.CreateDelegateType("TestDeleType", null, genParamNames, typeof(int), paramsArray, [ "serializer" ], null, _proxyGenerator);

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
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, lclObjArray);
            il.Emit(OpCodes.Ldloc, lclObjArray);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldtoken, typeBuilder.MakeGenericType(currentGenericParameters));
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
            il.Emit(OpCodes.Stelem_Ref);
            il.Emit(OpCodes.Ldloc, lclObjArray);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldloc_S, lclTypeArray);
            il.Emit(OpCodes.Stelem_Ref);

            // invoke InitTypeStatic method via reflection since it's private
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
            il.Emit(OpCodes.Ldtoken, typeBuilder.MakeGenericType(currentGenericParameters));
            il.Emit(OpCodes.Call, getTypeFromHandleMethod);

            il.Emit(OpCodes.Ldloc_S, lclTypeArray);
            il.Emit(OpCodes.Call, Accessor.GetMethod(InitTypeStatic)!);
        }
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineField(GetSizeMethodField, deleType, FieldAttributes.Static | FieldAttributes.Public);

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
            return IntPtr.Size;
        }

        throw new ArgumentException("Not a primitve type.");
    }
    private void InitType(Type thisType, Type[] genTypes)
    {
        MethodInfo[] methods = typeof(IRpcSerializer).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        MethodInfo getSizeTypeRefMethod = methods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize) && !x.IsGenericMethod && x.GetParameters() is { Length: 1 } p && p[0].ParameterType == typeof(TypedReference))
                ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.GetSize))
                    .DeclaredIn<IRpcSerializer>(isStatic: false)
                    .WithParameter(typeof(TypedReference), "value")
                    .Returning<int>()
                )}");

        MethodInfo getSizeTypeMethod = methods.FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize) && x.IsGenericMethod && x.GetParameters() is { Length: 1 } p && p[0].ParameterType == x.GetGenericArguments()[0])
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

        Type[] paramsArray = GetTypeParams(genTypes, 1, 0, false);
        paramsArray[0] = typeof(IRpcSerializer);

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);
        DynamicMethod getSize = new DynamicMethod("GetSize", attr, conv, typeof(int), paramsArray, _proxyGenerator.ModuleBuilder, true);

        DefineParams(getSize, genTypes, 1, true);
        getSize.DefineParameter(1, ParameterAttributes.None, "serializer");
        IOpCodeEmitter il = getSize.AsEmitter(debuggable: ProxyGenerator.DebugPrint, addBreakpoints: ProxyGenerator.BreakpointPrint);

        LocalBuilder sizeLcl = il.DeclareLocal(typeof(int));
        int ttl = 0;
        for (int i = 0; i < genTypes.Length; i++)
        {
            if (IsPrimitiveLikeType(genTypes[i]))
                ttl += GetPrimitiveTypeSize(genTypes[i]);
        }

        il.Emit(OpCodes.Ldc_I4, ttl);
        il.Emit(OpCodes.Stloc_S, sizeLcl);

        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
            if (IsPrimitiveLikeType(genType))
                continue;

            il.Emit(OpCodes.Ldloc_S, sizeLcl);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, i + 1);

            if (ShouldBePassedByReference(genType))
            {
                il.Emit(OpCodes.Mkrefany, genType);
                il.Emit(OpCodes.Callvirt, getSizeTypeRefMethod);
            }
            else
            {
                il.Emit(OpCodes.Ldind_Ref);
                il.Emit(OpCodes.Callvirt, getSizeTypeMethod.MakeGenericMethod(genType));
            }

            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_S, sizeLcl);
        }

        il.Emit(OpCodes.Ldloc_S, sizeLcl);
        il.Emit(OpCodes.Ret);

        field.SetValue(null, getSize.CreateDelegate(field.FieldType));
    }
    private static void DefineParams(object methodBuilder, Type[] genTypes, int countBefore, bool nonPrim, bool valueTypesByRef = true)
    {
        int pInd = countBefore;
        for (int i = 0; i < genTypes.Length; ++i)
        {
            Type genType = genTypes[i];
            if (nonPrim && IsPrimitiveLikeType(genType))
                continue;

            string name = "arg" + i.ToString(CultureInfo.InvariantCulture);

            ParameterAttributes attr = valueTypesByRef && ShouldBePassedByReference(genType) ? ParameterAttributes.In : ParameterAttributes.None;

            if (methodBuilder is MethodBuilder methodBuilder2)
                methodBuilder2.DefineParameter(++pInd, attr, name);
            else if (methodBuilder is DynamicMethod dynamicMethod)
                dynamicMethod.DefineParameter(++pInd, attr, name);
        }
    }
}
