#if DEBUG
// #define WRITE_TO_FILE
#endif

using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using Exception = System.Exception;
using RuntimeMethodHandle = System.RuntimeMethodHandle;
using Type = System.Type;
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Creates inherited proxy types for classes with virtual or abstract methods decorated with the <see cref="RpcSendAttribute"/> to provide implementations of them at runtime.
/// </summary>
public sealed class ProxyGenerator : IRefSafeLoggable
{
    static ProxyGenerator() { }

    private readonly Dictionary<Type, ProxyTypeInfo> _proxies = new Dictionary<Type, ProxyTypeInfo>();

    private readonly ConcurrentDictionary<Type, Func<object, WeakReference?>> _getObjectFunctions =
        new ConcurrentDictionary<Type, Func<object, WeakReference?>>();

    private readonly ConcurrentDictionary<Type, Func<object, bool>> _releaseObjectFunctions =
        new ConcurrentDictionary<Type, Func<object, bool>>();

    private readonly ConcurrentDictionary<RuntimeMethodHandle, Delegate?> _getCallInfoFunctions =
        new ConcurrentDictionary<RuntimeMethodHandle, Delegate?>();

    private readonly ConcurrentDictionary<Type, GetOverheadSize?> _getOverheadSizeFunctions =
        new ConcurrentDictionary<Type, GetOverheadSize?>();

    private readonly ConcurrentDictionary<Type, WriteIdentifierHandler?> _writeIdentifierFunctions =
        new ConcurrentDictionary<Type, WriteIdentifierHandler?>();

    private readonly ConcurrentDictionary<RuntimeMethodHandle, Delegate> _invokeMethodsStream =
        new ConcurrentDictionary<RuntimeMethodHandle, Delegate>();

    private readonly ConcurrentDictionary<RuntimeMethodHandle, Delegate> _invokeMethodsBytes =
        new ConcurrentDictionary<RuntimeMethodHandle, Delegate>();

    private readonly ConcurrentDictionary<Type, IReadOnlyList<RpcEndpointTarget>> _broadcastMethods =
        new ConcurrentDictionary<Type, IReadOnlyList<RpcEndpointTarget>>();


    private readonly List<Assembly> _accessIgnoredAssemblies = new List<Assembly>(2);
    private readonly ConstructorInfo _identifierErrorConstructor;

    /// <summary>
    /// Used by source generators.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [UsedImplicitly]
    public delegate int GetOverheadSize(object targetObject, RuntimeMethodHandle method,
        ref RpcCallMethodInfo callInfo, out int sizeWithoutId);

    private unsafe delegate int WriteIdentifierHandler(object targetObject, byte* bytes, int maxSize);
#if DEBUG
    // set to true to print IL code
    internal const bool DebugPrint = true;
    internal const bool BreakpointPrint = false;
#else
    internal const bool DebugPrint = false;
    internal const bool BreakpointPrint = false;
#endif


    private object? _logger;
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

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
    /// Name of the static method added to all proxy classes implementing <see cref="IRpcObject{T}"/>. It has the signature: <c>WeakReference GetInstance&lt;RPC_Proxy&gt;(object)</c>.
    /// </summary>
    public string GetInstanceMethodName => "GetInstance<RPC_Proxy>";

    /// <summary>
    /// Name of the public instance field created for unity objects to set the router after Awake() is called.
    /// </summary>
    public string UnityRouterFieldName => "_router<RPC_Proxy>";

    /// <summary>
    /// Name of the instance method added to all proxy classes implementing <see cref="IRpcObject{T}"/>. It has the signature: <c>bool Release()</c>.
    /// Virtual or abstract methods in parent classes will be overridden and base-called.
    /// </summary>
    public string ReleaseMethodName => "Release";

    /// <summary>
    /// Name of the instance method added to all proxy classes. It has the signature: <c>int CalculateOverheadSize(RuntimeMethodHandle method, ref RpcCallMethodInfo callInfo, out int sizeWithoutId)</c>.
    /// </summary>
    public string CalculateOverheadSizeMethodName => "CalculateOverheadSize";

    /// <summary>
    /// Name of the instance method added to all proxy classes. It has the signature: <c>int WriteIdentifier(byte* bytes, int maxSize)</c>.
    /// </summary>
    public string WriteIdentifierMethodName => "WriteIdentifier";

    /// <summary>
    /// Static field of type <see cref="RpcCallMethodInfo"/> that stores information about the target of a call method in a proxy type.
    /// <para>
    /// This may change in cases where there are more than one method with the same name, or in generated classes. Find the name by getting the <see cref="CallerInfoFieldNameAttribute"/> from the overriding method.
    /// </para>
    /// </summary>
    /// <remarks>Use this value with <see cref="string.Format(string, object)"/>, where {0} is the call method name.</remarks>
    public string CallMethodInfoFieldFormat => "_callMtdInfo<{0}>";

    /// <summary>
    /// Maximum size in bytes that a stackalloc can be used instead of creating an new byte array for writing messages to.
    /// </summary>
    /// <remarks>Defaults to 512.</remarks>
    [UsedImplicitly]
    public int MaxSizeForStackalloc { get; set; } = 512;

    /// <summary>
    /// If task-like-returning RPCs should be awaited using 'ConfigureAwait(false)' if available.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool UseConfigureAwaitWhenAwaitingRpcInvocations { get; set; } = true;

    /// <summary>
    /// Default timeout for all RPCs unless otherwise specified with a <see cref="RpcTimeoutAttribute"/>.
    /// </summary>
    /// <remarks>Defaults to 15 seconds.</remarks>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(15d);

    /// <summary>
    /// Dictionary of types to lists of broadcast targets (receive methods that specify a send method).
    /// </summary>
    public IReadOnlyDictionary<Type, IReadOnlyList<RpcEndpointTarget>> BroadcastTargets { get; }

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

    private ProxyGenerator()
    {
        SerializerGenerator = new SerializerGenerator(this);
        Assembly thisAssembly = Assembly.GetExecutingAssembly();
        ProxyAssemblyName = new AssemblyName(thisAssembly.GetName().Name + ".Proxy");
#if NET9_0_OR_GREATER && WRITE_TO_FILE
        AssemblyBuilder = new PersistedAssemblyBuilder(ProxyAssemblyName, typeof(object).Assembly);
#elif NETFRAMEWORK && WRITE_TO_FILE
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.RunAndSave);
#else
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.Run);
#endif
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(ProxyAssemblyName.Name!);
        _identifierErrorConstructor =
            typeof(RpcObjectInitializationException).GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                null, [ typeof(string) ], null)
            ?? throw new MemberAccessException("Failed to find RpcObjectInitializationException(string).");

        CustomAttributeBuilder attr = new CustomAttributeBuilder(
            typeof(InternalsVisibleToAttribute).GetConstructor([ typeof(string) ])!,
            [ thisAssembly.GetName().Name ]
        );

        BroadcastTargets = new ReadOnlyDictionary<Type, IReadOnlyList<RpcEndpointTarget>>(_broadcastMethods);

        AssemblyBuilder.SetCustomAttribute(attr);

        _generatedTypeBuilder = new GeneratedProxyTypeBuilder(
            _getCallInfoFunctions,
            _invokeMethodsStream,
            _invokeMethodsBytes,
            _getObjectFunctions,
            _releaseObjectFunctions,
            _getOverheadSizeFunctions,
            _broadcastMethods
        );
        SerializerGenerator.InitializeGeneratedProxyBuilder(_generatedTypeBuilder);
        _generatedTypeBuilderArgs = [ _generatedTypeBuilder ];

        if (Compatibility.IncompatibleWithIgnoresAccessChecksToAttribute)
            return;

        attr = new CustomAttributeBuilder(
            typeof(IgnoresAccessChecksToAttribute).GetConstructor([ typeof(string) ])!,
            [ thisAssembly.GetName().Name ]
        );

        AssemblyBuilder.SetCustomAttribute(attr);
        _accessIgnoredAssemblies.Add(thisAssembly);

#if NET7_0_OR_GREATER
        new GeneratedMethodInvokeStaticSetupMethod<TestGeneratedType>().Execute(null);
#endif
    }

#if NET5_0_OR_GREATER
    private const DynamicallyAccessedMemberTypes CreateProxyDynamicMembers = DynamicallyAccessedMemberTypes.NonPublicMethods
                                                                             | DynamicallyAccessedMemberTypes.NonPublicMethods
                                                                             | DynamicallyAccessedMemberTypes.NonPublicProperties
                                                                             | DynamicallyAccessedMemberTypes.PublicProperties
#if NET6_0_OR_GREATER
                                                                             | DynamicallyAccessedMemberTypes.Interfaces
#endif
                                                                             | DynamicallyAccessedMemberTypes.NonPublicFields
                                                                             | DynamicallyAccessedMemberTypes.PublicFields;
#endif

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]

    public TRpcClass CreateProxy<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | CreateProxyDynamicMembers)]
#endif
    TRpcClass
    >(IRpcRouter router) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, false, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public TRpcClass CreateProxy<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | CreateProxyDynamicMembers)]
#endif
    TRpcClass>(IRpcRouter router, bool nonPublic) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, nonPublic, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public TRpcClass CreateProxy<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | CreateProxyDynamicMembers)]
