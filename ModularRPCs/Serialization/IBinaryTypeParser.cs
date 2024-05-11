using System;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;
public interface IBinaryTypeParser
{
    bool IsVariableSize { get; }
    int MinimumSize { get; }
    Type Type { get; }

    int GetSize(TypedReference value);
    unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize);
    int WriteObject(TypedReference value, Stream stream);

    unsafe void ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj);
    void ReadObject(Stream stream, out int bytesRead, TypedReference outObj);

    int GetSize(object value);
    unsafe int WriteObject(object value, byte* bytes, uint maxSize);
    int WriteObject(object value, Stream stream);

    unsafe object ReadObject(byte* bytes, uint maxSize, out int bytesRead);
    object ReadObject(Stream stream, out int bytesRead);
}

/// <summary>
/// Type-safe implementation of <see cref="IBinaryTypeParser{T}"/>. Consider inheriting <see cref="BinaryTypeParser{T}"/> instead.
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public interface IBinaryTypeParser<T> : IBinaryTypeParser
{
    int GetSize(T value);

    unsafe int WriteObject(T value, byte* bytes, uint maxSize);
    int WriteObject(T value, Stream stream);

    new unsafe T ReadObject(byte* bytes, uint maxSize, out int bytesRead);
    new T ReadObject(Stream stream, out int bytesRead);
}