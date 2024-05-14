using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DanielWillett.ModularRpcs.Routing;
public class DefaultRpcRouter : IRpcRouter
{
    private readonly IRpcSerializer _defaultSerializer;
    public DefaultRpcRouter(IRpcSerializer defaultSerializer)
    {
        _defaultSerializer = defaultSerializer;
    }

    /// <summary>
    /// A dictionary of unique IDs to invocation points.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, IRpcInvocationPoint> CachedDescriptors = new ConcurrentDictionary<uint, IRpcInvocationPoint>();

    /// <summary>
    /// The next Id to be used, actually a <see cref="uint"/> but stored as <see cref="int"/> to be used with <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    protected int NextId;
    public virtual IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId)
    {
        // ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
        return CachedDescriptors.TryGetValue(endpointSharedId, out IRpcInvocationPoint? endpoint) ? endpoint : null;
    }

    public uint AddRpcEndpoint(IRpcInvocationPoint endPoint)
    {
        // keep trying to add if the id is taken, could've been added by a third party
        while (true)
        {
            uint id = unchecked((uint)Interlocked.Increment(ref NextId));
            if (!CachedDescriptors.TryAdd(id, endPoint))
            {
                if (NextId == 0)  // NextId rolled over. Realistically memory will run out before this gets called, but better to prevent an infinite loop.
                    throw new InvalidOperationException($"There are too many saved endpoints {CachedDescriptors.Count}.");
            }
            else
            {
                endPoint.EndpointId = id;
                return id;
            }
        }
    }
    protected virtual IRpcInvocationPoint CreateEndpoint(uint key, string typeName, string methodName, string[]? args, int signatureHash, bool isStatic)
    {
        return new RpcEndpoint(key, typeName, methodName, args, signatureHash, isStatic, null, null);
    }
    public IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, int signatureHash, bool isStatic, string[] args, int byteSize, object? identifier)
        => ResolveEndpoint(_defaultSerializer, knownRpcShortcutId, typeName, methodName, signatureHash, isStatic, args, byteSize, identifier);
    public virtual IRpcInvocationPoint ResolveEndpoint(IRpcSerializer serializer, uint knownRpcShortcutId, string typeName, string methodName, int signatureHash, bool isStatic, string[] args, int byteSize, object? identifier)
    {
        IRpcInvocationPoint cachedEndpoint = knownRpcShortcutId == 0u
            ? ValueFactory(0u)
            : CachedDescriptors.GetOrAdd(knownRpcShortcutId, ValueFactory);

        return ReferenceEquals(cachedEndpoint.Identifier, identifier)
            ? cachedEndpoint
            : cachedEndpoint.CloneWithIdentifier(serializer, identifier);

        IRpcInvocationPoint ValueFactory(uint key)
        {
            IRpcInvocationPoint endPoint = CreateEndpoint(key, typeName, methodName, args, signatureHash, isStatic);
            return endPoint;
        }
    }
    public void GetDefaultProxyContext(Type proxyType, out ProxyContext context)
    {
        context = default;
        context.DefaultSerializer = _defaultSerializer;
        context.Router = this;
    }
}