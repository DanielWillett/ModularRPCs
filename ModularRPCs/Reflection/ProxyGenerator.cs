using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Creates inherited proxy types for classes with virtual or abstract methods decorated with the <see cref="RpcSendAttribute"/> to provide implementations of them at runtime.
/// </summary>
public sealed class ProxyGenerator
{
    private readonly ConcurrentDictionary<Type, Type> _proxies = new ConcurrentDictionary<Type, Type>();
    private readonly ConstructorInfo _identifierErrorConstructor;

    /// <summary>
    /// Set using <see cref="LoggingExtensions.SetLogger(ProxyGenerator, ILogger)"/>. 
    /// </summary>
    internal object? Logger;

    /// <summary>
    /// Name of the private field used to store instances in a proxy class that implemnets <see cref="IRpcObject{T}"/>.
    /// </summary>
    public string InstancesFieldName => "_instances<RPC_Proxy>";
    internal AssemblyBuilder AssemblyBuilder { get; }
    internal ModuleBuilder ModuleBuilder { get; }

    /// <summary>
    /// The assembly name being used to store dynamically generated types and methods.
    /// </summary>
    public AssemblyName ProxyAssemblyName { get; }

    /// <summary>
    /// The singleton instance of <see cref="ProxyGenerator"/>, which stores information about the assembly used to store dynamically generated types.
    /// </summary>
    public static ProxyGenerator Instance { get; } = new ProxyGenerator();

