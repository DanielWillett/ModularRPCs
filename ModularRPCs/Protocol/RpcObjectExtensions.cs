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
}