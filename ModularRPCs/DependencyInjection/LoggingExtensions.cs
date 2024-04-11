using DanielWillett.ModularRpcs.Data;
using Microsoft.Extensions.Logging;
using System;
using DanielWillett.ModularRpcs.Reflection;

namespace DanielWillett.ModularRpcs.DependencyInjection;

/// <summary>
/// Contains extensions to add loggers to some RPC components.
/// </summary>
/// <remarks>This class is separated into extension methods to remove reliance on Microsoft.Extensions.Logging.Abstractions.</remarks>
public static class LoggingExtensions
{
    /// <summary>
    /// Tells a <see cref="ContiguousBuffer"/> to use <paramref name="logger"/> to log messages.
    /// </summary>
    public static void SetLogger(this ContiguousBuffer buffer, ILogger logger)
    {
        buffer.Logger = logger;
    }

    /// <summary>
    /// Tells a <see cref="ContiguousBuffer"/> to use the <see cref="Console"/> to log messages.
    /// </summary>
    /// <remarks>This is the default behavior.</remarks>
    public static void SetConsoleLogger(this ContiguousBuffer buffer)
    {
        buffer.Logger = null;
    }

    /// <summary>
    /// Tells the <see cref="ProxyGenerator"/> to use <paramref name="logger"/> to log messages.
    /// </summary>
    public static void SetLogger(this ProxyGenerator proxyGenerator, ILogger logger)
    {
        proxyGenerator.Logger = logger;
    }

    /// <summary>
    /// Tells the <see cref="ProxyGenerator"/> to use the <see cref="Console"/> to log messages.
    /// </summary>
    /// <remarks>This is the default behavior.</remarks>
    public static void SetConsoleLogger(this ProxyGenerator proxyGenerator)
    {
        proxyGenerator.Logger = null;
    }
}
