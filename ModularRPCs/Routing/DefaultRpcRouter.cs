using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Data;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RpcEndpointTarget = DanielWillett.ModularRpcs.Reflection.RpcEndpointTarget;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace DanielWillett.ModularRpcs.Routing;
public class DefaultRpcRouter : IRpcRouter, IDisposable, IRefSafeLoggable
{
    private readonly IRpcSerializer _defaultSerializer;
    private readonly IRpcConnectionLifetime _connectionLifetime;
    private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
    private long _lastMsgId;
    private readonly ConcurrentDictionary<ulong, RpcTask> _listeningTasks = new ConcurrentDictionary<ulong, RpcTask>();
    private readonly ConcurrentDictionary<UniqueMessageKey, CancellationTokenSource> _pendingCancellableMessages = new ConcurrentDictionary<UniqueMessageKey, CancellationTokenSource>();
    private readonly Dictionary<string, IReadOnlyList<RpcEndpointTarget>> _broadcastListeners = new Dictionary<string, IReadOnlyList<RpcEndpointTarget>>(StringComparer.Ordinal);

    private readonly struct UniqueMessageKey : IEquatable<UniqueMessageKey>
    {
        public readonly ulong MessageId;
        public readonly IModularRpcRemoteConnection Sender;
        public UniqueMessageKey(ulong messageId, IModularRpcRemoteConnection sender)
        {
            MessageId = messageId;
            Sender = sender;
        }

        public bool Equals(UniqueMessageKey key) => key.MessageId == MessageId && Sender.Equals(key.Sender);
        public override bool Equals(object? obj) => obj is UniqueMessageKey key && key.MessageId == MessageId && Sender.Equals(key.Sender);
        public override int GetHashCode() => HashCode.Combine(MessageId, Sender);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<RpcEndpointTarget>> BroadcastTargets { get; }

    /// <summary>
    /// A dictionary of unique IDs to invocation points.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, IRpcInvocationPoint> CachedDescriptors = new ConcurrentDictionary<uint, IRpcInvocationPoint>();

    protected internal const byte OvhCodeIdCancel = 5;
    protected internal const byte OvhCodeIdVoidRtnSuccess = 4;
    protected internal const byte OvhCodeIdValueRtnSuccess = 3;
    protected internal const byte OvhCodeIdException = 2;
    private object? _logger;
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

    /// <summary>
    /// Manages which connections are available to send to and receive from.
    /// </summary>
    public IRpcConnectionLifetime ConnectionLifetime => _connectionLifetime;

    /// <summary>
    /// The <see cref="IRpcSerializer"/> that's used by default to read and write messages.
    /// </summary>
    public IRpcSerializer Serializer => _defaultSerializer;

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> that looks for <see cref="RpcClassAttribute"/>'s in the given <paramref name="scannableAssemblies"/>.
    /// </summary>
    public DefaultRpcRouter(IRpcSerializer defaultSerializer, IRpcConnectionLifetime lifetime, IEnumerable<Assembly> scannableAssemblies)
    {
        _defaultSerializer = defaultSerializer ?? throw new ArgumentNullException(nameof(defaultSerializer));
        _connectionLifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        BroadcastTargets = new ReadOnlyDictionary<string, IReadOnlyList<RpcEndpointTarget>>(_broadcastListeners);

        foreach (Assembly asm in scannableAssemblies ?? throw new ArgumentNullException(nameof(scannableAssemblies)))
            ScanAssemblyForRpcClasses(asm);
    }

    /// <summary>
    /// Create an <see cref="IRpcRouter"/> that looks for <see cref="RpcClassAttribute"/>'s in all referenced assemblies to the one calling this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public DefaultRpcRouter(IRpcSerializer defaultSerializer, IRpcConnectionLifetime lifetime) : this(defaultSerializer, lifetime, Assembly.GetCallingAssembly()) { }
    protected internal DefaultRpcRouter(IRpcSerializer defaultSerializer, IRpcConnectionLifetime lifetime, Assembly callingAssembly)
    {
        _defaultSerializer = defaultSerializer ?? throw new ArgumentNullException(nameof(defaultSerializer));
        _connectionLifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        BroadcastTargets = new ReadOnlyDictionary<string, IReadOnlyList<RpcEndpointTarget>>(_broadcastListeners);

        ScanAssemblyForRpcClasses(callingAssembly);
        foreach (AssemblyName asmName in callingAssembly.GetReferencedAssemblies())
        {
            try
            {
                ScanAssemblyForRpcClasses(Assembly.Load(asmName));
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }
        }
    }

    public void CleanupConnection(IModularRpcConnection connection)
    {
        if (connection is not IModularRpcRemoteConnection remote)
            return;

        foreach (KeyValuePair<UniqueMessageKey, CancellationTokenSource> cancellation in _pendingCancellableMessages.ToArray())
        {
            if (!cancellation.Key.Sender.Equals(remote) || !_pendingCancellableMessages.TryRemove(cancellation.Key, out CancellationTokenSource? value))
                continue;

            value.Cancel();
            value.Dispose();
        }
    }

    private void ScanAssemblyForRpcClasses(Assembly assembly)
    {
        List<RpcEndpointTarget> broadcastInfos = new List<RpcEndpointTarget>();
        foreach (Type type in Accessor.GetTypesSafe(assembly))
        {
            if (type.IsIgnored() || !type.IsDefinedSafe<RpcClassAttribute>())
                continue;

            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods)
            {
                RpcReceiveAttribute? receiveAttribute = method.GetAttributeSafe<RpcReceiveAttribute>();

                if (receiveAttribute == null || method.IsIgnored() || receiveAttribute.MethodName == null)
                    continue;

                RpcEndpointTarget info = RpcEndpointTarget.FromReceiveMethod(method);
                info.OwnerMethodInfo = method;
                broadcastInfos.Add(info);
            }
        }

        broadcastInfos.Sort((a, b) => string.Compare(a.DeclaringTypeName, b.DeclaringTypeName, CultureInfo.InvariantCulture, CompareOptions.StringSort));

        for (int i = 0; i < broadcastInfos.Count; ++i)
        {
            RpcEndpointTarget target = broadcastInfos[i];

            int nextInd = i + 1;
            while (nextInd < broadcastInfos.Count && string.Equals(target.DeclaringTypeName, broadcastInfos[nextInd].DeclaringTypeName))
                ++nextInd;

            int ct = nextInd - i;
            if (_broadcastListeners.TryGetValue(target.DeclaringTypeName, out IReadOnlyList<RpcEndpointTarget>? value))
            {
                RpcEndpointTarget[] oldArr = (RpcEndpointTarget[])value;
                RpcEndpointTarget[] newArr = new RpcEndpointTarget[oldArr.Length + ct];
                Array.Copy(oldArr, newArr, oldArr.Length);
                broadcastInfos.CopyTo(i, newArr, oldArr.Length, ct);
            }
            else
            {
                RpcEndpointTarget[] newArr = new RpcEndpointTarget[ct];
                value = newArr;
                broadcastInfos.CopyTo(i, newArr, 0, ct);
            }

            _broadcastListeners[target.DeclaringTypeName] = value;
            i = nextInd - 1;
        }
    }
    private ulong GetNewMessageId()
    {
        return unchecked( (ulong)Interlocked.Increment(ref _lastMsgId) );
    }

