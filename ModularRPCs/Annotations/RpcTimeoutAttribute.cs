using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Define the timeout after which a request will be considered not responded to. Use the <see cref="Timeouts"/> utility class to specify units.
/// <code>
/// [RpcSend, RpcTimeout(10 * Timeouts.Seconds)]
/// protected virtual RpcTask CallRpc() { }
/// </code>
/// </summary>
/// <param name="timeout">Timeout in milliseconds of the request.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RpcTimeoutAttribute(int timeout) : Attribute
{
    /// <summary>
    /// Timeout in milliseconds of the request.
    /// </summary>
    public int Timeout { get; } = timeout;
}

/// <summary>
/// Contains common timeout units that can be multiplied by scalars in attributes.
/// </summary>
public static class Timeouts
{
    /// <summary>
    /// Multiply by this to convert seconds to milliseconds.
    /// <code>
    /// [RpcTimeout(30 * Timeouts.Seconds)]
    /// </code>
    /// </summary>
    public const int Seconds = 1000;

    /// <summary>
    /// Multiply by this to convert minutes to milliseconds.
    /// <code>
    /// [RpcTimeout(5 * Timeouts.Minutes)]
    /// </code>
    /// </summary>
    public const int Minutes = Seconds * 60;

    /// <summary>
    /// Multiply by this to convert hours to milliseconds.
    /// <code>
    /// [RpcTimeout(2 * Timeouts.Hours)]
    /// </code>
    /// </summary>
    public const int Hours = Minutes * 60;

    /// <summary>
    /// Multiply by this to convert hours to milliseconds.
    /// <code>
    /// [RpcTimeout(1 * Timeouts.Days)]
    /// </code>
    /// </summary>
    public const int Days = Hours * 24;
}