using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.DependencyInjection;

public class DependencyInjectionRpcRouter : DefaultRpcRouter
{
    /// <summary>
    /// A single service provider used to pull instances from.
    /// </summary>
    /// <remarks>One of <see cref="ServiceProvider"/> and <see cref="ServiceProviders"/> will always have a value, never both.</remarks>
    public IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Multiple service providers used to pull instances from.
    /// </summary>
    /// <remarks>One of <see cref="ServiceProviders"/> and <see cref="ServiceProvider"/> will always have a value, never both.</remarks>
    public IEnumerable<IServiceProvider>? ServiceProviders { get; }

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> with one service provider that looks for <see cref="RpcClassAttribute"/>'s in all assemblies referenced by the calling one.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"><see cref="IRpcSerializer"/> and/or <see cref="IRpcConnectionLifetime"/> are not available from the service provider.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider)
        : this(serviceProvider, Assembly.GetCallingAssembly()) { }
    protected internal DependencyInjectionRpcRouter(IServiceProvider serviceProvider, Assembly callingAssembly)
        : base(
            (IRpcSerializer?)serviceProvider.GetService(typeof(IRpcSerializer))
               ?? throw new InvalidOperationException(string.Format(
                    Properties.Exceptions.ServiceNotFound,
                    Accessor.ExceptionFormatter.Format(typeof(IRpcSerializer))
            )),
            (IRpcConnectionLifetime?)serviceProvider.GetService<IRpcConnectionLifetime>()
            ?? throw new InvalidOperationException(string.Format(
                Properties.Exceptions.ServiceNotFound,
                Accessor.ExceptionFormatter.Format(typeof(IRpcConnectionLifetime))
            )),
            (ProxyGenerator?)serviceProvider.GetService<ProxyGenerator>()
            ?? throw new InvalidOperationException(string.Format(
                Properties.Exceptions.ServiceNotFound,
                Accessor.ExceptionFormatter.Format(typeof(ProxyGenerator))
            )),
            callingAssembly
        )
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> with one service provider that looks for <see cref="RpcClassAttribute"/>'s in the given <paramref name="scannableAssemblies"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"><see cref="IRpcSerializer"/> and/or <see cref="IRpcConnectionLifetime"/> are not available from the service provider.</exception>
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider, IEnumerable<Assembly> scannableAssemblies)
        : base(
            (IRpcSerializer?)serviceProvider.GetService(typeof(IRpcSerializer))
               ?? throw new InvalidOperationException(string.Format(
                    Properties.Exceptions.ServiceNotFound,
                    Accessor.ExceptionFormatter.Format(typeof(IRpcSerializer))
            )),
            (IRpcConnectionLifetime?)serviceProvider.GetService<IRpcConnectionLifetime>()
            ?? throw new InvalidOperationException(string.Format(
                Properties.Exceptions.ServiceNotFound,
                Accessor.ExceptionFormatter.Format(typeof(IRpcConnectionLifetime))
            )),
            (ProxyGenerator?)serviceProvider.GetService<ProxyGenerator>()
            ?? throw new InvalidOperationException(string.Format(
                Properties.Exceptions.ServiceNotFound,
                Accessor.ExceptionFormatter.Format(typeof(ProxyGenerator))
            )),
            scannableAssemblies
        )
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> with one service provider that looks for <see cref="RpcClassAttribute"/>'s in all assemblies referenced by the calling one.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider, IRpcSerializer serializer, IRpcConnectionLifetime lifetime, ProxyGenerator proxyGenerator)
        : this(serviceProvider, serializer, lifetime, proxyGenerator, Assembly.GetCallingAssembly()) { }
    protected internal DependencyInjectionRpcRouter(IServiceProvider serviceProvider, IRpcSerializer serializer, IRpcConnectionLifetime lifetime, ProxyGenerator proxyGenerator, Assembly callingAssembly)
        : base(serializer, lifetime, proxyGenerator, callingAssembly)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> with one service provider that looks for <see cref="RpcClassAttribute"/>'s in the given <paramref name="scannableAssemblies"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider, IRpcSerializer serializer, IRpcConnectionLifetime lifetime, ProxyGenerator proxyGenerator, IEnumerable<Assembly> scannableAssemblies)
        : base(serializer, lifetime, proxyGenerator, scannableAssemblies)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Create an RPC router with multiple service providers that looks for <see cref="RpcClassAttribute"/>'s in all assemblies referenced by the calling one. The sooner a provider is in the enumerable, the higher priority it is.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public DependencyInjectionRpcRouter(IEnumerable<IServiceProvider> serviceProviders, IRpcSerializer serializer, IRpcConnectionLifetime lifetime, ProxyGenerator proxyGenerator)
        : this(serviceProviders, serializer, lifetime, proxyGenerator, Assembly.GetCallingAssembly()) { }

    protected internal DependencyInjectionRpcRouter(IEnumerable<IServiceProvider> serviceProviders, IRpcSerializer serializer, IRpcConnectionLifetime lifetime, ProxyGenerator proxyGenerator, Assembly callingAssembly)
        : base(serializer, lifetime, proxyGenerator, callingAssembly)
    {
        ServiceProviders = serviceProviders?.ToArray() ?? throw new ArgumentNullException(nameof(serviceProviders));
    }

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> with multiple service providers that looks for <see cref="RpcClassAttribute"/>'s in the given <paramref name="scannableAssemblies"/>. The sooner a provider is in the enumerable, the higher priority it is.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public DependencyInjectionRpcRouter(IEnumerable<IServiceProvider> serviceProviders, IRpcSerializer serializer, IRpcConnectionLifetime lifetime, ProxyGenerator proxyGenerator, IEnumerable<Assembly> scannableAssemblies)
        : base(serializer, lifetime, proxyGenerator, scannableAssemblies)
    {
        ServiceProviders = serviceProviders?.ToArray() ?? throw new ArgumentNullException(nameof(serviceProviders));
    }
    protected override IRpcInvocationPoint CreateEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[]? args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
    {
        return ServiceProviders != null
            ? new DependencyInjectionRpcEndpoint(ServiceProviders, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
            : new DependencyInjectionRpcEndpoint(ServiceProvider!, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation);
    }
}