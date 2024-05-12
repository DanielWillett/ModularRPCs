// todo remove this
#define DEBUG
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Creates inherited proxy types for classes with virtual or abstract methods decorated with the <see cref="RpcSendAttribute"/> to provide implementations of them at runtime.
/// </summary>
public sealed class ProxyGenerator
{
    private readonly ConcurrentDictionary<Type, Type> _proxies = new ConcurrentDictionary<Type, Type>();
    private readonly ConcurrentDictionary<Type, Func<object, WeakReference?>> _getObjectFunctions = new ConcurrentDictionary<Type, Func<object, WeakReference?>>();
    private readonly ConcurrentDictionary<Type, Func<object, bool>> _releaseObjectFunctions = new ConcurrentDictionary<Type, Func<object, bool>>();
    private readonly List<Assembly> _accessIgnoredAssemblies = new List<Assembly>(2);
    private readonly ConstructorInfo _identifierErrorConstructor;
#if DEBUG
    internal const bool DebugPrint = true;
    internal const bool BreakpointPrint = false;
#else
    internal const bool DebugPrint = false;
    internal const bool BreakpointPrint = false;
#endif

    private readonly string[] _identifierFieldNamesToSearch =
    [
        "_identifier",
        "m_identifier",
        "identifier",
        "_id",
        "id",
        "m_id",
    ];

    /// <summary>
    /// Set using <see cref="LoggingExtensions.SetLogger(ProxyGenerator, ILogger)"/>. 
    /// </summary>
    internal object? Logger;

    /// <summary>
    /// Name of the private field used to store instances in a proxy class that implemnets <see cref="IRpcObject{T}"/>.
    /// </summary>
    public string InstancesFieldName => "_instances<RPC_Proxy>";

    /// <summary>
    /// Name of the private field used to store the original identifier of a <see cref="IRpcObject{T}"/>.
    /// </summary>
    public string IdentifierFieldName => "_identifier<RPC_Proxy>";

    /// <summary>
    /// Name of the private field used to store if the key of this <see cref="IRpcObject{T}"/> has been removed from the the dictionary,
    /// thus the finalizer doesn't need to be called.
    /// </summary>
    public string SuppressFinalizeFieldName => "_suppressFinalize<RPC_Proxy>";

    /// <summary>
    /// Name of the private field used to store implementations of various interfaces used internally, such as <see cref="IRpcRouter"/>.
    /// </summary>
    public string ProxyContextFieldName => "_proxyContext<RPC_Proxy>";

    /// <summary>
    /// Name of the static method added to all proxy classes implementing <see cref="IRpcObject{T}"/>. It has the overload: <c>bool(T, out WeakReference)</c>.
    /// </summary>
    public string TryGetInstanceMethodName => "TryGetInstance<RPC_Proxy>";

    /// <summary>
    /// Name of the static method added to all proxy classes implementing <see cref="IRpcObject{T}"/>. It has the overload: <c>WeakReference(object)</c>.
    /// </summary>
    public string GetInstanceMethodName => "GetInstance<RPC_Proxy>";

    /// <summary>
    /// Name of the instance method added to all proxy classes implementing <see cref="IRpcObject{T}"/>. It has the overload: <c>bool Release()</c>.
    /// Virtual or abstract methods in parent classes will be overridden and base-called.
    /// </summary>
    public string ReleaseMethodName => "Release";

    internal SerializerGenerator SerializerGenerator { get; }
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
        SerializerGenerator = new SerializerGenerator(this);
        Assembly thisAssembly = Assembly.GetExecutingAssembly();
        ProxyAssemblyName = new AssemblyName(thisAssembly.GetName().Name + ".Proxy");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(ProxyAssemblyName.Name!);
        _identifierErrorConstructor = typeof(RpcObjectInitializationException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [ typeof(string) ], null)
                                      ?? throw new MemberAccessException("Failed to find RpcObjectInitializationException(string).");

        if (Compatibility.IncompatibleWithIgnoresAccessChecksToAttribute)
            return;

        _accessIgnoredAssemblies.Add(thisAssembly);
        CustomAttributeBuilder attr = new CustomAttributeBuilder(
            typeof(IgnoresAccessChecksToAttribute).GetConstructor([ typeof(string) ])!,
            [ thisAssembly.GetName().Name ]
        );

