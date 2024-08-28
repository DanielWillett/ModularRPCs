using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen;
internal static class TestSetup
{
    public static Task<LoopbackRpcServersideRemoteConnection> SetupTest<T>(out IServiceProvider server, out IServiceProvider client, bool useStreams) where T : class
    {
        ServiceCollection collection = new ServiceCollection();

        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        collection.AddLogging();
        collection.AddReflectionTools();
        collection.AddModularRpcs(isServer: true);
        collection.AddRpcSingleton<T>();

        IServiceProvider serverProvider = collection.BuildServiceProvider();
        server = serverProvider;

        collection = new ServiceCollection();

        collection.AddLogging();
        collection.AddReflectionTools();
        collection.AddModularRpcs(isServer: false);
        collection.AddRpcSingleton<T>();

        IServiceProvider clientProvider = collection.BuildServiceProvider();
        client = clientProvider;

        ProxyGenerator.Instance.DefaultTimeout = TimeSpan.FromMinutes(2d);

        return Intl();

        async Task<LoopbackRpcServersideRemoteConnection> Intl()
        {
            LoopbackEndpoint endpoint = new LoopbackEndpoint(false, useStreams);

            LoopbackRpcClientsideRemoteConnection remote =
                (LoopbackRpcClientsideRemoteConnection)await endpoint.RequestConnectionAsync(
                    clientProvider.GetRequiredService<IRpcRouter>(),
                    serverProvider.GetRequiredService<IRpcRouter>(),
                    clientProvider.GetRequiredService<IRpcConnectionLifetime>(),
                    serverProvider.GetRequiredService<IRpcConnectionLifetime>()
                );

            Assert.That(remote.IsClosed,              Is.False);
            Assert.That(remote.Local.IsClosed,        Is.False);
            Assert.That(remote.Server.IsClosed,       Is.False);
            Assert.That(remote.Server.Local.IsClosed, Is.False);

            return remote.Server;
        }
    }
}
