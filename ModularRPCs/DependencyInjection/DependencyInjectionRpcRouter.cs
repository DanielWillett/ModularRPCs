using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DanielWillett.ModularRpcs.DependencyInjection;
public class DependencyInjectionRpcRouter : DefaultRpcRouter
{
    public IServiceProvider ServiceProvider { get; }
    public DependencyInjectionRpcRouter(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<IRpcSerializer>())
    {
        ServiceProvider = serviceProvider;
    }
    protected override IRpcInvocationPoint CreateEndpoint(uint key, string typeName, string methodName, string[]? args, bool isStatic)
    {
        return new DependencyInjectionRpcEndpoint(ServiceProvider, key, typeName, methodName, args, isStatic, null, null);
    }
}