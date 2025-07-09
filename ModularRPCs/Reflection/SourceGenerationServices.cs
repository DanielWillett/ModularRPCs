using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Utilities used by code emitted from source generators.
/// </summary>
/// <remarks>Everything in here is an internal API and is subject to change, and therefore should not be used by user code.</remarks>
[EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
public static class SourceGenerationServices
{
    [UsedImplicitly] public delegate ref RpcCallMethodInfo GetCallInfo();
    [UsedImplicitly] public delegate RpcCallMethodInfo GetCallInfoByVal();

    [UsedImplicitly] public static string ResxRpcInjectionExceptionInstanceNull => Properties.Exceptions.RpcInjectionExceptionInstanceNull;
    [UsedImplicitly] public static string ResxRpcParseExceptionBufferRunOutFastRead => Properties.Exceptions.RpcParseExceptionBufferRunOutFastRead;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionMultipleServiceProviders => Properties.Exceptions.RpcInjectionExceptionMultipleServiceProviders;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionInfo => Properties.Exceptions.RpcInjectionExceptionInfo;
    [UsedImplicitly] public static string ResxRpcParseExceptionBufferRunOutNativeIntOverflow => Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionMultipleCanTakeOwnership => Properties.Exceptions.RpcInjectionExceptionMultipleCanTakeOwnership;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionMultipleByteCount => Properties.Exceptions.RpcInjectionExceptionMultipleByteCount;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionMultipleByteData => Properties.Exceptions.RpcInjectionExceptionMultipleByteData;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionInvalidRawParameter => Properties.Exceptions.RpcInjectionExceptionInvalidRawParameter;
    [UsedImplicitly] public static string ResxInstanceIdDefaultValue => Properties.Exceptions.InstanceIdDefaultValue;
    [UsedImplicitly] public static string ResxInstanceWithThisIdAlreadyExists => Properties.Exceptions.InstanceWithThisIdAlreadyExists;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionNoByteCount => Properties.Exceptions.RpcInjectionExceptionNoByteCount;
    [UsedImplicitly] public static string ResxRpcInjectionExceptionNoByteData => Properties.Exceptions.RpcInjectionExceptionNoByteData;

    [UsedImplicitly]
    public static MethodInfo GetMethodByExpression<TObject, TDelegate>(Expression<Func<TObject, TDelegate>> expr)
    {
        return expr.Body is UnaryExpression { Operand: MethodCallExpression { Object: ConstantExpression { Value: MethodInfo method } } }
            ? method
            : throw new MemberAccessException("Failed to parse method expression.");
    }

    [UsedImplicitly]
    public static MethodInfo GetMethodByDelegate(Delegate d)
    {
        return d?.Method ?? throw new MemberAccessException();
    }

    [UsedImplicitly]
    public abstract class GeneratedClosure
    {
        [UsedImplicitly] protected readonly RpcOverhead Overhead;
        [UsedImplicitly] protected readonly IRpcRouter Router;
        [UsedImplicitly] protected readonly IRpcSerializer Serializer;

        [UsedImplicitly]
        protected GeneratedClosure(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer)
        {
            Overhead = overhead;
            Router = router;
            Serializer = serializer;
        }
    }

    [UsedImplicitly]
    public static byte[] GetArrayFromMemory(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        if (mem.Length == 0)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        if (!MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> arr))
        {
            canTakeOwnership = true;
            return mem.ToArray();
        }

        if (arr.Count == 0 || arr.Array == null)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        if (arr.Offset == 0 && arr.Count == arr.Array.Length)
        {
            canTakeOwnership = couldTakeOwnership;
            return arr.Array;
        }

        byte[] newArray = new byte[arr.Count];
        Buffer.BlockCopy(arr.Array, arr.Offset, newArray, 0, newArray.Length);
        canTakeOwnership = true;
        return newArray;
    }

    [UsedImplicitly]
    public static ArraySegment<byte> GetArraySegmentFromMemory(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        if (MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> arr))
        {
            canTakeOwnership = couldTakeOwnership;
        }
        else
        {
            arr = new ArraySegment<byte>(mem.ToArray());
            canTakeOwnership = true;
        }

        return arr.Array == null ? new ArraySegment<byte>(Array.Empty<byte>()) : arr;
    }

    [UsedImplicitly]
    public static IList<byte> GetIListFromMemory(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        if (mem.Length == 0)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        if (!MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> arr))
        {
            canTakeOwnership = true;
            return mem.ToArray();
        }

        if (arr.Count == 0 || arr.Array == null)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        canTakeOwnership = couldTakeOwnership;
        if (arr.Offset == 0 && arr.Count == arr.Array.Length)
            return arr.Array;

        return new ArraySegment<byte>(arr.Array, arr.Offset, arr.Count);
    }

    [UsedImplicitly]
    public static IReadOnlyList<byte> GetIReadOnlyListFromMemory(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        if (mem.Length == 0)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        if (!MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> arr))
        {
            canTakeOwnership = true;
            return mem.ToArray();
        }

        if (arr.Count == 0 || arr.Array == null)
        {
            canTakeOwnership = true;
            return Array.Empty<byte>();
        }

        canTakeOwnership = couldTakeOwnership;
        if (arr.Offset == 0 && arr.Count == arr.Array.Length)
            return arr.Array;

        return new ArraySegment<byte>(arr.Array, arr.Offset, arr.Count);
    }

    [UsedImplicitly]
    public static List<byte> GetListFromMemory(ReadOnlyMemory<byte> mem, bool couldTakeOwnership, out bool canTakeOwnership)
    {
        ArraySegment<byte> arr = GetArraySegmentFromMemory(mem, couldTakeOwnership, out canTakeOwnership);
        List<byte> list = new List<byte>(0);
        if (arr.Count == 0 || arr.Array == null)
        {
            canTakeOwnership = true;
            return list;
        }

        if (arr.Offset == 0 && arr.Count == arr.Array.Length)
        {
            if (list.TrySetUnderlyingArray(arr.Array, arr.Array.Length))
                return list;

            list.AddRange(arr.Array);
            canTakeOwnership = true;

            return list;
        }

        byte[] newArray = new byte[arr.Count];
        Buffer.BlockCopy(arr.Array, arr.Offset, newArray, 0, newArray.Length);

        if (!list.TrySetUnderlyingArray(newArray, newArray.Length))
        {
            list.AddRange(newArray);
        }
        canTakeOwnership = true;
        return list;
    }

    [UsedImplicitly]
    public static ArrayList GetArrayListFromMemory(ReadOnlyMemory<byte> mem)
    {
        object?[] boxxedPrimtives = new object?[byte.MaxValue];
        ReadOnlySpan<byte> span = mem.Span;

        ArrayList arrayList = new ArrayList(mem.Length);
        for (int i = 0; i < span.Length; ++i)
        {
            byte b = span[i];
            object obj = boxxedPrimtives[b] ??= b;
            arrayList.Add(obj);
        }

        return arrayList;
    }

    [UsedImplicitly]
    public static ArrayList GetArrayListFromStream(uint size, Stream stream)
    {
        object?[] boxxedPrimtives = new object?[byte.MaxValue];

        const int bufferSize = 4096;

        ArrayList arrayList;
        if (size <= bufferSize)
        {
            byte[] bytes = GetArrayFromStream(size, stream);
            arrayList = new ArrayList(bytes.Length);
            foreach (byte b in bytes)
            {
                arrayList.Add(boxxedPrimtives[b] ??= b);
            }
        }
        else
        {
            int bytesLeft = checked((int)size);
            arrayList = new ArrayList(bytesLeft);
            byte[] buffer = new byte[bufferSize];
            do
            {
                int read = stream.Read(buffer, 0, Math.Min(bytesLeft, bufferSize));
                if (read == 0)
                {
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
                }

                bytesLeft -= read;

                for (int i = 0; i < read; ++i)
                {
                    byte b = buffer[i];
                    arrayList.Add(boxxedPrimtives[b] ??= b);
                }

            } while (bytesLeft > 0);
        }

        return arrayList;
    }

    [UsedImplicitly]
    public static byte[] GetArrayFromStream(uint size, Stream stream)
    {
        if (size <= 0)
        {
            return Array.Empty<byte>();
        }

        byte[] newArray = new byte[size];
        int sizeActuallyRead = stream.Read(newArray, 0, newArray.Length);

        if ((uint)sizeActuallyRead < size)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };

        return newArray;
    }

    [UsedImplicitly]
    public static List<byte> GetListFromStream(uint size, Stream stream)
    {
        byte[] arr = GetArrayFromStream(size, stream);
        List<byte> list = new List<byte>(0);
        if (arr.Length == 0 || list.TrySetUnderlyingArray(arr, arr.Length))
            return list;

        list.AddRange(arr);
        return list;
    }

    [UsedImplicitly]
    public static RpcTask InvokeRpcInvokerByStream(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, Stream stream, uint byteCt, byte[] hdr, bool leaveOpen, ref RpcCallMethodInfo callInfo)
    {
        return router.InvokeRpc(connections, serializer, method, token, new ArraySegment<byte>(hdr), stream, leaveOpen, byteCt, ref callInfo);
    }

    [UsedImplicitly]
    public static RpcTask InvokeRpcInvokerByByteWriter(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, object writerBox, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        ByteWriter writer = (ByteWriter)writerBox;
        if (writer.Stream != null)
            throw new NotSupportedException(Properties.Exceptions.WriterStreamModeNotSupported);

        if (byteCt == uint.MaxValue)
            byteCt = checked ( (uint)writer.Count );

        return InvokeRpcInvokerByArraySegment(router, connections, serializer, method, token, writer.ToArraySegmentAndDontFlush(), byteCt, hdr, ref callInfo);
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByByteReader(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, object readerBox, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        ByteReader reader = (ByteReader)readerBox;
        if (reader.Stream != null)
        {
            if (byteCt == uint.MaxValue)
            {
                byteCt = checked((uint)(reader.Stream.Length - reader.Stream.Position));
            }

            return router.InvokeRpc(connections, serializer, method, token, new ArraySegment<byte>(hdr), reader.Stream, true, byteCt, ref callInfo);
        }

        if (reader.Data.Array == null)
        {
            throw new ArgumentException(Properties.Exceptions.NoDataLoadedRaw, nameof(byteCt));
        }

        if (byteCt == uint.MaxValue)
        {
            byteCt = (uint)reader.BytesLeft;
        }
        else if (reader.BytesLeft < byteCt)
        {
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));
        }

        byte[] newArr = new byte[byteCt + hdr.Length];
        reader.ReadBlockTo(newArr.AsSpan(hdr.Length));
        Buffer.BlockCopy(hdr, 0, newArr, 0, hdr.Length);
        fixed (byte* ptr = newArr)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt + hdr.Length, byteCt, ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByPointer(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, byte* bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (byteCt < hdr.Length)
            throw new RpcOverflowException(Properties.Exceptions.RawOverflow) { ErrorCode = 6 };

        fixed (byte* ptr = hdr)
        {
            Buffer.MemoryCopy(ptr, bytes, byteCt, hdr.Length);
        }

        return router.InvokeRpc(connections, serializer, method, token, bytes, (int)byteCt, (uint)(byteCt - hdr.Length), ref callInfo);
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByReference(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, ref byte bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (byteCt < hdr.Length)
            throw new RpcOverflowException(Properties.Exceptions.RawOverflow) { ErrorCode = 6 };

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Span<byte> span = MemoryMarshal.CreateSpan(ref bytes, (int)byteCt);
        hdr.AsSpan().CopyTo(span);

        fixed (byte* ptr = span)
#else
        Unsafe.CopyBlockUnaligned(ref bytes, ref hdr[0], (uint)hdr.Length);

        fixed (byte* ptr = &bytes)
#endif
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt, (uint)(byteCt - hdr.Length), ref callInfo);
        }
    }

    [UsedImplicitly]
    public static RpcTask InvokeRpcInvokerByMemory(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, Memory<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
        => InvokeRpcInvokerBySpan(router, connections, serializer, method, token, bytes.Span, byteCt, hdr, ref callInfo);

    [UsedImplicitly]
    public static RpcTask InvokeRpcInvokerByReadOnlyMemory(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, ReadOnlyMemory<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
        => InvokeRpcInvokerByReadOnlySpan(router, connections, serializer, method, token, bytes.Span, byteCt, hdr, ref callInfo);

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerBySpan(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, Span<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (bytes.Length < hdr.Length || byteCt < hdr.Length)
            throw new RpcOverflowException(Properties.Exceptions.RawOverflow) { ErrorCode = 6 };

        if (byteCt > bytes.Length)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        hdr.AsSpan().CopyTo(bytes.Slice(0, hdr.Length));
        fixed (byte* ptr = bytes)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt, (uint)(byteCt - hdr.Length), ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByReadOnlySpan(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, ReadOnlySpan<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (byteCt > bytes.Length)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        byte[] ttlArray = new byte[byteCt + (uint)hdr.Length];
        Buffer.BlockCopy(hdr, 0, ttlArray, 0, hdr.Length);
        bytes.Slice(0, checked((int)byteCt)).CopyTo(ttlArray.AsSpan(hdr.Length));
        fixed (byte* ptr = ttlArray)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt + hdr.Length, byteCt, ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByArray(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, byte[] bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (bytes.Length < hdr.Length || byteCt < hdr.Length)
            throw new RpcOverflowException(Properties.Exceptions.RawOverflow) { ErrorCode = 6 };

        if (byteCt > bytes.Length)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        Buffer.BlockCopy(hdr, 0, bytes, 0, hdr.Length);
        fixed (byte* ptr = bytes)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt, (uint)(byteCt - hdr.Length), ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByArraySegment(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, ArraySegment<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (bytes.Array == null || bytes.Count < hdr.Length || byteCt < hdr.Length)
            throw new RpcOverflowException(Properties.Exceptions.RawOverflow) { ErrorCode = 6 };

        if (byteCt > bytes.Count)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        Buffer.BlockCopy(hdr, 0, bytes.Array, bytes.Offset, hdr.Length);
        fixed (byte* ptr = &bytes.Array[bytes.Offset])
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt, (uint)(byteCt - hdr.Length), ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByCollection(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, ICollection<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (bytes is List<byte> list)
        {
            byte[]? arr;
            try
            {
                arr = list.GetUnderlyingArray();
            }
            catch
            {
                arr = null;
            }

            if (arr != null)
            {
                return InvokeRpcInvokerByArraySegment(router, connections, serializer, method, token, new ArraySegment<byte>(arr, 0, list.Count), byteCt, hdr, ref callInfo);
            }
        }

        if (byteCt > bytes.Count)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        byte[] ttlArray = new byte[byteCt];
        Buffer.BlockCopy(hdr, 0, ttlArray, 0, hdr.Length);
        bytes.CopyTo(ttlArray, hdr.Length);
        fixed (byte* ptr = ttlArray)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt + hdr.Length, byteCt, ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByReadOnlyCollection(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, IReadOnlyCollection<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (bytes is ICollection<byte> c)
        {
            return InvokeRpcInvokerByCollection(router, connections, serializer, method, token, c, byteCt, hdr, ref callInfo);
        }

        if (byteCt > bytes.Count)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        byte[] ttlArray = new byte[byteCt];
        Buffer.BlockCopy(hdr, 0, ttlArray, 0, hdr.Length);
        int index = hdr.Length;
        foreach (byte b in bytes)
        {
            ttlArray[index] = b;
            ++index;
            if (index >= byteCt)
                break;
        }

        fixed (byte* ptr = ttlArray)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt + hdr.Length, byteCt, ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByEnumerable(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, IEnumerable<byte> bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        byte[] ttlArray;
        if (byteCt == uint.MaxValue)
        {
            ttlArray = hdr.Concat(bytes).ToArray();
            fixed (byte* ptr = ttlArray)
            {
                return router.InvokeRpc(connections, serializer, method, token, ptr, ttlArray.Length, (uint)(ttlArray.Length - hdr.Length), ref callInfo);
            }
        }

        ttlArray = new byte[byteCt];
        Buffer.BlockCopy(hdr, 0, ttlArray, 0, hdr.Length);
        int index = hdr.Length - 1;
        foreach (byte b in bytes)
        {
            ttlArray[++index] = b;
        }

        fixed (byte* ptr = ttlArray)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt + hdr.Length, byteCt, ref callInfo);
        }
    }

    [UsedImplicitly]
    public static unsafe RpcTask InvokeRpcInvokerByArrayList(IRpcRouter router, object? connections, IRpcSerializer serializer, RuntimeMethodHandle method, CancellationToken token, ArrayList bytes, uint byteCt, byte[] hdr, ref RpcCallMethodInfo callInfo)
    {
        if (byteCt > bytes.Count)
            throw new ArgumentException(Properties.Exceptions.ByteCountTooLargeRaw, nameof(byteCt));

        byte[] ttlArray = new byte[byteCt];
        Buffer.BlockCopy(hdr, 0, ttlArray, 0, hdr.Length);
        int index = hdr.Length;
        foreach (byte obj in bytes!)
        {
            ttlArray[index] = obj;
            ++index;
            if (index >= byteCt)
                break;
        }

        fixed (byte* ptr = ttlArray)
        {
            return router.InvokeRpc(connections, serializer, method, token, ptr, (int)byteCt + hdr.Length, byteCt, ref callInfo);
        }
    }
}