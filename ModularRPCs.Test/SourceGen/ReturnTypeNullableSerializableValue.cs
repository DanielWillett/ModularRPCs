using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class ReturnTypeNullableSerializableValue
    {
        private IDisposable _disposable;

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
        }

        private static bool _wasInvoked;
        private const ulong RtnValueUInt64 = 536ul;
        private const string RtnValueString = "test";

        [Test]
        public async Task ServerToClientBytesSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeFromServerSerializableTypeStructFixed(isNull, connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeFromClientSerializableTypeStructFixed(isNull);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeFromServerSerializableTypeStructFixed(isNull, connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeFromClientSerializableTypeStructFixed(isNull);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeTaskFromServerSerializableTypeStructFixed(isNull, connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeTaskFromClientSerializableTypeStructFixed(isNull);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeTaskFromServerSerializableTypeStructFixed(isNull, connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructFixed([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed? rtnValue = await proxy.InvokeTaskFromClientSerializableTypeStructFixed(isNull);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }


        [Test]
        public async Task ServerToClientBytesSerializableTypeStructVariable([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable? rtnValue = await proxy.InvokeFromServerSerializableTypeStructVariable(isNull, connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructVariable([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable? rtnValue = await proxy.InvokeFromClientSerializableTypeStructVariable(isNull);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructVariable([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable? rtnValue = await proxy.InvokeFromServerSerializableTypeStructVariable(isNull, connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructVariable([Values(true, false)] bool isNull)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable? rtnValue = await proxy.InvokeFromClientSerializableTypeStructVariable(isNull);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, isNull ? Is.Null : Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }

        [GenerateRpcSource]
        public partial class TestClass
        {
            [RpcSend(nameof(ReceiveSerializableTypeStructFixed))]
            public partial RpcTask<SerializableTypeStructFixed?> InvokeFromClientSerializableTypeStructFixed(bool isNull);

            [RpcSend(nameof(ReceiveSerializableTypeStructFixed))]
            public partial RpcTask<SerializableTypeStructFixed?> InvokeFromServerSerializableTypeStructFixed(bool isNull, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableTypeStructFixedTask))]
            public partial RpcTask<SerializableTypeStructFixed?> InvokeTaskFromClientSerializableTypeStructFixed(bool isNull);

            [RpcSend(nameof(ReceiveSerializableTypeStructFixedTask))]
            public partial RpcTask<SerializableTypeStructFixed?> InvokeTaskFromServerSerializableTypeStructFixed(bool isNull, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private SerializableTypeStructFixed? ReceiveSerializableTypeStructFixed(bool isNull)
            {
                _wasInvoked = true;

                return isNull ? null : new SerializableTypeStructFixed { Value = RtnValueUInt64 };
            }

            [RpcReceive]
            private async Task<SerializableTypeStructFixed?> ReceiveSerializableTypeStructFixedTask(bool isNull)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return isNull ? null : new SerializableTypeStructFixed { Value = RtnValueUInt64 };
            }



            [RpcSend(nameof(ReceiveSerializableTypeStructVariable))]
            public partial RpcTask<SerializableTypeStructVariable?> InvokeFromClientSerializableTypeStructVariable(bool isNull);

            [RpcSend(nameof(ReceiveSerializableTypeStructVariable))]
            public partial RpcTask<SerializableTypeStructVariable?> InvokeFromServerSerializableTypeStructVariable(bool isNull, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableTypeStructVariableTask))]
            public partial RpcTask<SerializableTypeStructVariable?> InvokeTaskFromClientSerializableTypeStructVariable(bool isNull);

            [RpcSend(nameof(ReceiveSerializableTypeStructVariableTask))]
            public partial RpcTask<SerializableTypeStructVariable?> InvokeTaskFromServerSerializableTypeStructVariable(bool isNull, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private SerializableTypeStructVariable? ReceiveSerializableTypeStructVariable(bool isNull)
            {
                _wasInvoked = true;

                return isNull ? null : new SerializableTypeStructVariable { Value = RtnValueString };
            }

            [RpcReceive]
            private async Task<SerializableTypeStructVariable?> ReceiveSerializableTypeStructVariableTask(bool isNull)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return isNull ? null : new SerializableTypeStructVariable { Value = RtnValueString };
            }
        }
    }
}