using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Logging;
using System;

namespace DanielWillett.ModularRpcs.Abstractions;

/// <summary>
/// Allows various common logging tools to be used safely without needing all of their assembly's referenced. Use extension methods in <see cref="LoggingExtensions"/> and <see cref="LoggingExtensionsILogger"/> to 
/// </summary>
public interface IRefSafeLoggable
{
    /// <summary>
    /// Can be of type <see cref="ILogger"/>, <see cref="DBNull"/> (for console), <see cref="IReflectionToolsLogger"/>, <see cref="IAccessor"/>, <see cref="Action{T1,T2,T3,T4}"/> of (<see cref="Type"/> sourceType, <see cref="LogSeverity"/> severity, <see cref="Exception"/>? exception, <see cref="string"/> message), or <see langword="null"/> to disable logging.
    /// </summary>
    /// <remarks>You shouldn't interact directly with this interface's properties, instead use the extension methods contained in <see cref="LoggingExtensions"/> and <see cref="LoggingExtensionsILogger"/>.</remarks>
    ref object? Logger { get; }

    /// <summary>
    /// The type of logger used.
    /// </summary>
    /// <remarks>You shouldn't interact directly with this interface's properties, instead use the extension methods contained in <see cref="LoggingExtensions"/> and <see cref="LoggingExtensionsILogger"/>.</remarks>
    LoggerType LoggerType { get; set; }
}

public enum LoggerType
{
    /// <summary>
    /// <see langword="null"/>
    /// </summary>
    None,

    /// <summary>
    /// <see cref="DBNull.Value"/>.
    /// </summary>
    Console,

    /// <summary>
    /// <see cref="Action{T1,T2,T3,T4}"/> of (<see cref="Type"/> sourceType, <see cref="LogSeverity"/> severity, <see cref="Exception"/>? exception, <see cref="string"/> message)
    /// </summary>
    Callback,

    /// <summary>
    /// <see cref="ILogger"/>
    /// </summary>
    MicrosoftLogger,

    /// <summary>
    /// <see cref="IReflectionToolsLogger"/>
    /// </summary>
    ReflectionToolsLogger,

    /// <summary>
    /// <see cref="IAccessor"/>
    /// </summary>
    ReflectionToolsAccessor,

    /// <summary>
    /// Another <see cref="IRefSafeLoggable"/>.
    /// </summary>
    RefSafeLoggable
}