        AssemblyBuilder.SetCustomAttribute(attr);
        _accessIgnoredAssemblies.Add(thisAssembly);
    }

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(IRpcRouter router) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, false, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(IRpcRouter router, bool nonPublic) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, nonPublic, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(IRpcRouter router, params object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, false, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(IRpcRouter router, bool nonPublic, params object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, nonPublic, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(IRpcRouter router, bool nonPublic, Binder? binder, object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, nonPublic, binder, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    public TRpcClass CreateProxy<TRpcClass>(IRpcRouter router, bool nonPublic, Binder? binder, object[] constructorParameters, CultureInfo culture, object[]? activationAttributes) where TRpcClass : class
        => (TRpcClass)CreateProxy(router, typeof(TRpcClass), nonPublic, binder, constructorParameters, culture, activationAttributes);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(IRpcRouter router, Type type)
        => CreateProxy(router, type, false, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(IRpcRouter router, Type type, bool nonPublic)
        => CreateProxy(router, type, nonPublic, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(IRpcRouter router, Type type, params object[] constructorParameters)
        => CreateProxy(router, type, false, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(IRpcRouter router, Type type, bool nonPublic, params object[] constructorParameters)
        => CreateProxy(router, type, nonPublic, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(IRpcRouter router, Type type, bool nonPublic, Binder? binder, object[] constructorParameters)
        => CreateProxy(router, type, nonPublic, binder, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    public object CreateProxy(IRpcRouter router, Type type, bool nonPublic, Binder? binder, object[] constructorParameters, CultureInfo culture, object[]? activationAttributes)
    {
        if (type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;
        Type newType = _proxies.GetOrAdd(type, CreateProxyType);

        try
        {
            if (constructorParameters == null || constructorParameters.Length == 0)
            {
                constructorParameters = [ router ];
            }
            else
            {
                object[] oldParams = constructorParameters;
                constructorParameters = new object[oldParams.Length + 1];
                constructorParameters[0] = router;
                Array.Copy(oldParams, 0, constructorParameters, 0, oldParams.Length);
            }

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
        if (type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;

        return _proxies.GetOrAdd(type, CreateProxyType);
    }

    /// <summary>
    /// Gets a weak reference to the object represented by the identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="instanceType"/> does not implement <see cref="IRpcObject{T}"/>.</exception>
    /// <exception cref="InvalidCastException">The identifier is not the correct type.</exception>
    /// <returns>A weak reference to the object, or <see langword="null"/> if it's not found.</returns>
    public WeakReference? GetObjectByIdentifier(Type instanceType, object identifier)
    {
        if (identifier == null)
            throw new ArgumentNullException(nameof(identifier));
        if (instanceType == null)
            throw new ArgumentNullException(nameof(instanceType));

        if (!instanceType.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            instanceType = GetProxyType(instanceType);

        if (!_getObjectFunctions.ContainsKey(instanceType))
        {
            Type? intxType = instanceType.GetInterfaces().FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRpcObject<>));
            if (intxType == null)
                throw new ArgumentException(Properties.Exceptions.ObjectNotIdentifyableType, nameof(instanceType));

            Type idType = intxType.GetGenericArguments()[0];
            if (!idType.IsInstanceOfType(identifier))
            {
                throw new InvalidCastException(string.Format(
                    Properties.Exceptions.GetObjectByIdentifierIdentityTypeNotCorrectType,
                    Accessor.ExceptionFormatter.Format(idType),
                    Accessor.ExceptionFormatter.Format(identifier.GetType()))
                );
            }
        }

        return _getObjectFunctions.GetOrAdd(
            instanceType,
            type =>
            {
                MethodInfo getInstanceMethod = type.GetMethod(GetInstanceMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                                ?? throw new ArgumentException(Properties.Exceptions.ObjectNotIdentifyableType, nameof(instanceType));

                return (Func<object, WeakReference>)getInstanceMethod.CreateDelegate(typeof(Func<object, WeakReference>));
            }
        )(identifier);
    }

    /// <summary>
    /// Try's to release an object by it's identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="instanceType"/> does not implement <see cref="IRpcObject{T}"/>.</exception>
    /// <returns><see langword="true"/> if the object was found and released, otherwise <see langword="false"/>.</returns>
    public bool ReleaseObject<T>(T obj) where T : class
    {
        return ReleaseObject(typeof(T), obj);
    }

    /// <summary>
    /// Try's to release an object by it's identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="instanceType"/> does not implement <see cref="IRpcObject{T}"/>.</exception>
    /// <returns><see langword="true"/> if the object was found and released, otherwise <see langword="false"/>.</returns>
    public bool ReleaseObject(Type instanceType, object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (!instanceType.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            instanceType = GetProxyType(instanceType);

        if (!_releaseObjectFunctions.ContainsKey(instanceType))
        {
            if (!instanceType.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRpcObject<>)))
                throw new ArgumentException(Properties.Exceptions.ObjectNotIdentifyableType, nameof(instanceType));
        }

        return _releaseObjectFunctions.GetOrAdd(
            instanceType,
            type =>
            {
                MethodInfo getInstanceMethod = type.GetMethod(ReleaseMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                               ?? throw new ArgumentException(Properties.Exceptions.ObjectNotIdentifyableType, nameof(instanceType));

                return Accessor.GenerateInstanceCaller<Func<object, bool>>(getInstanceMethod, throwOnError: true, allowUnsafeTypeBinding: true)!;
            }
        )(obj);
    }

    private TypeBuilder StartProxyType(Type type, bool typeGivesInternalAccess, out FieldBuilder proxyContextField)
    {
        TypeBuilder typeBuilder = ModuleBuilder.DefineType(type.Name + "<RPC_Proxy>",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, type);
        Assembly asm = type.Assembly;
        
        // most builds of mono don't work with that attribute
        if (!Compatibility.IncompatibleWithIgnoresAccessChecksToAttribute)
        {
            lock (_accessIgnoredAssemblies)
            {
                if (!_accessIgnoredAssemblies.Contains(asm))
                {
                    CustomAttributeBuilder attr = new CustomAttributeBuilder(
                        typeof(IgnoresAccessChecksToAttribute).GetConstructor([typeof(string)])!,
                        [ asm.GetName().Name ]
                    );

                    AssemblyBuilder.SetCustomAttribute(attr);
                    _accessIgnoredAssemblies.Add(asm);
                }
            }
        }

        Type? interfaceType = type.GetInterfaces().FirstOrDefault(intx => intx.IsGenericType && intx.GetGenericTypeDefinition() == typeof(IRpcObject<>));
        Type? idType = interfaceType?.GenericTypeArguments[0];
        Type? elementType = idType;
        Type dictType;
        MethodInfo dictTryAddMethod;
        FieldBuilder dictField;
        FieldBuilder idField;
        FieldBuilder suppressFinalizeField;
        proxyContextField = typeBuilder.DefineField(ProxyContextFieldName, typeof(ProxyContext), FieldAttributes.Private);
        bool isNullable = false;
        if (idType != null)
        {
            isNullable = idType is { IsValueType: true, IsGenericType: true } && idType.GetGenericTypeDefinition() == typeof(Nullable<>);

            if (isNullable)
                elementType = idType.GetGenericArguments()[0];

            dictType = typeof(ConcurrentDictionary<,>).MakeGenericType(elementType!, typeof(WeakReference));

            dictTryAddMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryAdd), BindingFlags.Instance | BindingFlags.Public)
                                          ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(
                                              new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryAdd))
                                                  .Returning<bool>()
                                                  .DeclaredIn(dictType, isStatic: false)
                                                  .WithParameter(elementType!, "key")
                                                  .WithParameter(typeof(WeakReference), "value")
                                              )}.");

            // define original identifier field
            idField = typeBuilder.DefineField(
                IdentifierFieldName,
                isNullable ? elementType! : idType,
                FieldAttributes.Private | FieldAttributes.InitOnly
            );

            // define field to track if the object has been removed already
            suppressFinalizeField = typeBuilder.DefineField(
                SuppressFinalizeFieldName,
                typeof(int),
                FieldAttributes.Private | FieldAttributes.InitOnly
            );

            // define identifier static concurrent dictionary
            dictField = typeBuilder.DefineField(
                InstancesFieldName,
                dictType,
                FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.Static
            );
        }
        else
        {
            dictType = null!;
            dictTryAddMethod = null!;
            dictField = null!;
            idField = null!;
            suppressFinalizeField = null!;
        }

        MethodInfo? getHasValueMethod = null;
        MethodInfo? getValueMethod = null;

        if (isNullable)
        {
            getHasValueMethod = idType!.GetProperty(nameof(Nullable<int>.HasValue), BindingFlags.Public | BindingFlags.Instance)?
                .GetMethod ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(Nullable<int>.HasValue))
                    .DeclaredIn(idType, isStatic: false)
                    .WithPropertyType<bool>()
                    .WithNoSetter())}.");

            getValueMethod = idType.GetProperty(nameof(Nullable<int>.Value), BindingFlags.Public | BindingFlags.Instance)?
                .GetMethod ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(Nullable<int>.Value))
                    .DeclaredIn(idType, isStatic: false)
                    .WithPropertyType(elementType!)
                    .WithNoSetter())
                }.");
        }

        MethodInfo getProxyContextMethod = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.GetDefaultProxyContext), BindingFlags.Public | BindingFlags.Instance) 
                                           ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcRouter.GetDefaultProxyContext))
                                               .DeclaredIn<IRpcRouter>(isStatic: false)
                                               .WithParameter<Type>("proxyType")
                                               .WithParameter<ProxyContext>("context", ByRefTypeMode.Out)
                                               .ReturningVoid()
                                            )}.");

        // create pass-through constructors for all base constructors.
        // for IRpcObject<T> types the identifier will be validated and the object added to the underlying dictionary.
        foreach (ConstructorInfo baseCtor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (baseCtor.IsPrivate)
                continue;

            if (!VisibilityUtility.IsMethodOverridable(baseCtor))
            {
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.ConstructorNotVisibileToOverridingClasses, Accessor.Formatter.Format(baseCtor), type.FullName));
                else if (Accessor.LogWarningMessages)
                    Accessor.Logger?.LogWarning(nameof(ProxyGenerator), string.Format(Properties.Logging.ConstructorNotVisibileToOverridingClasses, Accessor.Formatter.Format(baseCtor), type.FullName));
                continue;
            }

            ParameterInfo[] parameters = baseCtor.GetParameters();
            Type[] types = new Type[parameters.Length + 1];
            Type?[][] reqMods = new Type[parameters.Length + 1][];
            Type?[][] optMods = new Type[parameters.Length + 1][];
            types[0] = typeof(IRpcRouter);
            for (int i = 0; i < parameters.Length; ++i)
            {
                int index = i + 1;
                ParameterInfo p = parameters[i];
                types[index] = p.ParameterType;
                reqMods[index] = p.GetRequiredCustomModifiers();
                optMods[index] = p.GetOptionalCustomModifiers();
            }

            ConstructorBuilder builder = typeBuilder.DefineConstructor(baseCtor.Attributes & ~MethodAttributes.HasSecurity, baseCtor.CallingConvention, types, reqMods, optMods);

            IOpCodeEmitter il = builder.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            // get proxy context
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, Accessor.GetMethod(Type.GetTypeFromHandle)!);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, proxyContextField);
            il.Emit(OpCodes.Callvirt, getProxyContextMethod);

            il.Emit(OpCodes.Ldarg_0);

            for (int i = 1; i < types.Length; ++i)
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
                string expectedPropertyName = identifierGetter.Name.Replace("get_", string.Empty);
                PropertyInfo? identifierProperty = type.GetProperty(expectedPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (identifierProperty == null || identifierProperty.GetGetMethod(true) != identifierGetter)
                {
                    PropertyInfo[] properties = type.GetProperties();
                    identifierProperty = properties.FirstOrDefault(prop => prop.GetGetMethod(true) == identifierGetter);
                }

                if (identifierProperty != null)
                    expectedPropertyName = identifierProperty.Name;

                FieldInfo? identifierBackingField = null;

                bool backingFieldIsExplicit = false;

                // try to identify the backing field for the property, if it exists.
                // this is not necessary but can reduce data copying by referencing the address of the field instead of getting from property
                if ((identifierProperty == null || !identifierProperty.IsDefinedSafe<RpcDontUseBackingFieldAttribute>())
                    && identifierGetter.DeclaringType is { IsInterface: false })
                {
                    FieldInfo[] fields = identifierGetter.DeclaringType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    identifierBackingField = fields.FirstOrDefault(field => field.IsDefinedSafe<RpcIdentifierBackingFieldAttribute>());
                    backingFieldIsExplicit = true;
                    if (identifierBackingField != null && (identifierBackingField.IsStatic || identifierBackingField.FieldType != idType || identifierBackingField.IsIgnored()))
                    {
                        if (Logger != null)
                            LogWarning(string.Format(Properties.Logging.BackingFieldNotValid, Accessor.Formatter.Format(type)));
                        else if (Accessor.LogWarningMessages)
                            Accessor.Logger?.LogWarning(nameof(ProxyGenerator), string.Format(Properties.Logging.BackingFieldNotValid, Accessor.Formatter.Format(type)));
                    }

                    if (identifierBackingField == null)
                    {
                        backingFieldIsExplicit = false;

                        // auto-property
                        identifierBackingField = identifierGetter.DeclaringType.GetField("<" + expectedPropertyName + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (identifierBackingField == null || identifierBackingField.FieldType != idType || identifierBackingField.IsIgnored())
                        {
                            // explicitly implemented auto-property
                            string explName = "<DanielWillett.ModularRpcs.Protocol.IRpcObject<" + (isNullable ? elementType! : idType) + (isNullable ? "?" : string.Empty) + ">.Identifier>k__BackingField";
                            identifierBackingField = identifierGetter.DeclaringType.GetField(explName, BindingFlags.NonPublic | BindingFlags.Instance);

                            if (identifierBackingField == null || identifierBackingField.FieldType != idType || identifierBackingField.IsIgnored())
                            {
                                // predefined field names
                                identifierBackingField = null;
                                for (int i = 0; i < _identifierFieldNamesToSearch.Length; ++i)
                                {
                                    identifierBackingField = identifierGetter.DeclaringType.GetField("_identifier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                    if (identifierBackingField != null && identifierBackingField.FieldType == idType && !identifierBackingField.IsIgnored())
                                        break;

                                    identifierBackingField = null;
                                }
                            }
                        }
                    }

                    if (identifierBackingField == null)
                    {
                        if (Logger != null)
                            LogDebug(string.Format(Properties.Logging.BackingFieldNotFound, Accessor.Formatter.Format(type)));
                        else if (Accessor.LogDebugMessages)
                            Accessor.Logger?.LogDebug(nameof(ProxyGenerator), string.Format(Properties.Logging.BackingFieldNotFound, Accessor.Formatter.Format(type)));
                    }
                }

                if (identifierBackingField != null && identifierBackingField.IsDefinedSafe<RpcDontUseBackingFieldAttribute>())
                    identifierBackingField = null;

                if (identifierBackingField != null && Compatibility.IncompatibleWithIgnoresAccessChecksToAttribute)
                {
                    MemberVisibility vis = identifierBackingField.GetVisibility();
                    if (!typeGivesInternalAccess && vis != MemberVisibility.Public
                        || typeGivesInternalAccess && vis is MemberVisibility.Private or MemberVisibility.Protected)
                    {
                        identifierBackingField = null;
                        if (backingFieldIsExplicit)
                        {
                            if (Logger != null)
                                LogWarning(string.Format(Properties.Logging.BackingFieldNotValid, Accessor.Formatter.Format(type)));
                            else if (Accessor.LogWarningMessages)
                                Accessor.Logger?.LogWarning(nameof(ProxyGenerator), string.Format(Properties.Logging.BackingFieldNotValid, Accessor.Formatter.Format(type)));
                        }
                    }
                }

                LocalBuilder identifier = il.DeclareLocal(idType);

                Label ifNotDefault = il.DefineLabel();
                Label ifDidntAdd = il.DefineLabel();
                Label? ifDefault = null;
                il.Emit(OpCodes.Ldarg_0);
                bool isAddr = false;
                LocalBuilder id2 = identifier;
                if (idType.IsValueType)
                {
                    if (isNullable)
                    {
                        if (identifierBackingField != null)
                        {
                            id2 = il.DeclareLocal(idType.MakeByRefType());
                            isAddr = true;

                            il.Emit(OpCodes.Ldflda, identifierBackingField);
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Stloc_S, id2);
                        }
                        else
                        {
                            il.Emit(identifierGetter.GetCallRuntime(), identifierGetter);
                            il.Emit(OpCodes.Stloc_S, id2);
                            il.Emit(OpCodes.Ldloca_S, id2);
                        }

                        il.Emit(OpCodes.Call, getHasValueMethod!);
                        il.Emit(OpCodes.Brtrue, ifNotDefault);
                    }
                    else if (idType.IsPrimitive)
                    {
                        if (identifierBackingField != null)
                            il.Emit(OpCodes.Ldfld, identifierBackingField);
                        else
                            il.Emit(identifierGetter.GetCallRuntime(), identifierGetter);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc_S, identifier);
                        il.Emit(OpCodes.Brtrue, ifNotDefault);
                    }
                    else
                    {
                        if (identifierBackingField != null)
                        {
                            id2 = il.DeclareLocal(idType.MakeByRefType());
                            isAddr = true;

                            il.Emit(OpCodes.Ldflda, identifierBackingField);
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Stloc_S, id2);
                        }
                        else
                        {
                            il.Emit(identifierGetter.GetCallRuntime(), identifierGetter);
                            il.Emit(OpCodes.Stloc_S, id2);
                            il.Emit(OpCodes.Ldloca_S, id2);
                        }

                        // check if value == default
                        LocalBuilder lclCheck = il.DeclareLocal(idType);
                        il.Emit(OpCodes.Ldloca_S, lclCheck);
                        il.Emit(OpCodes.Initobj, idType);
                        il.Emit(isAddr ? OpCodes.Ldloc_S : OpCodes.Ldloca_S, id2);

                        il.Emit(OpCodes.Ldloca_S, identifier);
                        MethodInfo? refEqual = idType.GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ idType.MakeByRefType() ], null);
                        if (refEqual != null && !refEqual.IsIgnored())
                        {
                            il.Emit(OpCodes.Ldloca_S, lclCheck);
                            il.Emit(OpCodes.Call, refEqual);
                            il.Emit(OpCodes.Brfalse, ifNotDefault);
                        }
                        else
                        {
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
                                    MethodInfo equal = typeof(object).GetMethod(nameof(Equals), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [typeof(object)], null)
                                                       ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Equals))
                                                           .DeclaredIn(typeof(object), isStatic: false)
                                                           .WithParameter<object?>("obj")
                                                           .Returning<bool>())
                                                       }.");

                                    il.Emit(OpCodes.Ldloc_S, lclCheck);
                                    il.Emit(OpCodes.Callvirt, equal);
                                    il.Emit(OpCodes.Brfalse, ifNotDefault);
                                }
                            }
                        }
                    }
                }
                else if (idType == typeof(string))
                {
                    if (identifierBackingField != null)
                        il.Emit(OpCodes.Ldfld, identifierBackingField);
                    else
                        il.Emit(identifierGetter.GetCallRuntime(), identifierGetter);
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
                    if (identifierBackingField != null)
                        il.Emit(OpCodes.Ldfld, identifierBackingField);
                    else
                        il.Emit(identifierGetter.GetCallRuntime(), identifierGetter);
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
                
                if (isNullable)
                {
                    il.Emit(isAddr ? OpCodes.Ldloc_S : OpCodes.Ldloca_S, id2);
                    il.Emit(OpCodes.Call, getValueMethod!);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc_S, id2);
                    if (isAddr)
                        il.Emit(OpCodes.Ldind_Ref);
                }

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Newobj, typeof(WeakReference).GetConstructor([ typeof(object) ])
                                        ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(typeof(WeakReference))
                    .WithParameter(typeof(object), "target"))
                }"));

                il.Emit(OpCodes.Call, dictTryAddMethod);

                il.Emit(OpCodes.Brfalse, ifDidntAdd);

                il.Emit(OpCodes.Ldarg_0);

                if (isNullable)
                {
                    il.Emit(isAddr ? OpCodes.Ldloc_S : OpCodes.Ldloca_S, id2);
                    il.Emit(OpCodes.Call, getValueMethod!);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc_S, id2);
                    if (isAddr)
                        il.Emit(OpCodes.Ldind_Ref);
                }

                il.Emit(OpCodes.Stfld, idField);
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

            MethodInfo dictTryRemoveMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryRemove), BindingFlags.Instance | BindingFlags.Public, null, [ elementType, typeof(WeakReference).MakeByRefType() ], null)
                                             ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryRemove))
                                                 .DeclaredIn(dictType, isStatic: false)
                                                 .Returning<bool>()
                                                 .WithParameter(idType, "key")
                                                 .WithParameter(type.MakeByRefType(), "value", ByRefTypeMode.Out))
                                             }.");

            MethodInfo compareExchange = typeof(Interlocked).GetMethod(nameof(Interlocked.Exchange), BindingFlags.Static | BindingFlags.Public, null, [typeof(int).MakeByRefType(), typeof(int)], null)
                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Interlocked.Exchange))
                                             .DeclaredIn(typeof(Interlocked), isStatic: true)
                                             .Returning<int>()
                                             .WithParameter(typeof(int), "location1", ByRefTypeMode.Ref)
                                             .WithParameter(typeof(int), "value"))
                                         }.");

            // static constructor to initalize the dictionary
            ConstructorBuilder typeInitializer = typeBuilder.DefineTypeInitializer();
            IOpCodeEmitter il = typeInitializer.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);
            il.Emit(OpCodes.Newobj, dictCtor);
            il.Emit(OpCodes.Stsfld, dictField);
            il.Emit(OpCodes.Ret);

            // look for finalizer of base type if it exists.
            MethodInfo? baseFinalizerMethod = null;
            for (Type? baseType = type; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
            {
                baseFinalizerMethod = type.GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (baseFinalizerMethod != null)
                    break;
            }

            // look for Release method of base type if it exists.
            MethodInfo? baseReleaseMethod = null;
            for (Type? baseType = type; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
            {
                baseReleaseMethod = type.GetMethod(ReleaseMethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                if (baseReleaseMethod == null)
                    continue;

                if (!(baseReleaseMethod.IsVirtual || baseReleaseMethod.IsAbstract) || baseReleaseMethod.IsIgnored())
                    baseReleaseMethod = null;
                else break;
            }

            if (baseReleaseMethod is { IsPublic: true })
            {
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.BaseReleaseMethodCantBePublic, Accessor.Formatter.Format(baseReleaseMethod.DeclaringType!)));
                else if (Accessor.LogWarningMessages)
                    Accessor.Logger?.LogWarning(nameof(ProxyGenerator), string.Format(Properties.Logging.BaseReleaseMethodCantBePublic, Accessor.Formatter.Format(baseReleaseMethod.DeclaringType!)));
                baseReleaseMethod = null;
            }

            // define a finalizer to remove this object from the dictionary
            MethodBuilder finalizerMethod = typeBuilder.DefineMethod("Finalize",
                MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Family,
                CallingConventions.Standard,
                typeof(void),
                null, null, null, null, null
            );

            il = finalizerMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            LocalBuilder lcl = il.DeclareLocal(typeof(WeakReference));
            Label retLbl = il.DefineLabel();

            if (baseFinalizerMethod != null)
                il.BeginExceptionBlock();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, suppressFinalizeField);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, compareExchange);
            il.Emit(OpCodes.Brtrue, retLbl);

            if (baseReleaseMethod is { IsAbstract: false })
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseReleaseMethod);
                if (baseReleaseMethod.ReturnType != typeof(void))
                    il.Emit(OpCodes.Pop);
            }

            il.Emit(OpCodes.Ldsfld, dictField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, idField);
            il.Emit(OpCodes.Ldloca_S, lcl);
            il.Emit(OpCodes.Call, dictTryRemoveMethod);
