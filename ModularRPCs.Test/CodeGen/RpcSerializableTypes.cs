using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen;

[RpcSerializable(sizeof(int) + sizeof(char) + SerializationHelper.MinimumStringSize, isFixedSize: false)]
public struct RpcSerializable : IRpcSerializable
{
    public int Int32;
    public string String;
    public char Character;

    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return sizeof(int) + sizeof(char) + serializer.GetSize(String);
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int w = sizeof(int) + sizeof(char);
        Unsafe.WriteUnaligned(ref writeTo[0], Int32);
        Unsafe.WriteUnaligned(ref writeTo[4], Character);
        w += serializer.WriteObject(String, writeTo.Slice(6));
        return w;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int r = sizeof(int) + sizeof(char);
        Int32 = Unsafe.ReadUnaligned<int>(ref readFrom[0]);
        Character = Unsafe.ReadUnaligned<char>(ref readFrom[4]);
        String = serializer.ReadObject<string>(readFrom.Slice(6), out int bytesRead);
        r += bytesRead;
        return r;
    }
}

[NonParallelizable]
public class RpcSerializableTypes
{
    private const int Param1 = 48;
    private const int Param2 = -94;
    private const int Int32 = 35;
    private const char Character = 'c';
    private const string String = "test string omg";

    private static bool _wasInvoked;

    [Test]
    public async Task ServerToClientBytesSingleParameter()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        RpcSerializable s = await proxy.InvokeFromServer(Param1, new RpcSerializable
        {
            String = String,
            Int32 = Int32,
            Character = Character
        }, Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.String, Is.EqualTo(String));
        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ClientToServerBytesSingleParameter()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        RpcSerializable s = await proxy.InvokeFromClient(Param1, new RpcSerializable
        {
            String = String,
            Character = Character,
            Int32 = Int32
        }, Param2);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.String, Is.EqualTo(String));
        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ServerToClientStreamSingleParameter()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        RpcSerializable s = await proxy.InvokeFromServer(Param1, new RpcSerializable
        {
            String = String,
            Character = Character,
            Int32 = Int32
        }, Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.String, Is.EqualTo(String));
        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ClientToServerStreamSingleParameter()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        RpcSerializable s = await proxy.InvokeFromClient(Param1, new RpcSerializable
        {
            String = String,
            Character = Character,
            Int32 = Int32
        }, Param2);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.String, Is.EqualTo(String));
        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }


    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive)), RpcTimeout(Timeouts.Hours)]
        public virtual RpcTask<RpcSerializable> InvokeFromClient(int param1, RpcSerializable obj, int param2)
        {
            return RpcTask<RpcSerializable>.NotImplemented;
        }

        [RpcSend(nameof(Receive)), RpcTimeout(Timeouts.Hours)]
        public virtual RpcTask<RpcSerializable> InvokeFromServer(int param1, RpcSerializable obj, int param2,
            IModularRpcRemoteConnection connection)
        {
            return RpcTask<RpcSerializable>.NotImplemented;
        }

        [RpcReceive]
        private RpcSerializable Receive(int param1, RpcSerializable obj, int param2)
        {
            _wasInvoked = true;

            Assert.That(param1, Is.EqualTo(Param1));
            Assert.That(param2, Is.EqualTo(Param2));

            Assert.That(obj.String, Is.EqualTo(String));
            Assert.That(obj.Int32, Is.EqualTo(Int32));
            Assert.That(obj.Character, Is.EqualTo(Character));
            return obj;
        }
    }
}