    static ProxyGenerator() { }
    private ProxyGenerator()
    {
        Assembly thisAssembly = Assembly.GetExecutingAssembly();
        ProxyAssemblyName = new AssemblyName(thisAssembly.GetName().Name + ".Proxy");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(ProxyAssemblyName.Name!);
        _identifierErrorConstructor = typeof(RpcObjectInitializationException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [ typeof(string) ], null)
                                      ?? throw new MemberAccessException($"Failed to find RpcObjectInitializationException(string).");
    }

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>() where TRpcClass : class
        => CreateProxy<TRpcClass>(false, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(bool nonPublic) where TRpcClass : class
        => CreateProxy<TRpcClass>(nonPublic, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(params object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(false, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(bool nonPublic, params object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(nonPublic, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(bool nonPublic, Binder? binder, object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(nonPublic, binder, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(bool nonPublic, Binder? binder, object[] constructorParameters, CultureInfo culture, object[]? activationAttributes) where TRpcClass : class
        => (TRpcClass)CreateProxy(typeof(TRpcClass), nonPublic, binder, constructorParameters, culture, activationAttributes);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(Type type)
        => CreateProxy(type, false, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(Type type, bool nonPublic)
        => CreateProxy(type, nonPublic, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(Type type, params object[] constructorParameters)
        => CreateProxy(type, false, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(Type type, bool nonPublic, params object[] constructorParameters)
        => CreateProxy(type, nonPublic, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(Type type, bool nonPublic, Binder? binder, object[] constructorParameters)
        => CreateProxy(type, nonPublic, binder, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(Type type, bool nonPublic, Binder? binder, object[] constructorParameters, CultureInfo culture, object[]? activationAttributes)
    {
        Type newType = _proxies.GetOrAdd(type, CreateProxyType);

        try
        {
            object newProxiedObject = Activator.CreateInstance(
                newType,
                BindingFlags.CreateInstance | BindingFlags.Instance | (nonPublic ? BindingFlags.Public | BindingFlags.NonPublic : BindingFlags.Public),
                binder,
                constructorParameters ?? Array.Empty<object>(),
                culture,
                activationAttributes
            )!;

            return newProxiedObject;
        }
        catch (MissingMethodException ex)
        {
            throw new MissingMethodException(Properties.Exceptions.PrivatesNotVisibleMissingMethodException, ex);
        }
        catch (TargetInvocationException ex)
        {
            if (ex.InnerException is not MemberAccessException mae)
                throw ex.InnerException ?? ex;

            if (mae is MethodAccessException)
                throw new MethodAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, mae);

            throw new MemberAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, mae);
        }
        catch (MethodAccessException ex)
        {
            throw new MethodAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, ex);
        }
        catch (MemberAccessException ex)
        {
            throw new MemberAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, ex);
        }
    }

    /// <summary>
    /// Returns the RPC proxy type of <typeparamref name="TRpcClass"/>.
    /// </summary>
    /// <remarks>The RPC proxy type overrides virtual methods decorated with the <see cref="RpcSendAttribute"/>.</remarks>
    /// <exception cref="ArgumentException"/>
    public Type GetProxyType<TRpcClass>() where TRpcClass : class
        => GetProxyType(typeof(TRpcClass));

    /// <summary>
    /// Returns the RPC proxy type of <paramref name="type"/>.
    /// </summary>
    /// <remarks>The RPC proxy type overrides virtual methods decorated with the <see cref="RpcSendAttribute"/>.</remarks>
    /// <exception cref="ArgumentException"/>
    public Type GetProxyType(Type type)
    {
        return _proxies.GetOrAdd(type, CreateProxyType);
    }
    private TypeBuilder StartProxyType(Type type, bool typeGivesInternalAccess)
    {
        const bool debugPrint = true;

        TypeBuilder typeBuilder = ModuleBuilder.DefineType(type.Name + "<RPC_Proxy>",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, type);
        Type? interfaceType = type.GetInterfaces().FirstOrDefault(intx => intx.IsGenericType && intx.GetGenericTypeDefinition() == typeof(IRpcObject<>));
        Type? idType = interfaceType?.GenericTypeArguments[0];
        Type dictType;
        MethodInfo dictTryAddMethod;
        FieldBuilder dictField;
        if (idType != null)
        {
            dictType = typeof(ConcurrentDictionary<,>).MakeGenericType(idType, type);

            dictTryAddMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryAdd), BindingFlags.Instance | BindingFlags.Public)
                                          ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(
                                              new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryAdd))
                                                  .Returning<bool>()
                                                  .DeclaredIn(dictType, isStatic: false)
                                                  .WithParameter(idType, "key")
                                                  .WithParameter(type, "value")
                                              )}.");

            dictField = typeBuilder.DefineField(
                InstancesFieldName,
                dictType,
                FieldAttributes.Private | FieldAttributes.InitOnly
            );
        }
        else
        {
            dictType = null!;
            dictTryAddMethod = null!;
            dictField = null!;
        }

        // create constructors for all base constructors
        foreach (ConstructorInfo baseCtor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (baseCtor.IsPrivate)
                continue;

            if (!VisibilityUtility.IsMethodOverridable(baseCtor, typeGivesInternalAccess))
            {
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.ConstructorNotVisibileToOverridingClasses, Accessor.Formatter.Format(baseCtor), type.FullName));
                else
                    Console.WriteLine(Properties.Logging.ConstructorNotVisibileToOverridingClasses, Accessor.Formatter.Format(baseCtor), type.FullName);
                continue;
            }

            ParameterInfo[] parameters = baseCtor.GetParameters();
            Type[] types = new Type[parameters.Length];
            Type[][] reqMods = new Type[parameters.Length][];
            Type[][] optMods = new Type[parameters.Length][];
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo p = parameters[i];
                types[i] = p.ParameterType;
                reqMods[i] = p.GetRequiredCustomModifiers();
                optMods[i] = p.GetOptionalCustomModifiers();
            }

            ConstructorBuilder builder = typeBuilder.DefineConstructor(baseCtor.Attributes & ~MethodAttributes.HasSecurity, baseCtor.CallingConvention, types, reqMods, optMods);

            IOpCodeEmitter il = builder.GetILGenerator().AsEmitter(debuggable: debugPrint);
            il.Emit(OpCodes.Ldarg_0);

            for (int i = 0; i < types.Length; ++i)
                EmitUtility.EmitArgument(il, i + 1, false);

            il.Emit(OpCodes.Call, baseCtor);
            if (idType != null)
            {
                MethodInfo identifierGetter = interfaceType!
                                                  .GetProperty(nameof(IRpcObject<int>.Identifier), BindingFlags.Public | BindingFlags.Instance)?
                                                  .GetGetMethod()
                                              ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(IRpcObject<int>.Identifier))
                                                  .DeclaredIn(interfaceType, isStatic: false)
                                                  .WithPropertyType(idType)
                                                  .WithNoSetter()
                                                  )}.");

                identifierGetter = Accessor.GetImplementedMethod(type, identifierGetter) ?? identifierGetter;

                LocalBuilder identifier = il.DeclareLocal(idType);

                Label ifNotDefault = il.DefineLabel();
                Label ifDidntAdd = il.DefineLabel();
                Label? ifDefault = null;
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(identifierGetter.GetCallRuntime(), identifierGetter);
                if (idType.IsValueType)
                {
                    if (idType.IsPrimitive && (idType == typeof(long)
                                               || idType == typeof(ulong)
                                               || idType == typeof(int)
                                               || idType == typeof(uint)
                                               || idType == typeof(short)
                                               || idType == typeof(ushort)
                                               || idType == typeof(sbyte)
                                               || idType == typeof(byte)
                                               || idType == typeof(char)
                                               || idType == typeof(float)
                                               || idType == typeof(double)
                                               || idType == typeof(nint)
                                               || idType == typeof(nuint)))
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc_S, identifier);
                        il.Emit(OpCodes.Brtrue, ifNotDefault);
                    }
                    else
                    {
                        LocalBuilder lclCheck = il.DeclareLocal(idType);

                        il.Emit(OpCodes.Stloc_S, identifier);
                        il.Emit(OpCodes.Ldloca_S, lclCheck);
                        il.Emit(OpCodes.Initobj, idType);
                        il.Emit(OpCodes.Ldloca_S, identifier);
                        Type equatableType = typeof(IEquatable<>).MakeGenericType(idType);
                        if (equatableType.IsAssignableFrom(idType))
                        {
                            MethodInfo equal = equatableType.GetMethod(nameof(IEquatable<int>.Equals), BindingFlags.Public | BindingFlags.Instance)
                                                ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IEquatable<int>.Equals))
                                                    .DeclaredIn(equatableType, isStatic: false)
                                                    .WithParameter(idType, "other")
                                                    .Returning<bool>())
                                                }.");
                            equal = Accessor.GetImplementedMethod(idType, equal) ?? equal;
                            il.Emit(OpCodes.Ldloc_S, lclCheck);
                            il.Emit(equal.GetCallRuntime(), equal);
                            il.Emit(OpCodes.Brfalse, ifNotDefault);
                        }
                        else
                        {
                            Type comparableType = typeof(IComparable<>).MakeGenericType(idType);
                            if (equatableType.IsAssignableFrom(idType))
                            {
                                MethodInfo equal = comparableType.GetMethod(nameof(IComparable<int>.CompareTo), BindingFlags.Public | BindingFlags.Instance)
                                                   ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IComparable<int>.CompareTo))
                                                       .DeclaredIn(comparableType, isStatic: false)
                                                       .WithParameter(idType, "other")
                                                       .Returning<int>())
                                                   }.");
                                equal = Accessor.GetImplementedMethod(idType, equal) ?? equal;
                                il.Emit(OpCodes.Ldloc_S, lclCheck);
                                il.Emit(equal.GetCallRuntime(), equal);
                                il.Emit(OpCodes.Brtrue, ifNotDefault);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldloca_S, identifier);
                                MethodInfo? refEqual = idType.GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ idType.MakeByRefType() ], null);
                                if (refEqual != null)
                                {
                                    il.Emit(OpCodes.Ldloca_S, lclCheck);
                                    il.Emit(OpCodes.Call, refEqual);
                                }
                                else
                                {
                                    MethodInfo equal = typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(object) ], null)
                                                       ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Equals))
                                                           .DeclaredIn(typeof(object), isStatic: false)
                                                           .WithParameter<object?>("obj")
                                                           .Returning<bool>())
                                                       }.");
                                    il.Emit(OpCodes.Ldloc_S, lclCheck);
                                    il.Emit(OpCodes.Callvirt, equal);
                                }
                                il.Emit(OpCodes.Brfalse, ifNotDefault);
                            }
                        }
                    }
                }
                else if (idType == typeof(string))
                {
                    ifDefault = il.DefineLabel();
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc_S, identifier);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Beq, ifDefault.Value);
                    il.Emit(OpCodes.Ldloc_S, identifier);
                    MethodInfo getStrLen = typeof(string)
                                            .GetProperty(nameof(string.Length), BindingFlags.Public | BindingFlags.Instance)?
                                            .GetMethod
                                           ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(string.Length))
                                               .DeclaredIn<string>(isStatic: false)
                                               .WithPropertyType<int>()
                                               .WithNoSetter())
                                           }.");
                    il.Emit(OpCodes.Call, getStrLen);
                    il.Emit(OpCodes.Brtrue, ifNotDefault);
                }
                else
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc_S, identifier);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Bne_Un, ifNotDefault);
                }

                if (ifDefault.HasValue)
                    il.MarkLabel(ifDefault.Value);
