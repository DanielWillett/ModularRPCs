using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using DanielWillett.ModularRpcs.Configuration;
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