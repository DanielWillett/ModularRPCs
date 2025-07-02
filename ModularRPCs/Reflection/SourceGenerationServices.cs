using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Utilities used by source generators.
/// </summary>
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
}