#if DEBUG
            Label brtrue = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, brtrue);
            il.EmitWriteLine($"Removed {Accessor.Formatter.Format(type)} from finalizer.");
            il.Emit(OpCodes.Br, retLbl);
            il.MarkLabel(brtrue);
            il.EmitWriteLine($"Didn't remove {Accessor.Formatter.Format(type)} from finalizer.");
#else
            il.Emit(OpCodes.Pop);
#endif
            if (baseFinalizerMethod != null)
            {
                il.MarkLabel(retLbl);
                il.BeginFinallyBlock();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseFinalizerMethod);
                il.EndExceptionBlock();
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.MarkLabel(retLbl);
                il.Emit(OpCodes.Ret);
            }

            MethodBuilder releaseMethod = typeBuilder.DefineMethod(ReleaseMethodName,
                baseReleaseMethod != null
                    ? MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.Public
                    : MethodAttributes.Public,
                CallingConventions.Standard,
                typeof(bool),
                null, null, Type.EmptyTypes, null, null
            );

            il = releaseMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            lcl = il.DeclareLocal(typeof(WeakReference));
            Label alreadyDoneLabel = il.DefineLabel();
            retLbl = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, suppressFinalizeField);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, compareExchange);
            il.Emit(OpCodes.Brtrue, alreadyDoneLabel);

            if (baseReleaseMethod is { IsAbstract: false })
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseReleaseMethod);
                if (baseReleaseMethod.ReturnType != typeof(void))
                    il.Emit(OpCodes.Pop);
            }

            il.Emit(OpCodes.Ldsfld, dictField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, idField);
            il.Emit(OpCodes.Ldloca_S, lcl);
            il.Emit(OpCodes.Call, dictTryRemoveMethod);
