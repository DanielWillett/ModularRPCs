using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Lets an object define how it should be serialized. The object must have a parameterless constructor if it's not a value type.
/// </summary>
/// <remarks>Objects using this attribute must also implement <see cref="IRpcSerializable"/>.</remarks>
[BaseTypeRequired(typeof(IRpcSerializable))]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class RpcSerializableAttribute : Attribute
{
    /// <summary>
    /// The minimum amount of bytes this object can take up.
    /// </summary>
    public int MinimumSize { get; }

    /// <summary>
    /// If the object will always be the same exact size no matter the data.
    /// </summary>
    public bool IsFixedSize { get; }

    /// <summary>
    /// Define a <see cref="IRpcSerializable"/> type.
    /// </summary>
    /// <param name="minimumSize">The minimum amount of bytes this object can take up.</param>
    /// <param name="isFixedSize">If the object will always be the same exact size no matter the data.</param>
    public RpcSerializableAttribute(int minimumSize, bool isFixedSize)
    {
        MinimumSize = minimumSize;
        IsFixedSize = isFixedSize;
    }
}

/// <summary>
/// Lets an object define how it should be serialized. The object must have a parameterless constructor if it's not a value type.
/// </summary>
/// <remarks>Objects implementing this interface must also decorate themselves with a <see cref="RpcSerializableAttribute"/>.</remarks>
public interface IRpcSerializable
{
    /// <summary>
    /// The exact size of this object in it's current state.
    /// </summary>
    /// <remarks>This property should be marked <see langword="readonly"/> on value types, otherwise the behaivor is undefined.</remarks>
    int GetSize(IRpcSerializer serializer);

    /// <summary>
    /// Write the data in this type to <paramref name="writeTo"/>.
    /// </summary>
    /// <returns>The number of bytes written. Must be consistant with <see cref="GetSize"/>.</returns>
    /// <remarks>This method should be marked <see langword="readonly"/> on value types, otherwise the behaivor is undefined.</remarks>
    int Write(Span<byte> writeTo, IRpcSerializer serializer);

    /// <summary>
    /// Read the data from <paramref name="readFrom"/> to this type.
    /// </summary>
    /// <returns>-1 if there's not enough data, otherwise the amount of bytes read.</returns>
    int Read(Span<byte> readFrom, IRpcSerializer serializer);
}