using DanielWillett.ModularRpcs.Exceptions;
using JetBrains.Annotations;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using DanielWillett.ModularRpcs.Data;

namespace DanielWillett.ModularRpcs.Async;

/// <summary>
/// Represents a pending remote RPC invocation with a return value of type <typeparamref name="T"/>. Void-returning methods should use <see cref="RpcTask"/> instead.
/// </summary>
/// <typeparam name="T">The type of value to be returned.</typeparam>
public class RpcTask<T> : RpcTask
{
    /// <summary>
    /// The result value of this task.
    /// </summary>
    protected internal T? ResultIntl;

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

    /// <inheritdoc />
    public override Type ValueType => typeof(T);

    internal RpcTask() : base(false)
    {
        Awaiter = new RpcTaskAwaiter<T>(this, false);
    }
    internal RpcTask(T value) : base(false)
    {
        Awaiter = new RpcTaskAwaiter<T>(this, true);
        ResultIntl = value;
    }

    /// <summary>
    /// Get the awaiter object for this task used by <see langword="async"/> method builders to queue continuations.
    /// </summary>
    [Pure]
    public new RpcTaskAwaiter<T> GetAwaiter() => (RpcTaskAwaiter<T>)Awaiter;

    /// <summary>
    /// Configures this task to not continue the current <see langword="async"/> method on the current <see cref="SynchronizationContext"/>, if supported by the runtime..
    /// </summary>
    /// <param name="continueOnCapturedContext">Whether or not the current <see langword="async"/> method will continue on the current <see cref="SynchronizationContext"/>, if supported by the runtime.</param>
    /// <returns>A configured <see cref="RpcTask{T}"/>.</returns>
    [Pure]
    public new ConfiguredRpcTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
    {
        return new ConfiguredRpcTaskAwaitable<T>((RpcTaskAwaiter<T>)Awaiter, continueOnCapturedContext);
    }

    internal void TriggerComplete(Exception? exception, T value)
    {
        Exception = exception;
        Awaiter.TriggerComplete();
    }

    protected internal override bool TrySetResult(object? value)
    {
        if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is { } underlyingType)
        {
            if (value == null)
            {
                ResultIntl = default;
                return true;
            }

            if (!underlyingType.IsInstanceOfType(value))
            {
                return false;
            }

            ResultIntl = (T)value;
            return true;
        }

        if (value is not T convValue)
        {
            if (value == null && !typeof(T).IsValueType)
            {
                ResultIntl = default;
                return true;
            }

#if NET8_0_OR_GREATER
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

#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                {
                    bool v = value is bool a ? a : Convert.ToBoolean(value);
                    ResultIntl = Unsafe.As<bool, T>(ref v);
                    return true;
                }

                case TypeCode.Char:
                {
                    char v = value is char a ? a : Convert.ToChar(value);
                    ResultIntl = Unsafe.As<char, T>(ref v);
                    return true;
                }

                case TypeCode.SByte:
                {
                    sbyte v = value is sbyte a ? a : Convert.ToSByte(value);
                    ResultIntl = Unsafe.As<sbyte, T>(ref v);
                    return true;
                }

                case TypeCode.Byte:
                {
                    byte v = value is byte a ? a : Convert.ToByte(value);
                    ResultIntl = Unsafe.As<byte, T>(ref v);
                    return true;
                }

                case TypeCode.Int16:
                {
                    short v = value is short a ? a : Convert.ToInt16(value);
                    ResultIntl = Unsafe.As<short, T>(ref v);
                    return true;
                }

                case TypeCode.UInt16:
                {
                    ushort v = value is ushort a ? a : Convert.ToUInt16(value);
                    ResultIntl = Unsafe.As<ushort, T>(ref v);
                    return true;
                }

                case TypeCode.Int32:
                {
                    int v = value is int a ? a : Convert.ToInt32(value);
                    ResultIntl = Unsafe.As<int, T>(ref v);
                    return true;
                }

                case TypeCode.UInt32:
                {
                    uint v = value is uint a ? a : Convert.ToUInt32(value);
                    ResultIntl = Unsafe.As<uint, T>(ref v);
                    return true;
                }
                case TypeCode.Int64:
                {
                    long v = value is long a ? a : Convert.ToInt64(value);
                    ResultIntl = Unsafe.As<long, T>(ref v);
                    return true;
                }

                case TypeCode.UInt64:
                {
                    ulong v = value is ulong a ? a : Convert.ToUInt64(value);
                    ResultIntl = Unsafe.As<ulong, T>(ref v);
                    return true;
                }

                case LegacyEnumCache<T>.NativeInt:
                {
                    nint v = value is nint a ? a : (nint)Convert.ToInt64(value);
                    ResultIntl = Unsafe.As<nint, T>(ref v);
                    return true;
                }

                case LegacyEnumCache<T>.NativeUInt:
                {
                    nuint v = value is nuint a ? a : (nuint)Convert.ToUInt64(value);
                    ResultIntl = Unsafe.As<nuint, T>(ref v);
                    return true;
                }
            }
#endif

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