#if DEBUG
            brtrue = il.DefineLabel();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse, brtrue);
            il.EmitWriteLine($"Removed {Accessor.Formatter.Format(type)} from Release.");
            il.Emit(OpCodes.Br, retLbl);
            il.MarkLabel(brtrue);
            il.EmitWriteLine($"Didn't remove {Accessor.Formatter.Format(type)} from Release.");
#endif
            il.Emit(OpCodes.Br, retLbl);

            il.MarkLabel(alreadyDoneLabel);
            il.Emit(OpCodes.Ldc_I4_0);
            il.MarkLabel(retLbl);
            il.Emit(OpCodes.Ret);

            MethodBuilder tryFetchMethod = typeBuilder.DefineMethod(
                TryGetInstanceMethodName,
                MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof(bool),
                [ elementType, typeof(WeakReference).MakeByRefType() ]
            );

            tryFetchMethod.DefineParameter(1, ParameterAttributes.None, "key");
            tryFetchMethod.DefineParameter(2, ParameterAttributes.Out, "object");

            il = tryFetchMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            il.Emit(OpCodes.Ldsfld, dictField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, dictTryGetValueMethod);
            il.Emit(OpCodes.Ret);

            MethodBuilder fetchMethod = typeBuilder.DefineMethod(
                GetInstanceMethodName,
                MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof(WeakReference),
                [ typeof(object) ]
            );

            fetchMethod.DefineParameter(1, ParameterAttributes.None, "key");

            il = fetchMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            lcl = il.DeclareLocal(typeof(WeakReference));

            il.Emit(OpCodes.Ldsfld, dictField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca_S, lcl);
            il.Emit(OpCodes.Call, dictTryGetValueMethod);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldloc_S, lcl);
            il.Emit(OpCodes.Ret);
        }

        return typeBuilder;
    }

    private ProxyContext ctx;
    private Type CreateProxyType(Type type)
    {
        if (type.IsValueType)
            throw new ArgumentException(Properties.Exceptions.TypeNotReferenceType, nameof(type));

        if (type.IsSealed)
            throw new ArgumentException(Properties.Exceptions.TypeNotInheritable, nameof(type));
        MethodInfo[] methods = type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (type.IsAbstract)
        {
            // check that there are no abstract methods that aren't send methods.
            foreach (MethodInfo method in methods)
            {
                if (method.IsAbstract && method.DeclaringType == type && !method.IsDefinedSafe<RpcSendAttribute>())
                    throw new ArgumentException(Properties.Exceptions.TypeNotInheritable, nameof(type));
            }
        }

        bool typeGivesInternalAccess = VisibilityUtility.AssemblyGivesInternalAccess(type.Assembly);

        if (!VisibilityUtility.IsTypeVisible(type, typeGivesInternalAccess))
            throw new ArgumentException(Properties.Exceptions.TypeNotPublic, nameof(type));

        TypeBuilder builder = StartProxyType(type, typeGivesInternalAccess, out FieldBuilder proxyContextField);

        foreach (MethodInfo method in methods)
        {
            if (!method.IsDefinedSafe<RpcSendAttribute>() || method.DeclaringType == typeof(object))
            {
                if (method.IsAbstract && method.DeclaringType == type)
                    throw new ArgumentException(Properties.Exceptions.TypeNotInheritable, nameof(type));
                continue;
            }

            if (!VisibilityUtility.IsMethodOverridable(method))
            {
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.MethodNotVisibileToOverridingClasses, Accessor.Formatter.Format(method), type.FullName));
                else if (Accessor.LogWarningMessages)
                    Accessor.Logger?.LogWarning(nameof(ProxyGenerator), string.Format(Properties.Logging.MethodNotVisibileToOverridingClasses, Accessor.Formatter.Format(method), type.FullName));
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

            Type serializerCache = SerializerGenerator.GetSerializerType(parameters.Length);
            Type[] genericArguments = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo param = parameters[i];
                if (param.ParameterType.IsByRef)
                {
                    if (param.IsOut)
                        throw new RpcInvalidParameterException(i, param, method, Properties.Exceptions.RpcInvalidParameterExceptionOutMessage);

                    try
                    {
                        genericArguments[i] = param.ParameterType.GetElementType() ?? param.ParameterType;
                        continue;
                    }
                    catch (NotSupportedException) { }
                }
                genericArguments[i] = param.ParameterType;
            }

            serializerCache = serializerCache.MakeGenericType(genericArguments);
            FieldInfo getSizeMethod = serializerCache.GetField(SerializerGenerator.GetSizeMethodField)
                                       ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(SerializerGenerator.GetSizeMethodField)
                                           .DeclaredIn(serializerCache, isStatic: true)
                                           .Returning<int>()
                                       )}.");

            MethodBuilder methodBuilder = builder.DefineMethod(method.Name,
                privacyAttributes | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                method.CallingConvention,
                method.ReturnType,
                method.ReturnParameter?.GetRequiredCustomModifiers(),
                method.ReturnParameter?.GetOptionalCustomModifiers(),
                types, reqMods, optMods);

            IOpCodeEmitter generator = methodBuilder.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);
