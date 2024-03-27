using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Creates inherited proxy types for classes with virtual or abstract methods decorated with the <see cref="RpcSendAttribute"/> to provide implementations of them at runtime.
/// </summary>
public sealed class ProxyGenerator
{
    private readonly ConcurrentDictionary<Type, Type> _proxies = new ConcurrentDictionary<Type, Type>();
    internal object? Logger;

    /// <summary>
    /// The singleton instance of <see cref="ProxyGenerator"/>, which stores information about the assembly used to store dynamically generated types.
    /// </summary>
    public static ProxyGenerator Instance { get; } = new ProxyGenerator();
    internal AssemblyBuilder AssemblyBuilder { get; }
    internal ModuleBuilder ModuleBuilder { get; }

    /// <summary>
    /// The assembly name being used to store dynamically generated types and methods.
    /// </summary>
    public AssemblyName ProxyAssemblyName { get; }

    static ProxyGenerator() { }
    private ProxyGenerator()
    {
        Assembly thisAssembly = Assembly.GetExecutingAssembly();
        ProxyAssemblyName = new AssemblyName(thisAssembly.GetName().Name + ".Proxy");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(ProxyAssemblyName.Name);
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
            );

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
        TypeBuilder typeBuilder = ModuleBuilder.DefineType(type.Name + "<RPC_Proxy>",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, type);

        // create constructors for all base constructors
        foreach (ConstructorInfo baseCtor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (baseCtor.IsPrivate)
                continue;
            
            if (!VisibilityUtility.IsMethodOverridable(baseCtor, typeGivesInternalAccess))
            {
                string pList = $"({string.Join(", ", baseCtor.GetParameters().Select(x => x.ParameterType.Name))})";
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.ConstructorNotVisibileToOverridingClasses, pList, type.FullName));
                else
                    Console.WriteLine(Properties.Logging.ConstructorNotVisibileToOverridingClasses, pList, type.FullName);
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

            ILGenerator il = builder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);

            for (int i = 0; i < types.Length; ++i)
                EmitUtility.EmitArgument(il, i + 1, false);

            il.Emit(OpCodes.Call, baseCtor);
            il.Emit(OpCodes.Ret);
        }

        return typeBuilder;
    }
    private Type CreateProxyType(Type type)
    {
        if (type.IsValueType)
            throw new ArgumentException(Properties.Exceptions.TypeNotReferenceType, nameof(type));

        if (type.IsSealed)
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
                string mDesc = method.Name + $"({string.Join(", ", method.GetParameters().Select(x => x.ParameterType.Name))})";
                if (Logger != null)
                    LogWarning(string.Format(Properties.Logging.MethodNotVisibileToOverridingClasses, mDesc, type.FullName));
                else
                    Console.WriteLine(Properties.Logging.MethodNotVisibileToOverridingClasses, mDesc, type.FullName);
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
            generator.EmitWriteLine("Calling " + method.Name);

            MethodInfo getter = typeof(RpcTask).GetProperty(nameof(RpcTask.CompletedTask), BindingFlags.Public | BindingFlags.Static)!.GetMethod;
            generator.Emit(OpCodes.Call, getter);
            generator.Emit(OpCodes.Ret);
        }

        try
        {
            return builder.CreateType();
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