    /// <inheritdoc />
    public uint GetOverheadSize(RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo)
    {
        uint size = 20;
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
        int ct = RpcOverhead.WriteToBytes(serializer, this, sourceMethodHandle, ref callMethodInfo, bytes, byteCt, dataCt, messageId, subMsgId);
        return ct;
    }

    public virtual void InvokeCancellation(RpcTask task)
    {
        ulong messageId = task.MessageId;

        IModularRpcRemoteConnection? connection = task.ConnectionIntl;
        if (connection == null)
            return;

        ValueTask vt;
        try
        {
            vt = FollowupRpcCancel(messageId, checked( (byte)(task.SubMessageId + 1) ), connection, _defaultSerializer);

            if (vt.IsCompleted)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex,
                string.Format(Properties.Exceptions.RpcCancellationExceptionWithInvocationPointMessage,
                    Accessor.Formatter.Format(ex.GetType()),
                    task.Endpoint,
                    ex.Message)
            );

            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await vt;
            }
            catch (Exception ex)
            {
                this.LogError(ex,
                    string.Format(Properties.Exceptions.RpcCancellationExceptionWithInvocationPointMessage,
                        Accessor.Formatter.Format(ex.GetType()),
                        task.Endpoint,
                        ex.Message)
                );
            }
        });
    }

    protected virtual async ValueTask InvokeInvocationPoint(IRpcInvocationPoint rpc, RpcOverhead overhead, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        CombinedTokenSources src = default;
        bool didAddTokenSrc = false;
        UniqueMessageKey key = default;
        try
        {
            if (rpc.SupportsRemoteCancellation && (overhead.Flags & RpcFlags.FireAndForget) == 0 && overhead.SendingConnection != null)
            {
                CancellationTokenSource newTokenSource = new CancellationTokenSource();
                CancellationTokenSource existingSource = _pendingCancellableMessages.GetOrAdd(key = new UniqueMessageKey(overhead.MessageId, overhead.SendingConnection), newTokenSource);
                if (!ReferenceEquals(existingSource, newTokenSource))
                {
                    existingSource.Dispose();
                }

                src = token.CombineTokensIfNeeded(newTokenSource.Token);
                didAddTokenSrc = true;
            }
            
            RpcInvocationResult result = rpc.Invoke(overhead, this, serializer, stream, token);
            ValueTask vt = result.Task;
            await vt;

            if ((overhead.Flags & RpcFlags.FireAndForget) != 0 || overhead.SendingConnection == null)
                return;

            if (didAddTokenSrc && _pendingCancellableMessages.TryRemove(key, out CancellationTokenSource? cancellable))
            {
                cancellable.Dispose();
                src.Dispose();
                didAddTokenSrc = false;
            }

            try
            {
                await HandleValueTaskReply(result, overhead);
            }
            catch (Exception ex2)
            {
                HandleInvokeException(overhead, ex2);
            }
        }
        catch (Exception ex)
        {
            if (overhead.SendingConnection != null)
            {
                try
                {
                    await ReplyRpcException(overhead.MessageId, checked( (byte)(overhead.SubMessageId + 1) ), overhead.SendingConnection, ex, serializer);
                }
                catch (Exception ex2)
                {
                    HandleInvokeException(overhead, ex2);
                }
            }

            HandleInvokeException(overhead, ex);
        }
        finally
        {
            if (didAddTokenSrc)
            {
                if (_pendingCancellableMessages.TryRemove(key, out CancellationTokenSource? cancellable))
                {
                    cancellable.Dispose();
                }

                src.Dispose();
            }
        }
    }
    protected virtual ValueTask InvokeInvocationPoint(IRpcInvocationPoint rpc, RpcOverhead overhead, IRpcSerializer serializer, ReadOnlyMemory<byte> bytes, bool canTakeOwnership, CancellationToken token = default)
    {
        CombinedTokenSources src = default;
        bool didAddTokenSrc = false;
        UniqueMessageKey key = default;
        try
        {
            if (rpc.SupportsRemoteCancellation && (overhead.Flags & RpcFlags.FireAndForget) == 0 && overhead.SendingConnection != null)
            {
                CancellationTokenSource newTokenSource = new CancellationTokenSource();
                CancellationTokenSource existingSource = _pendingCancellableMessages.GetOrAdd(key = new UniqueMessageKey(overhead.MessageId, overhead.SendingConnection), newTokenSource);
                if (!ReferenceEquals(existingSource, newTokenSource))
                {
                    existingSource.Dispose();
                }

                src = token.CombineTokensIfNeeded(newTokenSource.Token);
                didAddTokenSrc = true;
            }

            RpcInvocationResult result = rpc.Invoke(overhead, this, serializer, bytes, canTakeOwnership, token);
            ValueTask vt = result.Task;
            if (!vt.IsCompleted)
                return new ValueTask(FinishVtTask(this, result, overhead, serializer, didAddTokenSrc, key, src));

            if ((overhead.Flags & RpcFlags.FireAndForget) != 0 || overhead.SendingConnection == null)
                return default;

            if (didAddTokenSrc && _pendingCancellableMessages.TryRemove(key, out CancellationTokenSource? cancellable))
            {
                cancellable.Dispose();
                src.Dispose();
                didAddTokenSrc = false;
            }

            try
            {
                return HandleValueTaskReply(result, overhead);
            }
            catch (Exception ex2)
            {
                HandleInvokeException(overhead, ex2);
            }

            return default;
        }
        catch (Exception ex)
        {
            if (didAddTokenSrc)
            {
                if (_pendingCancellableMessages.TryRemove(key, out CancellationTokenSource? cancellable))
                {
                    cancellable.Dispose();
                }

                src.Dispose();
            }
            if (overhead.SendingConnection != null)
            {
                try
                {
                    ValueTask vt = ReplyRpcException(overhead.MessageId, checked((byte)(overhead.SubMessageId + 1)), overhead.SendingConnection, ex, serializer);
                    return vt.IsCompleted ? default : new ValueTask(FinishExVtTask(this, vt, overhead));
                }
                catch (Exception ex2)
                {
                    HandleInvokeException(overhead, ex2);
                }
            }

            HandleInvokeException(overhead, ex);
            return default;
        }

        static async Task FinishVtTask(DefaultRpcRouter router, RpcInvocationResult result, RpcOverhead overhead, IRpcSerializer serializer, bool didAddTokenSrc, UniqueMessageKey key, CombinedTokenSources src)
        {
            try
            {
                ValueTask vt = result.Task;
                await vt;

                if ((overhead.Flags & RpcFlags.FireAndForget) != 0 || overhead.SendingConnection == null)
                    return;

                try
                {
                    await router.HandleValueTaskReply(result, overhead);
                }
                catch (Exception ex2)
                {
                    router.HandleInvokeException(overhead, ex2);
                }
            }
            catch (Exception ex)
            {
                if (overhead.SendingConnection != null)
                {
                    try
                    {
                        await ReplyRpcException(overhead.MessageId, checked((byte)(overhead.SubMessageId + 1)),
                            overhead.SendingConnection, ex, serializer);
                    }
                    catch (Exception ex2)
                    {
                        router.HandleInvokeException(overhead, ex2);
                    }
                }

                router.HandleInvokeException(overhead, ex);
            }
            finally
            {
                if (didAddTokenSrc)
                {
                    if (router._pendingCancellableMessages.TryRemove(key, out CancellationTokenSource? cancellable))
                    {
                        cancellable.Dispose();
                    }

                    src.Dispose();
                }
            }
        }

        static async Task FinishExVtTask(DefaultRpcRouter router, ValueTask vt, RpcOverhead overhead)
        {
            try
            {
                await vt;
            }
            catch (Exception ex)
            {
                router.HandleInvokeException(overhead, ex);
            }
        }
    }

    /// <inheritdoc />
    public virtual unsafe ValueTask ReceiveData(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token = default)
    {
        PrimitiveRpcOverhead overhead;
        fixed (byte* ptr = rawData.Span)
        {
            overhead = PrimitiveRpcOverhead.ReadFromBytes(sendingConnection, serializer, ptr, (uint)rawData.Length);
        }

        return ReceiveData(in overhead, sendingConnection, serializer, rawData, canTakeOwnership, token);
    }

    /// <inheritdoc />
    public virtual unsafe ValueTask ReceiveData(in PrimitiveRpcOverhead primitiveOverhead, IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token = default)
    {
        if (rawData.Length <= 1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        switch (primitiveOverhead.CodeId)
        {
            case RpcOverhead.OvhCodeId:
                RpcOverhead? overhead = primitiveOverhead.FullOverhead;

                if (overhead == null)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                bool isEmpty = rawData.Length == overhead.OverheadSize;
                return InvokeInvocationPoint(overhead.Rpc, overhead, serializer, isEmpty ? default : rawData.Slice((int)overhead.OverheadSize), canTakeOwnership || isEmpty, token);

            case OvhCodeIdException:
                uint index;
                RpcTask? task;
                Exception? ex;
                int len = rawData.Length - (int)primitiveOverhead.OverheadSize;
                fixed (byte* ptr = rawData.Span)
                {
                    _listeningTasks.TryRemove(primitiveOverhead.MessageId, out task);
                    IRpcInvocationPoint? invPt = task?.Endpoint;
                    index = 0;
                    ex = ReadException(sendingConnection, invPt, ptr + primitiveOverhead.OverheadSize, (uint)len, ref index, serializer);
                }

                task?.TriggerComplete(ex);
                FinishListening(task);
                break;

            case OvhCodeIdValueRtnSuccess:
                object? rtn;
                len = rawData.Length - (int)primitiveOverhead.OverheadSize;
                fixed (byte* ptr = rawData.Span)
                {
                    _listeningTasks.TryRemove(primitiveOverhead.MessageId, out task);
                    index = 0;
                    rtn = ReadReturnValue(serializer, task, ptr + primitiveOverhead.OverheadSize, len, ref index);
                }

                if (rtn == null || task == null)
                    break;

                if (task.TrySetResult(rtn))
                    task.TriggerComplete(null);
                else
                {
                    Type taskType = task.GetType();
                    taskType = taskType.IsGenericType ? taskType.GetGenericArguments()[0] : typeof(void);
                    task.TriggerComplete(new RpcParseException(
                        string.Format(Properties.Exceptions.RpcParseExceptionInvalidReturnType,
                        Accessor.ExceptionFormatter.Format(rtn.GetType()),
                        Accessor.ExceptionFormatter.Format(taskType))
                    ));
                }
                FinishListening(task);
                break;

            case OvhCodeIdVoidRtnSuccess:
                _listeningTasks.TryRemove(primitiveOverhead.MessageId, out task);
                task?.TriggerComplete(null);
                FinishListening(task);
                break;

            case OvhCodeIdCancel:
                if (_pendingCancellableMessages.TryRemove(new UniqueMessageKey(primitiveOverhead.MessageId, sendingConnection), out CancellationTokenSource? tokenSrc))
                {
                    tokenSrc.Cancel();
                    tokenSrc.Dispose();
                }

                break;
        }

        return default;
    }

    /// <inheritdoc />
    public virtual ValueTask ReceiveData(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        PrimitiveRpcOverhead overhead = PrimitiveRpcOverhead.ReadFromStream(sendingConnection, serializer, stream);

        return ReceiveData(in overhead, sendingConnection, serializer, stream, token);
    }

    /// <inheritdoc />
    public virtual ValueTask ReceiveData(in PrimitiveRpcOverhead primitiveOverhead, IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        switch (primitiveOverhead.CodeId)
        {
            case RpcOverhead.OvhCodeId:
                RpcOverhead? overhead = primitiveOverhead.FullOverhead;

                if (overhead == null)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                return InvokeInvocationPoint(overhead.Rpc, overhead, serializer, stream, token);

            case OvhCodeIdException:
                _listeningTasks.TryRemove(primitiveOverhead.MessageId, out RpcTask? task);
                IRpcInvocationPoint? invPt = task?.Endpoint;
                Exception ex = ReadException(sendingConnection, invPt, stream, serializer);

                task?.TriggerComplete(ex);
                FinishListening(task);
                break;

            case OvhCodeIdValueRtnSuccess:
                _listeningTasks.TryRemove(primitiveOverhead.MessageId, out task);
                object? rtn = ReadReturnValue(serializer, task, stream);

                if (rtn == null || task == null)
                    break;

                if (task.TrySetResult(rtn))
                    task.TriggerComplete(null);
                else
                {
                    Type taskType = task.GetType();
                    taskType = taskType.IsGenericType ? taskType.GetGenericArguments()[0] : typeof(void);
                    task.TriggerComplete(new RpcParseException(
                        string.Format(Properties.Exceptions.RpcParseExceptionInvalidReturnType,
                        Accessor.ExceptionFormatter.Format(rtn.GetType()),
                        Accessor.ExceptionFormatter.Format(taskType))
                    ));
                }
                FinishListening(task);
                break;

            case OvhCodeIdVoidRtnSuccess:
                _listeningTasks.TryRemove(primitiveOverhead.MessageId, out task);
                task?.TriggerComplete(null);
                FinishListening(task);
                break;

            case OvhCodeIdCancel:
                if (_pendingCancellableMessages.TryRemove(new UniqueMessageKey(primitiveOverhead.MessageId, sendingConnection), out CancellationTokenSource? tokenSrc))
                {
                    tokenSrc.Cancel();
                    tokenSrc.Dispose();
                }

                break;
        }

        return default;
    }
    public void HandleInvokeException(RpcOverhead overhead, Exception ex)
    {
        this.LogError(ex,
            string.Format(Properties.Exceptions.RpcInvocationExceptionWithInvocationPointMessage,
                Accessor.Formatter.Format(ex.GetType()),
                overhead.Rpc,
                overhead.MessageId + "." + overhead.SubMessageId)
        );
    }

    /// <inheritdoc />
    public unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, CancellationToken token, byte* bytes, int byteCt, uint dataCt, ref RpcCallMethodInfo callMethodInfo)
    {
        ulong messageId = GetNewMessageId();
        int ovhSize = (int)((uint)byteCt - dataCt);
        int ovhEnd = WriteOverhead(sourceMethodHandle, serializer, ref callMethodInfo, bytes, ovhSize, dataCt, messageId, 0);
        WriteEndpoint(sourceMethodHandle, serializer, ref callMethodInfo, callMethodInfo.Endpoint.GetEndpoint(), bytes + ovhEnd, ovhSize - ovhEnd);

        RpcTask? rpcTask = null;
        CombinedTokenSources tokens = token.CombineTokensIfNeeded(_cancelTokenSource.Token);
        if (connections == null)
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            if (!_connectionLifetime.IsSingleConnection && !callMethodInfo.IsFireAndForget)
            {
                tokens.Dispose();
                throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));
            }

            rpcTask = !callMethodInfo.IsFireAndForget
                ? CreateRpcTaskListener(in callMethodInfo, sourceMethodHandle, messageId) 
                : new RpcBroadcastTask(true) { CompleteCount = 1, MessageId = messageId, SubMessageId = 0 };

            rpcTask.CombinedTokensToDisposeOnComplete = tokens;
            int ct = _connectionLifetime.ForEachRemoteConnection(connection =>
            {
                Interlocked.CompareExchange(ref rpcTask.ConnectionIntl, connection, null);

                Interlocked.Increment(ref rpcTask.CompleteCount);
                if (rpcTask is RpcBroadcastTask bt)
                    Interlocked.Increment(ref bt.ConnectionCountIntl);
                
                try
                {
                    ValueTask vt = connection.SendDataAsync(_defaultSerializer, new ReadOnlySpan<byte>(bytes, byteCt), false, token);
                    
                    if (!vt.IsCompleted)
                        Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask), token);
                }
                catch (Exception ex)
                {
                    HandleInvokeException(sourceMethodHandle, ex);
                    rpcTask.TriggerComplete(ex);
                    FinishListening(rpcTask);
                }

                return true;
            });

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            if (ct == 0)
            {
                rpcTask.TriggerComplete(
                    new RpcNoConnectionsException(string.Format(Properties.Exceptions.RpcNoConnectionsExceptionConnectionLifetime, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)))
                );
            }
            else
            {
                if (token.CanBeCanceled)
                {
                    rpcTask.SetToken(token, this);
                }

                // maybe throw exceptions if all threw
                rpcTask.TriggerComplete(null);
            }

            return rpcTask;
        }
        
        if (connections is IModularRpcRemoteConnection remote1)
        {
            rpcTask = !callMethodInfo.IsFireAndForget
                ? CreateRpcTaskListener(in callMethodInfo, sourceMethodHandle, messageId)
                : new RpcTask(true) { MessageId = messageId, SubMessageId = 0 };

            rpcTask.ConnectionIntl = remote1;

            rpcTask.CombinedTokensToDisposeOnComplete = tokens;
            try
            {
                ValueTask vt = remote1.SendDataAsync(_defaultSerializer, new ReadOnlySpan<byte>(bytes, byteCt), false, token);

                if (!vt.IsCompleted)
                    Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask), token);
            }
            catch (Exception ex)
            {
                tokens.Dispose();
                HandleInvokeException(sourceMethodHandle, ex);
                rpcTask.TriggerComplete(ex);
                FinishListening(rpcTask);
            }

            if (token.CanBeCanceled)
            {
                rpcTask.SetToken(token, this);
            }

            return rpcTask;
        }

        if (connections is not IEnumerable<IModularRpcRemoteConnection> remotes)
        {
            tokens.Dispose();
            throw new ArgumentException(Properties.Exceptions.InvokeRpcConnectionsInvalidType, nameof(connections));
        }

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        if (!callMethodInfo.IsFireAndForget)
        {
            tokens.Dispose();
            throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));
        }

        foreach (IModularRpcRemoteConnection connection in remotes)
        {
            if (rpcTask == null)
            {
                rpcTask = new RpcBroadcastTask(true) { CompleteCount = 1, MessageId = messageId, SubMessageId = 0 };
                rpcTask.CombinedTokensToDisposeOnComplete = tokens;
            }

            Interlocked.Increment(ref ((RpcBroadcastTask)rpcTask).ConnectionCountIntl);
            Interlocked.Increment(ref rpcTask.CompleteCount);
            try
            {
                ValueTask vt = connection.SendDataAsync(_defaultSerializer, new ReadOnlySpan<byte>(bytes, byteCt), false, token);

                if (!vt.IsCompleted)
                    Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask), token);
            }
            catch (Exception ex)
            {
                HandleInvokeException(sourceMethodHandle, ex);
                rpcTask.TriggerComplete(ex);
                FinishListening(rpcTask);
            }
        }

        if (rpcTask == null)
        {
            tokens.Dispose();
            return RpcTask.CompletedTask;
        }

        if (token.CanBeCanceled)
        {
            rpcTask.SetToken(token, this);
        }

        // maybe throw exceptions if all threw
        rpcTask.TriggerComplete(null);
        FinishListening(rpcTask);
        return rpcTask;
    }

    /// <inheritdoc />
    public unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, CancellationToken token, ArraySegment<byte> overheadBuffer, Stream stream, bool leaveOpen, uint dataCt, ref RpcCallMethodInfo callMethodInfo)
    {
        bool isDisposed = false;
        try
        {
            if (overheadBuffer.Array == null || overheadBuffer.Count == 0)
            {
                throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };
            }

            ulong messageId = GetNewMessageId();
            fixed (byte* ptr = &overheadBuffer.Array![overheadBuffer.Offset])
            {
                int ovhEnd = WriteOverhead(sourceMethodHandle, serializer, ref callMethodInfo, ptr, overheadBuffer.Count, dataCt, messageId, 0);
                WriteEndpoint(sourceMethodHandle, serializer, ref callMethodInfo, callMethodInfo.Endpoint.GetEndpoint(), ptr + ovhEnd, overheadBuffer.Count - ovhEnd);
            }

            OverheadStreamPrepender ovhStream = new OverheadStreamPrepender(stream, overheadBuffer, dataCt, true);
            ArraySegment<byte> nonSingle;
            RpcTask? rpcTask = null;
            CombinedTokenSources tokens = token.CombineTokensIfNeeded(_cancelTokenSource.Token);
            if (connections == null)
            {
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                if (!_connectionLifetime.IsSingleConnection && !callMethodInfo.IsFireAndForget)
                {
                    tokens.Dispose();
                    throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));
                }

                rpcTask = !callMethodInfo.IsFireAndForget
                    ? CreateRpcTaskListener(in callMethodInfo, sourceMethodHandle, messageId)
                    : new RpcBroadcastTask(true) { CompleteCount = 1, MessageId = messageId, SubMessageId = 0 };

                nonSingle = default;
                if (!_connectionLifetime.IsSingleConnection)
                {
                    nonSingle = new ArraySegment<byte>(new byte[ovhStream.Length]);
                    int actualLen = ovhStream.Read(nonSingle.Array!, 0, nonSingle.Count);
                    if (actualLen != nonSingle.Count)
                        nonSingle = new ArraySegment<byte>(nonSingle.Array!, 0, actualLen);

                    if (!leaveOpen)
                    {
                        stream.Dispose();
                        isDisposed = true;
                    }
                }

                rpcTask.CombinedTokensToDisposeOnComplete = tokens;

                int ct = _connectionLifetime.ForEachRemoteConnection(connection =>
                {
                    Interlocked.CompareExchange(ref rpcTask.ConnectionIntl, connection, null);

                    Interlocked.Increment(ref rpcTask.CompleteCount);
                    if (rpcTask is RpcBroadcastTask bt)
                        Interlocked.Increment(ref bt.ConnectionCountIntl);

                    try
                    {
                        if (nonSingle.Array != null)
                        {
                            ValueTask vt = connection.SendDataAsync(_defaultSerializer, nonSingle.AsSpan(), true, token);

                            if (!vt.IsCompleted)
                                Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask), token);
                        }
                        else
                        {
                            ValueTask vt = connection.SendDataAsync(_defaultSerializer, ovhStream, token);

                            if (!vt.IsCompleted)
                            {
                                // ReSharper disable once AccessToDisposedClosure
                                Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask, leaveOpen ? null : stream), token);
                                isDisposed = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleInvokeException(sourceMethodHandle, ex);
                        rpcTask.TriggerComplete(ex);
                        FinishListening(rpcTask);
                    }

                    return nonSingle.Array != null;
                });

                // ReSharper disable once RedundantSuppressNullableWarningExpression
                if (ct == 0)
                {
                    rpcTask.TriggerComplete(
                        new RpcNoConnectionsException(string.Format(Properties.Exceptions.RpcNoConnectionsExceptionConnectionLifetime, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)))
                    );
                }
                else
                {
                    if (token.CanBeCanceled)
                    {
                        rpcTask.SetToken(token, this);
                    }
                    // maybe throw exceptions if all threw
                    rpcTask.TriggerComplete(null);

                    return rpcTask;
                }
            }

            if (connections is IModularRpcRemoteConnection remote1)
            {
                rpcTask = !callMethodInfo.IsFireAndForget
                    ? CreateRpcTaskListener(in callMethodInfo, sourceMethodHandle, messageId)
                    : new RpcTask(true) { MessageId = messageId, SubMessageId = 0 };

                rpcTask.ConnectionIntl = remote1;
                rpcTask.CombinedTokensToDisposeOnComplete = tokens;

                try
                {
                    ValueTask vt = remote1.SendDataAsync(_defaultSerializer, ovhStream, token);

                    if (!vt.IsCompleted)
                    {
                        Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask, leaveOpen ? null : stream), token);
                        isDisposed = true;
                    }
                }
                catch (Exception ex)
                {
                    HandleInvokeException(sourceMethodHandle, ex);
                    rpcTask.TriggerComplete(ex);
                    FinishListening(rpcTask);
                }

                if (token.CanBeCanceled)
                {
                    rpcTask.SetToken(token, this);
                }

                return rpcTask;
            }

            if (connections is not IEnumerable<IModularRpcRemoteConnection> remotes)
            {
                tokens.Dispose();
                throw new ArgumentException(Properties.Exceptions.InvokeRpcConnectionsInvalidType, nameof(connections));
            }

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            if (!callMethodInfo.IsFireAndForget)
            {
                tokens.Dispose();
                throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));
            }

            nonSingle = default;
            if (remotes is not ICollection<IModularRpcRemoteConnection> { Count: 1 })
            {
                nonSingle = new ArraySegment<byte>(new byte[ovhStream.Length]);
                int actualLen = ovhStream.Read(nonSingle.Array!, 0, nonSingle.Count);
                if (actualLen != nonSingle.Count)
                    nonSingle = new ArraySegment<byte>(nonSingle.Array!, 0, actualLen);

                if (!leaveOpen)
                {
                    stream.Dispose();
                    isDisposed = true;
                }
            }

            int ind = 0;
            foreach (IModularRpcRemoteConnection connection in remotes)
            {
                if (nonSingle.Array == null && ind > 0)
                    break;

                ++ind;
                if (rpcTask == null)
                {
                    rpcTask = new RpcBroadcastTask(true) { CompleteCount = 1, MessageId = messageId, SubMessageId = 0 };
                    rpcTask.CombinedTokensToDisposeOnComplete = tokens;
                }
                Interlocked.Increment(ref ((RpcBroadcastTask)rpcTask).ConnectionCountIntl);
                Interlocked.Increment(ref rpcTask.CompleteCount);
                try
                {
                    if (nonSingle.Array != null)
                    {
                        ValueTask vt = connection.SendDataAsync(_defaultSerializer, nonSingle.AsSpan(), true, token);

                        if (!vt.IsCompleted)
                            Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask), token);
                    }
                    else
                    {
                        ValueTask vt = connection.SendDataAsync(_defaultSerializer, ovhStream, token);

                        if (!vt.IsCompleted)
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            Task.Run(WrapInvokeTaskInTryBlock(sourceMethodHandle, vt, rpcTask, leaveOpen ? null : stream), token);
                            isDisposed = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleInvokeException(sourceMethodHandle, ex);
                    rpcTask.TriggerComplete(ex);
                    FinishListening(rpcTask);
                }
            }

            if (rpcTask == null)
            {
                tokens.Dispose();
                return RpcTask.CompletedTask;
            }

            if (token.CanBeCanceled)
            {
                rpcTask.SetToken(token, this);
            }

            // maybe throw exceptions if all threw
            rpcTask.TriggerComplete(null);
            FinishListening(rpcTask);
            return rpcTask;
        }
        finally
        {
            if (!isDisposed && !leaveOpen)
                stream.Dispose();
        }
    }
    private ValueTask HandleValueTaskReply(RpcInvocationResult result, RpcOverhead overhead)
    {
        object? returnValue = ProxyGenerator.Instance.ConvertValueTaskToValue(result);

        return returnValue == null
            ? ReplyRpcVoidSuccessRtn(overhead.MessageId, checked( (byte)(overhead.SubMessageId + 1) ), overhead.SendingConnection!, Serializer)
            : ReplyRpcValueSuccessRtn(overhead.MessageId, checked( (byte)(overhead.SubMessageId + 1) ), overhead.SendingConnection!, returnValue, Serializer);
    }
    private Func<Task> WrapInvokeTaskInTryBlock(RuntimeMethodHandle sourceMethodHandle, ValueTask vt, RpcTask? rpcTask, IDisposable? toDispose = null)
    {
        return async () =>
        {
            try
            {
                await vt;
                if (rpcTask is { IsFireAndForget: true })
                {
                    rpcTask.TriggerComplete(null);
                    FinishListening(rpcTask);
                }
            }
            catch (Exception ex)
            {
                HandleInvokeException(sourceMethodHandle, ex);
                rpcTask?.TriggerComplete(ex);
                FinishListening(rpcTask);
            }

            if (toDispose == null)
                return;

            try
            {
                toDispose.Dispose();
            }
            catch (Exception ex)
            {
                this.LogWarning(ex, $"Failed to dispose: {Accessor.Formatter.Format(toDispose.GetType())}.");
            }
        };
    }
    private RpcTask CreateRpcTaskListener(in RpcCallMethodInfo callInfo, RuntimeMethodHandle sourceMethodHandle, ulong messageId)
    {
        MethodInfo method = (MethodBase.GetMethodFromHandle(sourceMethodHandle) as MethodInfo)!;

        RpcTask rpcTask;
        if (method.ReturnType == typeof(void) || method.ReturnType == typeof(RpcTask))
            rpcTask = new RpcTask(false);
        else
            rpcTask = (RpcTask?)Activator.CreateInstance(method.ReturnType, nonPublic: true) ?? new RpcTask(false);
        
        rpcTask.MessageId = messageId;
        rpcTask.SubMessageId = 1;
        StartListening(rpcTask, messageId, callInfo.Timeout);
        return rpcTask;
    }
    private static void FinishListening(RpcTask? rpcTask)
    {
        if (rpcTask == null)
            return;

        Timer? timer = Interlocked.Exchange(ref rpcTask.Timer, null);
        if (timer == null)
            return;

        try
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            // ignored
        }
        timer.Dispose();
    }
    private void StartListening(RpcTask rpcTask, ulong messageId, TimeSpan timeout)
    {
        _listeningTasks.TryAdd(messageId, rpcTask);
        rpcTask.Timeout = timeout;
        if (timeout == Timeout.InfiniteTimeSpan || rpcTask.IsFireAndForget)
            return;
        
        rpcTask.Timer = new Timer(RpcTaskTimerCompleted, rpcTask, timeout, Timeout.InfiniteTimeSpan);
    }
    private void RpcTaskTimerCompleted(object? state)
    {
        if (state is not RpcTask rpcTask)
            return;

        if (!rpcTask.GetAwaiter().IsCompleted && _listeningTasks.TryRemove(rpcTask.MessageId, out _))
            rpcTask.TriggerComplete(new RpcTimeoutException(rpcTask.Timeout));

        FinishListening(rpcTask);
    }
    private void HandleInvokeException(RuntimeMethodHandle sourceMethodHandle, Exception ex)
    {
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        this.LogError(ex,
            string.Format(Properties.Exceptions.RpcInvocationExceptionWithInvocationPointMessage,
                Accessor.Formatter.Format(ex.GetType()),
                Accessor.Formatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!),
                ex.Message)
        );
    }
    private static unsafe ValueTask ReplyRpcException(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, Exception ex, IRpcSerializer serializer)
    {
        uint size = GetExceptionSize(ex, serializer);
        uint pfxSize = GetPrefixSize(serializer);
        size += pfxSize;

        bool didStackAlloc = size <= serializer.Configuration.MaximumStackAllocationSize;
        Span<byte> alloc = didStackAlloc ? stackalloc byte[(int)size] : new byte[size];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, size - pfxSize, OvhCodeIdException, messageId, subMessageId, serializer);
            WriteException(ex, ptr, size, ref index, serializer);
        }

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), !didStackAlloc, CancellationToken.None);
    }

    private static unsafe ValueTask FollowupRpcCancel(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, IRpcSerializer serializer)
    {
        uint pfxSize = GetPrefixSize(serializer);

        bool didStackAlloc = pfxSize <= serializer.Configuration.MaximumStackAllocationSize;
        Span<byte> alloc = didStackAlloc ? stackalloc byte[(int)pfxSize] : new byte[pfxSize];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, 0, OvhCodeIdCancel, messageId, subMessageId, serializer);
        }

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), !didStackAlloc, CancellationToken.None);
    }
    
    private static unsafe ValueTask ReplyRpcVoidSuccessRtn(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, IRpcSerializer serializer)
    {
        uint pfxSize = GetPrefixSize(serializer);

        bool didStackAlloc = pfxSize <= serializer.Configuration.MaximumStackAllocationSize;
        Span<byte> alloc = didStackAlloc ? stackalloc byte[(int)pfxSize] : new byte[pfxSize];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, 0, OvhCodeIdVoidRtnSuccess, messageId, subMessageId, serializer);
        }

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), !didStackAlloc, CancellationToken.None);
    }
    private static unsafe ValueTask ReplyRpcValueSuccessRtn(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, object? value, IRpcSerializer serializer)
    {
        uint pfxSize = GetPrefixSize(serializer);
        uint size = pfxSize;
        uint knownTypeId = 0;
        bool hasKnownTypeId = false;
        string? typeName = null;
        TypeCode tc = value switch
        {
            null => TypeCode.DBNull,
            IConvertible c => c.GetTypeCode(),
            DateTimeOffset => TypeUtility.TypeCodeDateTimeOffset,
            TimeSpan => TypeUtility.TypeCodeTimeSpan,
            Guid => TypeUtility.TypeCodeGuid,
            _ => TypeCode.Object
        };

        if (tc is TypeCode.String or TypeCode.Object || !serializer.CanFastReadPrimitives)
        {
            if (tc == TypeCode.Object)
            {
                Type type = value!.GetType();
                size += 1u + (uint)serializer.GetSize(type, value);
                // ReSharper disable once AssignmentInConditionalExpression
                if (hasKnownTypeId = serializer.TryGetKnownTypeId(type, out knownTypeId))
                    size += 5u;
                else
                    size += 1u + (uint)serializer.GetSize(typeName = TypeUtility.GetAssemblyQualifiedNameNoVersion(type));
            }
            else if (tc == TypeCode.String)
            {
                size += 1u + (uint)serializer.GetSize((string)value!);
            }
            else
            {
                size += 1u + (uint)serializer.GetSize(value!);
            }
        }
        else
        {
            size += 1u + (uint)TypeUtility.GetTypeCodeSize(tc);
        }

        bool didStackAlloc = size <= serializer.Configuration.MaximumStackAllocationSize;
        Span<byte> alloc = didStackAlloc ? stackalloc byte[(int)size] : new byte[size];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, size - pfxSize, OvhCodeIdValueRtnSuccess, messageId, subMessageId, serializer);

            ptr[index] = (byte)tc;
            ++index;

            if (tc == TypeCode.Object)
            {
                RpcEndpoint.IdentifierFlags f = hasKnownTypeId
                    ? RpcEndpoint.IdentifierFlags.IsKnownTypeOnly
                    : RpcEndpoint.IdentifierFlags.IsTypeNameOnly;

                ptr[index] = (byte)f;
                ++index;

                if (hasKnownTypeId)
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(ptr + index, knownTypeId);
                    }
                    else
                    {
                        ptr[index + 3] = unchecked( (byte) knownTypeId );
                        ptr[index + 2] = unchecked( (byte)(knownTypeId >>> 8) );
                        ptr[index + 1] = unchecked( (byte)(knownTypeId >>> 16) );
                        ptr[index]     = unchecked( (byte)(knownTypeId >>> 24) );
                    }

                    index += 4;
                }
                else
                {
                    index += (uint)serializer.WriteObject(typeName!, ptr + index, size - index);
                }
            }
            else
            {
                TypeUtility.WriteTypeCode(tc, serializer, value!, ptr, ref index, size);
            }
        }

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), !didStackAlloc, CancellationToken.None);
    }
    private static unsafe object? ReadReturnValue(IRpcSerializer serializer, RpcTask? task, byte* data, int maxSize, ref uint index)
    {
        if (maxSize - index < 1)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

        TypeCode tc = (TypeCode)data[index];
        ++index;

        object? rtnValue;
        int bytesRead;
        if (tc == TypeCode.Object)
        {
            if (maxSize - index < 1)
                throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

            RpcEndpoint.IdentifierFlags f = (RpcEndpoint.IdentifierFlags)data[index];
            ++index;

            bool hasName = (f & RpcEndpoint.IdentifierFlags.IsKnownTypeOnly) == 0;
            bool hasId   = (f & RpcEndpoint.IdentifierFlags.IsTypeNameOnly) == 0;

            string? typeName = null;
            uint typeId = 0u;
            if (hasId)
            {
                if (maxSize - index < 4)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

                typeId = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref data[index])
                    : unchecked( (uint)(data[index] << 24 | data[index + 1] << 16 | data[index + 2] << 8 | data[index + 3]) );
                index += 4;
            }

            if (hasName)
            {
                typeName = serializer.ReadObject<string>(data, (uint)maxSize - index, out bytesRead);
                index += (uint)bytesRead;
            }

            Type? type = null;
            if (hasId)
                serializer.TryGetKnownType(typeId, out type);

            if (hasName && type == null)
                type = Type.GetType(typeName!, false, false);

            if (type == null && task != null)
            {
                Type taskType = task.GetType();
                taskType = taskType.IsGenericType ? taskType.GetGenericArguments()[0] : typeof(void);
                task.TriggerComplete(new RpcParseException(
                    string.Format(Properties.Exceptions.RpcParseExceptionUnknownReturnType,
                        !hasName ? hasId ? typeId.ToString(CultureInfo.InvariantCulture) : "unknown type" : typeName,
                        Accessor.ExceptionFormatter.Format(taskType)))
                );
                FinishListening(task);
            }

            if (type == null)
                return null;

            rtnValue = serializer.ReadObject(type, data + index, (uint)maxSize - index, out bytesRead);
            index += (uint)bytesRead;
            return rtnValue;
        }

        rtnValue = TypeUtility.ReadTypeCode(tc, serializer, data, maxSize, ref index, out bytesRead);

        if (rtnValue == null && task != null)
        {
            Type taskType = task.GetType();
            taskType = taskType.IsGenericType ? taskType.GetGenericArguments()[0] : typeof(void);
            task.TriggerComplete(new RpcParseException(
                string.Format(Properties.Exceptions.RpcParseExceptionUnknownReturnType, tc.ToString(), Accessor.ExceptionFormatter.Format(taskType))
            ));
            FinishListening(task);
        }

        index += (uint)bytesRead;
        return rtnValue;
    }
    private static object? ReadReturnValue(IRpcSerializer serializer, RpcTask? task, Stream stream)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        TypeCode tc = (TypeCode)b;

        object? rtnValue;
        Type taskType;
        if (tc == TypeCode.Object)
        {
            b = stream.ReadByte();
            if (b == -1)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            RpcEndpoint.IdentifierFlags f = (RpcEndpoint.IdentifierFlags)b;

            bool hasName = (f & RpcEndpoint.IdentifierFlags.IsKnownTypeOnly) == 0;
            bool hasId   = (f & RpcEndpoint.IdentifierFlags.IsTypeNameOnly) == 0;

            string? typeName = null;
            uint typeId = 0u;
            int rdCt;
            if (hasId)
            {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                Span<byte> data = stackalloc byte[4];
                rdCt = stream.Read(data);
                if (rdCt != 4)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

                typeId = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(data))
                    : unchecked( (uint)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]) );
