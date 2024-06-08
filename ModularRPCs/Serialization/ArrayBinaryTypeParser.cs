using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Implementation of <see cref="IArrayBinaryTypeParser{T}"/> to take some boilerplate away from writing array parsers. Supports arrays, <see cref="IList{T}"/>,
/// <see cref="IReadOnlyList{T}"/>, <see cref="IEnumerable{T}"/>, <see cref="ICollection{T}"/>, <see cref="IReadOnlyCollection{T}"/>,
/// <see cref="ArraySegment{T}"/>, <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> pointers (with <see cref="TypedReference"/>'s), 
/// and <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>
/// </summary>
/// <typeparam name="T">The element type to parse.</typeparam>
public abstract unsafe class ArrayBinaryTypeParser<T> : IArrayBinaryTypeParser<T> where T : unmanaged
{
    private static readonly Type ArrType = typeof(T[]);
    private static readonly Type ListType = typeof(IList<T>);
    private static readonly Type RoListType = typeof(IReadOnlyList<T>);
    private static readonly Type EnumerableType = typeof(IEnumerable<T>);
    private static readonly Type CollectionType = typeof(ICollection<T>);
    private static readonly Type RoCollectionType = typeof(IReadOnlyCollection<T>);
    private static readonly Type ArrSegmentType = typeof(ArraySegment<T>);
    private static readonly Type SpanType = typeof(Span<T>);
    private static readonly Type RoSpanType = typeof(ReadOnlySpan<T>);
    private static readonly Type SpanPtrType = typeof(Span<T>*);
    private static readonly Type RoSpanPtrType = typeof(ReadOnlySpan<T>*);

    /// <inheritdoc />
    public bool IsVariableSize => true;

