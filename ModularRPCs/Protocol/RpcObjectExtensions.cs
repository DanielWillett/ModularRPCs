using DanielWillett.ModularRpcs.Reflection;
using System;

namespace DanielWillett.ModularRpcs.Protocol;
public static class RpcObjectExtensions
{
    /// <summary>
    /// Try's to release an object by it's identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <returns><see langword="true"/> if the object was found and released, otherwise <see langword="false"/>.</returns>
    public static bool Release<T>(this IRpcObject<T> obj)
    {
        return ProxyGenerator.Instance.ReleaseObject(obj.GetType(), obj);
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="data"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="data"/>.</returns>
    public static unsafe int WriteIdentifier<T>(this IRpcObject<T> obj, ArraySegment<byte> data)
    {
        fixed (byte* ptr = &data.Array![data.Offset])
        {
            return ProxyGenerator.Instance.WriteIdentifier(obj.GetType(), obj, ptr, data.Count);
        }
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="data"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="data"/>.</returns>
    public static unsafe int WriteIdentifier<T>(this IRpcObject<T> obj, Memory<byte> data)
    {
        fixed (byte* ptr = data.Span)
        {
            return ProxyGenerator.Instance.WriteIdentifier(obj.GetType(), obj, ptr, data.Length);
        }
    }

    /// <summary>
    /// Writes this object's identifier to <paramref name="data"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="data"/>.</returns>
    public static unsafe int WriteIdentifier<T>(this IRpcObject<T> obj, Span<byte> data)
    {
        fixed (byte* ptr = data)
        {
            return ProxyGenerator.Instance.WriteIdentifier(obj.GetType(), obj, ptr, data.Length);
        }
    }

    /// <summary>
    /// Writes this object's identifier starting at <paramref name="data"/>.
    /// </summary>
    /// <returns>Number of bytes written to <paramref name="data"/>.</returns>
    public static unsafe int WriteIdentifier<T>(this IRpcObject<T> obj, byte* data, int maxLength)
    {
        return ProxyGenerator.Instance.WriteIdentifier(obj.GetType(), obj, data, maxLength);
    }
}