using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.Logging;

namespace ModularRPCs.Test.CodeGen;

[NonParallelizable, TestFixture]
public class InjectionTests
{
    [Test]
    public async Task ServerToClientBytes()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        await proxy.InvokeFromServer(connection);
    }
    
    [Test]
    public async Task ClientToServerBytes()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        await proxy.InvokeFromClient();
    }

    [Test]
    public async Task ServerToClientStream()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        await proxy.InvokeFromServer(connection);
    }
    
    [Test]
    public async Task ClientToServerStream()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        await proxy.InvokeFromClient();
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive))]
        public virtual RpcTask InvokeFromClient() => RpcTask.NotImplemented;

        [RpcSend(nameof(Receive))]
        public virtual RpcTask InvokeFromServer(IModularRpcRemoteConnection connection) => RpcTask.NotImplemented;

        [RpcReceive]
        private void Receive([RpcInject] ILogger<TestClass> logger,
            IModularRpcRemoteConnection remote,
            IModularRpcLocalConnection local,
            IRpcRouter router,
            IRpcSerializer serializer,
            RpcOverhead overhead,
            IEnumerable<IModularRpcConnection> connections,
            RpcFlags flags
            )
        {
            Assert.That(logger, Is.Not.Null);
            Assert.That(remote, Is.Not.Null);
            Assert.That(local, Is.Not.Null);
            Assert.That(router, Is.Not.Null);
            Assert.That(serializer, Is.Not.Null);
            Assert.That(overhead, Is.Not.Null);
            Assert.That(connections, Is.Not.Null);
        }
    }
}