#if DEBUG
            generator.EmitWriteLine("Calling " + Accessor.Formatter.Format(method));
#endif
            LocalBuilder lclSize = generator.DeclareLocal(typeof(int));

            // invoke the delegate
            generator.Emit(OpCodes.Ldsfld, getSizeMethod);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, proxyContextField);
            generator.Emit(OpCodes.Ldfld, ProxyContext.SerializerField);
            for (int i = 0; i < genericArguments.Length; ++i)
            {
                generator.Emit(!parameters[i].ParameterType.IsByRef ? OpCodes.Ldarga : OpCodes.Ldarg, i + 1);
            }

            Type delegateType = getSizeMethod.FieldType;
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)
                                      ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition("Invoke")
                                          .DeclaredIn(delegateType, isStatic: true)
                                          .Returning<int>()
                                      )}.");
            Type[] paramArray = new Type[genericArguments.Length + 1];
            paramArray[0] = typeof(IRpcSerializer);
            for (int i = 1; i < paramArray.Length; ++i)
            {
                Type parameterType = parameters[i - 1].ParameterType;
                paramArray[i] = parameterType.IsByRef ? parameterType : parameterType.MakeByRefType();
            }

            generator.Emit(OpCodes.Callvirt, invokeMethod);
            generator.Emit(OpCodes.Stloc, lclSize);
            generator.EmitWriteLine(lclSize);

            MethodInfo getter = typeof(RpcTask).GetProperty(nameof(RpcTask.CompletedTask), BindingFlags.Public | BindingFlags.Static)!.GetMethod!;
            generator.Emit(OpCodes.Call, getter);
            generator.Emit(OpCodes.Ret);
        }

        try
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
#if NETSTANDARD2_0
            return builder.CreateTypeInfo()!;
#else
            return builder.CreateType()!;
#endif
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LogDebug(string text)
    {
        // separated it like this to avoid having a strict reliance on Microsoft.Extensions.Logging.Abstractions.dll
        if (Logger is ILogger logger)
            logger.LogDebug(text);
    }
}
