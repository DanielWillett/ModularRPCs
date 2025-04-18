using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace DanielWillett.ModularRpcs;

/// <summary>
/// Contains extensions to add loggers to some RPC components.
/// </summary>
/// <remarks>This class is separated into extension methods to remove reliance on Microsoft.Extensions.Logging.Abstractions.</remarks>
public static class LoggingExtensionsILogger
{
    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to use <paramref name="logger"/> to log messages.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    public static void SetLogger(this IRefSafeLoggable loggable, ILogger logger)
    {
        loggable.LoggerType = LoggerType.MicrosoftLogger;
        LoggingExtensions.SwapLoggerAndDispose(loggable, logger ?? throw new ArgumentNullException(nameof(logger)));
    }
}

/// <summary>
/// Contains extensions to add loggers to some RPC components.
/// </summary>
/// <remarks>This class is separated into extension methods to remove reliance on Microsoft.Extensions.Logging.Abstractions.</remarks>
public static class LoggingExtensions
{
    internal static void SwapLoggerAndDispose(IRefSafeLoggable loggable, object? newLogger)
    {
        ref object? logger = ref loggable.Logger;
        object? otherLogger = Interlocked.Exchange(ref logger, newLogger);
        if (ReferenceEquals(otherLogger, newLogger) || otherLogger is not IDisposable disp)
            return;

        try
        {
            disp.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to use <paramref name="logger"/> to log messages.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    public static void SetLogger(this IRefSafeLoggable loggable, IReflectionToolsLogger logger)
    {
        SwapLoggerAndDispose(loggable, logger ?? throw new ArgumentNullException(nameof(logger)));
        loggable.LoggerType = LoggerType.ReflectionToolsLogger;
    }

    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to use another <see cref="IRefSafeLoggable"/> to log messages.
    /// </summary>
    /// <remarks>It is possible to dead-lock execution if loggers have circular references. Avoid this at all costs.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    public static void SetLogger(this IRefSafeLoggable loggable, IRefSafeLoggable logger)
    {
        SwapLoggerAndDispose(loggable, logger ?? throw new ArgumentNullException(nameof(logger)));
        loggable.LoggerType = LoggerType.RefSafeLoggable;
    }

    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to use <paramref name="accessor"/>'s <see cref="Accessor.Logger"/> to log messages.
    /// </summary>
    /// <param name="accessor">The instance of <see cref="IAccessor"/> to use. If this is <see langword="null"/>, it'll default to <see cref="Accessor.Active"/>.</param>
    public static void SetLogger(this IRefSafeLoggable loggable, IAccessor? accessor)
    {
        SwapLoggerAndDispose(loggable, accessor ?? Accessor.Active);
        loggable.LoggerType = LoggerType.ReflectionToolsAccessor;
    }

    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to log messages using <paramref name="logCallback"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="logCallback"/> is <see langword="null"/>.</exception>
    public static void SetLogger(this IRefSafeLoggable loggable, Action<Type, LogSeverity, Exception?, string?> logCallback)
    {
        SwapLoggerAndDispose(loggable, logCallback ?? throw new ArgumentNullException(nameof(logCallback)));
        loggable.LoggerType = LoggerType.Callback;
    }

    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to use <see cref="Console"/> to log messages.
    /// </summary>
    public static void SetLoggerToConsole(this IRefSafeLoggable loggable)
    {
        SwapLoggerAndDispose(loggable, DBNull.Value);
        loggable.LoggerType = LoggerType.Console;
    }

    /// <summary>
    /// Tells an <see cref="IRefSafeLoggable"/> object to not log messages.
    /// </summary>
    public static void SetLoggerToNone(this IRefSafeLoggable loggable)
    {
        SwapLoggerAndDispose(loggable, null);
        loggable.LoggerType = LoggerType.None;
    }

    /// <summary>
    /// Will any text be written to this <see cref="IRefSafeLoggable"/>?
    /// </summary>
    public static bool IsLogging(this IRefSafeLoggable loggable) => loggable.Logger != null;

    /// <summary>
    /// Log information to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogInformation(this IRefSafeLoggable loggable, string? message) => loggable.LogInformation(null, message);

    /// <summary>
    /// Log information to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogInformation(this IRefSafeLoggable loggable, Exception? ex, string? message)
    {
        while (true)
        {
            LoggerType type = loggable.LoggerType;
            object? logger = loggable.Logger;
            switch (type)
            {
                default:
                    return;

                case LoggerType.Console:
                    string hdr = "[INF] [" + Accessor.Formatter.Format(loggable.GetType()) + "]";
                    if (message != null)
                        hdr += " " + message;
                    if (ex != null)
                        hdr += Environment.NewLine + ex;
                    Accessor.Logger!.LogInfo("source", hdr);
                    return;

                case LoggerType.Callback:
                    if (logger is Action<Type, LogSeverity, Exception?, string?> callback)
                        callback(loggable.GetType(), LogSeverity.Information, ex, message);

                    return;

                case LoggerType.MicrosoftLogger:
                    LogMsLogger(logger, LogSeverity.Information, ex, message);
                    return;

                case LoggerType.ReflectionToolsLogger:
                    if (logger is not IReflectionToolsLogger reflLogger)
                        return;

                    string src = Accessor.Formatter.Format(loggable.GetType());
                    if (message != null)
                        reflLogger.LogInfo(src, message);
                    if (ex != null)
                        reflLogger.LogInfo(src, ex.ToString());
                    return;

                case LoggerType.ReflectionToolsAccessor:
                    if (logger is not IAccessor accessor)
                        return;

                    reflLogger = accessor.Logger!;
                    if (reflLogger == null)
                        return;

                    src = accessor.Formatter.Format(loggable.GetType());
                    if (message != null)
                        reflLogger.LogInfo(src, message);
                    if (ex != null)
                        reflLogger.LogInfo(src, ex.ToString());
                    return;

                case LoggerType.RefSafeLoggable:
                    if (logger is not IRefSafeLoggable loggable2)
                        return;

                    loggable = loggable2;
                    break;
            }
        }
    }

    /// <summary>
    /// Log debug information to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogDebug(this IRefSafeLoggable loggable, string? message) => loggable.LogDebug(null, message);

    /// <summary>
    /// Log debug information to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogDebug(this IRefSafeLoggable loggable, Exception? ex, string? message)
    {
        while (true)
        {
            LoggerType type = loggable.LoggerType;
            object? logger = loggable.Logger;
            switch (type)
            {
                default:
                    return;

                case LoggerType.Console:
                    string hdr = "[DBG] [" + Accessor.Formatter.Format(loggable.GetType()) + "]";
                    if (message != null)
                        hdr += " " + message;
                    if (ex != null)
                        hdr += Environment.NewLine + ex;
                    Accessor.Logger!.LogInfo("source", hdr);
                    return;

                case LoggerType.Callback:
                    if (logger is Action<Type, LogSeverity, Exception?, string?> callback)
                        callback(loggable.GetType(), LogSeverity.Debug, ex, message);

                    return;

                case LoggerType.MicrosoftLogger:
                    LogMsLogger(logger, LogSeverity.Debug, ex, message);
                    return;

                case LoggerType.ReflectionToolsLogger:
                    if (logger is not IReflectionToolsLogger reflLogger)
                        return;

                    string src = Accessor.Formatter.Format(loggable.GetType());
                    if (message != null)
                        reflLogger.LogDebug(src, message);
                    if (ex != null)
                        reflLogger.LogDebug(src, ex.ToString());
                    return;

                case LoggerType.ReflectionToolsAccessor:
                    if (logger is not IAccessor accessor)
                        return;

                    reflLogger = accessor.Logger!;
                    if (reflLogger == null)
                        return;

                    src = accessor.Formatter.Format(loggable.GetType());
                    if (message != null)
                        reflLogger.LogDebug(src, message);
                    if (ex != null)
                        reflLogger.LogDebug(src, ex.ToString());
                    return;

                case LoggerType.RefSafeLoggable:
                    if (logger is not IRefSafeLoggable loggable2)
                        return;

                    loggable = loggable2;
                    break;
            }
        }
    }

    /// <summary>
    /// Log a warning to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogWarning(this IRefSafeLoggable loggable, string? message) => loggable.LogWarning(null, message);

    /// <summary>
    /// Log a warning to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogWarning(this IRefSafeLoggable loggable, Exception? ex, string? message)
    {
        while (true)
        {
            LoggerType type = loggable.LoggerType;
            object? logger = loggable.Logger;
            switch (type)
            {
                default:
                    return;

                case LoggerType.Console:
                    string hdr = "[WRN] [" + Accessor.Formatter.Format(loggable.GetType()) + "]";
                    if (message != null)
                        hdr += " " + message;
                    if (ex != null)
                        hdr += Environment.NewLine + ex;
                    Accessor.Logger!.LogInfo("source", hdr);
                    return;

                case LoggerType.Callback:
                    if (logger is Action<Type, LogSeverity, Exception?, string?> callback)
                        callback(loggable.GetType(), LogSeverity.Warning, ex, message);

                    return;

                case LoggerType.MicrosoftLogger:
                    LogMsLogger(logger, LogSeverity.Warning, ex, message);
                    return;

                case LoggerType.ReflectionToolsLogger:
                    if (logger is not IReflectionToolsLogger reflLogger)
                        return;

                    string src = Accessor.Formatter.Format(loggable.GetType());
                    if (message != null)
                        reflLogger.LogWarning(src, message);
                    if (ex != null)
                        reflLogger.LogWarning(src, ex.ToString());
                    return;

                case LoggerType.ReflectionToolsAccessor:
                    if (logger is not IAccessor accessor)
                        return;

                    reflLogger = accessor.Logger!;
                    if (reflLogger == null)
                        return;

                    src = accessor.Formatter.Format(loggable.GetType());
                    if (message != null)
                        reflLogger.LogWarning(src, message);
                    if (ex != null)
                        reflLogger.LogWarning(src, ex.ToString());
                    return;

                case LoggerType.RefSafeLoggable:
                    if (logger is not IRefSafeLoggable loggable2)
                        return;

                    loggable = loggable2;
                    break;
            }
        }
    }

    /// <summary>
    /// Log an error to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogError(this IRefSafeLoggable loggable, string? message) => loggable.LogError(null, message);

    /// <summary>
    /// Log an error to an <see cref="IRefSafeLoggable"/> object.
    /// </summary>
    public static void LogError(this IRefSafeLoggable loggable, Exception? ex, string? message)
    {
        while (true)
        {
            LoggerType type = loggable.LoggerType;
            object? logger = loggable.Logger;
            switch (type)
            {
                default:
                    return;

                case LoggerType.Console:
                    string hdr = "[ERR] [" + Accessor.Formatter.Format(loggable.GetType()) + "]";
                    if (message != null)
                        hdr += " " + message;
                    if (ex != null)
                        hdr += Environment.NewLine + ex;
                    Accessor.Logger!.LogInfo("source", hdr);
                    return;

                case LoggerType.Callback:
                    if (logger is Action<Type, LogSeverity, Exception?, string?> callback)
                        callback(loggable.GetType(), LogSeverity.Error, ex, message);

                    return;

                case LoggerType.MicrosoftLogger:
                    LogMsLogger(logger, LogSeverity.Error, ex, message);
                    return;

                case LoggerType.ReflectionToolsLogger:
                    if (logger is not IReflectionToolsLogger reflLogger)
                        return;

                    reflLogger.LogError(Accessor.Formatter.Format(loggable.GetType()), ex, message);
                    return;

                case LoggerType.ReflectionToolsAccessor:
                    if (logger is not IAccessor accessor)
                        return;

                    reflLogger = accessor.Logger!;
                    if (reflLogger == null)
                        return;

                    reflLogger.LogError(accessor.Formatter.Format(loggable.GetType()), ex, message);
                    return;

                case LoggerType.RefSafeLoggable:
                    if (logger is not IRefSafeLoggable loggable2)
                        return;
                    loggable = loggable2;
                    break;
            }
        }
    }
    private static void LogMsLogger(object? logger, LogSeverity severity, Exception? ex, string? message)
    {
        if (logger is not ILogger msLogger)
            return;

        msLogger.Log((LogLevel)severity, ex, message, Array.Empty<object>());
    }
}

/// <summary>
/// Severity of a log message. Can be casted to <see cref="LogLevel"/>.
/// </summary>
public enum LogSeverity
{
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4
}