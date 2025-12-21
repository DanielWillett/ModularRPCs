using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen
{
    internal static class TestSetup
    {
        public static Task<LoopbackRpcServersideRemoteConnection> SetupTest<T>(out IServiceProvider server, out IServiceProvider client, bool useStreams, out IDisposable disposable) where T : class
        {
            ServiceCollection collection = new ServiceCollection();

            ServiceProviderOptions opt = new ServiceProviderOptions
            {
                ValidateOnBuild = false,
                ValidateScopes = false
            };

            Accessor.LogILTraceMessages = true;
            Accessor.LogDebugMessages = true;
            Accessor.LogInfoMessages = true;
            Accessor.LogWarningMessages = true;
            Accessor.LogErrorMessages = true;

            collection.AddLogging(l => l.AddConsole());

            collection.AddReflectionTools();
            collection.AddModularRpcs(isServer: true);
            collection.AddRpcSingleton<T>();
            if (typeof(T).BaseType != typeof(object))
                collection.AddTransient(typeof(T).BaseType, sp => sp.GetRequiredService<T>());

            IServiceProvider serverProvider = collection.BuildServiceProvider(opt);
            server = serverProvider;

            _ = serverProvider.GetService<IAccessor>();

            collection = new ServiceCollection();

            collection.AddLogging(l => l.AddConsole());

            collection.AddReflectionTools();
            collection.AddModularRpcs(isServer: false);
            collection.AddRpcSingleton<T>();
            if (typeof(T).BaseType != typeof(object))
                collection.AddTransient(typeof(T).BaseType, sp => sp.GetRequiredService<T>());

            IServiceProvider clientProvider = collection.BuildServiceProvider(opt);
            client = clientProvider;

            ProxyGenerator.Instance.DefaultTimeout = TimeSpan.FromSeconds(5d);

            disposable = new TestState(new IDisposable[] { serverProvider as IDisposable, clientProvider as IDisposable });

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

        public static Task<LoopbackRpcServersideRemoteConnection[]> SetupTestWithMultipleClients<T>(int clientCt, out IServiceProvider server, out IServiceProvider[] clients, bool useStreams, out IDisposable disposable) where T : class
        {
            ServiceCollection collection = new ServiceCollection();

            ServiceProviderOptions opt = new ServiceProviderOptions
            {
                ValidateOnBuild = false,
                ValidateScopes = false
            };

            Accessor.LogILTraceMessages = true;
            Accessor.LogDebugMessages = true;
            Accessor.LogInfoMessages = true;
            Accessor.LogWarningMessages = true;
            Accessor.LogErrorMessages = true;

            collection.AddLogging(l => l.AddConsole());

            collection.AddReflectionTools();
            collection.AddModularRpcs(isServer: true);
            collection.AddRpcSingleton<T>();
            if (typeof(T).BaseType != typeof(object))
                collection.AddTransient(typeof(T).BaseType, sp => sp.GetRequiredService<T>());

            IServiceProvider serverProvider = collection.BuildServiceProvider(opt);
            server = serverProvider;

            _ = serverProvider.GetService<IAccessor>();

            IDisposable[] disposables = new IDisposable[clientCt + 1];
            clients = new IServiceProvider[clientCt];
            disposables[0] = serverProvider as IDisposable;

            for (int i = 0; i < clientCt; ++i)
            {
                collection = new ServiceCollection();

                collection.AddLogging(l => l.AddConsole());

                collection.AddReflectionTools();
                collection.AddModularRpcs(isServer: false);
                collection.AddRpcSingleton<T>();
                if (typeof(T).BaseType != typeof(object))
                    collection.AddTransient(typeof(T).BaseType, sp => sp.GetRequiredService<T>());

                IServiceProvider clientProvider = collection.BuildServiceProvider(opt);

                ProxyGenerator.Instance.DefaultTimeout = TimeSpan.FromSeconds(5d);
                disposables[i + 1] = clientProvider as IDisposable;
                clients[i] = clientProvider;
            }

            disposable = new TestState(disposables);

            IServiceProvider[] clientList = clients;

            return Intl();

            async Task<LoopbackRpcServersideRemoteConnection[]> Intl()
            {
                LoopbackRpcServersideRemoteConnection[] connections = new LoopbackRpcServersideRemoteConnection[clientCt];
                for (int i = 0; i < clientCt; ++i)
                {
                    LoopbackEndpoint endpoint = new LoopbackEndpoint(false, useStreams);

                    LoopbackRpcClientsideRemoteConnection remote;
                    try
                    {
                        remote = (LoopbackRpcClientsideRemoteConnection)await endpoint.RequestConnectionAsync(clientList[i], serverProvider!);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Assert.Fail("Exception thrown");
                        throw;
                    }
                    remote.UseContiguousBuffer = true;
                    remote.Server.UseContiguousBuffer = false; // todo

                    Assert.That(remote.IsClosed, Is.False);
                    Assert.That(remote.Local.IsClosed, Is.False);
                    Assert.That(remote.Server.IsClosed, Is.False);
                    Assert.That(remote.Server.Local.IsClosed, Is.False);

                    connections[i] = remote.Server;
                }

                return connections;
            }
        }

        private class TestState : IDisposable
        {
            private readonly IDisposable[] _disposables;

            public TestState(IDisposable[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                List<ExceptionDispatchInfo> ex = null;
                for (int i = 0; i < _disposables.Length; ++i)
                {
                    try
                    {
                        _disposables[i]?.Dispose();
                    }
                    catch (Exception e)
                    {
                        ex ??= new List<ExceptionDispatchInfo>(4);
                        ex.Add(ExceptionDispatchInfo.Capture(e));
                    }
                }

                if (ex == null)
                    return;

                if (ex.Count == 1)
                    ex[0].Throw();
                else
                    throw new AggregateException(ex.Select(x => x.SourceException));
            }
        }
    }
}