    /// <inheritdoc />
    public int MinimumSize => 1;
    public virtual int ElementSize => sizeof(T);
    private int CalcLen(int length)
    {
        if (length == 0)
            return 1;
        byte lenFlag = SerializationHelper.GetLengthFlag(length, false);
        int hdrSize = SerializationHelper.GetHeaderSize(lenFlag);
        return hdrSize + length * ElementSize;
    }
    protected static ArraySegment<T> EmptySegment()
    {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return ArraySegment<T>.Empty;
#else
        return new ArraySegment<T>(Array.Empty<T>());
#endif
    }
    protected static void ResetOrReMake(ref IEnumerator<T> enumerator, IEnumerable<T> enumerable)
    {
        try
        {
            enumerator.Reset();
        }
        catch (NotSupportedException)
        {
            enumerator.Dispose();
            enumerator = enumerable.GetEnumerator();
        }
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
        {
            T[]? arr = __refvalue(value, T[]?);
            if (arr == null)
                return 1;
            len = arr.Length;
        }
        else if (t == ListType)
        {
            IList<T>? arr = __refvalue(value, IList<T>?);
            if (arr == null)
                return 1;
            len = arr.Count;
        }
        else if (t == RoListType)
        {
            IReadOnlyList<T>? arr = __refvalue(value, IReadOnlyList<T>?);
            if (arr == null)
                return 1;
            len = arr.Count;
        }
        else if (t == ArrSegmentType)
            len = __refvalue(value, ArraySegment<T>).Count;
        else if (t == RoSpanType)
            len = __refvalue(value, ReadOnlySpan<T>).Length;
        else if (t == EnumerableType)
        {
            IEnumerable<T>? arr = __refvalue(value, IEnumerable<T>?);
            if (arr == null)
                return 1;
            len = arr.Count();
        }
        else if (t == CollectionType)
        {
            ICollection<T>? arr = __refvalue(value, ICollection<T>?);
            if (arr == null)
                return 1;
            len = arr.Count;
        }
        else if (t == RoCollectionType)
        {
            IReadOnlyCollection<T>? arr = __refvalue(value, IReadOnlyCollection<T>?);
            if (arr == null)
                return 1;
            len = arr.Count;
        }
        else if (t == SpanType)
            len = __refvalue(value, Span<T>).Length;
        else if (t == RoSpanPtrType)
        {
            ReadOnlySpan<T>* span = __refvalue(value, ReadOnlySpan<T>*);
            if (span == null)
                return 1;
            len = span->Length;
        }
        else if (t == SpanPtrType)
        {
            Span<T>* span = __refvalue(value, Span<T>*);
            if (span == null)
                return 1;
            len = span->Length;
        }
        else
            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));

        return CalcLen(len);
    }

    /// <inheritdoc />
    public int WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        Type t = __reftype(value);
        if (t == ArrType)
            return WriteObject(__refvalue(value, T[]?), bytes, maxSize);
        if (t == ListType)
            return WriteObject(__refvalue(value, IList<T>?), bytes, maxSize);
        if (t == RoListType)
            return WriteObject(__refvalue(value, IReadOnlyList<T>?), bytes, maxSize);
        if (t == ArrSegmentType)
            return WriteObject(__refvalue(value, ArraySegment<T>), bytes, maxSize);
        if (t == RoSpanType)
            return WriteObject(__refvalue(value, ReadOnlySpan<T>), bytes, maxSize);
        if (t == EnumerableType)
            return WriteObject(__refvalue(value, IEnumerable<T>?), bytes, maxSize);
        if (t == CollectionType)
            return WriteObject(__refvalue(value, ICollection<T>?), bytes, maxSize);
        if (t == RoCollectionType)
            return WriteObject(__refvalue(value, IReadOnlyCollection<T>?), bytes, maxSize);
        if (t == SpanType)
            return WriteObject(__refvalue(value, Span<T>), bytes, maxSize);
        if (t == RoSpanPtrType)
            return WriteObject(*__refvalue(value, ReadOnlySpan<T>*), bytes, maxSize);
        if (t == SpanPtrType)
            return WriteObject(*__refvalue(value, Span<T>*), bytes, maxSize);

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public int WriteObject(TypedReference value, Stream stream)
    {
        Type t = __reftype(value);
        if (t == ArrType)
            return WriteObject(__refvalue(value, T[]?), stream);
        if (t == ListType)
            return WriteObject(__refvalue(value, IList<T>?), stream);
        if (t == RoListType)
            return WriteObject(__refvalue(value, IReadOnlyList<T>?), stream);
        if (t == ArrSegmentType)
            return WriteObject(__refvalue(value, ArraySegment<T>), stream);
        if (t == RoSpanType)
            return WriteObject(__refvalue(value, ReadOnlySpan<T>), stream);
        if (t == EnumerableType)
            return WriteObject(__refvalue(value, IEnumerable<T>?), stream);
        if (t == CollectionType)
            return WriteObject(__refvalue(value, ICollection<T>?), stream);
        if (t == RoCollectionType)
            return WriteObject(__refvalue(value, IReadOnlyCollection<T>?), stream);
        if (t == SpanType)
            return WriteObject(__refvalue(value, Span<T>), stream);
        if (t == RoSpanPtrType)
            return WriteObject(*__refvalue(value, ReadOnlySpan<T>*), stream);
        if (t == SpanPtrType)
            return WriteObject(*__refvalue(value, Span<T>*), stream);

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
        else if (t == ArrSegmentType)
        {
            T[]? arr = ReadObject(bytes, maxSize, out bytesRead);
            __refvalue(outObj, ArraySegment<T>) = arr == null ? EmptySegment() : new ArraySegment<T>(arr);
        }
        else if (t == RoSpanType)
            __refvalue(outObj, ReadOnlySpan<T>) = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
        else if (t == EnumerableType)
            __refvalue(outObj, IEnumerable<T>?) = ReadObject(bytes, maxSize, out bytesRead);
        else if (t == CollectionType)
            __refvalue(outObj, ICollection<T>?) = ReadObject(bytes, maxSize, out bytesRead);
        else if (t == RoCollectionType)
            __refvalue(outObj, IReadOnlyCollection<T>?) = ReadObject(bytes, maxSize, out bytesRead);
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
        else if (t == RoSpanPtrType)
            *__refvalue(outObj, ReadOnlySpan<T>*) = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
        else if (t == SpanPtrType)
        {
            Span<T>* existingSpan = __refvalue(outObj, Span<T>*);
            if (!existingSpan->IsEmpty)
            {
                ReadObject(bytes, maxSize, *existingSpan, out bytesRead, false);
            }
            else
            {
                *existingSpan = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
            }
        }
        else
            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    IList<T>? IBinaryTypeParser<IList<T>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        return ReadObject(bytes, maxSize, out bytesRead);
    }

    /// <inheritdoc />
    IReadOnlyList<T>? IBinaryTypeParser<IReadOnlyList<T>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        return ReadObject(bytes, maxSize, out bytesRead);
    }

    /// <inheritdoc />
    ICollection<T>? IBinaryTypeParser<ICollection<T>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        return ReadObject(bytes, maxSize, out bytesRead);
    }

    /// <inheritdoc />
    IReadOnlyCollection<T>? IBinaryTypeParser<IReadOnlyCollection<T>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        return ReadObject(bytes, maxSize, out bytesRead);
    }

    /// <inheritdoc />
    IEnumerable<T>? IBinaryTypeParser<IEnumerable<T>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        return ReadObject(bytes, maxSize, out bytesRead);
    }

    /// <inheritdoc />
    ArraySegment<T> IBinaryTypeParser<ArraySegment<T>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        T[]? arr = ReadObject(bytes, maxSize, out bytesRead);
        return arr == null ? default : new ArraySegment<T>(arr);
    }

    /// <inheritdoc />
    IList<T>? IBinaryTypeParser<IList<T>>.ReadObject(Stream stream, out int bytesRead)
    {
        return ReadObject(stream, out bytesRead);
    }

    /// <inheritdoc />
    IReadOnlyList<T>? IBinaryTypeParser<IReadOnlyList<T>>.ReadObject(Stream stream, out int bytesRead)
    {
        return ReadObject(stream, out bytesRead);
    }

    /// <inheritdoc />
    ICollection<T>? IBinaryTypeParser<ICollection<T>>.ReadObject(Stream stream, out int bytesRead)
    {
        return ReadObject(stream, out bytesRead);
    }

    /// <inheritdoc />
    IReadOnlyCollection<T>? IBinaryTypeParser<IReadOnlyCollection<T>>.ReadObject(Stream stream, out int bytesRead)
    {
        return ReadObject(stream, out bytesRead);
    }

    /// <inheritdoc />
    IEnumerable<T>? IBinaryTypeParser<IEnumerable<T>>.ReadObject(Stream stream, out int bytesRead)
    {
        return ReadObject(stream, out bytesRead);
    }

    /// <inheritdoc />
    ArraySegment<T> IBinaryTypeParser<ArraySegment<T>>.ReadObject(Stream stream, out int bytesRead)
    {
        T[]? arr = ReadObject(stream, out bytesRead);
        return arr == null ? default : new ArraySegment<T>(arr);
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
        else if (t == ArrSegmentType)
        {
            T[]? arr = ReadObject(stream, out bytesRead);
            __refvalue(outObj, ArraySegment<T>) = arr == null ? EmptySegment() : new ArraySegment<T>(arr);
        }
        else if (t == RoSpanType)
            __refvalue(outObj, ReadOnlySpan<T>) = ReadObject(stream, out bytesRead).AsSpan();
        else if (t == EnumerableType)
            __refvalue(outObj, IEnumerable<T>?) = ReadObject(stream, out bytesRead);
        else if (t == CollectionType)
            __refvalue(outObj, ICollection<T>?) = ReadObject(stream, out bytesRead);
        else if (t == RoCollectionType)
            __refvalue(outObj, IReadOnlyCollection<T>?) = ReadObject(stream, out bytesRead);
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
        else if (t == RoSpanPtrType)
            *__refvalue(outObj, ReadOnlySpan<T>*) = ReadObject(stream, out bytesRead).AsSpan();
        else if (t == SpanPtrType)
        {
            Span<T>* existingSpan = __refvalue(outObj, Span<T>*);
            if (!existingSpan->IsEmpty)
            {
                ReadObject(stream, *existingSpan, out bytesRead, false);
            }
            else
            {
                *existingSpan = ReadObject(stream, out bytesRead).AsSpan();
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
            case ArraySegment<T> seg:
                len = seg.Count;
                break;
            case IList<T> list:
                len = list.Count;
                break;
            case IReadOnlyList<T> list:
                len = list.Count;
                break;
            case ICollection<T> collection:
                len = collection.Count;
                break;
            case IReadOnlyCollection<T> collection:
                len = collection.Count;
                break;
            case IEnumerable<T> collection:
                len = collection.Count();
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
            ArraySegment<T> seg => WriteObject(seg, bytes, maxSize),
            IList<T> list => WriteObject(list, bytes, maxSize),
            IReadOnlyList<T> list => WriteObject(list, bytes, maxSize),
            ICollection<T> collection => WriteObject(collection, bytes, maxSize),
            IReadOnlyCollection<T> collection => WriteObject(collection, bytes, maxSize),
            IEnumerable<T> enu => WriteObject(enu, bytes, maxSize),
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
            ArraySegment<T> seg => WriteObject(seg, stream),
            IList<T> list => WriteObject(list, stream),
            IReadOnlyList<T> list => WriteObject(list, stream),
            ICollection<T> collection => WriteObject(collection, stream),
            IReadOnlyCollection<T> collection => WriteObject(collection, stream),
            IEnumerable<T> enu => WriteObject(enu, stream),
            null => WriteObject(null!, stream),
            _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
        };
    }

    /// <inheritdoc />
    public object? ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (type == ArrType || type == ListType || type == RoListType)
            return ReadObject(bytes, maxSize, out bytesRead);

        if (type == ArrSegmentType)
        {
            T[]? arr = ReadObject(bytes, maxSize, out bytesRead);
            return arr == null ? EmptySegment() : new ArraySegment<T>(arr);
        }

        if (type.IsAssignableFrom(ArrType))
        {
            return ReadObject(bytes, maxSize, out bytesRead);
        }

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public object? ReadObject(Type type, Stream stream, out int bytesRead)
    {
        if (type == ArrType || type == ListType || type == RoListType)
            return ReadObject(stream, out bytesRead);

        if (type == ArrSegmentType)
        {
            T[]? arr = ReadObject(stream, out bytesRead);
            return arr == null ? EmptySegment() : new ArraySegment<T>(arr);
        }

        if (type.IsAssignableFrom(ArrType))
        {
            return ReadObject(stream, out bytesRead);
        }

        throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
    }

    /// <inheritdoc />
    public int GetSize([InstantHandle] T[]? value) => value == null ? 1 : CalcLen(value.Length);

    /// <inheritdoc />
    public int GetSize([InstantHandle] ArraySegment<T> value) => value.Array == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize([InstantHandle] IList<T>? value) => value == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize([InstantHandle] IReadOnlyList<T>? value) => value == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize([InstantHandle] ICollection<T>? value) => value == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize([InstantHandle] IReadOnlyCollection<T>? value) => value == null ? 1 : CalcLen(value.Count);

    /// <inheritdoc />
    public int GetSize([InstantHandle] IEnumerable<T>? value) => value == null ? 1 : CalcLen(value.Count());

    /// <inheritdoc />
    public int GetSize([InstantHandle] scoped ReadOnlySpan<T> value) => CalcLen(value.Length);

    /// <inheritdoc />
    public int WriteObject([InstantHandle] T[]? value, byte* bytes, uint maxSize)
    {
        return WriteObject(value == null ? default : new ArraySegment<T>(value), bytes, maxSize);
    }

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] ArraySegment<T> value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] scoped ReadOnlySpan<T> value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IList<T>? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IReadOnlyList<T>? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] ICollection<T>? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IReadOnlyCollection<T>? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IEnumerable<T>? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public int WriteObject([InstantHandle] T[]? value, Stream stream)
    {
        return WriteObject(value == null ? default : new ArraySegment<T>(value), stream);
    }

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] ArraySegment<T> value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] scoped ReadOnlySpan<T> value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IList<T>? value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IReadOnlyList<T>? value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] ICollection<T>? value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IReadOnlyCollection<T>? value, Stream stream);

    /// <inheritdoc />
    public abstract int WriteObject([InstantHandle] IEnumerable<T>? value, Stream stream);

    /// <inheritdoc />
    public abstract T[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead);

    /// <inheritdoc />
    public abstract int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<T> output, out int bytesRead, bool hasReadLength = true);

    /// <inheritdoc />
    public abstract int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<T> output, out int bytesRead, bool hasReadLength = true);

    /// <inheritdoc />
    public abstract int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<T> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false);

    /// <inheritdoc />
    public abstract T[]? ReadObject(Stream stream, out int bytesRead);

    /// <inheritdoc />
    public abstract int ReadObject(Stream stream, [InstantHandle] ArraySegment<T> output, out int bytesRead, bool hasReadLength = true);

    /// <inheritdoc />
    public abstract int ReadObject(Stream stream, [InstantHandle] scoped Span<T> output, out int bytesRead, bool hasReadLength = true);

    /// <inheritdoc />
    public abstract int ReadObject(Stream stream, [InstantHandle] IList<T> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false);
}
