using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DanielWillett.ModularRpcs.Configuration;
using JetBrains.Annotations;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Manages reading and writing various types in various forms to and from streams or raw binary buffers.
/// </summary>
/// <remarks>Default implementation: <see cref="DefaultSerializer"/>.</remarks>
public interface IRpcSerializer
{
    /// <summary>
    /// Should primitive-like types be read from and written to the buffer directly instead of calling the read, write, and get size methods on them?
    /// </summary>
    /// <remarks>Primitive types also include some fixed-size system types like DateTime[Offset], TimeSpan, Guid, etc.</remarks>
    bool CanFastReadPrimitives { get; }

    /// <summary>
    /// The configuration currently loaded in this serializer. This is immutable.
    /// </summary>
    SerializationConfiguration Configuration { get; }

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize<T>(T? value);

    /// <summary>
    /// Get the size of a nullable object in bytes.
    /// </summary>
    /// <typeparam name="T">Underlying type of the nullable type.</typeparam>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize<T>(in T? value) where T : struct;

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. Consider using <see cref="GetSize(Type, object)"/> instead.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize(object value);

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSerializableSize<TSerializable>(in TSerializable value) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSerializablesSize<TSerializable>(IEnumerable<TSerializable?> value) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="type"/> does not implement <see cref="IRpcSerializable"/>.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSerializableSize(Type type, IRpcSerializable? value);

    /// <summary>
    /// Get the size of an object in bytes.
    /// </summary>
    /// <param name="type">The type of <see cref="IRpcSerializable"/> elements in <paramref name="value"/>.</param>
    /// <param name="value">Set of values assignable to <paramref name="type"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="type"/> does not implement <see cref="IRpcSerializable"/>.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSerializablesSize(Type type, object? value);

    /// <summary>
    /// Get the size of an object of type <paramref name="valueType"/> in bytes.
    /// </summary>
    /// <exception cref="InvalidCastException"><paramref name="value"/> is not an instance of <paramref name="valueType"/>.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSize(Type valueType, object? value);

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

    /// <summary>
    /// Gets the minimum size of a type in bytes.
    /// </summary>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetMinimumSize<T>();

    /// <summary>
    /// Gets the minimum size of a type in bytes.
    /// </summary>
    /// <param name="isFixedSize">Will the amount of bytes written always be the same, no matter the value?</param>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetMinimumSize<T>(out bool isFixedSize);

    /// <summary>
    /// Gets the minimum size of a type in bytes.
    /// </summary>
    /// <param name="isFixedSize">Will the amount of bytes written always be the same, no matter the value?</param>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int GetSerializableMinimumSize<TSerializable>(out bool isFixedSize) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Write a nullable value type to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteObject<T>(in T? value, byte* bytes, uint maxSize) where T : struct;

    /// <summary>
    /// Write a nullable reference type or value type to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteObject<T>(T? value, byte* bytes, uint maxSize);

    /// <summary>
    /// Write a serializable object to a raw binary buffer. 
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteSerializableObject<TSerializable>(in TSerializable serializable, byte* bytes, uint maxSize) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Write a list of serializable objects to a raw binary buffer. 
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteSerializableObjects<TSerializable>(IEnumerable<TSerializable?> serializable, byte* bytes, uint maxSize) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Write a serializable object to a raw binary buffer. 
    /// </summary>
    /// <param name="type">The type of <paramref name="value"/>.</param>
    /// <param name="value">The <see cref="IRpcSerializable"/> to write.</param>
    /// <param name="bytes"/>
    /// <param name="maxSize"/>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteSerializableObject(Type type, IRpcSerializable? value, byte* bytes, uint maxSize);

    /// <summary>
    /// Write a list of serializable objects to a raw binary buffer. 
    /// </summary>
    /// <param name="type">The type of <see cref="IRpcSerializable"/> elements in <paramref name="serializable"/>.</param>
    /// <param name="serializable">Set of values assignable to <paramref name="type"/>.</param>
    /// <param name="bytes"/>
    /// <param name="maxSize"/>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteSerializableObjects(Type type, object? serializable, byte* bytes, uint maxSize);

    /// <summary>
    /// Write a nullable reference type or value type to a raw binary buffer via a <see cref="TypedReference"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize);

    /// <summary>
    /// Write a reference type or value type to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. Consider using <see cref="WriteObject(Type, object, byte*, uint)"/> instead.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteObject(object value, byte* bytes, uint maxSize);

    /// <summary>
    /// Write a nullable reference type or value type of type <paramref name="valueType"/> to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="InvalidCastException"><paramref name="value"/> is not an instance of <paramref name="valueType"/>.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe int WriteObject(Type valueType, object? value, byte* bytes, uint maxSize);

