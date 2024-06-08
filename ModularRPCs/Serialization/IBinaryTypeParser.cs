using DanielWillett.ModularRpcs.Exceptions;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Handles converting objects to and from bytes or a <see cref="Stream"/>. Consider inheriting <see cref="BinaryTypeParser{T}"/> or <see cref="ArrayBinaryTypeParser{T}"/> instead.
/// </summary>
public interface IBinaryTypeParser
{
    bool IsVariableSize { get; }
    int MinimumSize { get; }

    /// <exception cref="InvalidCastException"><paramref name="value"/> was not of the expected type.</exception>
    [Pure]
    int GetSize(TypedReference value);

    /// <exception cref="RpcOverflowException">Buffer was not large enough.</exception>
    /// <exception cref="InvalidCastException"><paramref name="value"/> was not of the expected type.</exception>
    unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize);

    /// <exception cref="InvalidCastException"><paramref name="value"/> was not of the expected type.</exception>
    int WriteObject(TypedReference value, Stream stream);

    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    /// <exception cref="InvalidCastException"><paramref name="outObj"/> was not of the expected type.</exception>
    unsafe void ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj);

    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    /// <exception cref="InvalidCastException"><paramref name="outObj"/> was not of the expected type.</exception>
    void ReadObject(Stream stream, out int bytesRead, TypedReference outObj);

    /// <exception cref="InvalidCastException"><paramref name="value"/> was not of the expected type.</exception>
    [Pure]
    int GetSize(object? value);

    /// <exception cref="RpcOverflowException">Buffer was not large enough.</exception>
    /// <exception cref="InvalidCastException"><paramref name="value"/> was not of the expected type.</exception>
    unsafe int WriteObject(object? value, byte* bytes, uint maxSize);

    /// <exception cref="InvalidCastException"><paramref name="value"/> was not of the expected type.</exception>
    int WriteObject(object? value, Stream stream);

    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    unsafe object? ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead);

    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    object? ReadObject(Type type, Stream stream, out int bytesRead);
}

/// <summary>
/// Type-safe implementation of <see cref="IBinaryTypeParser"/>. Consider inheriting <see cref="BinaryTypeParser{T}"/> instead.
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public interface IBinaryTypeParser<T> : IBinaryTypeParser
{

    [Pure]
    int GetSize([InstantHandle] T? value);

    /// <exception cref="RpcOverflowException">Buffer was not large enough.</exception>
    unsafe int WriteObject([InstantHandle] T? value, byte* bytes, uint maxSize);
    int WriteObject([InstantHandle] T? value, Stream stream);

    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    unsafe T? ReadObject(byte* bytes, uint maxSize, out int bytesRead);

    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    T? ReadObject(Stream stream, out int bytesRead);
}

/// <summary>
/// Array implementation of <see cref="IBinaryTypeParser"/>. Consider inheriting <see cref="ArrayBinaryTypeParser{T}"/> instead.
/// </summary>
/// <typeparam name="T">Element type to be serialized.</typeparam>
public interface IArrayBinaryTypeParser<T> : IBinaryTypeParser<T?[]>, IBinaryTypeParser<IEnumerable<T>>, IBinaryTypeParser<IList<T>>, IBinaryTypeParser<IReadOnlyList<T>>, IBinaryTypeParser<ICollection<T>>, IBinaryTypeParser<IReadOnlyCollection<T>>, IBinaryTypeParser<ArraySegment<T>>
{
    [Pure]
    int GetSize([InstantHandle] scoped ReadOnlySpan<T?> value);

    /// <exception cref="RpcOverflowException">Buffer was not large enough.</exception>
    unsafe int WriteObject([InstantHandle] scoped ReadOnlySpan<T?> value, byte* bytes, uint maxSize);

    int WriteObject([InstantHandle] scoped ReadOnlySpan<T?> value, Stream stream);

    /// <returns>-1 if the array is <see langword="null"/>, otherwise the length of the new array in elements.</returns>
    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    unsafe int ReadArrayLength(byte* bytes, uint maxSize, out int bytesRead);

