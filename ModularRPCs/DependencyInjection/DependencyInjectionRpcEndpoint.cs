using System;
using System.Reflection;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace DanielWillett.ModularRpcs.DependencyInjection;

/// <summary>
/// An <see cref="RpcEndpoint"/> supporting a service provider, and fetching objects from them.
/// </summary>
public class DependencyInjectionRpcEndpoint : RpcEndpoint
{
    public IServiceProvider ServiceProvider { get; }

    protected DependencyInjectionRpcEndpoint(IRpcRouter router, DependencyInjectionRpcEndpoint other, object? identifier)
        : base(router, other, identifier)
    {
        ServiceProvider = other.ServiceProvider;
    }
    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, IRpcRouter router, MethodInfo method, object? identifier)
        : base(router, method, identifier)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, uint knownId, string declaringTypeName, string methodName, string[]? argumentTypeNames, bool isStatic, Assembly? expectedAssembly = null, Type? expectedType = null)
        : base(knownId, declaringTypeName, methodName, argumentTypeNames, isStatic, expectedAssembly, expectedType)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, string declaringTypeName, string methodName, string[]? argumentTypeNames, bool isStatic, Assembly? expectedAssembly = null, Type? expectedType = null)
        : base(declaringTypeName, methodName, argumentTypeNames, isStatic, expectedAssembly, expectedType)
    {
        ServiceProvider = serviceProvider;
    }

    protected override object? GetTargetObject()
    {
        if (IsStatic)
            return null;

        if (Identifier != null)
            return base.GetTargetObject();

        if (DeclaringType == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierDeclaringTypeNotFound) { ErrorCode = 4 };

        return ServiceProvider.GetRequiredService(DeclaringType);
    }

    public override IRpcInvocationPoint CloneWithIdentifier(IRpcRouter router, object? identifier)
    {
        return new DependencyInjectionRpcEndpoint(router, this, identifier);
    }
}
