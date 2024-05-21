using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DanielWillett.ModularRpcs.DependencyInjection;
public class DependencyInjectionRpcRouter : DefaultRpcRouter
{
    public IServiceProvider ServiceProvider { get; }
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<IRpcSerializer>(), serviceProvider.GetRequiredService<IRpcConnectionLifetime>())
    {
        ServiceProvider = serviceProvider;
    }
    protected override IRpcInvocationPoint CreateEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[]? args, bool argsAreBindOnly, int signatureHash)
    {
        return new DependencyInjectionRpcEndpoint(ServiceProvider, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, signatureHash);
    }
}