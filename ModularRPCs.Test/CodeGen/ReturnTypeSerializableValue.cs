using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class ReturnTypeSerializableValue
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
        public async Task ServerToClientBytesSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeFromServerSerializableTypeClassFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeFromClientSerializableTypeClassFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeFromServerSerializableTypeClassFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeFromClientSerializableTypeClassFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeTaskFromServerSerializableTypeClassFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeTaskFromClientSerializableTypeClassFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeTaskFromServerSerializableTypeClassFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed rtnValue = await proxy.InvokeTaskFromClientSerializableTypeClassFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassFixed { Value = RtnValueUInt64 }));
        }


        [Test]
        public async Task ServerToClientBytesSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeFromServerSerializableTypeClassVariable(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeFromClientSerializableTypeClassVariable();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeFromServerSerializableTypeClassVariable(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeFromClientSerializableTypeClassVariable();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }

        [Test]
        public async Task ServerToClientBytesSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeFromServerSerializableTypeStructFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeFromClientSerializableTypeStructFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeFromServerSerializableTypeStructFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeFromClientSerializableTypeStructFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeTaskFromServerSerializableTypeStructFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeTaskFromClientSerializableTypeStructFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeTaskFromServerSerializableTypeStructFixed(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructFixed()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed rtnValue = await proxy.InvokeTaskFromClientSerializableTypeStructFixed();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructFixed { Value = RtnValueUInt64 }));
        }


        [Test]
        public async Task ServerToClientBytesSerializableTypeStructVariable()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable rtnValue = await proxy.InvokeFromServerSerializableTypeStructVariable(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructVariable()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable rtnValue = await proxy.InvokeFromClientSerializableTypeStructVariable();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructVariable()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable rtnValue = await proxy.InvokeFromServerSerializableTypeStructVariable(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructVariable()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable rtnValue = await proxy.InvokeFromClientSerializableTypeStructVariable();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeStructVariable { Value = RtnValueString }));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeTaskFromServerSerializableTypeClassVariable(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeTaskFromClientSerializableTypeClassVariable();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeTaskFromServerSerializableTypeClassVariable(connection);

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassVariable()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable rtnValue = await proxy.InvokeTaskFromClientSerializableTypeClassVariable();

            Assert.That(_wasInvoked, Is.True);
            Assert.That(rtnValue, Is.EqualTo(new SerializableTypeClassVariable { Value = RtnValueString }));
        }

        
        public class TestClass
        {
            [RpcSend(nameof(ReceiveSerializableTypeClassFixed))]
            public virtual RpcTask<SerializableTypeClassFixed> InvokeFromClientSerializableTypeClassFixed() => RpcTask<SerializableTypeClassFixed>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeClassFixed))]
            public virtual RpcTask<SerializableTypeClassFixed> InvokeFromServerSerializableTypeClassFixed(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassFixed>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeClassFixedTask))]
            public virtual RpcTask<SerializableTypeClassFixed> InvokeTaskFromClientSerializableTypeClassFixed() => RpcTask<SerializableTypeClassFixed>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeClassFixedTask))]
            public virtual RpcTask<SerializableTypeClassFixed> InvokeTaskFromServerSerializableTypeClassFixed(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassFixed>.NotImplemented;

            [RpcReceive]
            private SerializableTypeClassFixed ReceiveSerializableTypeClassFixed()
            {
                _wasInvoked = true;

                return new SerializableTypeClassFixed { Value = RtnValueUInt64 };
            }

            [RpcReceive]
            private async Task<SerializableTypeClassFixed> ReceiveSerializableTypeClassFixedTask()
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return new SerializableTypeClassFixed { Value = RtnValueUInt64 };
            }



            [RpcSend(nameof(ReceiveSerializableTypeClassVariable))]
            public virtual RpcTask<SerializableTypeClassVariable> InvokeFromClientSerializableTypeClassVariable() => RpcTask<SerializableTypeClassVariable>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeClassVariable))]
            public virtual RpcTask<SerializableTypeClassVariable> InvokeFromServerSerializableTypeClassVariable(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassVariable>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeClassVariableTask))]
            public virtual RpcTask<SerializableTypeClassVariable> InvokeTaskFromClientSerializableTypeClassVariable() => RpcTask<SerializableTypeClassVariable>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeClassVariableTask))]
            public virtual RpcTask<SerializableTypeClassVariable> InvokeTaskFromServerSerializableTypeClassVariable(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassVariable>.NotImplemented;

            [RpcReceive]
            private SerializableTypeClassVariable ReceiveSerializableTypeClassVariable()
            {
                _wasInvoked = true;

                return new SerializableTypeClassVariable { Value = RtnValueString };
            }

            [RpcReceive]
            private async Task<SerializableTypeClassVariable> ReceiveSerializableTypeClassVariableTask()
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return new SerializableTypeClassVariable { Value = RtnValueString };
            }



            [RpcSend(nameof(ReceiveSerializableTypeStructFixed))]
            public virtual RpcTask<SerializableTypeStructFixed> InvokeFromClientSerializableTypeStructFixed() => RpcTask<SerializableTypeStructFixed>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeStructFixed))]
            public virtual RpcTask<SerializableTypeStructFixed> InvokeFromServerSerializableTypeStructFixed(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructFixed>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeStructFixedTask))]
            public virtual RpcTask<SerializableTypeStructFixed> InvokeTaskFromClientSerializableTypeStructFixed() => RpcTask<SerializableTypeStructFixed>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeStructFixedTask))]
            public virtual RpcTask<SerializableTypeStructFixed> InvokeTaskFromServerSerializableTypeStructFixed(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructFixed>.NotImplemented;

            [RpcReceive]
            private SerializableTypeStructFixed ReceiveSerializableTypeStructFixed()
            {
                _wasInvoked = true;

                return new SerializableTypeStructFixed { Value = RtnValueUInt64 };
            }

            [RpcReceive]
            private async Task<SerializableTypeStructFixed> ReceiveSerializableTypeStructFixedTask()
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return new SerializableTypeStructFixed { Value = RtnValueUInt64 };
            }



            [RpcSend(nameof(ReceiveSerializableTypeStructVariable))]
            public virtual RpcTask<SerializableTypeStructVariable> InvokeFromClientSerializableTypeStructVariable() => RpcTask<SerializableTypeStructVariable>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeStructVariable))]
            public virtual RpcTask<SerializableTypeStructVariable> InvokeFromServerSerializableTypeStructVariable(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructVariable>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeStructVariableTask))]
            public virtual RpcTask<SerializableTypeStructVariable> InvokeTaskFromClientSerializableTypeStructVariable() => RpcTask<SerializableTypeStructVariable>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableTypeStructVariableTask))]
            public virtual RpcTask<SerializableTypeStructVariable> InvokeTaskFromServerSerializableTypeStructVariable(IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructVariable>.NotImplemented;

            [RpcReceive]
            private SerializableTypeStructVariable ReceiveSerializableTypeStructVariable()
            {
                _wasInvoked = true;

                return new SerializableTypeStructVariable { Value = RtnValueString };
            }

            [RpcReceive]
            private async Task<SerializableTypeStructVariable> ReceiveSerializableTypeStructVariableTask()
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return new SerializableTypeStructVariable { Value = RtnValueString };
            }
        }
    }


    [RpcSerializable(sizeof(ulong), isFixedSize: true)]
    public class SerializableTypeClassFixed : IRpcSerializable
    {
        public ulong Value;

        /// <inheritdoc />
        public int GetSize(IRpcSerializer serializer)
        {
            return sizeof(ulong);
        }

        /// <inheritdoc />
        public int Write(Span<byte> writeTo, IRpcSerializer serializer)
        {
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(writeTo, in Value);
#else
        MemoryMarshal.Write(writeTo, ref Value);
#endif
            return sizeof(ulong);
        }

        /// <inheritdoc />
        public int Read(Span<byte> readFrom, IRpcSerializer serializer)
        {
            Value = MemoryMarshal.Read<ulong>(readFrom);
            return sizeof(ulong);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SerializableTypeClassFixed s && Value == s.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    [RpcSerializable(SerializationHelper.MinimumStringSize, isFixedSize: false)]
    public class SerializableTypeClassVariable : IRpcSerializable
    {
        public string Value;

        /// <inheritdoc />
        public int GetSize(IRpcSerializer serializer)
        {
            return serializer.GetSize(Value);
        }

        /// <inheritdoc />
        public int Write(Span<byte> writeTo, IRpcSerializer serializer)
        {
            return serializer.WriteObject(Value, writeTo);
        }

        /// <inheritdoc />
        public int Read(Span<byte> readFrom, IRpcSerializer serializer)
        {
            Value = serializer.ReadObject<string>(readFrom, out int bytesRead);
            return bytesRead;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SerializableTypeClassVariable s && Equals(Value, s.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }
    }

    [RpcSerializable(sizeof(ulong), isFixedSize: true)]
    public struct SerializableTypeStructFixed : IRpcSerializable
    {
        public ulong Value;

        /// <inheritdoc />
        public int GetSize(IRpcSerializer serializer)
        {
            return sizeof(ulong);
        }

        /// <inheritdoc />
        public int Write(Span<byte> writeTo, IRpcSerializer serializer)
        {
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(writeTo, in Value);
#else
        MemoryMarshal.Write(writeTo, ref Value);
#endif
            return sizeof(ulong);
        }

        /// <inheritdoc />
        public int Read(Span<byte> readFrom, IRpcSerializer serializer)
        {
            Value = MemoryMarshal.Read<ulong>(readFrom);
            return sizeof(ulong);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SerializableTypeStructFixed s && Value == s.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    [RpcSerializable(SerializationHelper.MinimumStringSize, isFixedSize: false)]
    public struct SerializableTypeStructVariable : IRpcSerializable
    {
        public string Value;

        /// <inheritdoc />
        public int GetSize(IRpcSerializer serializer)
        {
            return serializer.GetSize(Value);
        }

        /// <inheritdoc />
        public int Write(Span<byte> writeTo, IRpcSerializer serializer)
        {
            return serializer.WriteObject(Value, writeTo);
        }

        /// <inheritdoc />
        public int Read(Span<byte> readFrom, IRpcSerializer serializer)
        {
            Value = serializer.ReadObject<string>(readFrom, out int bytesRead);
            return bytesRead;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SerializableTypeStructVariable s && Equals(Value, s.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }
    }
}