    /// <summary>
    /// Write a nullable value type to a stream.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteObject<T>(in T? value, Stream stream) where T : struct;

    /// <summary>
    /// Write a nullable reference type or value type to a stream.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteObject<T>(T? value, Stream stream);

    /// <summary>
    /// Write a serializable object to a stream. 
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteSerializableObject<TSerializable>(in TSerializable serializable, Stream stream) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Write a list of serializable objects to a stream. 
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteSerializableObjects<TSerializable>(IEnumerable<TSerializable?> serializable, Stream stream) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Write a serializable object to a stream. 
    /// </summary>
    /// <param name="type">The type of <paramref name="value"/>.</param>
    /// <param name="value">The <see cref="IRpcSerializable"/> to write.</param>
    /// <param name="stream"/>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteSerializableObject(Type type, IRpcSerializable? value, Stream stream);

    /// <summary>
    /// Write a list of serializable objects to a stream. 
    /// </summary>
    /// <param name="type">The type of <see cref="IRpcSerializable"/> elements in <paramref name="serializable"/>.</param>
    /// <param name="serializable">Set of values assignable to <paramref name="type"/>.</param>
    /// <param name="stream"/>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteSerializableObjects(Type type, object? serializable, Stream stream);

    /// <summary>
    /// Write a nullable reference type or value type to a stream via a <see cref="TypedReference"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteObject(TypedReference value, Stream stream);

    /// <summary>
    /// Write a reference type or value type to a stream.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. Consider using <see cref="WriteObject(Type, object, Stream)"/> instead.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteObject(object value, Stream stream);

    /// <summary>
    /// Write a nullable reference type or value type of type <paramref name="valueType"/> to a stream.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    /// <exception cref="InvalidCastException"><paramref name="value"/> is not an instance of <paramref name="valueType"/>.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    int WriteObject(Type valueType, object? value, Stream stream);

    /// <summary>
    /// Read a nullable value type from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value type read, or <see langword="null"/>.</returns>
    unsafe T? ReadNullable<T>(byte* bytes, uint maxSize, out int bytesRead) where T : struct;

    /// <summary>
    /// Read a nullable value type from a raw binary buffer to a <see cref="TypedReference"/>.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe void ReadNullable<T>(TypedReference refOut, byte* bytes, uint maxSize, out int bytesRead) where T : struct;

    /// <summary>
    /// Read a nullable reference type or value type from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    unsafe T? ReadObject<T>(byte* bytes, uint maxSize, out int bytesRead);

    /// <summary>
    /// Read a serializable object from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    unsafe TSerializable? ReadSerializableObject<TSerializable>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Read a list of serializable objects from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    unsafe TSerializable?[]? ReadSerializableObjects<TSerializable>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Read a list of serializable objects from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    unsafe TCollectionType? ReadSerializableObjects<TSerializable, TCollectionType>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Read a nullable reference type or value type from a raw binary buffer to a <see cref="TypedReference"/>.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    unsafe void ReadObject(TypedReference refValue, byte* bytes, uint maxSize, out int bytesRead);

    /// <summary>
    /// Read a nullable reference type or value type of type <paramref name="objectType"/> from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    unsafe object? ReadObject(Type objectType, byte* bytes, uint maxSize, out int bytesRead);

    /// <summary>
    /// Read a nullable value type from a stream.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value type read, or <see langword="null"/>.</returns>
    T? ReadNullable<T>(Stream stream, out int bytesRead) where T : struct;

    /// <summary>
    /// Read a nullable value type from a stream to a <see cref="TypedReference"/>.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    void ReadNullable<T>(TypedReference refOut, Stream stream, out int bytesRead) where T : struct;

    /// <summary>
    /// Read a nullable reference type or value type from a stream.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    T? ReadObject<T>(Stream stream, out int bytesRead);

    /// <summary>
    /// Read a serializable object from a stream.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    TSerializable? ReadSerializableObject<TSerializable>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Read a list of serializable objects from a stream.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    TSerializable?[]? ReadSerializableObjects<TSerializable>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Read a list of serializable objects from a stream.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    TCollectionType? ReadSerializableObjects<TSerializable, TCollectionType>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable;

    /// <summary>
    /// Read a nullable reference type or value type from a stream to a <see cref="TypedReference"/>.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    void ReadObject(TypedReference refValue, Stream stream, out int bytesRead);

    /// <summary>
    /// Read a nullable reference type or value type of type <paramref name="objectType"/> from a stream.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="stream"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    object? ReadObject(Type objectType, Stream stream, out int bytesRead);

    /// <summary>
    /// Tries to find a recognized type by it's unique unsigned 32 bit id.
    /// </summary>
    bool TryGetKnownType(uint knownTypeId,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out Type knownType);

