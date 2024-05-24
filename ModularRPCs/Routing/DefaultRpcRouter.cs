using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes.Formatting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Routing;
public class DefaultRpcRouter : IRpcRouter, IDisposable
{
    private readonly IRpcSerializer _defaultSerializer;
    private readonly IRpcConnectionLifetime _connectionLifetime;
    private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
    private long _lastMsgId;
    private readonly ConcurrentDictionary<ulong, RpcTask> _listeningTasks = new ConcurrentDictionary<ulong, RpcTask>();

    /// <summary>
    /// A dictionary of unique IDs to invocation points.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, IRpcInvocationPoint> CachedDescriptors = new ConcurrentDictionary<uint, IRpcInvocationPoint>();

    protected internal const byte OvhCodeIdVoidRtnSuccess = 4;
    protected internal const byte OvhCodeIdValueRtnSuccess = 3;
    protected internal const byte OvhCodeIdException = 2;

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
        int size = 20;
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
        int ct = RpcOverhead.WriteToBytes(serializer, this, sourceMethodHandle, ref callMethodInfo, bytes + 1, byteCt - 1, dataCt, messageId, subMsgId);
        bytes[0] = RpcOverhead.OvhCodeId;
        return ct + 1;
    }
    protected virtual async ValueTask InvokeInvocationPoint(IRpcInvocationPoint rpc, RpcOverhead overhead, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        try
        {
            ValueTask vt = rpc.Invoke(overhead, serializer, stream, token);
            await vt;

            if ((overhead.Flags & RpcFlags.FireAndForget) != 0 || overhead.SendingConnection == null)
                return;

            try
            {
                await HandleValueTaskReply(vt, overhead);
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
    }
    protected virtual ValueTask InvokeInvocationPoint(IRpcInvocationPoint rpc, RpcOverhead overhead, IRpcSerializer serializer, ReadOnlySpan<byte> bytes, CancellationToken token = default)
    {
        try
        {
            ValueTask vt = rpc.Invoke(overhead, serializer, bytes, token);
            if (!vt.IsCompleted)
                return new ValueTask(FinishVtTask(vt, overhead, serializer));

            if ((overhead.Flags & RpcFlags.FireAndForget) != 0 || overhead.SendingConnection == null)
                return default;

            try
            {
                return HandleValueTaskReply(vt, overhead);
            }
            catch (Exception ex2)
            {
                HandleInvokeException(overhead, ex2);
            }

            return default;
        }
        catch (Exception ex)
        {
            if (overhead.SendingConnection != null)
            {
                try
                {
                    ValueTask vt = ReplyRpcException(overhead.MessageId, checked((byte)(overhead.SubMessageId + 1)), overhead.SendingConnection, ex, serializer);
                    return vt.IsCompleted ? default : new ValueTask(FinishExVtTask(vt, overhead));
                }
                catch (Exception ex2)
                {
                    HandleInvokeException(overhead, ex2);
                }
            }

            HandleInvokeException(overhead, ex);
            return default;
        }

        async Task FinishVtTask(ValueTask vt, RpcOverhead overhead, IRpcSerializer serializer)
        {
            try
            {
                await vt;
                
                if ((overhead.Flags & RpcFlags.FireAndForget) != 0 || overhead.SendingConnection == null)
                    return;

                try
                {
                    await HandleValueTaskReply(vt, overhead);
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
                        await ReplyRpcException(overhead.MessageId, checked((byte)(overhead.SubMessageId + 1)), overhead.SendingConnection, ex, serializer);
                    }
                    catch (Exception ex2)
                    {
                        HandleInvokeException(overhead, ex2);
                    }
                }

                HandleInvokeException(overhead, ex);
            }
        }
        async Task FinishExVtTask(ValueTask vt, RpcOverhead overhead)
        {
            try
            {
                await vt;
            }
            catch (Exception ex)
            {
                HandleInvokeException(overhead, ex);
            }
        }
    }
    public virtual unsafe ValueTask ReceiveData(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token = default)
    {
        if (rawData.Length <= 1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        byte codeId = rawData[0];
        switch (codeId)
        {
            case RpcOverhead.OvhCodeId:
                RpcOverhead overhead;
                fixed (byte* ptr = &rawData[1])
                {
                    overhead = RpcOverhead.ReadFromBytes(sendingConnection, serializer, ptr, (uint)rawData.Length - 1);
                }

                return InvokeInvocationPoint(overhead.Rpc, overhead, serializer, rawData.Length == overhead.OverheadSize + 1 ? default : rawData.Slice(overhead.OverheadSize + 1), token);

            case OvhCodeIdException:
                uint index;
                RpcTask? task;
                ulong messageId;
                Exception? ex;
                fixed (byte* ptr = &rawData[1])
                {
                    index = ReadSimpleMessagePrefix(serializer, ptr, rawData.Length - 1, out messageId, out _);
                    _listeningTasks.TryGetValue(messageId, out task);
                    IRpcInvocationPoint? invPt = task?.Endpoint;
                    ex = ReadException(invPt, ptr, (uint)rawData.Length - 1, ref index, serializer);
                }

                task?.TriggerComplete(ex);
                break;

            case OvhCodeIdValueRtnSuccess:
                object? rtn;
                fixed (byte* ptr = &rawData[1])
                {
                    index = ReadSimpleMessagePrefix(serializer, ptr, rawData.Length - 1, out messageId, out _);
                    _listeningTasks.TryGetValue(messageId, out task);
                    rtn = ReadReturnValue(serializer, task, ptr, rawData.Length - 1, ref index);
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

                break;

            case OvhCodeIdVoidRtnSuccess:
                fixed (byte* ptr = &rawData[1])
                {
                    ReadSimpleMessagePrefix(serializer, ptr, rawData.Length - 1, out messageId, out _);
                }

                _listeningTasks.TryGetValue(messageId, out task);
                task?.TriggerComplete(null);
                break;
        }

        return default;
    }
    public virtual ValueTask ReceiveData(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        int codeId = stream.ReadByte();
        if (codeId == -1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        switch (codeId)
        {
            case RpcOverhead.OvhCodeId:
                RpcOverhead overhead = RpcOverhead.ReadFromStream(sendingConnection, serializer, stream);
                return InvokeInvocationPoint(overhead.Rpc, overhead, serializer, stream, token);

            case OvhCodeIdException:
                break;
        }

        return default;
    }
    public void HandleInvokeException(RpcOverhead overhead, Exception ex)
    {
        Console.WriteLine($"Error invoking {overhead.MessageId} {overhead.SubMessageId}:{Environment.NewLine}" + ex);
        // todo
    }
    public unsafe RpcTask InvokeRpc(object? connections, IRpcSerializer serializer, RuntimeMethodHandle sourceMethodHandle, byte* bytes, int byteCt, uint dataCt, ref RpcCallMethodInfo callMethodInfo)
    {
        ulong messageId = GetNewMessageId();
        int ovhSize = (int)((uint)byteCt - dataCt);
        int ovhEnd = WriteOverhead(sourceMethodHandle, serializer, ref callMethodInfo, bytes, ovhSize, dataCt, messageId, 0);
        WriteEndpoint(sourceMethodHandle, serializer, ref callMethodInfo, callMethodInfo.Endpoint.GetEndpoint(), bytes + ovhEnd, ovhSize - ovhEnd);

#if DEBUG
        Console.WriteLine(ByteFormatter.FormatBinary(new ReadOnlySpan<byte>(bytes, byteCt), ByteStringFormat.ColumnLabels));
#endif

        RpcTask? rpcTask = null;
        if (connections == null)
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            if (!_connectionLifetime.IsSingleConnection && !callMethodInfo.IsFireAndForget)
                throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));

            rpcTask = !callMethodInfo.IsFireAndForget
                ? CreateRpcTaskListener(sourceMethodHandle, messageId) 
                : new RpcBroadcastTask(true) { CompleteCount = 1, MessageId = messageId, SubMessageId = 0 };

            int ct = _connectionLifetime.ForEachRemoteConnection(connection =>
            {
                Interlocked.Increment(ref rpcTask.CompleteCount);
                if (rpcTask is RpcBroadcastTask bt)
                    Interlocked.Increment(ref bt.ConnectionCountIntl);
                
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
                ? CreateRpcTaskListener(sourceMethodHandle, messageId)
                : new RpcTask(true) { MessageId = messageId, SubMessageId = 0 };

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

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        if (!callMethodInfo.IsFireAndForget)
            throw new RpcFireAndForgetException(string.Format(Properties.Exceptions.RpcFireAndForgetExceptionMultipleConnections, Accessor.ExceptionFormatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)!)));

        foreach (IModularRpcRemoteConnection connection in remotes)
        {
            rpcTask ??= new RpcBroadcastTask(true) { CompleteCount = 1, MessageId = messageId, SubMessageId = 0 };
            Interlocked.Increment(ref ((RpcBroadcastTask)rpcTask).ConnectionCountIntl);
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
    private ValueTask HandleValueTaskReply(ValueTask valueTask, RpcOverhead overhead)
    {
        Task task = valueTask.AsTask();
        
        if (task == Task.CompletedTask)
            return ReplyRpcVoidSuccessRtn(overhead.MessageId, checked( (byte)(overhead.SubMessageId + 1) ), overhead.SendingConnection!, Serializer);

        Type taskType = task.GetType();
        if (!taskType.IsGenericType)
            return ReplyRpcVoidSuccessRtn(overhead.MessageId, checked( (byte)(overhead.SubMessageId + 1) ), overhead.SendingConnection!, Serializer);
        
        // todo optimize
        MethodInfo? getResultMethod = taskType.GetProperty(nameof(Task<object>.Result), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true);
        object? result = getResultMethod?.Invoke(task, Array.Empty<object>());
        return ReplyRpcValueSuccessRtn(overhead.MessageId, checked( (byte)(overhead.SubMessageId + 1) ), overhead.SendingConnection!, result, Serializer);
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
    private RpcTask CreateRpcTaskListener(RuntimeMethodHandle sourceMethodHandle, ulong messageId)
    {
        MethodInfo method = (MethodBase.GetMethodFromHandle(sourceMethodHandle) as MethodInfo)!;

        RpcTask rpcTask;
        if (method.ReturnType == typeof(void) || method.ReturnType == typeof(RpcTask))
            rpcTask = new RpcTask(false);
        else
            rpcTask = (RpcTask)Activator.CreateInstance(method.ReturnType, nonPublic: true);
        
        rpcTask.MessageId = messageId;
        rpcTask.SubMessageId = 1;
        StartListening(rpcTask, messageId);
        return rpcTask;
    }
    private void StartListening(RpcTask rpcTask, ulong messageId)
    {
        _listeningTasks.TryAdd(messageId, rpcTask);
    }
    private static void HandleInvokeException(RuntimeMethodHandle sourceMethodHandle, Exception ex)
    {
        Console.WriteLine("Error invoking " + Accessor.Formatter.Format(MethodBase.GetMethodFromHandle(sourceMethodHandle)));
    }
    private static unsafe uint ReadSimpleMessagePrefix(IRpcSerializer serializer, byte* ptr, int maxSize, out ulong messageId, out byte subMessageId)
    {
        if (!serializer.CanFastReadPrimitives)
        {
            messageId = serializer.ReadObject<ulong>(ptr, (uint)maxSize, out int bytesRead);
            uint index = (uint)bytesRead;
            subMessageId = serializer.ReadObject<byte>(ptr + bytesRead, (uint)maxSize - index, out bytesRead);
            return index + (uint)bytesRead;
        }

        messageId = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(ptr)
            : ((ulong)((uint)*ptr << 24 | (uint)ptr[1] << 16 | (uint)ptr[2] << 8 | ptr[3]) << 32) | ((uint)ptr[4] << 24 | (uint)ptr[5] << 16 | (uint)ptr[6] << 8 | ptr[7]);

        subMessageId = ptr[8];
        return 9u;
    }
    private static unsafe ValueTask ReplyRpcException(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, Exception ex, IRpcSerializer serializer)
    {
        uint size = GetExceptionSize(ex, serializer);
        size += GetPrefixSize(serializer);

        Span<byte> alloc = size <= ProxyGenerator.Instance.MaxSizeForStackalloc ? stackalloc byte[(int)size] : new byte[size];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, size, OvhCodeIdException, messageId, subMessageId, serializer);
            WriteException(ex, ptr, size, ref index, serializer);
        }

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), CancellationToken.None);
    }
    private static unsafe ValueTask ReplyRpcVoidSuccessRtn(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, IRpcSerializer serializer)
    {
        uint size = GetPrefixSize(serializer);

        Span<byte> alloc = size <= ProxyGenerator.Instance.MaxSizeForStackalloc ? stackalloc byte[(int)size] : new byte[size];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, size, OvhCodeIdVoidRtnSuccess, messageId, subMessageId, serializer);
        }

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), CancellationToken.None);
    }
    private static unsafe ValueTask ReplyRpcValueSuccessRtn(ulong messageId, byte subMessageId, IModularRpcRemoteConnection connection, object? value, IRpcSerializer serializer)
    {
        uint size = GetPrefixSize(serializer);
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
                Type? nullableType = Nullable.GetUnderlyingType(type);
                if (nullableType != null)
                {
                    tc = value switch
                    {
                        null => TypeCode.DBNull,
                        IConvertible c => c.GetTypeCode(),
                        DateTimeOffset => TypeUtility.TypeCodeDateTimeOffset,
                        TimeSpan => TypeUtility.TypeCodeTimeSpan,
                        Guid => TypeUtility.TypeCodeGuid,
                        _ => TypeCode.Object
                    };

                    size += 1u + (!serializer.CanFastReadPrimitives ? (uint)serializer.GetSize(value!) : (uint)TypeUtility.GetTypeCodeSize(tc));
                }
                else
                {
                    size += 1u + (uint)serializer.GetSize((string)value!);
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (hasKnownTypeId = serializer.TryGetKnownTypeId(type, out knownTypeId))
                        size += 5u;
                    else
                        size += 1u + (uint)serializer.GetSize(typeName = TypeUtility.GetAssemblyQualifiedNameNoVersion(type));
                }
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

        Span<byte> alloc = size <= ProxyGenerator.Instance.MaxSizeForStackalloc ? stackalloc byte[(int)size] : new byte[size];

        uint index;
        fixed (byte* ptr = alloc)
        {
            index = WritePrefix(ptr, size, OvhCodeIdValueRtnSuccess, messageId, subMessageId, serializer);

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

        return connection.SendDataAsync(serializer, alloc.Slice(0, (int)index), CancellationToken.None);
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
        }

        index += (uint)bytesRead;
        return rtnValue;
    }

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
    private static uint GetPrefixSize(IRpcSerializer serializer)
    {
        if (serializer.CanFastReadPrimitives)
            return 1 + sizeof(ulong) + sizeof(byte);

        return (uint)(1 + serializer.GetSize(typeof(ulong)) + serializer.GetSize(typeof(byte)));
    }
    private static unsafe uint WritePrefix(byte* ptr, uint size, byte ovhCodeId, ulong messageId, byte subMessageId, IRpcSerializer serializer)
    {
        ptr[0] = ovhCodeId;
        if (!serializer.CanFastReadPrimitives)
        {
            uint index = 1;
            index += (uint)serializer.WriteObject(messageId, ptr + index, size - index);
            index += (uint)serializer.WriteObject(subMessageId, ptr + index, size - index);
            return index;
        }

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ptr + 1, messageId);
        }
        else
        {
            ptr[8] = unchecked((byte)messageId);
            ptr[7] = unchecked((byte)(messageId >>> 8));
            ptr[6] = unchecked((byte)(messageId >>> 16));
            ptr[5] = unchecked((byte)(messageId >>> 24));
            ptr[4] = unchecked((byte)(messageId >>> 32));
            ptr[3] = unchecked((byte)(messageId >>> 40));
            ptr[2] = unchecked((byte)(messageId >>> 48));
            ptr[1] = unchecked((byte)(messageId >>> 56));
        }

        ptr[9] = subMessageId;
        return 10;
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
                foreach (Exception ex2 in rtl.LoaderExceptions)
                    size += GetExceptionSize(ex2, serializer);
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
    private static unsafe RpcInvocationException ReadException(IRpcInvocationPoint? rpc, byte* ptr, uint size, ref uint index, IRpcSerializer serializer)
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

        string typeName = serializer.ReadObject<string>(ptr + index, size - index, out bytesRead);
        index += (uint)bytesRead;
        string message = serializer.ReadObject<string>(ptr + index, size - index, out bytesRead);
        index += (uint)bytesRead;
        string stackTrace = serializer.ReadObject<string>(ptr + index, size - index, out bytesRead);
        index += (uint)bytesRead;

        object exType = (object?)Type.GetType(typeName, false, false) ?? typeName;

        if (exSz == 0)
            return new RpcInvocationException(rpc, exType, message, stackTrace, null, null);
        
        if (exSz == 1)
        {
            RpcInvocationException innerEx = ReadException(rpc, ptr + index, size - index, ref index, serializer);
            return new RpcInvocationException(rpc, exType, message, stackTrace, innerEx, null);
        }

        RpcInvocationException[] inners = new RpcInvocationException[exSz];
        for (int i = 0; i < exSz; ++i)
            inners[i] = ReadException(rpc, ptr + index, size - index, ref index, serializer);

        return new RpcInvocationException(rpc, exType, message, stackTrace, null, inners);
    }
    private static RpcInvocationException ReadException(IRpcInvocationPoint? rpc, Stream stream, ref uint index, IRpcSerializer serializer)
    {
        int exSz = serializer.ReadObject<int>(stream, out int bytesRead);
        index += (uint)bytesRead;
        string typeName = serializer.ReadObject<string>(stream, out bytesRead);
        index += (uint)bytesRead;
        string message = serializer.ReadObject<string>(stream, out bytesRead);
        index += (uint)bytesRead;
        string stackTrace = serializer.ReadObject<string>(stream, out bytesRead);
        index += (uint)bytesRead;

        object exType = (object?)Type.GetType(typeName, false, false) ?? typeName;

        if (exSz == 0)
            return new RpcInvocationException(rpc, exType, message, stackTrace, null, null);

        if (exSz == 1)
        {
            RpcInvocationException innerEx = ReadException(rpc, stream, ref index, serializer);
            return new RpcInvocationException(rpc, exType, message, stackTrace, innerEx, null);
        }

        RpcInvocationException[] inners = new RpcInvocationException[exSz];
        for (int i = 0; i < exSz; ++i)
            inners[i] = ReadException(rpc, stream, ref index, serializer);

        return new RpcInvocationException(rpc, exType, message, stackTrace, null, inners);
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
                foreach (Exception ex2 in rtl.LoaderExceptions)
                    WriteException(ex2, ptr, size, ref index, serializer);
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
            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
        }
        catch (ObjectDisposedException) { }
    }
}