#endif
    TRpcClass>(IRpcRouter router, params object[] constructorParameters)
        where TRpcClass : class
        => CreateProxy<TRpcClass>(router, false, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public TRpcClass CreateProxy<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
    TRpcClass>(IRpcRouter router, bool nonPublic, params object[] constructorParameters)
        where TRpcClass : class
        => CreateProxy<TRpcClass>(router, nonPublic, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public TRpcClass CreateProxy<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
       TRpcClass>(IRpcRouter router, bool nonPublic, Binder? binder,
        object[] constructorParameters) where TRpcClass : class
        => CreateProxy<TRpcClass>(router, nonPublic, binder, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <typeparamref name="TRpcClass"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public TRpcClass CreateProxy<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        TRpcClass>(IRpcRouter router, bool nonPublic, Binder? binder,
        object[] constructorParameters, CultureInfo culture, object[]? activationAttributes) where TRpcClass : class
        => (TRpcClass)CreateProxy(router, typeof(TRpcClass), nonPublic, binder, constructorParameters, culture,
            activationAttributes);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public object CreateProxy(IRpcRouter router,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | CreateProxyDynamicMembers)]
#endif
        Type type)
        => CreateProxy(router, type, false, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public object CreateProxy(IRpcRouter router,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type, bool nonPublic)
        => CreateProxy(router, type, nonPublic, null, Array.Empty<object>(), CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public object CreateProxy(IRpcRouter router,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type, params object[] constructorParameters)
        => CreateProxy(router, type, false, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public object CreateProxy(IRpcRouter router,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type, bool nonPublic, params object[] constructorParameters)
        => CreateProxy(router, type, nonPublic, null, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public object CreateProxy(IRpcRouter router,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type, bool nonPublic, Binder? binder,
        object[] constructorParameters)
        => CreateProxy(router, type, nonPublic, binder, constructorParameters, CultureInfo.CurrentCulture, null);

    /// <summary>Create an instance of the RPC proxy of <paramref name="type"/>.</summary>
    /// <remarks>If using Unity on a Component, ensure you're using the extension method in ModularRPCs.Unity instead.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public object CreateProxy(IRpcRouter router,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type, bool nonPublic, Binder? binder,
        object[] constructorParameters, CultureInfo culture, object[]? activationAttributes)
    {
        if (type.Assembly.FullName != null && type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;
        ProxyTypeInfo newType;
        lock (_proxies)
        {
            if (!_proxies.TryGetValue(type, out newType))
            {
                _proxies.Add(type, newType = CreateProxyType(type));
            }
        }

        try
        {
            if (!newType.IsGenerated && newType.SetUnityRouterField == null)
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
                    Array.Copy(oldParams, 0, constructorParameters, 1, oldParams.Length);
                }
            }

            object newProxiedObject = Activator.CreateInstance(
                newType.Type,
                BindingFlags.CreateInstance | BindingFlags.Instance |
                (nonPublic ? BindingFlags.Public | BindingFlags.NonPublic : BindingFlags.Public),
                binder,
                constructorParameters,
                culture,
                activationAttributes
            )!;

            if (newType.IsGenerated)
            {
                IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)newProxiedObject;
                SetupGeneratedProxyInfo(genType, new GeneratedProxyTypeInfo(router, this));
            }
            else
            {
                newType.SetUnityRouterField?.Invoke(newProxiedObject, router);
            }

            return newProxiedObject;
        }
        catch (MissingMethodException ex) when (!newType.IsGenerated)
        {
            throw new MissingMethodException(Properties.Exceptions.PrivatesNotVisibleMissingMethodException, ex);
        }
        catch (TargetInvocationException ex) when (!newType.IsGenerated)
        {
            if (ex.InnerException is not MemberAccessException mae)
                throw ex.InnerException ?? ex;

            if (mae is MethodAccessException && !newType.IsGenerated)
                throw new MethodAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, mae);

            throw new MemberAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, mae);
        }
        catch (MethodAccessException ex) when (!newType.IsGenerated)
        {
            throw new MethodAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, ex);
        }
        catch (MemberAccessException ex) when (!newType.IsGenerated)
        {
            throw new MemberAccessException(Properties.Exceptions.InternalsNotVisibleMemberAccessException, ex);
        }
    }

    /// <summary>
    /// Calls <see cref="IRpcGeneratedProxyType.SetupGeneratedProxyInfo"/> with the correct arguments.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public void SetupGeneratedProxyInfo(IRpcGeneratedProxyType generatedProxy, GeneratedProxyTypeInfo info)
    {
        generatedProxy.SetupGeneratedProxyInfo(info);
    }

    /// <summary>
    /// Check to see if a type has a parent type that's been generated by <see cref="ProxyGenerator"/>.
    /// </summary>
    /// <remarks>This also returns <see langword="true"/> for proxy types created with the source generator.</remarks>
    [System.Diagnostics.Contracts.Pure]
    public bool HasProxyType(Type type)
    {
        lock (_proxies)
        {
            if (_proxies.ContainsKey(type))
                return true;

            if (type.Assembly.FullName == null || !type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
                return false;

            type = type.BaseType!;
            return _proxies.ContainsKey(type);
        }
    }

    /// <summary>
    /// Returns the RPC proxy type of <typeparamref name="TRpcClass"/>. For types generated by the source generator, the same type will be returned.
    /// </summary>
    /// <remarks>The RPC proxy type overrides virtual methods decorated with the <see cref="RpcSendAttribute"/>.</remarks>
    /// <exception cref="ArgumentException"/>
    [System.Diagnostics.Contracts.Pure]
    public Type GetProxyType<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        TRpcClass>() where TRpcClass : class
        => GetProxyType(typeof(TRpcClass));

    /// <summary>
    /// Returns the RPC proxy type of <paramref name="type"/>. For types generated by the source generator, the same type will be returned.
    /// </summary>
    /// <remarks>The RPC proxy type overrides virtual methods decorated with the <see cref="RpcSendAttribute"/>.</remarks>
    /// <exception cref="ArgumentException"/>
    [System.Diagnostics.Contracts.Pure]
    public Type GetProxyType(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type)
    {
        if (type.Assembly.FullName != null && type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;

        ProxyTypeInfo proxyType;
        lock (_proxies)
        {
            if (!_proxies.TryGetValue(type, out proxyType))
            {
                _proxies.Add(type, proxyType = CreateProxyType(type));
            }
        }

        return proxyType.Type;
    }

    /// <summary>
    /// Returns the RPC proxy type of <typeparamref name="TRpcClass"/> if it's already been created. For types generated by the source generator, the same type will be returned.
    /// </summary>
    /// <remarks>The RPC proxy type overrides virtual methods decorated with the <see cref="RpcSendAttribute"/>.</remarks>
    public bool TryGetProxyType<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        TRpcClass>(
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out Type proxyType) where TRpcClass : class
    {
        return TryGetProxyType(typeof(TRpcClass), out proxyType);
    }

    /// <summary>
    /// Returns the RPC proxy type of <paramref name="type"/> if it's already been created. For types generated by the source generator, the same type will be returned.
    /// </summary>
    /// <remarks>The RPC proxy type overrides virtual methods decorated with the <see cref="RpcSendAttribute"/>.</remarks>
    public bool TryGetProxyType(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | CreateProxyDynamicMembers)]
#endif
        Type type,
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out Type proxyType
    )
    {
        if (type.Assembly.FullName != null && type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;

        lock (_proxies)
        {
            if (_proxies.TryGetValue(type, out ProxyTypeInfo info))
            {
                proxyType = info.Type;
                return true;
            }
        }

        proxyType = null!;
        return false;
    }

    internal ProxyTypeInfo GetProxyTypeInfo(Type type)
    {
        if (type.Assembly.FullName != null && type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;

        ProxyTypeInfo proxyType;
        lock (_proxies)
        {
            if (!_proxies.TryGetValue(type, out proxyType))
            {
                _proxies.Add(type, proxyType = CreateProxyType(type));
            }
        }

        return proxyType;
    }

    internal bool TryGetProxyTypeInfo(Type type, out ProxyTypeInfo info)
    {
        if (type.Assembly.FullName != null && type.Assembly.FullName.Equals(AssemblyBuilder.FullName))
            type = type.BaseType!;

        lock (_proxies)
            return _proxies.TryGetValue(type, out info);
    }

    /// <summary>
    /// Gets a weak reference to the object represented by the identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="instanceType"/> does not implement <see cref="IRpcObject{T}"/>.</exception>
    /// <exception cref="InvalidCastException">The identifier is not the correct type.</exception>
    /// <returns>A weak reference to the object, or <see langword="null"/> if it's not found.</returns>
    [System.Diagnostics.Contracts.Pure]
    public WeakReference? GetObjectByIdentifier(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type instanceType, object identifier)
    {
        if (identifier == null)
            throw new ArgumentNullException(nameof(identifier));
        if (instanceType == null)
            throw new ArgumentNullException(nameof(instanceType));
        
        ProxyTypeInfo info = GetProxyTypeInfo(instanceType);

        if (_getObjectFunctions.TryGetValue(instanceType, out Func<object, WeakReference?>? weakRefGetter))
        {
            return weakRefGetter(identifier);
        }

        if (info.IsGenerated)
            throw new ArgumentException(Properties.Exceptions.ObjectNotIdentifyableType, nameof(instanceType));

        Type? intxType = instanceType.GetInterfaces().FirstOrDefault(
            type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRpcObject<>)
        );
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

        return _getObjectFunctions.GetOrAdd(
            info.Type,
            type =>
            {
                MethodInfo getInstanceMethod = type.GetMethod(GetInstanceMethodName,
                                                   BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.DeclaredOnly)
                                               ?? throw new ArgumentException(
                                                   Properties.Exceptions.ObjectNotIdentifyableType,
                                                   nameof(instanceType));

                return (Func<object, WeakReference>)getInstanceMethod.CreateDelegate(
                    typeof(Func<object, WeakReference>));
            }
        )(identifier);
    }

    /// <summary>
    /// Try's to release an object by it's identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> does not implement <see cref="IRpcObject{T}"/>.</exception>
    /// <returns><see langword="true"/> if the object was found and released, otherwise <see langword="false"/>.</returns>
    public bool ReleaseObject<T>(IRpcObject<T> obj)
    {
        return ReleaseObject(obj.GetType(), obj);
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

        if (instanceType == null)
            throw new ArgumentNullException(nameof(instanceType));

        ProxyTypeInfo info = GetProxyTypeInfo(instanceType);

        if (_releaseObjectFunctions.TryGetValue(instanceType, out Func<object, bool>? releaser))
        {
            return releaser(obj);
        }

        if (info.IsGenerated || !instanceType.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRpcObject<>)))
            throw new ArgumentException(Properties.Exceptions.ObjectNotIdentifyableType, nameof(instanceType));

        return _releaseObjectFunctions.GetOrAdd(
            info.Type,
            static type =>
            {
                MethodInfo getInstanceMethod = type.GetMethod(
                                                   Instance.ReleaseMethodName,
                                                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                                   null,
                                                   CallingConventions.Any,
                                                   Type.EmptyTypes,
                                                   null
                                                )
                                               ?? throw new ArgumentException(
                                                   Properties.Exceptions.ObjectNotIdentifyableType,
                                                   nameof(type));

                return Accessor.GenerateInstanceCaller<Func<object, bool>>(getInstanceMethod, throwOnError: true,
                    allowUnsafeTypeBinding: true)!;
            }
        )(obj);
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>Number of bytes written to <paramref name="buffer"/>, or 1 if the type doesn't have an identifier (therefore writing 0).</returns>
    public unsafe int WriteIdentifier(Type instanceType, object obj, Memory<byte> buffer)
    {
        fixed (byte* ptr = buffer.Span)
            return WriteIdentifier(instanceType, obj, ptr, buffer.Length);
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>Number of bytes written to <paramref name="buffer"/>, or 1 if the type doesn't have an identifier (therefore writing 0).</returns>
    public unsafe int WriteIdentifier(Type instanceType, object obj, Span<byte> buffer)
    {
        fixed (byte* ptr = buffer)
            return WriteIdentifier(instanceType, obj, ptr, buffer.Length);
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>Number of bytes written to <paramref name="buffer"/>, or 1 if the type doesn't have an identifier (therefore writing 0).</returns>
    public int WriteIdentifier(Type instanceType, object obj, ArraySegment<byte> buffer)
    {
        if (buffer.Array == null)
            throw new ArgumentOutOfRangeException(nameof(buffer));

        return WriteIdentifier(instanceType, obj, buffer.Array, buffer.Offset, buffer.Count);
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>Number of bytes written to <paramref name="buffer"/>, or 1 if the type doesn't have an identifier (therefore writing 0).</returns>
    public unsafe int WriteIdentifier(Type instanceType, object obj, byte[] buffer, int startIndex = 0, int count = -1)
    {
        if (startIndex < 0)
            startIndex = 0;
        if (count < 0)
            count = buffer.Length - startIndex;
        else if (count > buffer.Length - startIndex)
            throw new ArgumentOutOfRangeException(nameof(count));

        fixed (byte* ptr = buffer)
            return WriteIdentifier(instanceType, obj, ptr + startIndex, count);
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>Number of bytes written to <paramref name="buffer"/>, or 1 if the type doesn't have an identifier (therefore writing 0).</returns>
    public unsafe int WriteIdentifier(Type instanceType, object obj, byte* buffer, int maxSize)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (instanceType == null)
            throw new ArgumentNullException(nameof(instanceType));

        ProxyTypeInfo info = GetProxyTypeInfo(instanceType);

        if (!info.Type.IsInstanceOfType(obj))
        {
            if (!instanceType.IsInstanceOfType(obj))
                throw new ArgumentException(string.Format(Properties.Exceptions.ObjectNotOfType, Accessor.Formatter.Format(instanceType), nameof(obj)));

            throw new ArgumentException(string.Format(Properties.Exceptions.ObjectNotProxyType, Accessor.Formatter.Format(instanceType)), nameof(obj));
        }

        WriteIdentifierHandler? writeMtd = GetWriteIdentifierMethod(in info);

        if (writeMtd == null)
        {
            if (maxSize < 1)
                throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

            *buffer = 0;
            return 1;
        }

        return writeMtd(obj, buffer, maxSize);
    }

    /// <summary>
    /// Calculate the size in bytes of the overhead for using a given RpcSend <paramref name="method"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="method"/> is a static delegate.</exception>
    /// <returns>The overhead size if the call method was ever registered, otherwise -1.</returns>
    [System.Diagnostics.Contracts.Pure]
    public int CalculateOverheadSize(Delegate method, out int sizeWithoutId)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        object? target = method.Target;
        if (target == null)
            throw new ArgumentException(Properties.Exceptions.OverheadTargetMethodStatic, nameof(method));

        RuntimeMethodHandle tkn = method.Method.MethodHandle;
        return CalculateOverheadSize(tkn, target, out sizeWithoutId);
    }

    /// <summary>
    /// Calculate the size in bytes of the overhead for using a given RpcSend <paramref name="method"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>The overhead size if the call method was ever registered, otherwise -1.</returns>
    [System.Diagnostics.Contracts.Pure]
    public int CalculateOverheadSize(RuntimeMethodHandle method, object target, out int sizeWithoutId)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        sizeWithoutId = -1;

        Type targetType = target.GetType();

        ProxyTypeInfo info = GetProxyTypeInfo(targetType);

        Delegate? callInfoGetter = GetCallInfoGetter(in info, method);
        if (callInfoGetter == null)
            return -1;

        if (callInfoGetter is SourceGenerationServices.GetCallInfo callInfoByRefGetter)
        {
            ref RpcCallMethodInfo callInfo = ref callInfoByRefGetter();

            GetOverheadSize? calculateMethod = GetOverheadSizeMethod(in info);
            if (calculateMethod == null)
                return -1;

            return calculateMethod(target, method, ref callInfo, out sizeWithoutId);
        }
        else
        {
            RpcCallMethodInfo callInfo = ((SourceGenerationServices.GetCallInfoByVal)callInfoGetter)();

            GetOverheadSize? calculateMethod = GetOverheadSizeMethod(in info);
            if (calculateMethod == null)
                return -1;

            return calculateMethod(target, method, ref callInfo, out sizeWithoutId);
        }
    }

    private static readonly Type[] WriteIdentifierArgs = [ typeof(byte*), typeof(int) ];

    private WriteIdentifierHandler? GetWriteIdentifierMethod(in ProxyTypeInfo type)
    {
        if (_writeIdentifierFunctions.TryGetValue(type.Type, out WriteIdentifierHandler? h))
        {
            return h;
        }

        if (type.IsGenerated)
        {
            return null;
        }

        return _writeIdentifierFunctions.GetOrAdd(
            type.Type,
            static type =>
            {
                MethodInfo? method = type.GetMethod(
                    Instance.WriteIdentifierMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    CallingConventions.Any,
                    WriteIdentifierArgs,
                    null
                );
                if (method == null)
                    return null;

                return Accessor.GenerateInstanceCaller<WriteIdentifierHandler>(method, throwOnError: true, allowUnsafeTypeBinding: true);
            }
        );
    }

    private static readonly Type[] GetOverheadSizeArgs = [ typeof(RuntimeMethodHandle), typeof(RpcCallMethodInfo).MakeByRefType(), typeof(int).MakeByRefType() ];

    private GetOverheadSize? GetOverheadSizeMethod(in ProxyTypeInfo type)
    {
        if (_getOverheadSizeFunctions.TryGetValue(type.Type, out GetOverheadSize? h))
        {
            return h;
        }

        if (type.IsGenerated)
        {
            return null;
        }

        return _getOverheadSizeFunctions.GetOrAdd(
            type.Type,
            static type =>
            {
                MethodInfo? method = type.GetMethod(
                    Instance.CalculateOverheadSizeMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    CallingConventions.Any,
                    GetOverheadSizeArgs,
                    null
                );

                if (method == null)
                    return null;

                return Accessor.GenerateInstanceCaller<GetOverheadSize>(method, throwOnError: true, allowUnsafeTypeBinding: true);
            }
        );
    }

    private static MethodInfo GetImplementedMethod(in ProxyTypeInfo info, MethodInfo method)
    {
        Type? declaringType = method.DeclaringType;
        if (declaringType is { IsInterface: true })
        {
            MethodInfo? implemented = Accessor.GetImplementedMethod(info.Type, method);
            if (implemented != null)
                method = implemented;
        }

        if (declaringType == null || declaringType == info.Type || !(method.IsVirtual || method.IsAbstract) || method.IsFinal || method.IsStatic)
            return method;

        ParameterInfo[] parameters = method.GetParameters();
        Type[] types;
        if (parameters.Length == 0)
        {
            types = Type.EmptyTypes;
        }
        else
        {
            types = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
                types[i] = parameters[i].ParameterType;
        }

        try
        {
            MethodInfo? declared = info.Type.GetMethod(
                method.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null,
                method.CallingConvention,
                types,
                null);

            return declared ?? method;
        }
        catch
        {
            return method;
        }
    }

    private static bool _supportsByRefRtn = true;
    private Delegate? GetCallInfoGetter(in ProxyTypeInfo info, RuntimeMethodHandle methodHandle)
    {
        if (_getCallInfoFunctions.TryGetValue(methodHandle, out Delegate? h))
        {
            return h;
        }
        
        if (info.IsGenerated)
        {
            return null;
        }

        return _getCallInfoFunctions.GetOrAdd(
            methodHandle,
            static methodHandle =>
            {
                MethodBase? mtd = MethodBase.GetMethodFromHandle(methodHandle);
                if (mtd?.DeclaringType == null)
                    return null;

                ProxyTypeInfo info = Instance.GetProxyTypeInfo(mtd.DeclaringType);
                MethodBase declaredMethod = mtd;

                if (mtd is MethodInfo mtdInfo)
                {
                    if (mtd.ReflectedType == null || mtd.ReflectedType != info.Type)
                    {
                        declaredMethod = GetImplementedMethod(in info, mtdInfo) ?? mtd;
                    }
                }

                CallerInfoFieldNameAttribute? attribute = declaredMethod.GetAttributeSafe<CallerInfoFieldNameAttribute>();
                if (attribute == null)
                    return null;

                FieldInfo? field = declaredMethod.ReflectedType?.GetField(attribute.FieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null)
                    return null;

                Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);

            byVal:
                if (!_supportsByRefRtn)
                {
                    DynamicMethod newMethod = new DynamicMethod("<Proxy>_GetValFrom" + field.Name, attr, conv, typeof(RpcCallMethodInfo), Type.EmptyTypes, declaredMethod.ReflectedType!, true);
                    IOpCodeEmitter il = newMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

                    il.Emit(OpCodes.Ldsfld, field);
                    il.Emit(OpCodes.Ret);

                    Delegate d1 = newMethod.CreateDelegate(typeof(SourceGenerationServices.GetCallInfoByVal));
                    if (mtd != declaredMethod)
                        Instance._getCallInfoFunctions[declaredMethod.MethodHandle] = d1;
                    return d1;
                }

                try
                {
                    DynamicMethod newMethod = new DynamicMethod("<Proxy>_GetRefTo" + field.Name, attr, conv, typeof(RpcCallMethodInfo).MakeByRefType(), Type.EmptyTypes, declaredMethod.ReflectedType!, true);
                    IOpCodeEmitter il = newMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

                    il.Emit(OpCodes.Ldsflda, field);
                    il.Emit(OpCodes.Ret);

                    Delegate d1 = newMethod.CreateDelegate(typeof(SourceGenerationServices.GetCallInfo));
                    if (mtd != declaredMethod)
                        Instance._getCallInfoFunctions[declaredMethod.MethodHandle] = d1;
                    return d1;
                }
                catch (Exception) // older frameworks don't support return by ref for dynamic methods, ofc mono throws a different exception than .net framework
                {
                    _supportsByRefRtn = false;
                    goto byVal;
                }
            }
        );
    }

    private TypeBuilder StartProxyType(ref string typeName, Type type,
        bool typeGivesInternalAccess, bool unity,
        out FieldBuilder proxyContextField, out ConstructorBuilder? typeInitializer, out FieldBuilder? idField,
        out FieldBuilder? unityRouterField)
    {
        TypeBuilder typeBuilder;
        bool hasRetried = false;
        while (true)
        {
            try
            {
                typeBuilder = ModuleBuilder.DefineType(typeName,
                    TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, type);
                break;
            }
            catch (ArgumentException)
            {
                // duplicate type name, this is mainly for tests
                if (hasRetried)
                    throw;

                typeName += Guid.NewGuid();
                hasRetried = true;
            } 
        }
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

        MethodInfo? existingStartMethod = null,
                    existingDestroyMethod = null;
        unityRouterField = null;
        if (unity)
        {
            existingStartMethod = type.GetMethod(
                "Start",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null,
                CallingConventions.Any,
                Type.EmptyTypes,
                null
            );

            if (existingStartMethod != null && !VisibilityUtility.IsMethodOverridable(existingStartMethod, typeGivesInternalAccess))
            {
                throw new ArgumentException(string.Format(Properties.Exceptions.TypeUnityMessageMethodNotVirtualOrAbstract, Accessor.ExceptionFormatter.Format(existingStartMethod, includeDefinitionKeywords: true)), nameof(type));
            }

            existingDestroyMethod = type.GetMethod(
                "OnDestroy",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null,
                CallingConventions.Any,
                Type.EmptyTypes,
                null
            );

            if (existingDestroyMethod != null && !VisibilityUtility.IsMethodOverridable(existingDestroyMethod, typeGivesInternalAccess))
            {
                throw new ArgumentException(string.Format(Properties.Exceptions.TypeUnityMessageMethodNotVirtualOrAbstract, Accessor.ExceptionFormatter.Format(existingDestroyMethod, includeDefinitionKeywords: true)), nameof(type));
            }

            unityRouterField = typeBuilder.DefineField(UnityRouterFieldName, typeof(IRpcRouter), FieldAttributes.Public);
        }

        Type? interfaceType = type.GetInterfaces().FirstOrDefault(intx => intx.IsGenericType && intx.GetGenericTypeDefinition() == typeof(IRpcObject<>));
        Type? idType = interfaceType?.GenericTypeArguments[0];
        Type? elementType = idType;
        Type dictType;
        MethodInfo dictTryAddMethod;
        FieldBuilder dictField;
        FieldBuilder suppressFinalizeField;

        proxyContextField = typeBuilder.DefineField(
            ProxyContextFieldName,
            typeof(ProxyContext),
            FieldAttributes.Private | (!unity ? FieldAttributes.InitOnly : 0)
        );

        bool isIdNullable;
        if (idType != null)
        {
            isIdNullable = idType is { IsValueType: true, IsGenericType: true } && idType.GetGenericTypeDefinition() == typeof(Nullable<>);

            if (isIdNullable)
                elementType = idType.GetGenericArguments()[0];

            dictType = typeof(ConcurrentDictionary<,>).MakeGenericType(elementType!, typeof(WeakReference));

            dictTryAddMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryAdd), BindingFlags.Instance | BindingFlags.Public)
                               ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryAdd))
                                   .Returning<bool>()
                                   .DeclaredIn(dictType, isStatic: false)
                                   .WithParameter(elementType!, "key")
                                   .WithParameter(typeof(WeakReference), "value")
                               );

            // define original identifier field
            idField = typeBuilder.DefineField(
                IdentifierFieldName,
                isIdNullable ? elementType! : idType,
                FieldAttributes.Private | (!unity ? FieldAttributes.InitOnly : 0)
            );

            // define field to track if the object has been removed already
            suppressFinalizeField = typeBuilder.DefineField(
                SuppressFinalizeFieldName,
                typeof(int),
                FieldAttributes.Private
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
            isIdNullable = false;
            suppressFinalizeField = null!;
        }

        MethodInfo? getHasValueMethod = null;
        MethodInfo? getValueMethod = null;

        if (isIdNullable)
        {
            getHasValueMethod = idType!.GetProperty(nameof(Nullable<int>.HasValue), BindingFlags.Public | BindingFlags.Instance)?.GetMethod
                                ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.HasValue))
                                    .DeclaredIn(idType, isStatic: false)
                                    .WithPropertyType<bool>()
                                    .WithNoSetter()
                                );

            getValueMethod = idType.GetProperty(nameof(Nullable<int>.Value), BindingFlags.Public | BindingFlags.Instance)?.GetMethod
                             ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.Value))
                                 .DeclaredIn(idType, isStatic: false)
                                 .WithPropertyType(elementType!)
                                 .WithNoSetter()
                             );
        }

        IOpCodeEmitter il;
        // create pass-through constructors for all base constructors.
        // for IRpcObject<T> types the identifier will be validated and the object added to the underlying dictionary.
        foreach (ConstructorInfo baseCtor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (baseCtor.IsPrivate)
                continue;

            if (!VisibilityUtility.IsMethodOverridable(baseCtor))
            {
                this.LogWarning(string.Format(Properties.Logging.ConstructorNotVisibileToOverridingClasses, Accessor.Formatter.Format(baseCtor), type.FullName));
                continue;
            }

            ParameterInfo[] parameters = baseCtor.GetParameters();
            int paramCt = parameters.Length + (!unity ? 1 : 0);
            Type[] types = new Type[paramCt];
            Type[][] reqMods = new Type[paramCt][];
            Type[][] optMods = new Type[paramCt][];
            if (!unity)
                types[0] = typeof(IRpcRouter);
            for (int i = 0; i < parameters.Length; ++i)
            {
                int index = i + (!unity ? 1 : 0);
                ParameterInfo p = parameters[i];
                types[index] = p.ParameterType;
                reqMods[index] = p.GetRequiredCustomModifiers();
                optMods[index] = p.GetOptionalCustomModifiers();
            }

            ConstructorBuilder builder = typeBuilder.DefineConstructor(baseCtor.Attributes & ~MethodAttributes.HasSecurity, baseCtor.CallingConvention, types, reqMods, optMods);

            il = builder.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            if (!unity)
            {
                EmitLoadProxyContext(il, type, proxyContextField, null);
            }

            il.Emit(OpCodes.Ldarg_0);

            for (int i = 1; i < paramCt; ++i)
                EmitUtility.EmitArgument(il, i + 1, false);

            il.Emit(OpCodes.Call, baseCtor);
            
            if (!unity && idType != null)
            {
                EmitIdCheck(il, type, interfaceType!, idType, isIdNullable, typeGivesInternalAccess, elementType, getHasValueMethod, getValueMethod, dictTryAddMethod, idField, dictField);
            }
            else
            {
                il.Emit(OpCodes.Ret);
            }
        }

        if (unity)
        {
            MethodBuilder startMethod = typeBuilder.DefineMethod("Start",
                existingStartMethod != null
                    ? MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | (existingStartMethod.Attributes & MethodAttributes.MemberAccessMask)
                    : MethodAttributes.Private,
                CallingConventions.Standard,
                existingStartMethod?.ReturnType ?? typeof(void),
                null, null, Type.EmptyTypes, null, null
            );

            il = startMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

            EmitLoadProxyContext(il, type, proxyContextField, unityRouterField);

            if (existingStartMethod != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, existingStartMethod);
            }

            if (idType != null)
            {
                EmitIdCheck(il, type, interfaceType!, idType, isIdNullable, typeGivesInternalAccess, elementType, getHasValueMethod, getValueMethod, dictTryAddMethod, idField, dictField);
            }
            else
            {
                il.Emit(OpCodes.Ret);
            }
        }

        if (idType != null)
        {
            ConstructorInfo dictCtor = dictType.GetConstructor(Type.EmptyTypes)
                                       ?? throw new UnexpectedMemberAccessException(new MethodDefinition(dictType)
                                           .WithNoParameters()
                                       );

            MethodInfo dictTryGetValueMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryGetValue), BindingFlags.Instance | BindingFlags.Public)
                                               ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryGetValue))
                                                   .DeclaredIn(dictType, isStatic: false)
                                                   .Returning<bool>()
                                                   .WithParameter(idType, "key")
                                                   .WithParameter(type.MakeByRefType(), "value", ByRefTypeMode.Out)
                                               );

            MethodInfo dictTryRemoveMethod = dictType.GetMethod(nameof(ConcurrentDictionary<object, object>.TryRemove), BindingFlags.Instance | BindingFlags.Public, null, [ elementType, typeof(WeakReference).MakeByRefType() ], null)
                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ConcurrentDictionary<object, object>.TryRemove))
                                                 .DeclaredIn(dictType, isStatic: false)
                                                 .Returning<bool>()
                                                 .WithParameter(idType, "key")
                                                 .WithParameter(type.MakeByRefType(), "value", ByRefTypeMode.Out)
                                             );

            // static constructor to initalize the dictionary
            typeInitializer = typeBuilder.DefineTypeInitializer();
            il = typeInitializer.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);
            il.Emit(OpCodes.Newobj, dictCtor);
            il.Emit(OpCodes.Stsfld, dictField);

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
                this.LogWarning(string.Format(Properties.Logging.BaseReleaseMethodCantBePublic, Accessor.Formatter.Format(baseReleaseMethod.DeclaringType!)));
                baseReleaseMethod = null;
            }

            // define a finalizer to remove this object from the dictionary
            MethodBuilder finalizerMethod = typeBuilder.DefineMethod("Finalize",
                MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Family,
                CallingConventions.Standard,
                typeof(void),
                null, null, null, null, null
            );
            
            EmitFinalizer(finalizerMethod, type, baseFinalizerMethod, baseReleaseMethod, dictField, idField, dictTryRemoveMethod, suppressFinalizeField);

            if (unity)
            {
                MethodBuilder onDestroyMethod = typeBuilder.DefineMethod("OnDestroy",
                    existingDestroyMethod != null
                        ? MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | (existingDestroyMethod.Attributes & MethodAttributes.MemberAccessMask)
                        : MethodAttributes.Private,
                    CallingConventions.Standard,
                    existingDestroyMethod?.ReturnType ?? typeof(void),
                    null, null, Type.EmptyTypes, null, null
                );

                EmitFinalizer(onDestroyMethod, type, existingDestroyMethod, baseReleaseMethod, dictField, idField, dictTryRemoveMethod, suppressFinalizeField);
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

            LocalBuilder lcl = il.DeclareLocal(typeof(WeakReference));
            Label alreadyDoneLabel = il.DefineLabel();
            Label retLbl = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, suppressFinalizeField);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, CommonReflectionCache.InterlockedExchangeInt);
            il.Emit(OpCodes.Brtrue, alreadyDoneLabel);

            if (baseReleaseMethod is { IsAbstract: false })
            {
                il.BeginExceptionBlock();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseReleaseMethod);
                if (baseReleaseMethod.ReturnType != typeof(void))
                    il.Emit(OpCodes.Pop);

                il.BeginCatchBlock(typeof(Exception));

                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stfld, suppressFinalizeField);
                il.Emit(OpCodes.Rethrow);

                il.EndExceptionBlock();
            }

            il.Emit(OpCodes.Ldsfld, dictField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, idField);
            il.Emit(OpCodes.Ldloca, lcl);
            il.Emit(OpCodes.Call, dictTryRemoveMethod);
