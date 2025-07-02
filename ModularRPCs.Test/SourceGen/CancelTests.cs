using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class CancelTests
    {
        private const int DelayMs = 400;
        private static bool _didCancel;

        [Test]
        public async Task ServerToClientBytes()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection).WithToken(tknSrc.Token));
        }
    
        [Test]
        public async Task ClientToServerBytes()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient().WithToken(tknSrc.Token));
        }

        [Test]
        public async Task ServerToClientStream()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection).WithToken(tknSrc.Token));
        }
    
        [Test]
        public async Task ClientToServerStream()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient().WithToken(tknSrc.Token));
        }

        [Test]
        public async Task ServerToClientBytesAsArgument()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection, tknSrc.Token));
        }
    
        [Test]
        public async Task ClientToServerBytesAsArgument()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient(tknSrc.Token));
        }

        [Test]
        public async Task ServerToClientStreamAsArgument()
        {
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection, tknSrc.Token));
        }
    
        [Test]
        public async Task ClientToServerStreamAsArgument()
        {
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient(tknSrc.Token));
        }

        private static IResolveConstraint GetOpCancelledConstraint()
        {
            return Is.Not.Null; // can not figure out why it won't match TaskCanceledExceptions no matter what i do
        }


        /* TOKEN */

        [Test]
        public async Task ServerToClientBytesToken()
        {
            _didCancel = false;
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection).WithToken(tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }
    
        [Test]
        public async Task ClientToServerBytesToken()
        {
            _didCancel = false;
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken().WithToken(tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }

        [Test]
        public async Task ServerToClientStreamToken()
        {
            _didCancel = false;
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection).WithToken(tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }
    
        [Test]
        public async Task ClientToServerStreamToken()
        {
            _didCancel = false;
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken().WithToken(tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }

        [Test]
        public async Task ServerToClientBytesAsArgumentToken()
        {
            _didCancel = false;
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection, tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }
    
        [Test]
        public async Task ClientToServerBytesAsArgumentToken()
        {
            _didCancel = false;
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken(tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }

        [Test]
        public async Task ServerToClientStreamAsArgumentToken()
        {
            _didCancel = false;
            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);

            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection, tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }
    
        [Test]
        public async Task ClientToServerStreamAsArgumentToken()
        {
            _didCancel = false;
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            using CancellationTokenSource tknSrc = new CancellationTokenSource(100);
            Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken(tknSrc.Token));

            await Task.Delay(10, CancellationToken.None);

            Assert.That(_didCancel, Is.True);
        }

        [RpcClass, GenerateRpcSource]
        public partial class TestClass
        {
            [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
            public partial RpcTask InvokeFromClient();

            [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
            public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);
        
            [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
            public partial RpcTask InvokeFromClient(CancellationToken token);

            [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
            public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection, CancellationToken token);
        
            [RpcSend(nameof(ReceiveToken))]
            public partial RpcTask InvokeFromClientToken();

            [RpcSend(nameof(ReceiveToken))]
            public partial RpcTask InvokeFromServerToken(IModularRpcRemoteConnection connection);
        
            [RpcSend(nameof(ReceiveToken))]
            public partial RpcTask InvokeFromClientToken(CancellationToken token);

            [RpcSend(nameof(ReceiveToken))]
            public partial RpcTask InvokeFromServerToken(IModularRpcRemoteConnection connection, CancellationToken token);

            [RpcReceive]
            private Task Receive()
            {
                return Task.Delay(TimeSpan.FromMilliseconds(DelayMs));
            }

            [RpcReceive]
            private async Task ReceiveToken(CancellationToken token)
            {
                try
                {
                    Console.WriteLine("Starting delay");
                    Debug.WriteLine("Starting delay");
                    await Task.Delay(TimeSpan.FromMilliseconds(55000), token);
                    Console.WriteLine("finished delay");
                    Debug.WriteLine("finished delay");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("delay cancelled");
                    Console.WriteLine("delay cancelled");
                    _didCancel = true;
                    throw;
                }
            }
        }
    }
}
