using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, uint knownId, string declaringTypeName, string methodName, string[]? parameterTypeNames, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(knownId, declaringTypeName, methodName, parameterTypeNames, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, string declaringTypeName, string methodName, string[]? argumentTypeNames, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(declaringTypeName, methodName, argumentTypeNames, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, uint knownId, MethodInfo method, bool needsParameters, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(knownId, method, needsParameters, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IServiceProvider serviceProvider, MethodInfo method, bool needsParameters, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(method, needsParameters, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProvider = serviceProvider;
    }

    internal DependencyInjectionRpcEndpoint(IEnumerable<IServiceProvider> serviceProviders, uint knownId, string declaringTypeName, string methodName, string[]? parameterTypeNames, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(knownId, declaringTypeName, methodName, parameterTypeNames, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProviders = serviceProviders;
    }

    internal DependencyInjectionRpcEndpoint(IEnumerable<IServiceProvider> serviceProviders, string declaringTypeName, string methodName, string[]? argumentTypeNames, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(declaringTypeName, methodName, argumentTypeNames, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProviders = serviceProviders;
    }

    internal DependencyInjectionRpcEndpoint(IEnumerable<IServiceProvider> serviceProviders, uint knownId, MethodInfo method, bool needsParameters, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(knownId, method, needsParameters, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProviders = serviceProviders;
    }

    internal DependencyInjectionRpcEndpoint(IEnumerable<IServiceProvider> serviceProviders, MethodInfo method, bool needsParameters, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
        : base(method, needsParameters, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
    {
        ServiceProviders = serviceProviders;
    }

    private protected override unsafe void InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerBytes handlerBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, byte* bytes, uint maxSize, CancellationToken token)
    {
        handlerBytes((object?)ServiceProviders ?? ServiceProvider, targetObject, overhead, router, serializer, bytes, maxSize, token);
    }

    private protected override void InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        handlerStream((object?)ServiceProviders ?? ServiceProvider, targetObject, overhead, router, serializer, stream, token);
    }
    
    private protected override void InvokeRawInvokeMethod(ProxyGenerator.RpcInvokeHandlerRawBytes handlerRawBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        handlerRawBytes((object?)ServiceProviders ?? ServiceProvider, targetObject, overhead, router, serializer, rawData, canTakeOwnership, token);
    }

    private protected override void InvokeRawInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerRawStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        handlerRawStream((object?)ServiceProviders ?? ServiceProvider, targetObject, overhead, router, serializer, stream, token);
    }

    protected override object? GetTargetObject(MethodInfo? knownMethod)
    {
        Type? declType = knownMethod?.DeclaringType ?? DeclaringType;
        bool isStatic = knownMethod == null ? IsStatic : knownMethod.IsStatic;

        if (isStatic)
            return null;

        if (Identifier != null)
            return base.GetTargetObject(knownMethod);

        if (declType == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierDeclaringTypeNotFound) { ErrorCode = 4 };

        if (ServiceProviders == null && ServiceProvider != null)
        {
            return TypeUtility.GetService(ServiceProvider!, declType);
        }

        object? provider = (object?)ServiceProviders ?? ServiceProvider;

        return provider == null ? null : TypeUtility.GetServiceFromUnknownProviderType(provider, declType);
    }

    public override IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier)
    {
        return new DependencyInjectionRpcEndpoint(serializer, this, identifier);
    }
}