#if DEBUG
            Label brtrue = il.DefineLabel();
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

            if (idField.FieldType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, elementType!);
            
            il.Emit(OpCodes.Ldloca, lcl);
            il.Emit(OpCodes.Call, dictTryGetValueMethod);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldloc, lcl);
            il.Emit(OpCodes.Ret);
        }
        else
            typeInitializer = null;

        MethodBuilder calcOverheadSize = typeBuilder.DefineMethod(
            CalculateOverheadSizeMethodName,
            MethodAttributes.Public,
            typeof(int),
            GetOverheadSizeArgs
        );

        calcOverheadSize.DefineParameter(1, ParameterAttributes.None, "method");
        calcOverheadSize.DefineParameter(2, ParameterAttributes.None, "callInfo");
        calcOverheadSize.DefineParameter(3, ParameterAttributes.Out, "sizeWithoutId");

        il = calcOverheadSize.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

        LocalBuilder lclRouter = il.DeclareLocal(typeof(IRpcRouter));
        LocalBuilder lclOverheadSize = il.DeclareLocal(typeof(int));
        LocalBuilder lclSerializer = il.DeclareLocal(typeof(IRpcSerializer));

        il.CommentIfDebug("IRpcRouter lclRouter = this.proxyContextField.Router;");
        il.CommentIfDebug("IRpcSerializer lclSerializer = this.proxyContextField.Serializer;");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, proxyContextField);
        il.Emit(OpCodes.Dup);

        il.Emit(OpCodes.Ldfld, CommonReflectionCache.ProxyContextRouterField);
        il.Emit(OpCodes.Stloc, lclRouter);

        il.Emit(OpCodes.Ldfld, CommonReflectionCache.ProxyContextSerializerField);
        il.Emit(OpCodes.Stloc, lclSerializer);

        il.CommentIfDebug("int lclOverheadSize = lclRouter.GetOverheadSize(methodof(method), in methodInfoField);");
        il.Emit(OpCodes.Ldloc, lclRouter);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterGetOverheadSize);
        il.Emit(OpCodes.Stloc, lclOverheadSize);

        il.CommentIfDebug("sizeWithoutId = lclOverheadSize;");
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Ldloc, lclOverheadSize);
        il.Emit(OpCodes.Stind_I4);

        TypeCode tc = TypeCode.Object;
        LocalBuilder? lclIdTypeSize = null,
                      lclKnownTypeId = null,
                      lclHasKnownTypeId = null,
                      lclPreCalcId = null;

        string? idTypeName = null;
        bool canQuickSerialize = false;
        bool passByRef = false;
        EmitCalculateIdSize(il, lclOverheadSize, true, idField, lclSerializer, ref tc, ref lclIdTypeSize, ref idTypeName,
            ref lclKnownTypeId, ref lclHasKnownTypeId, ref canQuickSerialize, ref passByRef, ref lclPreCalcId);

        il.Emit(OpCodes.Ldloc, lclOverheadSize);
        il.Emit(OpCodes.Ret);

        MethodBuilder writeOverhead = typeBuilder.DefineMethod(
            WriteIdentifierMethodName,
            MethodAttributes.Public,
            typeof(int),
            WriteIdentifierArgs
        );


        writeOverhead.DefineParameter(1, ParameterAttributes.None, "bytes");
        writeOverhead.DefineParameter(2, ParameterAttributes.None, "maxSize");

        il = writeOverhead.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

        lclRouter = il.DeclareLocal(typeof(IRpcRouter));
        lclSerializer = il.DeclareLocal(typeof(IRpcSerializer));
        LocalBuilder lclValueSize = il.DeclareLocal(typeof(int));

        il.CommentIfDebug("IRpcRouter lclRouter = this.proxyContextField.Router;");
        il.CommentIfDebug("IRpcSerializer lclSerializer = this.proxyContextField.Serializer;");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, proxyContextField);
        il.Emit(OpCodes.Dup);

        il.Emit(OpCodes.Ldfld, CommonReflectionCache.ProxyContextRouterField);
        il.Emit(OpCodes.Stloc, lclRouter);

        il.Emit(OpCodes.Ldfld, CommonReflectionCache.ProxyContextSerializerField);
        il.Emit(OpCodes.Stloc, lclSerializer);

        LocalBuilder lclPreOverheadSize = il.DeclareLocal(typeof(int));
        LocalBuilder lclByteBuffer = il.DeclareLocal(typeof(byte*));

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stloc, lclByteBuffer);

        EmitWriteIdentifier(il, idField, lclSerializer, lclValueSize, null, 2, null, null, null, passByRef, canQuickSerialize, idTypeName, lclPreOverheadSize, lclByteBuffer, tc, null);

        il.Emit(OpCodes.Ldloc, lclValueSize);
        il.Emit(OpCodes.Ret);

        return typeBuilder;
    }
    private static void EmitFinalizer(MethodBuilder method, Type type, MethodInfo? baseMethod, MethodInfo? baseReleaseMethod, FieldInfo dictField, FieldInfo idField, MethodInfo dictTryRemoveMethod, FieldInfo suppressFinalizeField)
    {
        IOpCodeEmitter il = method.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);

        LocalBuilder lcl = il.DeclareLocal(typeof(WeakReference));
        Label retLbl = il.DefineLabel();

        if (baseMethod != null)
            il.BeginExceptionBlock();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, suppressFinalizeField);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Call, CommonReflectionCache.InterlockedExchangeInt);
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
        il.Emit(OpCodes.Ldloca, lcl);
        il.Emit(OpCodes.Call, dictTryRemoveMethod);