#if DEBUG
                il.EmitWriteLine($"Had a default value for instance id of type {Accessor.Formatter.Format(type)}.");
#endif
                il.Emit(OpCodes.Ldstr, string.Format(Properties.Exceptions.InstanceIdDefaultValue, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(interfaceType)));
                il.Emit(OpCodes.Newobj, _identifierErrorConstructor);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(ifNotDefault);
                il.Emit(OpCodes.Ldsfld, dictField);

                il.Emit(OpCodes.Ldloc_S, identifier);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, type);

                il.Emit(OpCodes.Call, dictTryAddMethod);

                il.Emit(OpCodes.Brfalse, ifDidntAdd);
#if DEBUG
                il.EmitWriteLine($"Added instance of {Accessor.Formatter.Format(type)} to dictionary.");
#endif
                il.Emit(OpCodes.Ret);
                
                il.MarkLabel(ifDidntAdd);
#if DEBUG
                il.EmitWriteLine($"Instance of {Accessor.Formatter.Format(type)} already exists.");
#endif
                il.Emit(OpCodes.Ldstr, string.Format(Properties.Exceptions.InstanceWithThisIdAlreadyExists, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(interfaceType)));
                il.Emit(OpCodes.Newobj, _identifierErrorConstructor);
                il.Emit(OpCodes.Throw);
            }
