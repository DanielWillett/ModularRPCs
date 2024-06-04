using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Define a specific interface to use with <see cref="IServiceProvider.GetService"/> when getting the target object if the actual class isn't available.
/// </summary>
/// <remarks>By default, all interfaces will be checked. Multiple of these attributes can be used on one object.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RpcServiceTypeAttribute(Type? serviceType) : Attribute
{
    /// <summary>
    /// The type of the service as it was registered with the service provider.
    /// </summary>
    /// <remarks>By setting this to <see langword="null"/>, you can effectively disable searching interface types.</remarks>
    public Type? ServiceType { get; } = serviceType;
}