#if DEBUG
        Label brtrue = il.DefineLabel();
        il.Emit(OpCodes.Brfalse, brtrue);
        il.EmitWriteLine($"Removed {Accessor.Formatter.Format(type)} from {method.Name}.");
        il.Emit(OpCodes.Br, retLbl);
        il.MarkLabel(brtrue);
        il.EmitWriteLine($"Didn't remove {Accessor.Formatter.Format(type)} from {method.Name}.");
#else
        il.Emit(OpCodes.Pop);
#endif
        if (baseMethod != null)
        {
            il.MarkLabel(retLbl);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseMethod);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ret);
        }
        else
        {
            il.MarkLabel(retLbl);
            il.Emit(OpCodes.Ret);
        }
    }

    private static void EmitLoadProxyContext(IOpCodeEmitter il, Type type, FieldInfo proxyContextField, FieldInfo? unityRouterField)
    {
        // get proxy context
        if (unityRouterField == null)
        {
            il.Emit(OpCodes.Ldarg_1);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, unityRouterField);

            il.Emit(OpCodes.Dup);
            Label label = il.DefineLabel();
            il.Emit(OpCodes.Brtrue_S, label);

            // if rpc field is null (not created with unity extensions)
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldstr, string.Format(Properties.Exceptions.ExceptionUnityComponentNotCreatedWithRouter, Accessor.ExceptionFormatter.Format(type)));
            il.Emit(OpCodes.Newobj, CommonReflectionCache.CtorInvalidOperationException);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(label);
        }

        il.Emit(OpCodes.Ldtoken, type);
        il.Emit(OpCodes.Call, Accessor.GetMethod(Type.GetTypeFromHandle)!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, proxyContextField);
        il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterGetDefaultProxyContext);
        if (unityRouterField != null)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stfld, unityRouterField);
        }
    }


    private void EmitIdCheck(IOpCodeEmitter il, Type type, Type interfaceType, Type idType, bool isIdNullable, bool typeGivesInternalAccess, Type? elementType, MethodInfo? getHasValueMethod, MethodInfo? getValueMethod, MethodInfo dictTryAddMethod, FieldInfo idField, FieldInfo dictField)
    {
        RpcObjectHelper.GetIdentifierLocation(interfaceType,
            idType,
            type,
            typeGivesInternalAccess,
            isIdNullable,
            elementType,
            this,
            out PropertyInfo identifierProperty,
            out FieldInfo? identifierBackingField
        );

        LocalBuilder identifier = il.DeclareLocal(idType);

        Label? ifNotNull = null;
        Label ifDidntAdd = il.DefineLabel();
        il.Emit(OpCodes.Ldarg_0);
        bool isAddr = false;
        LocalBuilder id2 = identifier;
        if (type.IsValueType)
        {
            if (identifierBackingField != null)
            {
                id2 = il.DeclareLocal(idType.MakeByRefType());
                isAddr = true;

                il.Emit(OpCodes.Ldflda, identifierBackingField);
            }
            else
            {
                il.Emit(identifierProperty.GetMethod!.GetCallRuntime(), identifierProperty.GetMethod);
            }

            il.Emit(OpCodes.Stloc, id2);
        }

        if (isIdNullable)
        {
            ifNotNull = il.DefineLabel();
            il.Emit(isAddr ? OpCodes.Ldloc : OpCodes.Ldloca, id2);
            il.Emit(OpCodes.Call, getHasValueMethod!);
            il.Emit(OpCodes.Brtrue, ifNotNull.Value);
        }
        else if (!idType.IsValueType)
        {
            ifNotNull = il.DefineLabel();
            if (identifierBackingField != null)
                il.Emit(OpCodes.Ldfld, identifierBackingField);
            else
                il.Emit(identifierProperty.GetMethod.GetCallRuntime(), identifierProperty.GetMethod);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, identifier);
            il.Emit(OpCodes.Brtrue, ifNotNull.Value);
        }

        if (ifNotNull.HasValue)
        {
#if DEBUG
            il.EmitWriteLine($"Had a default value for instance id of type {Accessor.Formatter.Format(type)}.");
#endif
            il.Emit(OpCodes.Ldstr, string.Format(Properties.Exceptions.InstanceIdDefaultValue, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(interfaceType)));
            il.Emit(OpCodes.Newobj, _identifierErrorConstructor);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(ifNotNull.Value);
        }

        il.Emit(OpCodes.Ldarg_0);
        if (isIdNullable)
        {
            il.Emit(isAddr ? OpCodes.Ldloc : OpCodes.Ldloca, id2);
            il.Emit(OpCodes.Call, getValueMethod!);
        }
        else
        {
            il.Emit(OpCodes.Ldloc, id2);
            if (isAddr)
                SerializerGenerator.LoadFromRef(idType, il);
        }
        il.Emit(OpCodes.Stfld, idField);

        il.Emit(OpCodes.Ldsfld, dictField);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, idField);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, CommonReflectionCache.WeakReferenceConstructor);

        il.Emit(OpCodes.Call, dictTryAddMethod);

        il.Emit(OpCodes.Brfalse, ifDidntAdd);

        il.Emit(OpCodes.Ret);

        il.MarkLabel(ifDidntAdd);
#if DEBUG
        il.EmitWriteLine($"Instance of {Accessor.Formatter.Format(type)} already exists.");
