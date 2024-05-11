using System;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;
public interface IRpcSerializer
{
    /// <summary>
    /// Should <see cref="GetSize"/> be called for all primitive-like types?
    /// </summary>
    /// <remarks>Primitive types also include some fixed-size system types like DateTime[Offset], TimeSpan, Guid, etc.</remarks>
    bool PreCalculatePrimitiveSizes { get; }
    int GetSize<T>(T value);
    int GetSize(TypedReference value);
    unsafe void WriteObject<T>(T value, byte* bytes, uint maxSize);
    unsafe void WriteObject(object value, byte* bytes, uint maxSize);
    void WriteObject<T>(T value, Stream stream);
    void WriteObject(object value, Stream stream);

    unsafe T ReadObject<T>(byte* bytes, uint maxSize, out int bytesRead);
    unsafe object ReadObject(Type objectType, byte* bytes, uint maxSize, out int bytesRead);
    T ReadObject<T>(Stream stream, out int bytesRead);
    object ReadObject(Type objectType, Stream stream, out int bytesRead);
}