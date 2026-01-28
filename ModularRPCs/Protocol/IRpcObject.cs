using System;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// An object that has a unique identifier to refer to it.
/// Must be cleaned up with <see cref="RpcObjectExtensions.Release{T}"/> unless this is a Unity component.
/// </summary>
/// <typeparam name="T">The type of identifier to use. Should provide hash code and equality methods. <see cref="IEquatable{T}"/> implementation will be used if available.</typeparam>
public interface IRpcObject<out T>
{
    /// <summary>
    /// This object's unique identifier.
    /// </summary>
    /// <remarks>An identifier can only be reused after <see cref="RpcObjectExtensions.Release{T}"/> is called on the old object.</remarks>
    T Identifier { get; }
}