#endif
        il.Emit(OpCodes.Ldstr, string.Format(Properties.Exceptions.InstanceWithThisIdAlreadyExists, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(interfaceType)));
        il.Emit(OpCodes.Newobj, _identifierErrorConstructor);
        il.Emit(OpCodes.Throw);
    }
    private static readonly Type[] GeneratedTypeSetupMethodArgs = [ typeof(GeneratedProxyTypeBuilder) ];

    private readonly GeneratedProxyTypeBuilder _generatedTypeBuilder;
    private readonly object[] _generatedTypeBuilderArgs;

    private unsafe ProxyTypeInfo CreateProxyType(Type type)
    {
        ProxyTypeInfo info = default;

        if (type.TryGetAttributeSafe(out RpcGeneratedProxyTypeAttribute generatedProxyAttribute))
        {
            if (!typeof(IRpcGeneratedProxyType).IsAssignableFrom(type))
            {
                throw new ArgumentException(Properties.Exceptions.GeneratedProxyTypeNotProperlyImplemented, nameof(type));
            }

#if NET7_0_OR_GREATER
            if (typeof(IRpcGeneratedProxyTypeWithSetupMethod).IsAssignableFrom(type))
            {
                InvokeStaticGeneratedTypeSetupMethod(type);
            }
            else
#else
            if (generatedProxyAttribute.TypeSetupMethodName != null)
            {
                MethodInfo? method = type.GetMethod(
                    generatedProxyAttribute.TypeSetupMethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    CallingConventions.Any,
                    GeneratedTypeSetupMethodArgs,
                    null);

                if (method == null)
                {
                    throw new ArgumentException(Properties.Exceptions.GeneratedProxyTypeNotProperlyImplemented, nameof(type));
                }

                method.Invoke(null, _generatedTypeBuilderArgs);
            }
#endif

            info.IsGenerated = true;
            info.Type = type;
            return info;
        }

        string typeName = (type.FullName ?? type.Name) + "<RPC_Proxy>";

        bool unity = false;

        // unity objects can not use constructors, so instead we add a Start() method.
        type.ForEachBaseType((bt, _) =>
        {
            if (!TypeUtility.GetAssemblyQualifiedNameNoVersion(bt).Equals("UnityEngine.MonoBehaviour, UnityEngine.CoreModule", StringComparison.Ordinal))
                return true;

            unity = true;
            return false;
        });

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

        IOpCodeEmitter? typeInitIl = null;
        
        TypeBuilder builder = StartProxyType(ref typeName, type, typeGivesInternalAccess, unity, out FieldBuilder proxyContextField, out ConstructorBuilder? typeInitializer, out FieldBuilder? idField, out FieldBuilder? unityRouterField);

        List<string> takenMethodNames = new List<string>();
        List<MethodInfo>? broadcastReceivers = null;
        foreach (MethodInfo method in methods)
        {
            if (!method.TryGetAttributeSafe(out RpcSendAttribute targetAttribute) || method.DeclaringType == typeof(object))
            {
                if (method.IsAbstract && method.DeclaringType == type)
                    throw new ArgumentException(Properties.Exceptions.TypeNotInheritable, nameof(type));

                if (method.TryGetAttributeSafe(out RpcReceiveAttribute recv) && !string.IsNullOrEmpty(recv.MethodName) && !method.IsIgnored())
                {
                    // is broadcast
                    (broadcastReceivers ??= new List<MethodInfo>(4)).Add(method);
                }

                continue;
            }

            if (method.IsGenericMethodDefinition)
                throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionGenericMethod, Accessor.ExceptionFormatter.Format(method)));

            bool isFireAndForget = false;
            if (method.ReturnType == typeof(RpcTask))
            {
                isFireAndForget = method.IsDefinedSafe<RpcFireAndForgetAttribute>();
            }
            else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(RpcTask<>))
            {
                if (method.IsDefinedSafe<RpcFireAndForgetAttribute>())
                    throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionReturnValue, Accessor.ExceptionFormatter.Format(method)));
            }
            else if (method.ReturnType == typeof(void))
            {
                isFireAndForget = true;
            }
            else
            {
                throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionReturnType, Accessor.ExceptionFormatter.Format(method)));
            }

            if (!VisibilityUtility.IsMethodOverridable(method))
            {
                this.LogWarning(string.Format(Properties.Logging.MethodNotVisibileToOverridingClasses, Accessor.Formatter.Format(method), Accessor.Formatter.Format(type)));
                continue;
            }

            bool raw = targetAttribute.Raw;

            ParameterInfo[] parameters = method.GetParameters();

            MethodAttributes privacyAttributes = method.Attributes & (
                MethodAttributes.Public
                | MethodAttributes.Private
                | MethodAttributes.Assembly
                | MethodAttributes.Family
                | MethodAttributes.FamANDAssem
                | MethodAttributes.FamORAssem);

            SerializerGenerator.BindParameters(parameters, out ArraySegment<ParameterInfo> toInject, out ArraySegment<ParameterInfo> toBind);

            Type[]? genericArguments = null;
            Type? serializerCache = null;
            if (!raw)
            {
                genericArguments = new Type[toBind.Count];
                for (int i = 0; i < toBind.Count; ++i)
                {
                    ParameterInfo param = toBind.Array![i + toBind.Offset];
                    if (param.ParameterType.IsByRef)
                    {
                        if (param.IsOut)
                            throw new RpcInvalidParameterException(param.Position, param, method, Properties.Exceptions.RpcInvalidParameterExceptionOutMessage);

                        try
                        {
                            genericArguments[i] = param.ParameterType.GetElementType() ?? param.ParameterType;
                            continue;
                        }
                        catch (NotSupportedException) { }
                    }
                    genericArguments[i] = param.ParameterType;
                }

                serializerCache = SerializerGenerator.GetSerializerType(genericArguments.Length);
            }

            ushort? cancellationToken = null;

            bool injectedConnection = false;
            ushort injectedConnectionArgNum = 0;
            for (int i = 0; i < toInject.Count; ++i)
            {
                ParameterInfo param = toInject.Array![i + toInject.Offset];
                if (param.IsOut)
                    throw new RpcInvalidParameterException(param.Position, param, method, Properties.Exceptions.RpcInvalidParameterExceptionOutMessage);

                if (param.ParameterType == typeof(CancellationToken))
                {
                    cancellationToken = checked ( (ushort)(param.Position + 1) );
                    continue;
                }

                bool multi = typeof(IEnumerable<IModularRpcConnection>).IsAssignableFrom(param.ParameterType);

                if (!typeof(IModularRpcConnection).IsAssignableFrom(param.ParameterType) && !multi)
                {
                    throw new RpcInvalidParameterException(param.Position, param, method, string.Format(Properties.Exceptions.RpcInvalidParameterInvalidInjection, Accessor.ExceptionFormatter.Format(param.ParameterType)));
                }

                if (injectedConnection)
                {
                    throw new RpcInvalidParameterException(param.Position, param, method, Properties.Exceptions.RpcInvalidParameterMultipleConnectionsInvokeMethod);
                }
                injectedConnection = true;
                injectedConnectionArgNum = checked ( (ushort)(param.Position + 1) );
            }

            MethodInfo? getConnectionsGetter = null;
            if (!injectedConnection)
            {
                if (typeof(IRpcSingleConnectionObject).IsAssignableFrom(type))
                {
                    getConnectionsGetter = Accessor.GetImplementedMethod(type, CommonReflectionCache.RpcSingleConnectionObjectConnection) ?? CommonReflectionCache.RpcSingleConnectionObjectConnection;
                }
                else if (typeof(IRpcMultipleConnectionsObject).IsAssignableFrom(type))
                {
                    getConnectionsGetter = Accessor.GetImplementedMethod(type, CommonReflectionCache.RpcMultipleConnectionsObjectConnections) ?? CommonReflectionCache.RpcMultipleConnectionsObjectConnections;
                }
            }

            if (!raw && serializerCache!.IsGenericTypeDefinition)
                serializerCache = serializerCache.MakeGenericType(genericArguments!);

            FieldInfo? getSizeMethod = null, writeBytesMethod = null;
            MethodInfo? getSizeInvokeMethod = null, writeBytesInvokeMethod = null;

            if (!raw)
            {
                getSizeMethod = serializerCache!.GetField(SerializerGenerator.GetSizeMethodField)
                                ?? throw new UnexpectedMemberAccessException(new MethodDefinition(SerializerGenerator.GetSizeMethodField)
                                    .DeclaredIn(serializerCache, isStatic: true)
                                    .Returning<int>()
                                );

                writeBytesMethod = serializerCache.GetField(SerializerGenerator.WriteToBytesMethodField)
                                   ?? throw new UnexpectedMemberAccessException(new MethodDefinition(SerializerGenerator.WriteToBytesMethodField)
                                       .DeclaredIn(serializerCache, isStatic: true)
                                       .ReturningVoid()
                                   );

                Type getSizeDelegateType = getSizeMethod.FieldType;
                Type writeBytesDelegateType = writeBytesMethod.FieldType;

                getSizeInvokeMethod = getSizeDelegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)
                                      ?? throw new UnexpectedMemberAccessException(new MethodDefinition("Invoke")
                                          .DeclaredIn(getSizeDelegateType, isStatic: false)
                                          .Returning<int>()
                                      );

                writeBytesInvokeMethod = writeBytesDelegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)
                                         ?? throw new UnexpectedMemberAccessException(new MethodDefinition("Invoke")
                                             .DeclaredIn(writeBytesDelegateType, isStatic: false)
                                             .Returning<int>()
                                         );
            }

            string fieldMethodName = method.Name;
            if (!takenMethodNames.Contains(fieldMethodName))
            {
                takenMethodNames.Add(fieldMethodName);
            }
            else for (int i = 1; ; ++i)
            {
                string newName = fieldMethodName + "<ovl" + i.ToString(CultureInfo.InvariantCulture) + ">";
                if (takenMethodNames.Contains(newName))
                    continue;

                takenMethodNames.Add(newName);
                fieldMethodName = newName;
                break;
            }

            fieldMethodName = string.Format(CallMethodInfoFieldFormat, fieldMethodName);
            FieldBuilder methodInfoField = builder.DefineField(fieldMethodName,
                typeof(RpcCallMethodInfo),
                FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly
            );
            
            RpcCallMethodInfo rpcCall = RpcCallMethodInfo.FromCallMethod(this, method, isFireAndForget);
            
            typeInitializer ??= builder.DefineTypeInitializer();
            typeInitIl ??= typeInitializer.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);
            
            typeInitIl.Emit(OpCodes.Ldsflda, methodInfoField);
            rpcCall.EmitToAddress(typeInitIl);

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

            MethodBuilder methodBuilder = builder.DefineMethod(method.Name,
                privacyAttributes | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                method.CallingConvention,
                method.ReturnType,
                method.ReturnParameter?.GetRequiredCustomModifiers(),
                method.ReturnParameter?.GetOptionalCustomModifiers(),
                types, reqMods, optMods);

            methodBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    CommonReflectionCache.CallerInfoFieldNameAttributeCtor,
                    [ fieldMethodName ]
                )
            );
            
            methodBuilder.InitLocals = false;

            IOpCodeEmitter il = methodBuilder.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);
#if DEBUG
            il.EmitWriteLine("Calling " + Accessor.Formatter.Format(method));
