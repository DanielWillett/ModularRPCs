using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Protocol;

namespace DanielWillett.ModularRpcs.Routing;
public class DefaultRpcRouter : IRpcRouter
{
    private object _sync = new object();
    /// <summary>
    /// A dictionary of unique IDs to invocation points.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, IRpcInvocationPoint> CachedDescriptors = new ConcurrentDictionary<uint, IRpcInvocationPoint>();

    /// <summary>
    /// The next Id to be used, actually a <see cref="uint"/> but stored as <see cref="int"/> to be used with <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    protected int NextId;
    public IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId)
    {
        return CachedDescriptors.TryGetValue(endpointSharedId, out IRpcInvocationPoint? endpoint) ? endpoint : null!;
    }
    public virtual IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, int byteSize)
    {
        return knownRpcShortcutId == 0u
            ? ValueFactory(0u)
            : CachedDescriptors.GetOrAdd(knownRpcShortcutId, ValueFactory);

        IRpcInvocationPoint ValueFactory(uint key)
        {
            RpcEndpoint endPoint = new RpcEndpoint(key, typeName, methodName, args, null, null);
            return endPoint;
        }
    }
    public ValueTask HandleReceivedData(IModularRpcLocalConnection connection, Stream streamData, CancellationToken token = default)
    {

        return default;
    }
    public unsafe ValueTask HandleReceivedData(IModularRpcLocalConnection connection, ReadOnlySpan<byte> byteData, CancellationToken token = default)
    {
        RpcOverhead overhead;
        fixed (byte* ptr = byteData)
        {
            overhead = RpcOverhead.ReadFromBytes(connection, ptr, (uint)byteData.Length);
        }
        
        overhead.CheckSizeHashValid();
        return default;
    }
}