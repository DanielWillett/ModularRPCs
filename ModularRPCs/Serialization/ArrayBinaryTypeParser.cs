using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;
public abstract unsafe class ArrayBinaryTypeParser<T> : IArrayBinaryTypeParser<T> where T : unmanaged
{
    private static readonly Type ArrType = typeof(T[]);
    private static readonly Type ListType = typeof(IList<T>);
    private static readonly Type RoListType = typeof(IReadOnlyList<T>);
    private static readonly Type SpanType = typeof(Span<T>);
    private static readonly Type RoSpanType = typeof(ReadOnlySpan<T>);

    /// <inheritdoc />
    public bool IsVariableSize => true;

    /// <inheritdoc />
    public int MinimumSize => 1;
    public virtual int ElementSize => sizeof(T);
    private int CalcLen(int length)
    {
        byte lenFlag = SerializationHelper.GetLengthFlag(length, false);
        int hdrSize = SerializationHelper.GetHeaderSize(lenFlag);
        return hdrSize + length * ElementSize;
    }

    /// <inheritdoc />
    public int ReadArrayLength(byte* bytes, uint maxSize, out int bytesRead)
    {
        uint index = 0;
        SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this);
        bytesRead = (int)index;
        return length;
    }

    /// <inheritdoc />
    public int ReadArrayLength(Stream stream, out int bytesRead)
    {
        SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this);
        return length;
    }

    /// <inheritdoc />
    public int GetSize(TypedReference value)
    {
        Type t = __reftype(value);
        int len;
        if (t == ArrType)
            len = __refvalue(value, T[]).Length;
        else if (t == ListType)
            len = __refvalue(value, IList<T>).Count;
        else if (t == RoListType)
            len = __refvalue(value, IReadOnlyList<T>).Count;
        else if (t == RoSpanType)
            len = __refvalue(value, ReadOnlySpan<T>).Length;
        else if (t == SpanType)
            len = __refvalue(value, Span<T>).Length;
        else
            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));

        return CalcLen(len);
    }

    /// <inheritdoc />
    public int WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        Type t = __reftype(value);
        if (t == ArrType)
            return WriteObject(__refvalue(value, T[]), bytes, maxSize);
        if (t == ListType)
            return WriteObject(__refvalue(value, IList<T>), bytes, maxSize);
        if (t == RoListType)
            return WriteObject(__refvalue(value, IReadOnlyList<T>), bytes, maxSize);
        if (t == RoSpanType)
            return WriteObject(__refvalue(value, ReadOnlySpan<T>), bytes, maxSize);
        if (t == SpanType)
            return WriteObject(__refvalue(value, Span<T>), bytes, maxSize);

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public int WriteObject(TypedReference value, Stream stream)
    {
        Type t = __reftype(value);
        if (t == ArrType)
            return WriteObject(__refvalue(value, T[]), stream);
        if (t == ListType)
            return WriteObject(__refvalue(value, IList<T>), stream);
        if (t == RoListType)
            return WriteObject(__refvalue(value, IReadOnlyList<T>), stream);
        if (t == RoSpanType)
            return WriteObject(__refvalue(value, ReadOnlySpan<T>), stream);
        if (t == SpanType)
            return WriteObject(__refvalue(value, Span<T>), stream);

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public void ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj)
    {
        Type t = __reftype(outObj);
        if (t == ArrType)
            __refvalue(outObj, T[]?) = ReadObject(bytes, maxSize, out bytesRead);
        else if (t == ListType)
            __refvalue(outObj, IList<T>?) = ReadObject(bytes, maxSize, out bytesRead);
        else if (t == RoListType)
            __refvalue(outObj, IReadOnlyList<T>?) = ReadObject(bytes, maxSize, out bytesRead);
        else if (t == RoSpanType)
            __refvalue(outObj, ReadOnlySpan<T>) = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
        else if (t == SpanType)
        {
            ref Span<T> existingSpan = ref __refvalue(outObj, Span<T>);
            if (!existingSpan.IsEmpty)
            {
                ReadObject(bytes, maxSize, existingSpan, out bytesRead, false);
            }
            else
            {
                existingSpan = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
            }
        }
        else
            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public void ReadObject(Stream stream, out int bytesRead, TypedReference outObj)
    {
        Type t = __reftype(outObj);
        if (t == ArrType)
            __refvalue(outObj, T[]?) = ReadObject(stream, out bytesRead);
        else if (t == ListType)
            __refvalue(outObj, IList<T>?) = ReadObject(stream, out bytesRead);
        else if (t == RoListType)
            __refvalue(outObj, IReadOnlyList<T>?) = ReadObject(stream, out bytesRead);
        else if (t == RoSpanType)
            __refvalue(outObj, ReadOnlySpan<T>) = ReadObject(stream, out bytesRead).AsSpan();
        else if (t == SpanType)
        {
            ref Span<T> existingSpan = ref __refvalue(outObj, Span<T>);
            if (!existingSpan.IsEmpty)
            {
                ReadObject(stream, existingSpan, out bytesRead, false);
            }
            else
            {
                existingSpan = ReadObject(stream, out bytesRead).AsSpan();
            }
        }
        else
            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public int GetSize(object? value)
    {
        int len;
        switch (value)
        {
            case T[] arr:
                len = arr.Length;
                break;
            case IList<T> list:
                len = list.Count;
                break;
            case IReadOnlyList<T> list:
                len = list.Count;
                break;
            case null:
                return 1;
            default:
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())));
        }

        return CalcLen(len);
    }

    /// <inheritdoc />
    public int WriteObject(object? value, byte* bytes, uint maxSize)
    {
        return value switch
        {
            T[] arr => WriteObject(arr, bytes, maxSize),
            IList<T> list => WriteObject(list, bytes, maxSize),
            IReadOnlyList<T> list => WriteObject(list, bytes, maxSize),
            null => WriteObject(null, bytes, maxSize),
            _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
        };
    }

    /// <inheritdoc />
    public int WriteObject(object? value, Stream stream)
    {
        return value switch
        {
            T[] arr => WriteObject(arr, stream),
            IList<T> list => WriteObject(list, stream),
            IReadOnlyList<T> list => WriteObject(list, stream),
            null => WriteObject(null!, stream),
            _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
        };
    }

    /// <inheritdoc />
    public object? ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (type == ArrType || type == ListType || type == RoListType)
            return ReadObject(bytes, maxSize, out bytesRead);

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public object? ReadObject(Type type, Stream stream, out int bytesRead)
    {
        if (type == ArrType || type == ListType || type == RoListType)
            return ReadObject(stream, out bytesRead);

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public int GetSize(T[]? value) => value == null ? 1 : CalcLen(value.Length);

    /// <inheritdoc />
    public int GetSize(IList<T>? value) => value == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize(IReadOnlyList<T>? value) => value == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize(ReadOnlySpan<T> value) => CalcLen(value.Length);

    /// <inheritdoc />
    public abstract int WriteObject(T[]? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject(ReadOnlySpan<T> value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject(IList<T> value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject(IReadOnlyList<T> value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject(T[]? value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject(ReadOnlySpan<T> value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject(IList<T> value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject(IReadOnlyList<T> value, Stream stream);

    /// <inheritdoc />
    public abstract T[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead);

    /// <inheritdoc />
    public abstract int ReadObject(byte* bytes, uint maxSize, Span<T> output, out int bytesRead, bool hasReadLength = true);

    /// <inheritdoc />
    public abstract int ReadObject(byte* bytes, uint maxSize, IList<T> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false);

    /// <inheritdoc />
    public abstract T[]? ReadObject(Stream stream, out int bytesRead);

    /// <inheritdoc />
    public abstract int ReadObject(Stream stream, Span<T> output, out int bytesRead, bool hasReadLength = true);

    /// <inheritdoc />
    public abstract int ReadObject(Stream stream, IList<T> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false);
}