#endif

            bool isReturningTask = method.ReturnType != typeof(void);
            LocalBuilder? lclTaskRtn = isReturningTask ? il.DeclareLocal(typeof(RpcTask)) : null;
            LocalBuilder lclRouter = il.DeclareLocal(typeof(IRpcRouter));
            LocalBuilder lclSerializer = il.DeclareLocal(typeof(IRpcSerializer));

            il.CommentIfDebug("IRpcRouter lclRouter = this.proxyContextField.Router;");
            il.CommentIfDebug("IRpcSerializer lclSerializer = this.proxyContextField.Serializer;");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, proxyContextField);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldfld, CommonReflectionCache.ProxyContextRouterField);
            il.Emit(OpCodes.Stloc, lclRouter);

            il.Emit(OpCodes.Ldfld, CommonReflectionCache.ProxyContextSerializerField);
            il.Emit(OpCodes.Stloc, lclSerializer);

            ushort? bytesParamIndex = null,
                countParamIndex = null,
                canTakeOwnershipParamIndex = null;

            Type? bytesType = null,
                countType = null;

            bool isByteWriter = false, isByteReader = false;

            if (raw)
            {
                for (int i = 0; i < toBind.Count; ++i)
                {
                    ParameterInfo param = toBind.Array![i + toBind.Offset];
                    Type parameterType = param.ParameterType;
                    
                    if (parameterType.IsByRef)
                        parameterType = parameterType.GetElementType()!;

                    if (parameterType == typeof(bool))
                    {
                        canTakeOwnershipParamIndex = checked( (ushort)(param.Position + 1) );
                    }
                    else if (parameterType.IsPrimitive && (
                            parameterType == typeof(int)
                            || parameterType == typeof(uint)
                            || parameterType == typeof(byte)
                            || parameterType == typeof(sbyte)
                            || parameterType == typeof(ushort)
                            || parameterType == typeof(short)
                            || parameterType == typeof(ulong)
                            || parameterType == typeof(long)
                            || parameterType == typeof(nuint)
                            || parameterType == typeof(nint)
                        ))
                    {
                        countType = parameterType;
                        countParamIndex = checked( (ushort)(param.Position + 1) );
                    }
                    else if (parameterType == typeof(byte*)
                             || typeof(IEnumerable<byte>).IsAssignableFrom(parameterType)
                             || parameterType == typeof(Span<byte>)
                             || parameterType == typeof(ReadOnlySpan<byte>)
                             || parameterType == typeof(Memory<byte>)
                             || parameterType == typeof(ReadOnlyMemory<byte>)
                             || typeof(Stream).IsAssignableFrom(parameterType)
                            )
                    {
                        isByteWriter = false;
                        isByteReader = false;
                        bytesType = parameterType;
                        bytesParamIndex = checked( (ushort)(param.Position + 1) );
                    }
                    else if (parameterType.AssemblyQualifiedName != null)
                    {
                        if (parameterType.AssemblyQualifiedName.StartsWith("DanielWillett.SpeedBytes.ByteReader, DanielWillett.SpeedBytes", StringComparison.Ordinal))
                        {
                            isByteReader = true;
                            isByteWriter = false;
                            bytesType = parameterType;
                            bytesParamIndex = checked( (ushort)(param.Position + 1) );
                        }
                        else if (parameterType.AssemblyQualifiedName.StartsWith("DanielWillett.SpeedBytes.ByteWriter, DanielWillett.SpeedBytes", StringComparison.Ordinal))
                        {
                            isByteWriter = true;
                            isByteReader = false;
                            bytesType = parameterType;
                            bytesParamIndex = checked( (ushort)(param.Position + 1) );
                        }
                    }
                }
            }

            LocalBuilder? lclSize = raw ? null : il.DeclareLocal(typeof(int));
            LocalBuilder lclOverheadSize = il.DeclareLocal(typeof(int));
            LocalBuilder lclPreOverheadSize = il.DeclareLocal(typeof(int));
            LocalBuilder lclByteBuffer = il.DeclareLocal(typeof(byte*));
            LocalBuilder lclByteBufferPin = il.DeclareLocal(typeof(byte[]), true);
            LocalBuilder? lclPreCalcId = null;
            LocalBuilder? lclIdTypeSize = null;
            LocalBuilder? lclKnownTypeId = null;
            LocalBuilder? lclHasKnownTypeId = null;
            bool canQuickSerialize = false;
            bool passByRef = false;
            if (!raw)
            {
                il.CommentIfDebug("int lclSize = getSizeMethod.Invoke(lclSerializer, ...);");
                // invoke the static get-size delegate in the static dynamically generated serializer class
                il.Emit(OpCodes.Ldsfld, getSizeMethod!);
                il.Emit(OpCodes.Ldloc, lclSerializer);
                for (int i = 0; i < genericArguments!.Length; ++i)
                {
                    il.Emit(!parameters[i].ParameterType.IsByRef ? OpCodes.Ldarga : OpCodes.Ldarg, checked((ushort)(toBind.Array![i + toBind.Offset].Position + 1)));
                }

                il.Emit(OpCodes.Callvirt, getSizeInvokeMethod!);
                il.Emit(OpCodes.Stloc, lclSize!);
            }

            il.CommentIfDebug("int lclOverheadSize = lclRouter.GetOverheadSize(methodof(method), in methodInfoField);");
            il.CommentIfDebug("int lclPreOverheadSize = lclOverheadSize;");
            il.Emit(OpCodes.Ldloc, lclRouter);
            il.Emit(OpCodes.Ldtoken, method);
            il.Emit(OpCodes.Ldsflda, methodInfoField);
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterGetOverheadSize);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, lclPreOverheadSize);
            il.Emit(OpCodes.Stloc, lclOverheadSize);

            TypeCode tc = TypeCode.Object;
            string? idTypeName = null;

            EmitCalculateIdSize(il, lclOverheadSize, false, idField, lclSerializer, ref tc, ref lclIdTypeSize, ref idTypeName, ref lclKnownTypeId, ref lclHasKnownTypeId, ref canQuickSerialize, ref passByRef, ref lclPreCalcId);

            if (!raw)
            {
                il.CommentIfDebug("lclSize += lclOverheadSize;");
                il.Emit(OpCodes.Ldloc, lclSize!);
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, lclSize!);
            }

            if (!raw)
            {
                il.CommentIfDebug($"if (lclSize > {MaxSizeForStackalloc})");
                il.CommentIfDebug("{");
                il.CommentIfDebug("    byte[] lclByteBufferPin = new byte[lclSize];");
                il.CommentIfDebug("    byte* lclByteBuffer = fixed {{ lclByteBufferPin; }}");
                il.CommentIfDebug("}");
                il.CommentIfDebug("else");
                il.CommentIfDebug("{");
                il.CommentIfDebug("    byte* lclByteBuffer = stackalloc byte[lclSize];");
                il.CommentIfDebug("}");
                Label lblSizeIsTooBigForLocalloc = il.DefineLabel();
                Label lblSizeIsFineForLocalloc = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, lclSize!);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, MaxSizeForStackalloc);
                il.Emit(OpCodes.Bgt, lblSizeIsTooBigForLocalloc);

                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Localloc);
                il.Emit(OpCodes.Stloc, lclByteBuffer);
                il.Emit(OpCodes.Br, lblSizeIsFineForLocalloc);

                il.MarkLabel(lblSizeIsTooBigForLocalloc);
                il.Emit(OpCodes.Newarr, typeof(byte));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lclByteBufferPin);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldelema, typeof(byte));
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Stloc, lclByteBuffer);

                il.MarkLabel(lblSizeIsFineForLocalloc);
                il.Emit(OpCodes.Nop);
            }
            else
            {
                il.CommentIfDebug("byte[] lclByteBufferPin = new byte[lclOverheadSize];");
                il.CommentIfDebug("byte* lclByteBuffer = fixed {{ lclByteBufferPin; }}");
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Newarr, typeof(byte));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lclByteBufferPin);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldelema, typeof(byte));
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Stloc, lclByteBuffer);
            }

            il.BeginExceptionBlock();

            EmitWriteIdentifier(il, idField, lclSerializer, null, lclIdTypeSize, 0, lclKnownTypeId, lclPreCalcId, lclOverheadSize, passByRef, canQuickSerialize, idTypeName, lclPreOverheadSize, lclByteBuffer, tc, lclHasKnownTypeId);

            if (!raw)
            {
                il.CommentIfDebug("lclSize = lclOverheadSize + writeBytesMethod.Invoke(lclSerializer, lclByteBuffer + lclOverheadSize, (uint)(lclSize - lclOverheadSize), ...);");
                // invoke the static write delegate in the serializer class
                il.Emit(OpCodes.Ldsfld, writeBytesMethod!);
                il.Emit(OpCodes.Ldloc, lclSerializer);
                il.Emit(OpCodes.Ldloc, lclByteBuffer);
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldloc, lclSize!);
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Conv_Ovf_U4);
                for (int i = 0; i < genericArguments!.Length; ++i)
                {
                    il.Emit(!parameters[i].ParameterType.IsByRef ? OpCodes.Ldarga : OpCodes.Ldarg, checked((ushort)(toBind.Array![i + toBind.Offset].Position + 1)));
                }

                il.Emit(OpCodes.Callvirt, writeBytesInvokeMethod!);
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, lclSize!);
            }

            il.CommentIfDebug(!raw
                ? "lclTaskRtn = lclRouter.InvokeRpc(<connections>, lclSerializer, methodof(method), lclByteBuffer, lclSize, lclSize - lclOverheadSize, ref methodInfoField);"
                : "lclTaskRtn = lclRouter.InvokeRpc(<connections>, lclSerializer, methodof(method), new ArraySegment<byte>(lclByteBufferPin), <stream>, !canTakeOwnership, <hasByteCount ? byteCount : stream.Length - stream.Position>, ref methodInfoField)");
            
            il.Emit(OpCodes.Ldloc, lclRouter);
            if (getConnectionsGetter != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(getConnectionsGetter.GetCallRuntime(), getConnectionsGetter);
            }
            else if (injectedConnection)
            {
                il.Emit(OpCodes.Ldarg, injectedConnectionArgNum);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ldloc, lclSerializer);
            il.Emit(OpCodes.Ldtoken, method);
            if (cancellationToken.HasValue)
            {
                il.Emit(OpCodes.Ldarg, cancellationToken.Value);
            }
            else
            {
                LocalBuilder lclToken = il.DeclareLocal(typeof(CancellationToken));
                il.Emit(OpCodes.Ldloca, lclToken);
                il.Emit(OpCodes.Initobj, typeof(CancellationToken));
                il.Emit(OpCodes.Ldloc, lclToken);
            }

            if (!raw || !bytesParamIndex.HasValue)
            {
                il.Emit(OpCodes.Ldloc, lclByteBuffer);
                if (!raw)
                {
                    il.Emit(OpCodes.Ldloc, lclSize!);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldloc, lclOverheadSize);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Conv_Ovf_U4_Un);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, lclOverheadSize);
                    il.Emit(OpCodes.Ldc_I4_0);
                }

                il.Emit(OpCodes.Ldsflda, methodInfoField);
                il.Emit(OpCodes.Ldc_I4, (int)RpcInvokeOptions.Default);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterInvokeRpcBytes);
            }
            else
            {
                if (typeof(Stream).IsAssignableFrom(bytesType))
                {
                    il.Emit(OpCodes.Ldloc, lclByteBufferPin);
                    il.Emit(OpCodes.Newobj, CommonReflectionCache.CtorByteArraySegmentJustArray);

                    il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                    if (canTakeOwnershipParamIndex.HasValue)
                    {
                        il.Emit(OpCodes.Ldarg, canTakeOwnershipParamIndex.Value);
                        il.Emit(OpCodes.Not);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4_1);
                    }

                    if (countParamIndex.HasValue)
                    {
                        il.Emit(OpCodes.Ldarg, countParamIndex.Value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.StreamLength);
                        il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                        il.Emit(OpCodes.Callvirt, CommonReflectionCache.StreamPosition);
                        il.Emit(OpCodes.Sub);
                    }

                    il.Emit(OpCodes.Conv_Ovf_U4_Un);
                    il.Emit(OpCodes.Ldsflda, methodInfoField);
                    il.Emit(OpCodes.Ldc_I4, (int)RpcInvokeOptions.Default);
                    il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcRouterInvokeRpcStream);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                    ParameterInfo bytesParam = toBind.First(x => x.Position == bytesParamIndex.Value - 1);
                    if (bytesParam.ParameterType.IsByRef)
                    {
                        if (bytesType!.IsValueType)
                            il.Emit(OpCodes.Ldobj, bytesType);
                        else
                            il.Emit(OpCodes.Ldind_Ref);
                    }
                    if (countParamIndex.HasValue)
                    {
                        ParameterInfo countParam = toBind.First(x => x.Position == countParamIndex.Value - 1);
                        il.Emit(OpCodes.Ldarg, countParamIndex.Value);
                        if (countParam.ParameterType.IsByRef)
                        {
                            SerializerGenerator.LoadFromRef(countType!, il);
                        }
                        il.Emit(OpCodes.Conv_Ovf_U4_Un);
                    }
                    else
                    {
                        if (isByteWriter || isByteReader)
                        {
                            il.Emit(OpCodes.Ldc_I4_M1 /* uint.MaxValue */);
                        }
                        else if (bytesType == typeof(byte[]))
                        {
                            il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                            il.Emit(OpCodes.Ldlen);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (bytesType == typeof(byte*))
                        {
                            throw new RpcInjectionException(Properties.Exceptions.RpcInjectionExceptionBytePointerMustPassLength);
                        }
                        else if (bytesType == typeof(ArraySegment<byte>))
                        {
                            il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                            il.Emit(OpCodes.Call, CommonReflectionCache.ByteArraySegmentCount);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (bytesType == typeof(ReadOnlySpan<byte>))
                        {
                            il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                            il.Emit(OpCodes.Call, CommonReflectionCache.GetReadOnlySpanLength);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (bytesType == typeof(Span<byte>))
                        {
                            il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                            il.Emit(OpCodes.Call, CommonReflectionCache.GetSpanLength);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (bytesType == typeof(ReadOnlyMemory<byte>))
                        {
                            il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                            il.Emit(OpCodes.Call, CommonReflectionCache.GetReadOnlyMemoryLength);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (bytesType == typeof(Memory<byte>))
                        {
                            il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                            il.Emit(OpCodes.Call, CommonReflectionCache.GetMemoryLength);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (typeof(ICollection<byte>).IsAssignableFrom(bytesType))
                        {
                            if (bytesType!.IsValueType)
                            {
                                il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                                il.Emit(OpCodes.Constrained, bytesType);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                            }
                            il.Emit(OpCodes.Callvirt, CommonReflectionCache.ByteCollectionCount);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (typeof(IReadOnlyCollection<byte>).IsAssignableFrom(bytesType))
                        {
                            if (bytesType!.IsValueType)
                            {
                                il.Emit(OpCodes.Ldarga, bytesParamIndex.Value);
                                il.Emit(OpCodes.Constrained, bytesType);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldarg, bytesParamIndex.Value);
                            }
                            il.Emit(OpCodes.Callvirt, CommonReflectionCache.ByteReadOnlyCollectionCount);
                            il.Emit(OpCodes.Conv_U4);
                        }
                        else if (typeof(IEnumerable<byte>).IsAssignableFrom(bytesType))
                        {
                            il.Emit(OpCodes.Ldc_I4_M1 /* uint.MaxValue */);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldc_I4_0);
                        }
                    }

                    il.Emit(OpCodes.Ldloc, lclByteBufferPin);
                    il.Emit(OpCodes.Ldsflda, methodInfoField);
                    if (bytesType == typeof(byte[]))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByArray)!);
                    }
                    else if (bytesType == typeof(ArraySegment<byte>))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByArraySegment)!);
                    }
                    else if (bytesType == typeof(Span<byte>))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerBySpan)!);
                    }
                    else if (bytesType == typeof(ReadOnlySpan<byte>))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByReadOnlySpan)!);
                    }
                    else if (bytesType == typeof(Memory<byte>))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByMemory)!);
                    }
                    else if (bytesType == typeof(ReadOnlyMemory<byte>))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByReadOnlyMemory)!);
                    }
                    else if (bytesType == typeof(byte*))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByPointer)!);
                    }
                    else if (typeof(ICollection<byte>).IsAssignableFrom(bytesType))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByCollection)!);
                    }
                    else if (typeof(IEnumerable<byte>).IsAssignableFrom(bytesType))
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByEnumerable)!);
                    }
                    else if (isByteWriter)
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByByteWriter)!);
                    }
                    else if (isByteReader)
                    {
                        il.Emit(OpCodes.Call, Accessor.GetMethod(SourceGenerationServices.InvokeRpcInvokerByByteReader)!);
                    }
                    else
                    {
                        for (int i = 0; i < 8; ++i)
                            il.Emit(OpCodes.Pop);

                        il.ThrowException(typeof(RpcInjectionException));
                    }
                }
            }

            if (isReturningTask)
                il.Emit(OpCodes.Stloc, lclTaskRtn!);
            else
                il.Emit(OpCodes.Pop);

            il.BeginFinallyBlock();

            il.CommentIfDebug("lclByteBufferPin = null;");
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc, lclByteBufferPin);

            il.EndExceptionBlock();

            if (isReturningTask)
            {
                il.CommentIfDebug("return lclTaskRtn;");
                il.Emit(OpCodes.Ldloc, lclTaskRtn!);
            }
            else
                il.CommentIfDebug("return;");
            il.Emit(OpCodes.Ret);
        }

        if (broadcastReceivers != null)
        {
            List<MethodInfo> r2 = broadcastReceivers;
            _generatedTypeBuilder.AddBroadcastReceiveMethods(type, r =>
            {
                foreach (MethodInfo m in r2)
                {
                    r.AddMethod(RpcEndpointTarget.FromReceiveMethod(m));
                }
            });
        }

        if (typeInitializer != null)
        {
            typeInitIl ??= typeInitializer.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint);
            typeInitIl.Emit(OpCodes.Ret);
        }

        try
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
#if NETSTANDARD2_0
            info.Type = builder.CreateTypeInfo()!;
#else
            info.Type = builder.CreateType()!;
#endif
        }
        catch (TypeLoadException ex)
        {
            throw new ArgumentException(Properties.Exceptions.TypeNotPublic, nameof(type), ex);
        }

        if (unityRouterField != null)
        {
            info.SetUnityRouterField = Accessor.GenerateInstanceSetter<object, IRpcRouter>(unityRouterField, throwOnError: true);
        }

        return info;
    }

