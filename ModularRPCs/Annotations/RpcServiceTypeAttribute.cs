using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Define a specific interface to use with <see cref="IServiceProvider.GetService"/> when getting the target object if the actual class isn't available.
/// </summary>
/// <remarks>By default, interfaces will not be supported. Multiple of these attributes can be used on one class.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RpcServiceTypeAttribute(Type? serviceType) : Attribute
{
    /// <summary>
    /// The type of the service as it was registered with the service provider.
    /// </summary>
    public Type? ServiceType { get; } = serviceType;
}
