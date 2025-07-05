using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DanielWillett.ReflectionTools.IoC;

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

            IServiceProvider serverProvider = collection.BuildServiceProvider(opt);
            server = serverProvider;

            _ = serverProvider.GetService<IAccessor>();

            collection = new ServiceCollection();

            collection.AddLogging(l => l.AddConsole());

            collection.AddReflectionTools();
            collection.AddModularRpcs(isServer: false);
            collection.AddRpcSingleton<T>();

            IServiceProvider clientProvider = collection.BuildServiceProvider(opt);
            client = clientProvider;

            ProxyGenerator.Instance.DefaultTimeout = TimeSpan.FromSeconds(5d);

            disposable = new TestState(serverProvider as IDisposable, clientProvider as IDisposable);

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

        private class TestState : IDisposable
        {
            private readonly IDisposable _d1;
            private readonly IDisposable _d2;

            public TestState(IDisposable d1, IDisposable d2)
            {
                _d1 = d1;
                _d2 = d2;
            }

            public void Dispose()
            {
                try
                {
                    _d1?.Dispose();
                }
                catch
                {
                    _d2?.Dispose();
                    throw;
                }

                _d2?.Dispose();
            }
        }
    }
}