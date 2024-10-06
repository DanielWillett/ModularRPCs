using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Async;
public class RpcTask<T> : RpcTask
{
    internal T? ResultIntl;

    /// <summary>
    /// This property will always throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <remarks>Mainly used as the default body for RPC callers.</remarks>
    /// <exception cref="NotImplementedException"/>
    public new static RpcTask<T> NotImplemented => throw new NotImplementedException(Properties.Exceptions.RpcNotImplemented);

    /// <summary>
    /// The value of the task. Will throw a <see cref="RpcGetResultUsageException"/> if the task hasn't completed yet.
    /// </summary>
    /// <exception cref="RpcGetResultUsageException">Task has yet to complete.</exception>
    public T Result => ((RpcTaskAwaiter<T>)Awaiter).GetResult();
    internal RpcTask() : base(false)
    {
        Awaiter = new RpcTaskAwaiter<T>(this, false);
    }
    internal RpcTask(T value) : base(false)
    {
        Awaiter = new RpcTaskAwaiter<T>(this, true);
        ResultIntl = value;
    }
    public new RpcTaskAwaiter<T> GetAwaiter() => (RpcTaskAwaiter<T>)Awaiter;
    internal void TriggerComplete(Exception? exception, T value)
    {
        Exception = exception;
        Awaiter.TriggerComplete();
    }

    protected internal override bool TrySetResult(object? value)
    {
        if (value is not T convValue)
        {
            // this gets optimized away
            if (typeof(T).IsEnum)
            {
                if (typeof(T).GetEnumUnderlyingType() == typeof(int))
                {
                    int v = value is int a ? a : Convert.ToInt32(value);
                    ResultIntl = Unsafe.As<int, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
                {
                    uint v = value is uint a ? a : Convert.ToUInt32(value);
                    ResultIntl = Unsafe.As<uint, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
                {
                    byte v = value is byte a ? a : Convert.ToByte(value);
                    ResultIntl = Unsafe.As<byte, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
                {
                    sbyte v = value is sbyte a ? a : Convert.ToSByte(value);
                    ResultIntl = Unsafe.As<sbyte, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(short))
                {
                    short v = value is short a ? a : Convert.ToInt16(value);
                    ResultIntl = Unsafe.As<short, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
                {
                    ushort v = value is ushort a ? a : Convert.ToUInt16(value);
                    ResultIntl = Unsafe.As<ushort, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(long))
                {
                    long v = value is long a ? a : Convert.ToInt64(value);
                    ResultIntl = Unsafe.As<long, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
                {
                    ulong v = value is ulong a ? a : Convert.ToUInt64(value);
                    ResultIntl = Unsafe.As<ulong, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
                {
                    nint v = value is nint a ? a : (nint)Convert.ToInt64(value);
                    ResultIntl = Unsafe.As<nint, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
                {
                    nuint v = value is nuint a ? a : (nuint)Convert.ToUInt64(value);
                    ResultIntl = Unsafe.As<nuint, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(char))
                {
                    char v = value is char a ? a : Convert.ToChar(value);
                    ResultIntl = Unsafe.As<char, T>(ref v);
                    return true;
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
                {
                    bool v = value is bool a ? a : Convert.ToBoolean(value);
                    ResultIntl = Unsafe.As<bool, T>(ref v);
                    return true;
                }
            }

            try
            {
                ResultIntl = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                /* ignored */
            }
            catch (Exception ex)
            {
                TriggerComplete(ex);
            }

            return false;
        }

        ResultIntl = convValue;
        return true;
    }
}