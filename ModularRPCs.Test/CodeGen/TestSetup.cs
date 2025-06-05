using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ModularRPCs.Test.CodeGen;
internal static class TestSetup
{
    public static Task<LoopbackRpcServersideRemoteConnection> SetupTest<T>(out IServiceProvider server, out IServiceProvider client, bool useStreams) where T : class
    {
        ServiceCollection collection = new ServiceCollection();

        Accessor.LogILTraceMessages = true;
        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        ILoggerFactory factory = LoggerFactory.Create(l => l
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole()
            .AddProvider(new FileLoggerProvider())
        );

        Accessor.Logger = new ReflectionToolsLoggerProxy(factory, false);

        collection.AddSingleton(factory);
        collection.AddTransient(typeof(ILogger<>), typeof(Logger<>));

        collection.AddSingleton(Accessor.Active);
        collection.AddTransient(_ => Accessor.Logger);
        collection.AddTransient(_ => Accessor.Formatter);
        collection.AddModularRpcs(isServer: true);
        collection.AddRpcSingleton<T>();

        IServiceProvider serverProvider = collection.BuildServiceProvider();
        server = serverProvider;

        _ = serverProvider.GetService<IAccessor>();

        collection = new ServiceCollection();

        collection.AddLogging(l => l
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole()
            .AddProvider(new FileLoggerProvider())
        );
        collection.AddSingleton(Accessor.Active);
        collection.AddTransient(_ => Accessor.Logger);
        collection.AddTransient(_ => Accessor.Formatter);
        collection.AddModularRpcs(isServer: false);
        collection.AddRpcSingleton<T>();

        IServiceProvider clientProvider = collection.BuildServiceProvider();
        client = clientProvider;

        ProxyGenerator.Instance.DefaultTimeout = TimeSpan.FromSeconds(5d);

        return Intl();

        async Task<LoopbackRpcServersideRemoteConnection> Intl()
        {
            LoopbackEndpoint endpoint = new LoopbackEndpoint(false, useStreams);

            LoopbackRpcClientsideRemoteConnection remote;
            try
            {
                remote = (LoopbackRpcClientsideRemoteConnection)await endpoint.RequestConnectionAsync(clientProvider, serverProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Assert.Fail("Exception thrown");
                throw;
            }
            remote.UseContiguousBuffer = true;
            remote.Server.UseContiguousBuffer = false; // todo

            Assert.That(remote.IsClosed,              Is.False);
            Assert.That(remote.Local.IsClosed,        Is.False);
            Assert.That(remote.Server.IsClosed,       Is.False);
            Assert.That(remote.Server.Local.IsClosed, Is.False);

            return remote.Server;
        }
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private static readonly StreamWriter StreamWriter = new StreamWriter(new FileStream("log.txt", FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan), Encoding.UTF8, 1024)
    {
        AutoFlush = true
    };
    
    /// <inheritdoc />
    public void Dispose()
    {
        StreamWriter.Dispose();
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName);
    }

    private class FileLogger(string category) : ILogger
    {
        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string fmt = category + " | " + formatter(state, exception);
            if (exception != null)
                fmt += Environment.NewLine + exception;

            lock (StreamWriter)
            {
                StreamWriter.WriteLine(fmt);
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}