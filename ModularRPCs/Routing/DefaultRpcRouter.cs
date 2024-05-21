using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.SpeedBytes.Formatting;

namespace DanielWillett.ModularRpcs.Routing;
public class DefaultRpcRouter : IRpcRouter, IDisposable
{
    private readonly IRpcSerializer _defaultSerializer;
    private readonly IRpcConnectionLifetime _connectionLifetime;
    private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
    private long _lastMsgId;

    /// <summary>
    /// Manages which connections are available to send to and receive from.
    /// </summary>
    public IRpcConnectionLifetime ConnectionLifetime => _connectionLifetime;

    /// <summary>
    /// The <see cref="IRpcSerializer"/> that's used by default to read and write messages.
    /// </summary>
    public IRpcSerializer Serializer => _defaultSerializer;
    public DefaultRpcRouter(IRpcSerializer defaultSerializer, IRpcConnectionLifetime lifetime)
    {
        _defaultSerializer = defaultSerializer;
        _connectionLifetime = lifetime;
    }
    private ulong GetNewMessageId()
    {
        return unchecked( (ulong)Interlocked.Increment(ref _lastMsgId) );
    }
    public int GetOverheadSize(RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo)
    {
        int size = 19;
        if (callMethodInfo.KnownId != 0)
            return size + 4;

        return size + callMethodInfo.Endpoint.GetEndpoint().Size;
    }
    protected virtual unsafe int WriteEndpoint(RuntimeMethodHandle sourceMethodHandle, IRpcSerializer serializer, ref RpcCallMethodInfo callMethodInfo, RpcEndpoint endpoint, byte* bytes, int byteCt)
    {
        if (callMethodInfo.KnownId != 0)
            return 0;
        return endpoint.WriteToBytes(serializer, this, bytes, (uint)byteCt);
    }
    protected virtual unsafe int WriteOverhead(RuntimeMethodHandle sourceMethodHandle, IRpcSerializer serializer, ref RpcCallMethodInfo callMethodInfo, byte* bytes, int byteCt, uint dataCt, ulong messageId, byte subMsgId)
    {
        return RpcOverhead.WriteToBytes(serializer, this, sourceMethodHandle, ref callMethodInfo, bytes, byteCt, dataCt, messageId, subMsgId);
    }
    public unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, byte* bytes, int byteCt, uint dataCt, ref RpcCallMethodInfo callMethodInfo)
    {
        ulong messageId = GetNewMessageId();
        int ovhSize = (int)((uint)byteCt - dataCt);
        const byte subMsgId = 0;
        int ovhEnd = WriteOverhead(sourceMethodHandle, serializer, ref callMethodInfo, bytes, ovhSize, dataCt, messageId, subMsgId);
        WriteEndpoint(sourceMethodHandle, serializer, ref callMethodInfo, callMethodInfo.Endpoint.GetEndpoint(), bytes + ovhEnd, ovhSize - ovhEnd);

        Console.WriteLine(ByteFormatter.FormatBinary(new ReadOnlySpan<byte>(bytes, byteCt), ByteStringFormat.ColumnLabels));

        RpcTask? rpcTask = null;
        if (connections == null)
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            if (!_connectionLifetime.IsSingleConnection && !callMethodInfo.IsFireAndForget)
                throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));

            rpcTask = !callMethodInfo.IsFireAndForget
                ? CreateRpcTaskListener(sourceMethodHandle, messageId, subMsgId + 1) 
                : new RpcBroadcastTask(true) { CompleteCount = 1 };

            int ct = _connectionLifetime.ForEachRemoteConnection(connection =>
            {
                Interlocked.Increment(ref rpcTask.CompleteCount);
                if (rpcTask is RpcBroadcastTask bt)
                    ++bt.ConnectionCountIntl;
                
                try
                {
                    ValueTask vt = connection.SendDataAsync(_defaultSerializer, new ReadOnlySpan<byte>(bytes, byteCt), _cancelTokenSource.Token);
                    
                    if (!vt.IsCompleted)
                        Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask));
                }
                catch (Exception ex)
                {
                    HandleInvokeException(sourceMethodHandle, ex);
                    rpcTask.TriggerComplete(ex);
                }

                return true;
            });

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            if (ct == 0)
                throw new RpcNoConnectionsException(string.Format(Properties.Exceptions.RpcNoConnectionsExceptionConnectionLifetime, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));

            // maybe throw exceptions if all threw
            rpcTask.TriggerComplete(null);

            return rpcTask;
        }
        
        if (connections is IModularRpcRemoteConnection remote1)
        {
            rpcTask = !callMethodInfo.IsFireAndForget
                ? CreateRpcTaskListener(sourceMethodHandle, messageId, subMsgId + 1)
                : new RpcTask(true);

            try
            {
                ValueTask vt = remote1.SendDataAsync(_defaultSerializer, new ReadOnlySpan<byte>(bytes, byteCt), _cancelTokenSource.Token);

                if (!vt.IsCompleted)
                    Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask));
            }
            catch (Exception ex)
            {
                HandleInvokeException(sourceMethodHandle, ex);
                rpcTask.TriggerComplete(ex);
            }

            return rpcTask;
        }

        if (connections is not IEnumerable<IModularRpcRemoteConnection> remotes)
            throw new ArgumentException(Properties.Exceptions.InvokeRpcConnectionsInvalidType, nameof(connections));
        
        if (!callMethodInfo.IsFireAndForget)
            throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));

        foreach (IModularRpcRemoteConnection connection in remotes)
        {
            rpcTask ??= new RpcBroadcastTask(true) { CompleteCount = 1 };
            ++((RpcBroadcastTask)rpcTask).ConnectionCountIntl;
            Interlocked.Increment(ref rpcTask.CompleteCount);
            try
            {
                ValueTask vt = connection.SendDataAsync(_defaultSerializer, new ReadOnlySpan<byte>(bytes, byteCt), _cancelTokenSource.Token);

                if (!vt.IsCompleted)
                    Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask));
            }
            catch (Exception ex)
            {
                HandleInvokeException(sourceMethodHandle, ex);
                rpcTask.TriggerComplete(ex);
            }
        }

        if (rpcTask == null)
            return RpcTask.CompletedTask;

        // maybe throw exceptions if all threw
        rpcTask.TriggerComplete(null);
        return rpcTask;
    }
    private Func<Task> WrapInvokeTaskInTryBlock(RuntimeMethodHandle sourceMethodHandle, ValueTask vt, RpcTask? rpcTask)
    {
        return async () =>
        {
            try
            {
                await vt;
                if (rpcTask is { IsFireAndForget: true })
                {
                    rpcTask.TriggerComplete(null);
                }
            }
            catch (Exception ex)
            {
                HandleInvokeException(sourceMethodHandle, ex);
                rpcTask?.TriggerComplete(ex);
            }
        };
    }
    private RpcTask CreateRpcTaskListener(RuntimeMethodHandle sourceMethodHandle, ulong messageId, byte subMsgId)
    {
        MethodInfo method = (MethodBase.GetMethodFromHandle(sourceMethodHandle) as MethodInfo)!;

        RpcTask rpcTask;
        if (method.ReturnType == typeof(void) || method.ReturnType == typeof(RpcTask))
            rpcTask = new RpcTask(false);
        else
            rpcTask = (RpcTask)Activator.CreateInstance(method.ReturnType);

        StartListening(rpcTask, messageId, subMsgId);
        return rpcTask;
    }
    private void StartListening(RpcTask rpcTask, ulong messageId, byte subMsgId)
    {

    }
    private void HandleInvokeException(RuntimeMethodHandle sourceMethodHandle, Exception ex)
    {
        Console.WriteLine("Error invoking " + Accessor.Formatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)));
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
    protected virtual IRpcInvocationPoint CreateEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[]? args, bool argsAreBindOnly, int signatureHash)
    {
        return new RpcEndpoint(knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, signatureHash);
    }
    public IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, int signatureHash, int byteSize, object? identifier)
        => ResolveEndpoint(_defaultSerializer, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, signatureHash, byteSize, identifier);
    public virtual IRpcInvocationPoint ResolveEndpoint(IRpcSerializer serializer, uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, int signatureHash, int byteSize, object? identifier)
    {
        IRpcInvocationPoint cachedEndpoint = knownRpcShortcutId == 0u
            ? CreateEndpoint(0u, typeName, methodName, args, argsAreBindOnly, signatureHash)
            : CachedDescriptors.GetOrAdd(knownRpcShortcutId, key => CreateEndpoint(key, typeName, methodName, args, argsAreBindOnly, signatureHash));

        return ReferenceEquals(cachedEndpoint.Identifier, identifier)
            ? cachedEndpoint
            : cachedEndpoint.CloneWithIdentifier(serializer, identifier);
    }
    public void GetDefaultProxyContext(Type proxyType, out ProxyContext context)
    {
        context = default;
        context.DefaultSerializer = _defaultSerializer;
        context.Router = this;
    }

    public void Dispose()
    {
        try
        {
            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
        }
        catch (ObjectDisposedException) { }
    }
}