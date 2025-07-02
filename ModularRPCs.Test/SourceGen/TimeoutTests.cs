using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class TimeoutTests
    {
        private const int MsTimeout = 100;
        private const int MsTolerance = 50;

        [Test]
        public async Task ServerToClientBytes()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            Stopwatch timer = Stopwatch.StartNew();
            Assert.ThrowsAsync(Is.TypeOf<RpcTimeoutException>(), async () => await proxy.InvokeFromServer(connection));
            timer.Stop();

            Assert.That(timer.ElapsedMilliseconds, Is.InRange(MsTimeout - MsTolerance, MsTimeout + MsTolerance));
        }
    
        [Test]
        public async Task ClientToServerBytes()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            Stopwatch timer = Stopwatch.StartNew();
            Assert.ThrowsAsync(Is.TypeOf<RpcTimeoutException>(), async () => await proxy.InvokeFromClient());
            timer.Stop();

            Assert.That(timer.ElapsedMilliseconds, Is.InRange(MsTimeout - MsTolerance, MsTimeout + MsTolerance));
        }

        [Test]
        public async Task ServerToClientStream()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            Stopwatch timer = Stopwatch.StartNew();
            Assert.ThrowsAsync(Is.TypeOf<RpcTimeoutException>(), async () => await proxy.InvokeFromServer(connection));
            timer.Stop();

            Assert.That(timer.ElapsedMilliseconds, Is.InRange(MsTimeout - MsTolerance, MsTimeout + MsTolerance));
        }
    
        [Test]
        public async Task ClientToServerStream()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            Stopwatch timer = Stopwatch.StartNew();
            Assert.ThrowsAsync(Is.TypeOf<RpcTimeoutException>(), async () => await proxy.InvokeFromClient());
            timer.Stop();

            Assert.That(timer.ElapsedMilliseconds, Is.InRange(MsTimeout - MsTolerance, MsTimeout + MsTolerance));
        }

        [RpcClass, GenerateRpcSource]
        public partial class TestClass
        {
            [RpcSend(nameof(Receive)), RpcTimeout(MsTimeout)]
            public partial RpcTask InvokeFromClient();

            [RpcSend(nameof(Receive)), RpcTimeout(MsTimeout)]
            public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);

            [RpcReceive]
            private Task Receive()
            {
                return Task.Delay(TimeSpan.FromMilliseconds(MsTimeout + 500));
            }
        }
    }
}
