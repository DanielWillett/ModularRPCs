using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;
public interface IRpcSerializer
{
    /// <summary>
    /// Should primitive-like types be read from and written to the buffer directly instead of calling the read, write, and get size methods on them?
    /// </summary>
    /// <remarks>Primitive types also include some fixed-size system types like DateTime[Offset], TimeSpan, Guid, etc.</remarks>
    bool CanFastReadPrimitives { get; }

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize<T>(T value);

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize(object value);

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize(TypedReference value);

    /// <summary>
    /// Gets the minimum size of a type in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetMinimumSize(Type type);

    /// <summary>
    /// Gets the minimum size of a type in bytes.
    /// </summary>
    /// <param name="isFixedSize">Will the amount of bytes written always be the same, no matter the value?</param>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetMinimumSize(Type type, out bool isFixedSize);
    unsafe int WriteObject<T>(T value, byte* bytes, uint maxSize);
    unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize);
    unsafe int WriteObject(object value, byte* bytes, uint maxSize);
    int WriteObject<T>(T value, Stream stream);
    int WriteObject(TypedReference value, Stream stream);
    int WriteObject(object value, Stream stream);

    unsafe T ReadObject<T>(byte* bytes, uint maxSize, out int bytesRead);
    unsafe void ReadObject(TypedReference refValue, byte* bytes, uint maxSize, out int bytesRead);
    unsafe object ReadObject(Type objectType, byte* bytes, uint maxSize, out int bytesRead);
    T ReadObject<T>(Stream stream, out int bytesRead);
    void ReadObject(TypedReference refValue, Stream stream, out int bytesRead);
    object ReadObject(Type objectType, Stream stream, out int bytesRead);

    /// <summary>
    /// Tries to find a recognized type by it's unique unsigned 32 bit id.
    /// </summary>
    bool TryGetKnownType(uint knownTypeId, out Type knownType);

    /// <summary>
    /// Tries to find a unique unsigned 32 bit id by it's type.
    /// </summary>
    bool TryGetKnownTypeId(Type knownType, out uint knownTypeId);

    /// <summary>
    /// 'Learn' a known type.
    /// </summary>
    void SaveKnownType(uint knownTypeId, Type knownType);
}