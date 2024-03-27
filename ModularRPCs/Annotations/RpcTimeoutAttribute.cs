using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Define the timeout after which a request will be considered not responded to.
/// <code>
/// [RpcSend, RpcTimeout(10 * RpcTimeoutAttribute.Seconds)]
/// protected virtual RpcTask CallRpc() { }
/// </code>
/// </summary>
/// <param name="timeout">Timeout in milliseconds of the request.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcTimeoutAttribute(int timeout) : Attribute
{
    /// <summary>
    /// Multiply by this to convert seconds to milliseconds.
    /// <code>
    /// [RpcTimeout(3 * RpcTimeoutAttribute.Seconds)]
    /// </code>
    /// </summary>
    public const int Seconds = 1000;

    /// <summary>
    /// Multiply by this to convert minutes to milliseconds.
    /// <code>
    /// [RpcTimeout(5 * RpcTimeoutAttribute.Minutes)]
    /// </code>
    /// </summary>
    public const int Minutes = Seconds * 60;

    /// <summary>
    /// Multiply by this to convert hours to milliseconds.
    /// <code>
    /// [RpcTimeout(1 * RpcTimeoutAttribute.Hours)]
    /// </code>
    /// </summary>
    public const int Hours = Minutes * 60;

    /// <summary>
    /// Timeout in milliseconds of the request.
    /// </summary>
    public int Timeout { get; } = timeout;
}