#if NET7_0_OR_GREATER
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(GeneratedMethodInvokeStaticSetupMethod<TestGeneratedType>))]
    private void InvokeStaticGeneratedTypeSetupMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        GeneratedMethodInvokeStaticSetupMethod instance = (GeneratedMethodInvokeStaticSetupMethod?)Activator.CreateInstance(typeof(GeneratedMethodInvokeStaticSetupMethod<>).MakeGenericType(type))!;
        instance.Execute(_generatedTypeBuilder);
    }

    private abstract class GeneratedMethodInvokeStaticSetupMethod
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public abstract void Execute(GeneratedProxyTypeBuilder? builder);
    }

    private class GeneratedMethodInvokeStaticSetupMethod<TGeneratedType>
        : GeneratedMethodInvokeStaticSetupMethod where TGeneratedType : IRpcGeneratedProxyTypeWithSetupMethod
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void Execute(GeneratedProxyTypeBuilder? builder)
        {
            // calling this method with null is used to generate the native code when compiling with AOT
            if (builder == null)
                return;
            TGeneratedType.__ModularRpcsGeneratedSetupStaticGeneratedProxy(builder);
        }
    }

    [Ignore, UsedImplicitly]
    private sealed class TestGeneratedType : IRpcGeneratedProxyTypeWithSetupMethod
    {
        void IRpcGeneratedProxyType.SetupGeneratedProxyInfo(in GeneratedProxyTypeInfo info) { }
        public static void __ModularRpcsGeneratedSetupStaticGeneratedProxy(GeneratedProxyTypeBuilder state) { }
    }
