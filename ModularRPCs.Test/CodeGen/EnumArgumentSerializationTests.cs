using System;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ModularRPCs.Test.CodeGen;

[NonParallelizable, TestFixture]
public class EnumArgumentSerializationTests
{
    private static bool _wasInvoked;

    [Test]
    public async Task ServerToClientInt8Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int8Enum value = EnumSerializationTests.Int8Enum.C;

        EnumSerializationTests.Int8Enum rtnValue = await proxy.InvokeInt8EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt8Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int8Enum value = EnumSerializationTests.Int8Enum.C;

        EnumSerializationTests.Int8Enum rtnValue = await proxy.InvokeInt8EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt8Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int8Enum value = EnumSerializationTests.Int8Enum.C;

        EnumSerializationTests.Int8Enum rtnValue = await proxy.InvokeInt8EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt8Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int8Enum value = EnumSerializationTests.Int8Enum.C;

        EnumSerializationTests.Int8Enum rtnValue = await proxy.InvokeInt8EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt8Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt8Enum value = EnumSerializationTests.UInt8Enum.C;

        EnumSerializationTests.UInt8Enum rtnValue = await proxy.InvokeUInt8EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt8Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt8Enum value = EnumSerializationTests.UInt8Enum.C;

        EnumSerializationTests.UInt8Enum rtnValue = await proxy.InvokeUInt8EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt8Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt8Enum value = EnumSerializationTests.UInt8Enum.C;

        EnumSerializationTests.UInt8Enum rtnValue = await proxy.InvokeUInt8EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt8Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt8Enum value = EnumSerializationTests.UInt8Enum.C;

        EnumSerializationTests.UInt8Enum rtnValue = await proxy.InvokeUInt8EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt16Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int16Enum value = EnumSerializationTests.Int16Enum.C;

        EnumSerializationTests.Int16Enum rtnValue = await proxy.InvokeInt16EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt16Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int16Enum value = EnumSerializationTests.Int16Enum.C;

        EnumSerializationTests.Int16Enum rtnValue = await proxy.InvokeInt16EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt16Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int16Enum value = EnumSerializationTests.Int16Enum.C;

        EnumSerializationTests.Int16Enum rtnValue = await proxy.InvokeInt16EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt16Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int16Enum value = EnumSerializationTests.Int16Enum.C;

        EnumSerializationTests.Int16Enum rtnValue = await proxy.InvokeInt16EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt16Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt16Enum value = EnumSerializationTests.UInt16Enum.C;

        EnumSerializationTests.UInt16Enum rtnValue = await proxy.InvokeUInt16EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt16Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt16Enum value = EnumSerializationTests.UInt16Enum.C;

        EnumSerializationTests.UInt16Enum rtnValue = await proxy.InvokeUInt16EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt16Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt16Enum value = EnumSerializationTests.UInt16Enum.C;

        EnumSerializationTests.UInt16Enum rtnValue = await proxy.InvokeUInt16EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt16Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt16Enum value = EnumSerializationTests.UInt16Enum.C;

        EnumSerializationTests.UInt16Enum rtnValue = await proxy.InvokeUInt16EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt32Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int32Enum value = EnumSerializationTests.Int32Enum.C;

        EnumSerializationTests.Int32Enum rtnValue = await proxy.InvokeInt32EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt32Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int32Enum value = EnumSerializationTests.Int32Enum.C;

        EnumSerializationTests.Int32Enum rtnValue = await proxy.InvokeInt32EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt32Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int32Enum value = EnumSerializationTests.Int32Enum.C;

        EnumSerializationTests.Int32Enum rtnValue = await proxy.InvokeInt32EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt32Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int32Enum value = EnumSerializationTests.Int32Enum.C;

        EnumSerializationTests.Int32Enum rtnValue = await proxy.InvokeInt32EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt32Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt32Enum value = EnumSerializationTests.UInt32Enum.C;

        EnumSerializationTests.UInt32Enum rtnValue = await proxy.InvokeUInt32EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt32Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt32Enum value = EnumSerializationTests.UInt32Enum.C;

        EnumSerializationTests.UInt32Enum rtnValue = await proxy.InvokeUInt32EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt32Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt32Enum value = EnumSerializationTests.UInt32Enum.C;

        EnumSerializationTests.UInt32Enum rtnValue = await proxy.InvokeUInt32EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt32Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt32Enum value = EnumSerializationTests.UInt32Enum.C;

        EnumSerializationTests.UInt32Enum rtnValue = await proxy.InvokeUInt32EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt64Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int64Enum value = EnumSerializationTests.Int64Enum.C;

        EnumSerializationTests.Int64Enum rtnValue = await proxy.InvokeInt64EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt64Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int64Enum value = EnumSerializationTests.Int64Enum.C;

        EnumSerializationTests.Int64Enum rtnValue = await proxy.InvokeInt64EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientInt64Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int64Enum value = EnumSerializationTests.Int64Enum.C;

        EnumSerializationTests.Int64Enum rtnValue = await proxy.InvokeInt64EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerInt64Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.Int64Enum value = EnumSerializationTests.Int64Enum.C;