#else
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(4);
                try
                {
                    rdCt = stream.Read(buffer, 0, 4);
                    if (rdCt != 4)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

                    typeId = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<uint>(ref buffer[0])
                        : unchecked((uint)(buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3]));
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
#endif
            }

            if (hasName)
            {
                typeName = serializer.ReadObject<string>(stream, out _);
            }

            Type? type = null;
            if (hasId)
                serializer.TryGetKnownType(typeId, out type);

            if (hasName && type == null && typeName != null)
                type = Type.GetType(typeName, false, false);

            if (type == null && task != null)
            {
                taskType = task.GetType();
                taskType = taskType.IsGenericType ? taskType.GetGenericArguments()[0] : typeof(void);
                task.TriggerComplete(new RpcParseException(
                    string.Format(Properties.Exceptions.RpcParseExceptionUnknownReturnType,
                        !hasName ? hasId ? typeId.ToString(CultureInfo.InvariantCulture) : "unknown type" : typeName,
                        Accessor.ExceptionFormatter.Format(taskType)))
                );
                FinishListening(task);
            }

            if (type == null)
                return null;

            rtnValue = serializer.ReadObject(type, stream, out _);
            return rtnValue;
        }

        rtnValue = TypeUtility.ReadTypeCode(tc, serializer, stream, out _);

        if (rtnValue != null || task == null)
            return rtnValue;
        
        taskType = task.GetType();
        taskType = taskType.IsGenericType ? taskType.GetGenericArguments()[0] : typeof(void);
        task.TriggerComplete(new RpcParseException(
                string.Format(Properties.Exceptions.RpcParseExceptionUnknownReturnType, tc.ToString(), Accessor.ExceptionFormatter.Format(taskType))
            ));
        FinishListening(task);
        return rtnValue;
    }

    /// <summary>
    /// The next Id to be used, actually a <see cref="uint"/> but stored as <see cref="int"/> to be used with <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    protected int NextId;

    /// <inheritdoc />
    public virtual IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId)
    {
        // ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
        return CachedDescriptors.TryGetValue(endpointSharedId, out IRpcInvocationPoint? endpoint) ? endpoint : null;
    }

    /// <inheritdoc />
    public uint AddRpcEndpoint(IRpcInvocationPoint endPoint)
    {
        // keep trying to add if the id is taken, could've been added by a third party
        while (true)
        {
            uint id = unchecked( (uint)Interlocked.Increment(ref NextId) );
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
    protected virtual IRpcInvocationPoint CreateEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[]? args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation)
    {
        return new RpcEndpoint(knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation);
    }

    /// <inheritdoc />
    public IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation, int byteSize, object? identifier)
        => ResolveEndpoint(_defaultSerializer, knownRpcShortcutId, typeName, methodName, args, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation, byteSize, identifier);

    /// <inheritdoc />
    public virtual IRpcInvocationPoint ResolveEndpoint(IRpcSerializer serializer, uint knownRpcShortcutId, string typeName, string methodName, string[] args, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash, bool supportsRemoteCancellation, int byteSize, object? identifier)
    {
        IRpcInvocationPoint cachedEndpoint = knownRpcShortcutId == 0u
            ? CreateEndpoint(0u, typeName, methodName, args, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation)
            : CachedDescriptors.GetOrAdd(knownRpcShortcutId, key => CreateEndpoint(key, typeName, methodName, args, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash, supportsRemoteCancellation));

        return ReferenceEquals(cachedEndpoint.Identifier, identifier)
            ? cachedEndpoint
            : cachedEndpoint.CloneWithIdentifier(serializer, identifier);
    }

    /// <inheritdoc />
    public void GetDefaultProxyContext(Type proxyType, out ProxyContext context)
    {
        context = default;
        context.DefaultSerializer = _defaultSerializer;
        context.Router = this;
    }
    private static uint GetPrefixSize(IRpcSerializer serializer)
    {
        if (serializer.CanFastReadPrimitives)
            return 1 + sizeof(ulong) + sizeof(byte) + sizeof(uint) + sizeof(uint);

        return (uint)(1 + serializer.GetSize(typeof(ulong)) + serializer.GetSize(typeof(byte)) + serializer.GetSize(typeof(uint)) + serializer.GetSize(typeof(uint)));
    }
    private static unsafe uint WritePrefix(byte* ptr, uint size, byte ovhCodeId, ulong messageId, byte subMessageId, IRpcSerializer serializer)
    {
        ptr[0] = ovhCodeId;
        uint index = 1;
        if (!serializer.CanFastReadPrimitives)
        {
            index += (uint)serializer.WriteObject(size, ptr + index, size - index);
            index += (uint)serializer.WriteObject(size, ptr + index, size - index);
            index += (uint)serializer.WriteObject(messageId, ptr + index, size - index);
            index += (uint)serializer.WriteObject(subMessageId, ptr + index, size - index);
            return index;
        }

        for (int i = 0; i < 2; ++i)
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ptr + index, size);
            }
            else
            {
                ptr[4] = unchecked( (byte) size );
                ptr[3] = unchecked( (byte)(size >>> 8)  );
                ptr[2] = unchecked( (byte)(size >>> 16) );
                ptr[1] = unchecked( (byte)(size >>> 24) );
            }

            index += 4;
        }

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ptr + index, messageId);
        }
        else
        {
            ptr[index + 7] = unchecked( (byte) messageId );
            ptr[index + 6] = unchecked( (byte)(messageId >>> 8)  );
            ptr[index + 5] = unchecked( (byte)(messageId >>> 16) );
            ptr[index + 4] = unchecked( (byte)(messageId >>> 24) );
            ptr[index + 3] = unchecked( (byte)(messageId >>> 32) );
            ptr[index + 2] = unchecked( (byte)(messageId >>> 40) );
            ptr[index + 1] = unchecked( (byte)(messageId >>> 48) );
            ptr[index    ] = unchecked( (byte)(messageId >>> 56) );
        }

        index += 8;

        ptr[index] = subMessageId;
        return index + 1;
    }
    private static uint GetExceptionSize(Exception ex, IRpcSerializer serializer)
    {
        uint size = (uint)(serializer.GetSize(TypeUtility.GetAssemblyQualifiedNameNoVersion(ex.GetType()))
                           + serializer.GetSize(ex.Message ?? string.Empty)
                           + serializer.GetSize(ex.StackTrace ?? string.Empty));

        int exSz = 0;

        switch (ex)
        {
            case AggregateException agg:
                foreach (Exception ex2 in agg.InnerExceptions)
                    size += GetExceptionSize(ex2, serializer);
                exSz = agg.InnerExceptions.Count;
                break;
            case ReflectionTypeLoadException rtl:
                foreach (Exception? ex2 in rtl.LoaderExceptions)
                    size += GetExceptionSize(ex2!, serializer);
                exSz = rtl.LoaderExceptions.Length;
                break;
            default:
                if (ex.InnerException != null)
                {
                    size += GetExceptionSize(ex.InnerException, serializer);
                    exSz = 1;
                }
                break;
        }

        size += serializer.CanFastReadPrimitives ? sizeof(int) : (uint)serializer.GetSize(exSz);
        return size;
    }
    private static unsafe RpcInvocationException ReadException(IModularRpcRemoteConnection connection, IRpcInvocationPoint? rpc, byte* ptr, uint size, ref uint index, IRpcSerializer serializer)
    {
        int exSz;
        int bytesRead;
        if (serializer.CanFastReadPrimitives)
        {
            if (size - index < 4)
                throw new RpcParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            exSz = BitConverter.IsLittleEndian
                ? Unsafe.ReadUnaligned<int>(ptr + index)
                : ptr[index] << 24 | ptr[index + 1] << 16 | ptr[index + 2] << 8 | ptr[index + 3];
            index += sizeof(int);
        }
        else
        {
            exSz = serializer.ReadObject<int>(ptr + index, size - index, out bytesRead);
            index += (uint)bytesRead;
        }

        string? typeName = serializer.ReadObject<string>(ptr + index, size - index, out bytesRead);
        index += (uint)bytesRead;
        string? message = serializer.ReadObject<string>(ptr + index, size - index, out bytesRead);
        index += (uint)bytesRead;
        string? stackTrace = serializer.ReadObject<string>(ptr + index, size - index, out bytesRead);
        index += (uint)bytesRead;

        object exType = typeName != null ? (object?)Type.GetType(typeName, false, false) ?? typeName : string.Empty;

        if (exSz == 0)
            return new RpcInvocationException(connection, rpc, exType, message, stackTrace, null, null);
        
        if (exSz == 1)
        {
            RpcInvocationException innerEx = ReadException(connection, rpc, ptr + index, size - index, ref index, serializer);
            return new RpcInvocationException(connection, rpc, exType, message, stackTrace, innerEx, null);
        }

        RpcInvocationException[] inners = new RpcInvocationException[exSz];
        for (int i = 0; i < exSz; ++i)
            inners[i] = ReadException(connection, rpc, ptr + index, size - index, ref index, serializer);

        return new RpcInvocationException(connection, rpc, exType, message, stackTrace, null, inners);
    }
    private static RpcInvocationException ReadException(IModularRpcRemoteConnection connection, IRpcInvocationPoint? rpc, Stream stream, IRpcSerializer serializer)
    {
        int exSz = serializer.ReadObject<int>(stream, out _);
        string? typeName = serializer.ReadObject<string>(stream, out _);
        string? message = serializer.ReadObject<string>(stream, out _);
        string? stackTrace = serializer.ReadObject<string>(stream, out _);

        object exType = typeName != null ? (object?)Type.GetType(typeName, false, false) ?? typeName : string.Empty;

        if (exSz == 0)
            return new RpcInvocationException(connection, rpc, exType, message, stackTrace, null, null);

        if (exSz == 1)
        {
            RpcInvocationException innerEx = ReadException(connection, rpc, stream, serializer);
            return new RpcInvocationException(connection, rpc, exType, message, stackTrace, innerEx, null);
        }

        RpcInvocationException[] inners = new RpcInvocationException[exSz];
        for (int i = 0; i < exSz; ++i)
            inners[i] = ReadException(connection, rpc, stream, serializer);

        return new RpcInvocationException(connection, rpc, exType, message, stackTrace, null, inners);
    }
    private static unsafe void WriteException(Exception ex, byte* ptr, uint size, ref uint index, IRpcSerializer serializer)
    {
        string typeName = TypeUtility.GetAssemblyQualifiedNameNoVersion(ex.GetType());

        int exSz = ex switch
        {
            AggregateException agg => agg.InnerExceptions.Count,
            ReflectionTypeLoadException rtl => rtl.LoaderExceptions.Length,
            _ => ex.InnerException != null ? 1 : 0
        };

        if (serializer.CanFastReadPrimitives)
        {
            if (size - index < sizeof(int))
                throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ptr + index, exSz);
            }
            else
            {
                ptr[index + 3] = unchecked((byte)exSz);
                ptr[index + 2] = unchecked((byte)(exSz >>> 8));
                ptr[index + 1] = unchecked((byte)(exSz >>> 16));
                ptr[index] = unchecked((byte)(exSz >>> 24));
            }

            index += sizeof(int);
        }
        else
        {
            index += (uint)serializer.WriteObject(exSz, ptr + index, size - index);
        }

        index += (uint)serializer.WriteObject(typeName, ptr + index, size - index);
        index += (uint)serializer.WriteObject(ex.Message ?? string.Empty, ptr + index, size - index);
        index += (uint)serializer.WriteObject(ex.StackTrace ?? string.Empty, ptr + index, size - index);

        switch (ex)
        {
            case AggregateException agg:
                foreach (Exception ex2 in agg.InnerExceptions)
                    WriteException(ex2, ptr, size, ref index, serializer);
                break;
            case ReflectionTypeLoadException rtl:
                foreach (Exception? ex2 in rtl.LoaderExceptions)
                    WriteException(ex2!, ptr, size, ref index, serializer);
                break;
            default:
                if (ex.InnerException != null)
                    WriteException(ex.InnerException, ptr, size, ref index, serializer);
                break;
        }
    }
    public void Dispose()
    {
        try
        {
            while (_pendingCancellableMessages.Count > 0)
            {
                UniqueMessageKey[] keys = _pendingCancellableMessages.Keys.ToArray();
                foreach (UniqueMessageKey key in keys)
                {
                    if (!_pendingCancellableMessages.TryRemove(key, out CancellationTokenSource source))
                        continue;

                    source.Cancel();
                    source.Dispose();
                }
            }

            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
        }
        catch (ObjectDisposedException) { }
    }
}