#endif

    private static void EmitCalculateIdSize(IOpCodeEmitter il, LocalBuilder lclOverheadSize, bool isOnce, FieldInfo? idField, LocalBuilder lclSerializer, ref TypeCode tc, ref LocalBuilder? lclIdTypeSize, ref string? idTypeName, ref LocalBuilder? lclKnownTypeId, ref LocalBuilder? lclHasKnownTypeId, ref bool canQuickSerialize, ref bool passByRef, ref LocalBuilder? lclPreCalcId)
    {
        if (idField == null)
        {
            il.CommentIfDebug("++lclOverheadSize;");
            il.Emit(OpCodes.Ldloc, lclOverheadSize);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclOverheadSize);
            return;
        }

        tc = TypeUtility.GetTypeCode(idField.FieldType);

        lclIdTypeSize = il.DeclareLocal(typeof(int));

        if (tc != TypeCode.Object)
        {
            il.CommentIfDebug("int lclIdTypeSize = 1;");
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, lclIdTypeSize);
        }
        else
        {
            idTypeName = TypeUtility.GetAssemblyQualifiedNameNoVersion(idField.FieldType);
            lclKnownTypeId = il.DeclareLocal(typeof(uint));
            lclHasKnownTypeId = isOnce ? null : il.DeclareLocal(typeof(bool));

            Label lblHasKnownId = il.DefineLabel();
            Label lblHasNoKnownId = il.DefineLabel();

            il.CommentIfDebug($"int lclIdTypeSize = !serializer.TryGetKnownTypeId(idType, out uint lclKnownTypeId) ? 2 + serializer.GetSize<string>(\"{idTypeName}\") : 6;");
            il.Emit(OpCodes.Ldloc, lclSerializer);
            il.Emit(OpCodes.Ldtoken, idField.FieldType);
            il.Emit(OpCodes.Call, Accessor.GetMethod(Type.GetTypeFromHandle)!);
            il.Emit(OpCodes.Ldloca, lclKnownTypeId);
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerTryGetKnownTypeId);
            if (!isOnce)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lclHasKnownTypeId!);
            }
            il.Emit(OpCodes.Brtrue, lblHasKnownId);

            il.Emit(OpCodes.Ldloc, lclSerializer);
            il.Emit(OpCodes.Ldstr, idTypeName);
            il.Emit(OpCodes.Callvirt,
                CommonReflectionCache.RpcSerializerGetSizeByVal.MakeGenericMethod([ typeof(string) ]));
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclIdTypeSize);
            il.Emit(OpCodes.Br, lblHasNoKnownId);

            il.MarkLabel(lblHasKnownId);
            il.Emit(OpCodes.Ldc_I4_6);
            il.Emit(OpCodes.Stloc, lclIdTypeSize);

            il.MarkLabel(lblHasNoKnownId);
        }

        il.Emit(OpCodes.Ldloc, lclOverheadSize);
        il.Emit(OpCodes.Ldloc, lclIdTypeSize);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc, lclOverheadSize);

        canQuickSerialize = tc != TypeCode.Object && SerializerGenerator.CanQuickSerializeType(idField.FieldType);
        passByRef = SerializerGenerator.ShouldBePassedByReference(idField.FieldType);

        Label? cantPrimWrite = null;
        Label? didPrimWrite = null;

        if (canQuickSerialize)
        {
            lclPreCalcId = isOnce ? null : il.DeclareLocal(typeof(bool));
            cantPrimWrite = il.DefineLabel();
            didPrimWrite = il.DefineLabel();
            int primSize = SerializerGenerator.GetPrimitiveTypeSize(idField.FieldType);

            il.CommentIfDebug("if (!lclSerializer.CanFastReadPrimitives) goto cantPrimWrite;");
            il.Emit(OpCodes.Ldloc, lclSerializer);
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerCanFastReadPrimitives);
            if (!isOnce)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, lclPreCalcId!);
            }
            il.Emit(OpCodes.Brfalse, cantPrimWrite.Value);

            il.CommentIfDebug($"lclOverheadSize += {primSize};");
            il.Emit(OpCodes.Ldloc, lclOverheadSize);
            il.Emit(OpCodes.Ldc_I4, primSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclOverheadSize);

            il.CommentIfDebug("goto didPrimWrite;");
            il.Emit(OpCodes.Br, didPrimWrite.Value);
        }

        if (cantPrimWrite.HasValue)
            il.MarkLabel(cantPrimWrite.Value);

        if (passByRef)
            il.CommentIfDebug("lclOverheadSize += lclSerializer.GetSize(__makeref(idField));");
        else
            il.CommentIfDebug($"lclOverheadSize += lclSerializer.GetSize<{Accessor.Formatter.Format(idField.FieldType)}>(idField);");

        il.Emit(OpCodes.Ldloc, lclOverheadSize);
        il.Emit(OpCodes.Ldloc, lclSerializer);

        if (passByRef)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, idField);
            il.Emit(OpCodes.Mkrefany, idField.FieldType);
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeByTRef);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, idField);
            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerGetSizeByVal.MakeGenericMethod(idField.FieldType));
        }

        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc, lclOverheadSize);

        if (didPrimWrite.HasValue)
            il.MarkLabel(didPrimWrite.Value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static void CheckLen(int size, int reqSize)
    {
        if (size < reqSize)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };
    }
    private static void EmitWriteIdentifier(IOpCodeEmitter il, FieldInfo? idField, LocalBuilder lclSerializer, LocalBuilder? lclValueSize, LocalBuilder? lclIdTypeSize, byte sizeArg, LocalBuilder? lclKnownTypeId, LocalBuilder? lclPreCalcId, LocalBuilder? lclOverheadSize, bool passByRef, bool canQuickSerialize, string? idTypeName, LocalBuilder lclPreOverheadSize, LocalBuilder lclByteBuffer, TypeCode tc, LocalBuilder? lclHasKnownTypeId)
    {
        if (idField == null)
        {
            il.Emit(OpCodes.Ldloc, lclByteBuffer);
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_U1);
            il.Emit(OpCodes.Unaligned, (byte)1);
            il.Emit(OpCodes.Stind_I1);

            if (lclIdTypeSize == null)
                return;

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, lclIdTypeSize);
            return;
        }

        bool checkSize = lclIdTypeSize == null;
        if (checkSize)
        {
            il.Emit(OpCodes.Ldarg_S, sizeArg);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, Accessor.GetMethod(CheckLen)!);
        }

        il.CommentIfDebug($"lclBytesBuffer[lclPreOverheadSize] = (byte)TypeCode.{tc};");
        il.Emit(OpCodes.Ldloc, lclByteBuffer);
        il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldc_I4_S, (byte)tc);
        il.Emit(OpCodes.Conv_U1);
        il.Emit(OpCodes.Unaligned, (byte)1);
        il.Emit(OpCodes.Stind_I1);

        bool hadTypeSize = lclIdTypeSize != null;
        lclIdTypeSize ??= il.DeclareLocal(typeof(int));
        if (!hadTypeSize)
        {
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, lclIdTypeSize);
        }

        if (tc == TypeCode.Object)
        {
            if (checkSize)
            {
                il.Emit(OpCodes.Ldarg_S, sizeArg);
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Call, Accessor.GetMethod(CheckLen)!);
            }

            Label lblHasKnownTypeId = il.DefineLabel();
            Label lblNoKnownTypeId = il.DefineLabel();

            il.CommentIfDebug("if (lclHasKnownTypeId) goto lblHasKnownTypeId;");
            il.Emit(OpCodes.Ldloc, lclByteBuffer);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Add);

            if (lclKnownTypeId == null || lclHasKnownTypeId == null)
            {
                lclKnownTypeId ??= il.DeclareLocal(typeof(int));
                il.CommentIfDebug("bool lclHasKnownTypeId = serializer.TryGetKnownTypeId(idType, out uint lclKnownTypeId);");
                il.Emit(OpCodes.Ldloc, lclSerializer);
                il.Emit(OpCodes.Ldtoken, idField.FieldType);
                il.Emit(OpCodes.Call, Accessor.GetMethod(Type.GetTypeFromHandle)!);
                il.Emit(OpCodes.Ldloca, lclKnownTypeId);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerTryGetKnownTypeId);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, lclHasKnownTypeId);
            }

            il.Emit(OpCodes.Brtrue, lblHasKnownTypeId);

            il.CommentIfDebug("lclByteBuffer[lclPreOverheadSize + 1] = (byte)IdentifierFlags.IsTypeNameOnly;");
            il.Emit(OpCodes.Ldc_I4_S, (byte)RpcEndpoint.IdentifierFlags.IsTypeNameOnly);
            il.Emit(OpCodes.Conv_U1);
            il.Emit(OpCodes.Unaligned, (byte)1);
            il.Emit(OpCodes.Stind_I1);

            if (!hadTypeSize)
            {
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, lclIdTypeSize);
            }

            idTypeName ??= TypeUtility.GetAssemblyQualifiedNameNoVersion(idField.FieldType);

            il.CommentIfDebug($"_ = serializer.WriteObject<string>({idTypeName}, lclByteBuffer + (2 + lclPreOverheadSize), (uint)(lclIdTypeSize - 2));");
            il.Emit(OpCodes.Ldloc, lclSerializer);
            il.Emit(OpCodes.Ldstr, idTypeName);
            il.Emit(OpCodes.Ldloc, lclByteBuffer);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Add);
            if (!hadTypeSize)
            {
                il.Emit(OpCodes.Ldarg_S, sizeArg);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
            }

            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Callvirt,
                CommonReflectionCache.RpcSerializerWriteObjectByValBytes.MakeGenericMethod([ typeof(string) ]));
            if (!hadTypeSize)
            {
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, lclIdTypeSize);
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }

            il.CommentIfDebug("goto lblNoKnownTypeId;");
            il.Emit(OpCodes.Br, lblNoKnownTypeId);

            il.CommentIfDebug("lblHasKnownTypeId:");
            il.MarkLabel(lblHasKnownTypeId);

            if (BitConverter.IsLittleEndian)
            {
                il.Emit(OpCodes.Dup);
            }

            if (checkSize)
            {
                il.Emit(OpCodes.Ldarg_S, sizeArg);
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Ldc_I4_5);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Call, Accessor.GetMethod(CheckLen)!);
            }

            il.CommentIfDebug("lclByteBuffer[lclPreOverheadSize + 1] = (byte)IdentifierFlags.IsKnownTypeOnly;");
            il.Emit(OpCodes.Ldc_I4_S, (byte)RpcEndpoint.IdentifierFlags.IsKnownTypeOnly);
            il.Emit(OpCodes.Conv_U1);
            il.Emit(OpCodes.Unaligned, (byte)1);
            il.Emit(OpCodes.Stind_I1);

            if (BitConverter.IsLittleEndian)
            {
                il.CommentIfDebug("*(uint*)(lclByteBuffer + (lclPreOverheadSize + 2)) = lclKnownTypeId;");
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldloc, lclKnownTypeId);
                il.Emit(OpCodes.Conv_U1);
                il.Emit(OpCodes.Unaligned, (byte)1);
                il.Emit(OpCodes.Stind_I4);

                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Ldc_I4_5);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, lclIdTypeSize);
            }
            else
            {
                il.CommentIfDebug("_ = serializer.WriteObject<uint>(lclKnownTypeId, lclByteBuffer + (2 + lclPreOverheadSize), 4u);");
                il.Emit(OpCodes.Ldloc, lclSerializer);
                il.Emit(OpCodes.Ldloc, lclKnownTypeId);
                il.Emit(OpCodes.Ldloc, lclByteBuffer);
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Add);
                if (!hadTypeSize)
                {
                    il.Emit(OpCodes.Ldarg_S, sizeArg);
                    il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Sub);
                }
                else
                    il.Emit(OpCodes.Ldc_I4_4);

                il.Emit(OpCodes.Conv_U4);
                il.Emit(OpCodes.Callvirt,
                    CommonReflectionCache.RpcSerializerWriteObjectByValBytes.MakeGenericMethod([ typeof(uint) ]));

                if (!hadTypeSize)
                {
                    il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, lclIdTypeSize);
                }
                else
                {
                    il.Emit(OpCodes.Pop);
                }
            }

            il.CommentIfDebug("lblNoKnownTypeId:");
            il.MarkLabel(lblNoKnownTypeId);

            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Ldloc, lclIdTypeSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclPreOverheadSize);
        }
        else
        {
            il.CommentIfDebug("++lclPreOverheadSize;");
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, lclPreOverheadSize);

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, lclIdTypeSize);
        }

        Label? cantPrimWrite = null;
        Label? didPrimWrite = null;

        if (canQuickSerialize)
        {
            cantPrimWrite = il.DefineLabel();
            didPrimWrite = il.DefineLabel();


            il.CommentIfDebug("if (!lclSerializer.CanFastReadPrimitives) goto cantPrimWrite;");
            if (lclPreCalcId == null)
            {
                il.Emit(OpCodes.Ldloc, lclSerializer);
                il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerCanFastReadPrimitives);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, lclPreCalcId);
            }

            il.Emit(OpCodes.Brfalse, cantPrimWrite.Value);

            int size = SerializerGenerator.GetPrimitiveTypeSize(idField.FieldType);
            if (checkSize)
            {
                il.Emit(OpCodes.Ldarg_S, sizeArg);
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Ldc_I4_S, (byte)size);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Call, Accessor.GetMethod(CheckLen)!);
            }

            il.CommentIfDebug($"unaligned {{ *({Accessor.Formatter.Format(idField.FieldType)}*)(bytes + lclSize) = idField; }}");
            il.Emit(OpCodes.Ldloc, lclByteBuffer);
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, idField);
            il.Emit(OpCodes.Unaligned, (byte)1);
            SerializerGenerator.SetToRef(idField.FieldType, il);

            if (lclValueSize != null)
            {
                il.Emit(OpCodes.Ldc_I4_S, (byte)size);
                il.Emit(OpCodes.Stloc, lclValueSize);
            }

            il.CommentIfDebug("goto didPrimWrite;");
            il.Emit(OpCodes.Br, didPrimWrite.Value);
        }

        if (cantPrimWrite.HasValue)
            il.MarkLabel(cantPrimWrite.Value);

        if (passByRef)
            il.CommentIfDebug("_ = lclSerializer.WriteObject(__makeref(idField), lclByteBuffer + lclPreOverheadSize, (uint)(lclOverheadSize - lclPreOverheadSize));");
        else
            il.CommentIfDebug($"_ = lclSerializer.WriteObject<{Accessor.Formatter.Format(idField.FieldType)}>(idField, lclByteBuffer + lclPreOverheadSize, (uint)(lclOverheadSize - lclPreOverheadSize));");

        il.Emit(OpCodes.Ldloc, lclSerializer);

        if (passByRef)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, idField);
            il.Emit(OpCodes.Mkrefany, idField.FieldType);

            il.Emit(OpCodes.Ldloc, lclByteBuffer);
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Add);

            if (lclOverheadSize != null)
            {
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
                il.Emit(OpCodes.Sub);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_S, sizeArg);
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Sub);
            }

            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerWriteObjectByTRefBytes);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, idField);

            il.Emit(OpCodes.Ldloc, lclByteBuffer);
            il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
            il.Emit(OpCodes.Add);

            if (lclOverheadSize != null)
            {
                il.Emit(OpCodes.Ldloc, lclOverheadSize);
                il.Emit(OpCodes.Ldloc, lclPreOverheadSize);
                il.Emit(OpCodes.Sub);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_S, sizeArg);
                il.Emit(OpCodes.Ldloc, lclIdTypeSize);
                il.Emit(OpCodes.Sub);
            }

            il.Emit(OpCodes.Callvirt, CommonReflectionCache.RpcSerializerWriteObjectByValBytes.MakeGenericMethod(idField.FieldType));
        }

        if (lclValueSize != null)
        {
            il.Emit(OpCodes.Stloc, lclValueSize);
        }
        else
        {
            il.Emit(OpCodes.Pop);
        }

        if (didPrimWrite.HasValue)
            il.MarkLabel(didPrimWrite.Value);

        if (lclValueSize == null)
            return;

        il.Emit(OpCodes.Ldloc, lclValueSize);
        il.Emit(OpCodes.Ldloc, lclIdTypeSize);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc, lclValueSize);
    }
    internal RpcInvokeHandlerStream GetInvokeStreamMethod(MethodInfo method)
    {
        if (_invokeMethodsStream.TryGetValue(method.MethodHandle, out Delegate? d))
            return (RpcInvokeHandlerStream)d;

        RpcInvokeHandlerStream entry = (RpcInvokeHandlerStream)_invokeMethodsStream.GetOrAdd(method.MethodHandle, GetOrAddInvokeMethodStream);
        return entry;
    }
    internal RpcInvokeHandlerBytes GetInvokeBytesMethod(MethodInfo method)
    {
        if (_invokeMethodsBytes.TryGetValue(method.MethodHandle, out Delegate? d))
            return (RpcInvokeHandlerBytes)d;

        RpcInvokeHandlerBytes entry = (RpcInvokeHandlerBytes)_invokeMethodsBytes.GetOrAdd(method.MethodHandle, GetOrAddInvokeMethodBytes);
        return entry;
    }
    internal RpcInvokeHandlerStream GetInvokeRawStreamMethod(MethodInfo method)
    {
        if (_invokeMethodsStream.TryGetValue(method.MethodHandle, out Delegate? d))
            return (RpcInvokeHandlerStream)d;

        RpcInvokeHandlerStream entry = (RpcInvokeHandlerStream)_invokeMethodsStream.GetOrAdd(method.MethodHandle, GetOrAddInvokeMethodRawStream);
        return entry;
    }
    internal RpcInvokeHandlerRawBytes GetInvokeRawBytesMethod(MethodInfo method)
    {
        if (_invokeMethodsBytes.TryGetValue(method.MethodHandle, out Delegate? d))
            return (RpcInvokeHandlerRawBytes)d;

        RpcInvokeHandlerRawBytes entry = (RpcInvokeHandlerRawBytes)_invokeMethodsBytes.GetOrAdd(method.MethodHandle, GetOrAddInvokeMethodRawBytes);
        return entry;
    }

    private RpcInvokeHandlerStream GetOrAddInvokeMethodStream(RuntimeMethodHandle handle)
    {
        MethodInfo method = (MethodInfo?)MethodBase.GetMethodFromHandle(handle)!;

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);

        DynamicMethod streamDynMethod = new DynamicMethod("Invoke<" + method.Name + ">", attr, conv, typeof(void), RpcInvokeHandlerStreamParams, method.DeclaringType ?? typeof(SerializerGenerator), true);

        streamDynMethod.DefineParameter(1, ParameterAttributes.None, "serviceProvider");
        streamDynMethod.DefineParameter(2, ParameterAttributes.None, "targetObject");
        streamDynMethod.DefineParameter(3, ParameterAttributes.None, "overhead");
        streamDynMethod.DefineParameter(4, ParameterAttributes.None, "router");
        streamDynMethod.DefineParameter(5, ParameterAttributes.None, "serializer");
        streamDynMethod.DefineParameter(6, ParameterAttributes.None, "stream");
        streamDynMethod.DefineParameter(7, ParameterAttributes.None, "token");

        SerializerGenerator.GenerateInvokeStream(method, streamDynMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint));
        return (RpcInvokeHandlerStream)streamDynMethod.CreateDelegate(typeof(RpcInvokeHandlerStream));
    }
    private RpcInvokeHandlerBytes GetOrAddInvokeMethodBytes(RuntimeMethodHandle handle)
    {
        MethodInfo method = (MethodInfo?)MethodBase.GetMethodFromHandle(handle)!;

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);

        DynamicMethod bytesDynMethod = new DynamicMethod("Invoke<" + method.Name + ">", attr, conv, typeof(void), RpcInvokeHandlerBytesParams, method.DeclaringType ?? typeof(SerializerGenerator), true);

        bytesDynMethod.DefineParameter(1, ParameterAttributes.None, "serviceProvider");
        bytesDynMethod.DefineParameter(2, ParameterAttributes.None, "targetObject");
        bytesDynMethod.DefineParameter(3, ParameterAttributes.None, "overhead");
        bytesDynMethod.DefineParameter(4, ParameterAttributes.None, "router");
        bytesDynMethod.DefineParameter(5, ParameterAttributes.None, "serializer");
        bytesDynMethod.DefineParameter(6, ParameterAttributes.None, "bytes");
        bytesDynMethod.DefineParameter(7, ParameterAttributes.None, "maxSize");
        bytesDynMethod.DefineParameter(8, ParameterAttributes.None, "token");

        SerializerGenerator.GenerateInvokeBytes(method, bytesDynMethod, bytesDynMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint));
        return (RpcInvokeHandlerBytes)bytesDynMethod.CreateDelegate(typeof(RpcInvokeHandlerBytes));
    }
    private RpcInvokeHandlerStream GetOrAddInvokeMethodRawStream(RuntimeMethodHandle handle)
    {
        MethodInfo method = (MethodInfo?)MethodBase.GetMethodFromHandle(handle)!;

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);

        DynamicMethod rawStreamDynMethod = new DynamicMethod("Invoke<" + method.Name + ">_Raw", attr, conv, typeof(void), RpcInvokeHandlerStreamParams, method.DeclaringType ?? typeof(SerializerGenerator), true);

        rawStreamDynMethod.DefineParameter(1, ParameterAttributes.None, "serviceProvider");
        rawStreamDynMethod.DefineParameter(2, ParameterAttributes.None, "targetObject");
        rawStreamDynMethod.DefineParameter(3, ParameterAttributes.None, "overhead");
        rawStreamDynMethod.DefineParameter(4, ParameterAttributes.None, "router");
        rawStreamDynMethod.DefineParameter(5, ParameterAttributes.None, "serializer");
        rawStreamDynMethod.DefineParameter(6, ParameterAttributes.None, "stream");
        rawStreamDynMethod.DefineParameter(7, ParameterAttributes.None, "token");

        SerializerGenerator.GenerateRawInvokeStream(method, rawStreamDynMethod, rawStreamDynMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint));
        return (RpcInvokeHandlerStream)rawStreamDynMethod.CreateDelegate(typeof(RpcInvokeHandlerStream));
    }
    private RpcInvokeHandlerRawBytes GetOrAddInvokeMethodRawBytes(RuntimeMethodHandle handle)
    {
        MethodInfo method = (MethodInfo?)MethodBase.GetMethodFromHandle(handle)!;

        Accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);

        DynamicMethod rawBytesDynMethod = new DynamicMethod("Invoke<" + method.Name + ">_Raw", attr, conv, typeof(void), RpcInvokeHandlerRawBytesParams, method.DeclaringType ?? typeof(SerializerGenerator), true);

        rawBytesDynMethod.DefineParameter(1, ParameterAttributes.None, "serviceProvider");
        rawBytesDynMethod.DefineParameter(2, ParameterAttributes.None, "targetObject");
        rawBytesDynMethod.DefineParameter(3, ParameterAttributes.None, "overhead");
        rawBytesDynMethod.DefineParameter(4, ParameterAttributes.None, "router");
        rawBytesDynMethod.DefineParameter(5, ParameterAttributes.None, "serializer");
        rawBytesDynMethod.DefineParameter(6, ParameterAttributes.None, "rawDat");
        rawBytesDynMethod.DefineParameter(7, ParameterAttributes.None, "canTakeOwnership");
        rawBytesDynMethod.DefineParameter(8, ParameterAttributes.None, "token");

        SerializerGenerator.GenerateRawInvokeBytes(method, rawBytesDynMethod, rawBytesDynMethod.AsEmitter(debuggable: DebugPrint, addBreakpoints: BreakpointPrint));
        return (RpcInvokeHandlerRawBytes)rawBytesDynMethod.CreateDelegate(typeof(RpcInvokeHandlerRawBytes));
    }

    internal static readonly Type[] RpcInvokeHandlerBytesParams =
    [
        typeof(object), typeof(object), typeof(RpcOverhead), typeof(IRpcRouter), typeof(IRpcSerializer), typeof(byte*), typeof(uint), typeof(CancellationToken)
    ];

    internal static readonly Type[] RpcInvokeHandlerStreamParams =
    [
        typeof(object), typeof(object), typeof(RpcOverhead), typeof(IRpcRouter), typeof(IRpcSerializer), typeof(Stream), typeof(CancellationToken)
    ];

    internal static readonly Type[] RpcInvokeHandlerRawBytesParams =
    [
        typeof(object), typeof(object), typeof(RpcOverhead), typeof(IRpcRouter), typeof(IRpcSerializer), typeof(ReadOnlyMemory<byte>), typeof(bool), typeof(CancellationToken)
    ];

    /// <summary>
    /// Used by source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe delegate void RpcInvokeHandlerBytes(
        object? serviceProvider,
        object? targetObject,
        RpcOverhead overhead,
        IRpcRouter router,
        IRpcSerializer serializer,
        byte* bytes,
        uint maxSize,
        CancellationToken token
    );

    /// <summary>
    /// Used by source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void RpcInvokeHandlerStream(
        object? serviceProvider,
        object? targetObject,
        RpcOverhead overhead,
        IRpcRouter router,
        IRpcSerializer serializer,
        Stream stream,
        CancellationToken token
    );

    /// <summary>
    /// Used by source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void RpcInvokeHandlerRawBytes(
        object? serviceProvider,
        object? targetObject,
        RpcOverhead overhead,
        IRpcRouter router,
        IRpcSerializer serializer,
        ReadOnlyMemory<byte> rawData,
        bool canTakeOwnership,
        CancellationToken token
    );

    internal struct ProxyTypeInfo
    {
        public Type Type;
        public bool IsGenerated;
        public InstanceSetter<object, IRpcRouter>? SetUnityRouterField;
    }

    /// <summary>
    /// Interal API used for specifying the name of the caller info field on an overrided call method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CallerInfoFieldNameAttribute(string fieldName) : Attribute
    {
        public string FieldName { get; } = fieldName;
    }
}