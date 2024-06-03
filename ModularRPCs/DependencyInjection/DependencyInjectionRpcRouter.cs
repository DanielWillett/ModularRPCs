using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Create an RPC router with one service provider.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"><see cref="IRpcSerializer"/> and/or <see cref="IRpcConnectionLifetime"/> are not available from the service provider.</exception>
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider)
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
            ))
        )
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Create an RPC router with one service provider.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider, IRpcSerializer serializer, IRpcConnectionLifetime lifetime)
        : base(serializer, lifetime)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Create an RPC router with multiple service providers. The sooner a provider is in the enumerable, the higher priority it is.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public DependencyInjectionRpcRouter(IEnumerable<IServiceProvider> serviceProviders, IRpcSerializer serializer, IRpcConnectionLifetime lifetime)
        : base(serializer, lifetime)
    {
        ServiceProviders = serviceProviders?.ToArray() ?? throw new ArgumentNullException(nameof(serviceProviders));
    }
    protected override IRpcInvocationPoint CreateEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[]? args, bool argsAreBindOnly, int signatureHash)
    {
        return ServiceProviders != null
            ? new DependencyInjectionRpcEndpoint(ServiceProviders, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, signatureHash)
            : new DependencyInjectionRpcEndpoint(ServiceProvider!, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, signatureHash);
    }
}