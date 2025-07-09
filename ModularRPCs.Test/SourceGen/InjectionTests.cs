using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class InjectionTests
    {
        private IDisposable _disposable;

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
        }

        [Test]
        public async Task ServerToClientBytes()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connection);
        }
    
        [Test]
        public async Task ClientToServerBytes()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient();
        }

        [Test]
        public async Task ServerToClientStream()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connection);
        }
    
        [Test]
        public async Task ClientToServerStream()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient();
        }

        [GenerateRpcSource]
        public partial class TestClass
        {
            [RpcSend(nameof(Receive))]
            public partial RpcTask InvokeFromClient();

            [RpcSend(nameof(Receive))]
            public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);

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
}
