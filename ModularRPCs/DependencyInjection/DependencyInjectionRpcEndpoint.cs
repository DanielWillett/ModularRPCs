using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using DanielWillett.ModularRpcs.Routing;

namespace DanielWillett.ModularRpcs.DependencyInjection;

/// <summary>
/// An <see cref="RpcEndpoint"/> supporting a service provider, and fetching objects from them.
/// </summary>
public class DependencyInjectionRpcEndpoint : RpcEndpoint
{
    public IServiceProvider ServiceProvider { get; }

    protected DependencyInjectionRpcEndpoint(IRpcSerializer serializer, DependencyInjectionRpcEndpoint other, object? identifier)
        : base(serializer, other, identifier)
    {
        ServiceProvider = other.ServiceProvider;
    }
    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, IRpcSerializer serializer, MethodInfo method, object? identifier)
        : base(serializer, method, identifier)
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

    private protected override unsafe object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerBytes handlerBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, byte* bytes, uint maxSize, CancellationToken token)
    {
        return handlerBytes(ServiceProvider, targetObject, overhead, router, serializer, bytes, maxSize, token);
    }
    private protected override object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        return handlerStream(ServiceProvider, targetObject, overhead, router, serializer, stream, token);
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

    public override IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier)
    {
        return new DependencyInjectionRpcEndpoint(serializer, this, identifier);
    }
}