        EnumSerializationTests.Int64Enum rtnValue = await proxy.InvokeInt64EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt64Bytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt64Enum value = EnumSerializationTests.UInt64Enum.C;

        EnumSerializationTests.UInt64Enum rtnValue = await proxy.InvokeUInt64EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt64Bytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt64Enum value = EnumSerializationTests.UInt64Enum.C;

        EnumSerializationTests.UInt64Enum rtnValue = await proxy.InvokeUInt64EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ServerToClientUInt64Stream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt64Enum value = EnumSerializationTests.UInt64Enum.C;

        EnumSerializationTests.UInt64Enum rtnValue = await proxy.InvokeUInt64EnumFromServer(value, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [Test]
    public async Task ClientToServerUInt64Stream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        const EnumSerializationTests.UInt64Enum value = EnumSerializationTests.UInt64Enum.C;

        EnumSerializationTests.UInt64Enum rtnValue = await proxy.InvokeUInt64EnumFromClient(value);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(value));
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(ReceiveInt8Enum))]
        public virtual RpcTask<EnumSerializationTests.Int8Enum> InvokeInt8EnumFromClient(EnumSerializationTests.Int8Enum value) => RpcTask<EnumSerializationTests.Int8Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveInt8Enum))]
        public virtual RpcTask<EnumSerializationTests.Int8Enum> InvokeInt8EnumFromServer(EnumSerializationTests.Int8Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.Int8Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.Int8Enum ReceiveInt8Enum(EnumSerializationTests.Int8Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveInt16Enum))]
        public virtual RpcTask<EnumSerializationTests.Int16Enum> InvokeInt16EnumFromClient(EnumSerializationTests.Int16Enum value) => RpcTask<EnumSerializationTests.Int16Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveInt16Enum))]
        public virtual RpcTask<EnumSerializationTests.Int16Enum> InvokeInt16EnumFromServer(EnumSerializationTests.Int16Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.Int16Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.Int16Enum ReceiveInt16Enum(EnumSerializationTests.Int16Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveInt32Enum))]
        public virtual RpcTask<EnumSerializationTests.Int32Enum> InvokeInt32EnumFromClient(EnumSerializationTests.Int32Enum value) => RpcTask<EnumSerializationTests.Int32Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveInt32Enum))]
        public virtual RpcTask<EnumSerializationTests.Int32Enum> InvokeInt32EnumFromServer(EnumSerializationTests.Int32Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.Int32Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.Int32Enum ReceiveInt32Enum(EnumSerializationTests.Int32Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveInt64Enum))]
        public virtual RpcTask<EnumSerializationTests.Int64Enum> InvokeInt64EnumFromClient(EnumSerializationTests.Int64Enum value) => RpcTask<EnumSerializationTests.Int64Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveInt64Enum))]
        public virtual RpcTask<EnumSerializationTests.Int64Enum> InvokeInt64EnumFromServer(EnumSerializationTests.Int64Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.Int64Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.Int64Enum ReceiveInt64Enum(EnumSerializationTests.Int64Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveUInt8Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt8Enum> InvokeUInt8EnumFromClient(EnumSerializationTests.UInt8Enum value) => RpcTask<EnumSerializationTests.UInt8Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveUInt8Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt8Enum> InvokeUInt8EnumFromServer(EnumSerializationTests.UInt8Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.UInt8Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.UInt8Enum ReceiveUInt8Enum(EnumSerializationTests.UInt8Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveUInt16Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt16Enum> InvokeUInt16EnumFromClient(EnumSerializationTests.UInt16Enum value) => RpcTask<EnumSerializationTests.UInt16Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveUInt16Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt16Enum> InvokeUInt16EnumFromServer(EnumSerializationTests.UInt16Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.UInt16Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.UInt16Enum ReceiveUInt16Enum(EnumSerializationTests.UInt16Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveUInt32Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt32Enum> InvokeUInt32EnumFromClient(EnumSerializationTests.UInt32Enum value) => RpcTask<EnumSerializationTests.UInt32Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveUInt32Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt32Enum> InvokeUInt32EnumFromServer(EnumSerializationTests.UInt32Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.UInt32Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.UInt32Enum ReceiveUInt32Enum(EnumSerializationTests.UInt32Enum val)
        {
            _wasInvoked = true;

            return val;
        }

        [RpcSend(nameof(ReceiveUInt64Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt64Enum> InvokeUInt64EnumFromClient(EnumSerializationTests.UInt64Enum value) => RpcTask<EnumSerializationTests.UInt64Enum>.NotImplemented;

        [RpcSend(nameof(ReceiveUInt64Enum))]
        public virtual RpcTask<EnumSerializationTests.UInt64Enum> InvokeUInt64EnumFromServer(EnumSerializationTests.UInt64Enum value, IModularRpcRemoteConnection connection) => RpcTask<EnumSerializationTests.UInt64Enum>.NotImplemented;

        [RpcReceive]
        private EnumSerializationTests.UInt64Enum ReceiveUInt64Enum(EnumSerializationTests.UInt64Enum val)
        {
            _wasInvoked = true;

            return val;
        }
    }
}