    /// <summary>
    /// Tries to find a unique unsigned 32 bit id by it's type.
    /// </summary>
    bool TryGetKnownTypeId(Type knownType, out uint knownTypeId);

    /// <summary>
    /// 'Learn' a known type.
    /// </summary>
    void SaveKnownType(uint knownTypeId, Type knownType);
}

public static class RpcSerializerExtensions
{

    /// <summary>
    /// Write a nullable value type to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteObject<T>(this IRpcSerializer serializer, in T? value, Span<byte> bytes) where T : struct
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteObject(in value, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Write a nullable reference type or value type to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteObject<T>(this IRpcSerializer serializer, T? value, Span<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteObject(value, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Write a serializable object to a raw binary buffer. 
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteSerializableObject<TSerializable>(this IRpcSerializer serializer, in TSerializable serializable, Span<byte> bytes) where TSerializable : IRpcSerializable
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteSerializableObject(in serializable, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Write a list of serializable objects to a raw binary buffer. 
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteSerializableObjects<TSerializable>(this IRpcSerializer serializer, IEnumerable<TSerializable> serializable, Span<byte> bytes) where TSerializable : IRpcSerializable
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteSerializableObjects(serializable, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Write a nullable reference type or value type to a raw binary buffer via a <see cref="TypedReference"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteObject(this IRpcSerializer serializer, TypedReference value, Span<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteObject(value, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Write a reference type or value type to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. Consider using <see cref="IRpcSerializer.WriteObject(Type, object, byte*, uint)"/> instead.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteObject(this IRpcSerializer serializer, object value, Span<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteObject(value, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Write a nullable reference type or value type of type <paramref name="valueType"/> to a raw binary buffer.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="bytes"/>.</returns>
    /// <exception cref="InvalidCastException"><paramref name="value"/> is not an instance of <paramref name="valueType"/>.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe int WriteObject(this IRpcSerializer serializer, Type valueType, object? value, Span<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.WriteObject(valueType, value, ptr, (uint)bytes.Length);
        }
    }

    /// <summary>
    /// Read a nullable value type from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value type read, or <see langword="null"/>.</returns>
    public static unsafe T? ReadNullable<T>(this IRpcSerializer serializer, Span<byte> bytes, out int bytesRead) where T : struct
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.ReadNullable<T>(ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a nullable value type from a raw binary buffer to a <see cref="TypedReference"/>.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe void ReadNullable<T>(this IRpcSerializer serializer, TypedReference refOut, Span<byte> bytes, out int bytesRead) where T : struct
    {
        fixed (byte* ptr = bytes)
        {
            serializer.ReadNullable<T>(refOut, ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a nullable reference type or value type from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    public static unsafe T? ReadObject<T>(this IRpcSerializer serializer, Span<byte> bytes, out int bytesRead)
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.ReadObject<T>(ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a serializable object from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    public static unsafe TSerializable? ReadSerializableObject<TSerializable>(this IRpcSerializer serializer, Span<byte> bytes, out int bytesRead) where TSerializable : IRpcSerializable
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.ReadSerializableObject<TSerializable>(ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a list of serializable objects from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    public static unsafe TSerializable?[]? ReadSerializableObjects<TSerializable>(this IRpcSerializer serializer, Span<byte> bytes, out int bytesRead) where TSerializable : IRpcSerializable
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.ReadSerializableObjects<TSerializable>(ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a list of serializable objects from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    public static unsafe TCollectionType? ReadSerializableObjects<TSerializable, TCollectionType>(this IRpcSerializer serializer, Span<byte> bytes, out int bytesRead) where TSerializable : IRpcSerializable
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.ReadSerializableObjects<TSerializable, TCollectionType>(ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a nullable reference type or value type from a raw binary buffer to a <see cref="TypedReference"/>.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    public static unsafe void ReadObject(this IRpcSerializer serializer, TypedReference refValue, Span<byte> bytes, out int bytesRead)
    {
        fixed (byte* ptr = bytes)
        {
            serializer.ReadObject(refValue, ptr, (uint)bytes.Length, out bytesRead);
        }
    }

    /// <summary>
    /// Read a nullable reference type or value type of type <paramref name="objectType"/> from a raw binary buffer.
    /// </summary>
    /// <exception cref="RpcParseException"><paramref name="bytes"/> was not long enough, or there was another parsing or formatting error.</exception>
    /// <exception cref="RpcInvalidParameterException">The type given is not serializable.</exception>
    /// <returns>The value read, or <see langword="null"/> if the reference type was written as <see langword="null"/>.</returns>
    public static unsafe object? ReadObject(this IRpcSerializer serializer, Type objectType, Span<byte> bytes, out int bytesRead)
    {
        fixed (byte* ptr = bytes)
        {
            return serializer.ReadObject(objectType, ptr, (uint)bytes.Length, out bytesRead);
        }
    }
}