#if DEBUG
            il.EmitWriteLine($"Created RPC proxy for {Accessor.Formatter.Format(type)}.");
#endif

            il.Emit(OpCodes.Ret);
        }

        if (idType != null)
        {
            ConstructorInfo dictCtor = dictType.GetConstructor(Type.EmptyTypes)
                                       ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(dictType)
                                           .WithNoParameters())
                                       }.");

            MethodInfo dictTryGetValueMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryGetValue), BindingFlags.Instance | BindingFlags.Public)
                                    ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryGetValue))
                                        .DeclaredIn(dictType, isStatic: false)
                                        .Returning<bool>()
                                        .WithParameter(idType, "key")
                                        .WithParameter(type.MakeByRefType(), "value", ByRefTypeMode.Out))
                                    }.");

            ConstructorBuilder typeInitializer = typeBuilder.DefineTypeInitializer();
            IOpCodeEmitter il = typeInitializer.GetILGenerator().AsEmitter(debuggable: debugPrint);
            il.Emit(OpCodes.Newobj, dictCtor);
            il.Emit(OpCodes.Stsfld, dictField);
            il.Emit(OpCodes.Ret);

            MethodBuilder tryFetchMethod = typeBuilder.DefineMethod(
                "GetInstance<RPC_Proxy>",
                MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof(bool),
                [idType, typeBuilder.MakeByRefType()]
            );

            tryFetchMethod.DefineParameter(1, ParameterAttributes.None, "key");
            tryFetchMethod.DefineParameter(2, ParameterAttributes.Out, "object");

            il = tryFetchMethod.GetILGenerator().AsEmitter(debuggable: debugPrint);
            il.Emit(OpCodes.Ldsfld, dictField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, dictTryGetValueMethod);
            il.Emit(OpCodes.Ret);
        }

        return typeBuilder;
    }
    private Type CreateProxyType(Type type)
    {
        if (type.IsValueType)
            throw new ArgumentException(Properties.Exceptions.TypeNotReferenceType, nameof(type));

        if (type.IsSealed || type.IsAbstract)
            throw new ArgumentException(Properties.Exceptions.TypeNotInheritable, nameof(type));

        bool typeGivesInternalAccess = VisibilityUtility.AssemblyGivesInternalAccess(type.Assembly);

        if (!VisibilityUtility.IsTypeVisible(type, typeGivesInternalAccess))
            throw new ArgumentException(Properties.Exceptions.TypeNotPublic, nameof(type));

        TypeBuilder builder = StartProxyType(type, typeGivesInternalAccess);

        MethodInfo[] methods = type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (MethodInfo method in methods)
        {
            if (!method.IsDefinedSafe<RpcSendAttribute>() || method.DeclaringType == typeof(object))
                continue;

            if (!VisibilityUtility.IsMethodOverridable(method, typeGivesInternalAccess))
            {
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.MethodNotVisibileToOverridingClasses, Accessor.Formatter.Format(method), type.FullName));
                else
                    Console.WriteLine(Properties.Logging.MethodNotVisibileToOverridingClasses, Accessor.Formatter.Format(method), type.FullName);
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            Type[] types = new Type[parameters.Length];
            Type[][] reqMods = new Type[parameters.Length][];
            Type[][] optMods = new Type[parameters.Length][];
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo p = parameters[i];
                types[i] = p.ParameterType;
                reqMods[i] = p.GetRequiredCustomModifiers();
                optMods[i] = p.GetOptionalCustomModifiers();
            }

            MethodAttributes privacyAttributes = method.Attributes & (
                MethodAttributes.Public
                | MethodAttributes.Private
                | MethodAttributes.Assembly
                | MethodAttributes.Family
                | MethodAttributes.FamANDAssem
                | MethodAttributes.FamORAssem);

            MethodBuilder methodBuilder = builder.DefineMethod(method.Name,
                privacyAttributes | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                method.CallingConvention,
                method.ReturnType,
                method.ReturnParameter?.GetRequiredCustomModifiers(),
                method.ReturnParameter?.GetOptionalCustomModifiers(),
                types, reqMods, optMods);

            ILGenerator generator = methodBuilder.GetILGenerator();
#if DEBUG
            generator.EmitWriteLine("Calling " + Accessor.Formatter.Format(method));
#endif

            MethodInfo getter = typeof(RpcTask).GetProperty(nameof(RpcTask.CompletedTask), BindingFlags.Public | BindingFlags.Static)!.GetMethod!;
            generator.Emit(OpCodes.Call, getter);
            generator.Emit(OpCodes.Ret);
        }

        try
        {
            return builder.CreateType()!;
        }
        catch (TypeLoadException ex)
        {
            throw new ArgumentException(Properties.Exceptions.TypeNotPublic, nameof(type), ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LogWarning(string text)
    {
        // separated it like this to avoid having a strict reliance on Microsoft.Extensions.Logging.Abstractions.dll
        if (Logger is ILogger logger)
            logger.LogWarning(text);
    }
}