    /// <returns>-1 if the array is <see langword="null"/>, otherwise the length of the new array in elements.</returns>
    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    int ReadArrayLength(Stream stream, out int bytesRead);

    /// <summary>
    /// Read the array to <paramref name="output"/>. Either use <see cref="ReadArrayLength(byte*,uint,out int)"/> and set <paramref name="hasReadLength"/> to <see langword="true"/>, or guess the max length and set it to <see langword="false"/>.
    /// </summary>
    /// <returns>Number of elements written to <paramref name="output"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><see langword="false"/> is null and <paramref name="output"/> is not long enough.</exception>
    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    unsafe int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<T?> output, out int bytesRead, bool hasReadLength = true);

    /// <summary>
    /// Read the array to <paramref name="output"/>. Either use <see cref="ReadArrayLength(byte*,uint,out int)"/> and set <paramref name="hasReadLength"/> to <see langword="true"/>, or guess the max length and set it to <see langword="false"/>.
    /// </summary>
    /// <returns>Number of elements written to <paramref name="output"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><see langword="false"/> is null and <paramref name="output"/> is not long enough.</exception>
    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    unsafe int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<T?> output, out int bytesRead, bool hasReadLength = true);

    /// <summary>
    /// Read the array to <paramref name="output"/>. Either use <see cref="ReadArrayLength(byte*,uint,out int)"/>, set <paramref name="hasReadLength"/> to <see langword="true"/>, and pass that length to <paramref name="measuredCount"/>, or don't read it and set it to <see langword="false"/>.
    /// </summary>
    /// <returns>Number of elements written to <paramref name="output"/>.</returns>
    /// <param name="setInsteadOfAdding">Makes this function behave like <see cref="ReadObject(byte*,uint,Span{T},out int,bool)"/> instead of adding to the list.</param>
    /// <exception cref="RpcParseException">Buffer was not large enough.</exception>
    /// <exception cref="ArgumentException"><paramref name="output"/> is readonly and <paramref name="setInsteadOfAdding"/> is <see langword="false"/>.</exception>
    unsafe int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<T?> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false);

    /// <summary>
    /// Read the array to <paramref name="output"/>. Either use <see cref="ReadArrayLength(Stream,out int)"/> and set <paramref name="hasReadLength"/> to <see langword="true"/>, or guess the max length and set it to <see langword="false"/>.
    /// </summary>
    /// <returns>Number of elements written to <paramref name="output"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><see langword="false"/> is null and <paramref name="output"/> is not long enough.</exception>
    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    int ReadObject(Stream stream, [InstantHandle] scoped Span<T?> output, out int bytesRead, bool hasReadLength = true);

    /// <summary>
    /// Read the array to <paramref name="output"/>. Either use <see cref="ReadArrayLength(Stream,out int)"/> and set <paramref name="hasReadLength"/> to <see langword="true"/>, or guess the max length and set it to <see langword="false"/>.
    /// </summary>
    /// <returns>Number of elements written to <paramref name="output"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><see langword="false"/> is null and <paramref name="output"/> is not long enough.</exception>
    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    int ReadObject(Stream stream, [InstantHandle] ArraySegment<T?> output, out int bytesRead, bool hasReadLength = true);

    /// <summary>
    /// Read the array to <paramref name="output"/>. Either use <see cref="ReadArrayLength(Stream,out int)"/>, set <paramref name="hasReadLength"/> to <see langword="true"/>, and pass that length to <paramref name="measuredCount"/>, or don't read it and set it to <see langword="false"/>.
    /// </summary>
    /// <returns>Number of elements written to <paramref name="output"/>.</returns>
    /// <param name="setInsteadOfAdding">Makes this function behave like <see cref="ReadObject(Stream,Span{T},out int,bool)"/> instead of adding to the list.</param>
    /// <exception cref="RpcParseException">Stream was not long enough.</exception>
    /// <exception cref="ArgumentException"><paramref name="output"/> is readonly and <paramref name="setInsteadOfAdding"/> is <see langword="false"/>.</exception>
    int ReadObject(Stream stream, [InstantHandle] IList<T?> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false);
}