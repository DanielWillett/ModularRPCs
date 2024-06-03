using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DanielWillett.ModularRpcs.DependencyInjection;

/// <summary>
/// An <see cref="RpcEndpoint"/> supporting a service provider, and fetching objects from them.
/// </summary>
public class DependencyInjectionRpcEndpoint : RpcEndpoint
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

    protected DependencyInjectionRpcEndpoint(IRpcSerializer serializer, DependencyInjectionRpcEndpoint other, object? identifier)
        : base(serializer, other, identifier)
    {
        ServiceProvider = other.ServiceProvider;
        ServiceProviders = other.ServiceProviders;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, uint knownId, string declaringTypeName, string methodName, string[]? parameterTypeNames, bool argsAreBindOnly, int signatureHash)
        : base(knownId, declaringTypeName, methodName, parameterTypeNames, argsAreBindOnly, signatureHash)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, string declaringTypeName, string methodName, string[]? argumentTypeNames, bool argsAreBindOnly, int signatureHash)
        : base(declaringTypeName, methodName, argumentTypeNames, argsAreBindOnly, signatureHash)
    {
        ServiceProvider = serviceProvider;
    }
    
    internal DependencyInjectionRpcEndpoint(IEnumerable<IServiceProvider> serviceProviders, uint knownId, string declaringTypeName, string methodName, string[]? parameterTypeNames, bool argsAreBindOnly, int signatureHash)
        : base(knownId, declaringTypeName, methodName, parameterTypeNames, argsAreBindOnly, signatureHash)
    {
        ServiceProviders = serviceProviders;
    }

    internal DependencyInjectionRpcEndpoint(IEnumerable<IServiceProvider> serviceProviders, string declaringTypeName, string methodName, string[]? argumentTypeNames, bool argsAreBindOnly, int signatureHash)
        : base(declaringTypeName, methodName, argumentTypeNames, argsAreBindOnly, signatureHash)
    {
        ServiceProviders = serviceProviders;
    }

    private protected override unsafe object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerBytes handlerBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, byte* bytes, uint maxSize, CancellationToken token)
    {
        return handlerBytes((object?)ServiceProviders ?? ServiceProvider, targetObject, overhead, router, serializer, bytes, maxSize, token);
    }

    private protected override object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        return handlerStream((object?)ServiceProviders ?? ServiceProvider, targetObject, overhead, router, serializer, stream, token);
    }

    protected override object? GetTargetObject()
    {
        if (IsStatic)
            return null;

        if (Identifier != null)
            return base.GetTargetObject();

        if (DeclaringType == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierDeclaringTypeNotFound) { ErrorCode = 4 };

        if (ServiceProviders == null)
            return ServiceProvider!.GetService(DeclaringType);

        foreach (IServiceProvider provider in ServiceProviders)
        {
            object service = provider.GetService(DeclaringType);
            if (service != null)
                return service;
        }

        return null;
    }

    public override IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier)
    {
        return new DependencyInjectionRpcEndpoint(serializer, this, identifier);
    }
}
