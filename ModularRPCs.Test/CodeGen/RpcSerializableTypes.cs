using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

[RpcSerializable(sizeof(int) + sizeof(char), isFixedSize: true)]
public struct FixedRpcSerializable : IRpcSerializable
{
    public int Int32;
    public char Character;

    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return sizeof(int) + sizeof(char);
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int w = sizeof(int) + sizeof(char);
        Unsafe.WriteUnaligned(ref writeTo[0], Int32);
        Unsafe.WriteUnaligned(ref writeTo[4], Character);
        return w;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        Int32 = Unsafe.ReadUnaligned<int>(ref readFrom[0]);
        Character = Unsafe.ReadUnaligned<char>(ref readFrom[4]);
        return sizeof(int) + sizeof(char);
    }
}

[NonParallelizable, TestFixture]
public class RpcSerializableTypes
{
    private const int Param1 = 48;
    private const int Param2 = -94;
    private const int Int32 = 35;
    private const char Character = 'c';
    private const string String = "test string omg";

    private static bool _wasInvoked;

    private const int ArrayCount = 5;
    private static readonly IEnumerable<RpcSerializable>[] RpcSerializableCollectionTypes =
    [
        new RpcSerializable[]
        {
            new RpcSerializable { Character = 'a', Int32 = 999999, String = "test string 1" },
            new RpcSerializable { Character = 'z', Int32 = 58, String = "woah" },
            new RpcSerializable { Character = '\0', Int32 = -1, String = "str 1" }
        },
        null,
        new List<RpcSerializable>()
        {
            new RpcSerializable { Character = 'a', Int32 = 999999, String = "test string 1" },
            new RpcSerializable { Character = 'z', Int32 = 58, String = "woah" },
            new RpcSerializable { Character = '\0', Int32 = -1, String = "str 1" }
        },
        new HashSet<RpcSerializable>()
        {
            new RpcSerializable { Character = 'a', Int32 = 999999, String = "test string 1" },
            new RpcSerializable { Character = 'z', Int32 = 58, String = "woah" },
            new RpcSerializable { Character = '\0', Int32 = -1, String = "str 1" }
        },
        new ArraySegment<RpcSerializable>(new RpcSerializable[]
        {
            new RpcSerializable { Character = 'a', Int32 = 999999, String = "test string 1" },
            new RpcSerializable { Character = 'z', Int32 = 58, String = "woah" },
            new RpcSerializable { Character = '\0', Int32 = -1, String = "str 1" }
        }),
        new RpcSerializable[0]
    ];

    private static readonly IEnumerable<FixedRpcSerializable>[] FixedRpcSerializableCollectionTypes =
    [
        new FixedRpcSerializable[]
        {
            new FixedRpcSerializable { Character = 'a', Int32 = 999999 },
            new FixedRpcSerializable { Character = 'z', Int32 = 58 },
            new FixedRpcSerializable { Character = '\0', Int32 = -1 }
        },
        null,
        new List<FixedRpcSerializable>()
        {
            new FixedRpcSerializable { Character = 'a', Int32 = 999999 },
            new FixedRpcSerializable { Character = 'z', Int32 = 58 },
            new FixedRpcSerializable { Character = '\0', Int32 = -1 }
        },
        new HashSet<FixedRpcSerializable>()
        {
            new FixedRpcSerializable { Character = 'a', Int32 = 999999 },
            new FixedRpcSerializable { Character = 'z', Int32 = 58 },
            new FixedRpcSerializable { Character = '\0', Int32 = -1 }
        },
        new ArraySegment<FixedRpcSerializable>(new FixedRpcSerializable[]
        {
            new FixedRpcSerializable { Character = 'a', Int32 = 999999 },
            new FixedRpcSerializable { Character = 'z', Int32 = 58 },
            new FixedRpcSerializable { Character = '\0', Int32 = -1 }
        }),
        new FixedRpcSerializable[0]
    ];

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

    [Test]
    public async Task ServerToClientBytesCollection([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        RpcSerializable[] s = await proxy.InvokeFromServer(Param1, RpcSerializableCollectionTypes[collectionType], Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        if (RpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(RpcSerializableCollectionTypes[collectionType]));
        }
    }

    [Test]
    public async Task ClientToServerBytesCollection([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        RpcSerializable[] s = await proxy.InvokeFromClient(Param1, RpcSerializableCollectionTypes[collectionType], Param2);

        Assert.That(_wasInvoked, Is.True);

        if (RpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(RpcSerializableCollectionTypes[collectionType]));
        }
    }

    [Test]
    public async Task ServerToClientStreamCollection([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        RpcSerializable[] s = await proxy.InvokeFromServer(Param1, RpcSerializableCollectionTypes[collectionType], Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        if (RpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(RpcSerializableCollectionTypes[collectionType]));
        }
    }

    [Test]
    public async Task ClientToServerStreamCollection([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        RpcSerializable[] s = await proxy.InvokeFromClient(Param1, RpcSerializableCollectionTypes[collectionType], Param2);

        Assert.That(_wasInvoked, Is.True);

        if (RpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(RpcSerializableCollectionTypes[collectionType]));
        }
    }


    [Test]
    public async Task ServerToClientBytesSingleParameterFixedSize()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        FixedRpcSerializable s = await proxy.InvokeFromServer(Param1, new FixedRpcSerializable
        {
            Int32 = Int32,
            Character = Character
        }, Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ClientToServerBytesSingleParameterFixedSize()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        FixedRpcSerializable s = await proxy.InvokeFromClient(Param1, new FixedRpcSerializable
        {
            Character = Character,
            Int32 = Int32
        }, Param2);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ServerToClientStreamSingleParameterFixedSize()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        FixedRpcSerializable s = await proxy.InvokeFromServer(Param1, new FixedRpcSerializable
        {
            Character = Character,
            Int32 = Int32
        }, Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ClientToServerStreamSingleParameterFixedSize()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        FixedRpcSerializable s = await proxy.InvokeFromClient(Param1, new FixedRpcSerializable
        {
            Character = Character,
            Int32 = Int32
        }, Param2);

        Assert.That(_wasInvoked, Is.True);

        Assert.That(s.Int32, Is.EqualTo(Int32));
        Assert.That(s.Character, Is.EqualTo(Character));
    }

    [Test]
    public async Task ServerToClientBytesCollectionFixedSize([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        FixedRpcSerializable[] s = await proxy.InvokeFromServer(Param1, FixedRpcSerializableCollectionTypes[collectionType], Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        if (FixedRpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(FixedRpcSerializableCollectionTypes[collectionType]));
        }
    }

    [Test]
    public async Task ClientToServerBytesCollectionFixedSize([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        FixedRpcSerializable[] s = await proxy.InvokeFromClient(Param1, FixedRpcSerializableCollectionTypes[collectionType], Param2);

        Assert.That(_wasInvoked, Is.True);

        if (FixedRpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(FixedRpcSerializableCollectionTypes[collectionType]));
        }
    }

    [Test]
    public async Task ServerToClientStreamCollectionFixedSize([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        FixedRpcSerializable[] s = await proxy.InvokeFromServer(Param1, FixedRpcSerializableCollectionTypes[collectionType], Param2, connection);

        Assert.That(_wasInvoked, Is.True);

        if (FixedRpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(FixedRpcSerializableCollectionTypes[collectionType]));
        }
    }

    [Test]
    public async Task ClientToServerStreamCollectionFixedSize([Range(0, ArrayCount - 1)] int collectionType)
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        FixedRpcSerializable[] s = await proxy.InvokeFromClient(Param1, FixedRpcSerializableCollectionTypes[collectionType], Param2);

        Assert.That(_wasInvoked, Is.True);

        if (FixedRpcSerializableCollectionTypes[collectionType] == null)
        {
            Assert.That(s, Is.Null);
        }
        else
        {
            Assert.That(s, Is.EquivalentTo(FixedRpcSerializableCollectionTypes[collectionType]));
        }
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<RpcSerializable> InvokeFromClient(int param1, RpcSerializable obj, int param2)
        {
            return RpcTask<RpcSerializable>.NotImplemented;
        }

        [RpcSend(nameof(Receive)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<RpcSerializable> InvokeFromServer(int param1, RpcSerializable obj, int param2,
            IModularRpcRemoteConnection connection)
        {
            return RpcTask<RpcSerializable>.NotImplemented;
        }

        [RpcSend(nameof(ReceiveArray)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<RpcSerializable[]> InvokeFromClient(int param1, IEnumerable<RpcSerializable> obj, int param2)
        {
            return RpcTask<RpcSerializable[]>.NotImplemented;
        }

        [RpcSend(nameof(ReceiveArray)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<RpcSerializable[]> InvokeFromServer(int param1, IEnumerable<RpcSerializable> obj, int param2,
            IModularRpcRemoteConnection connection)
        {
            return RpcTask<RpcSerializable[]>.NotImplemented;
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

        [RpcReceive]
        private RpcSerializable[] ReceiveArray(int param1, IEnumerable<RpcSerializable> obj, int param2)
        {
            _wasInvoked = true;

            Assert.That(param1, Is.EqualTo(Param1));
            Assert.That(param2, Is.EqualTo(Param2));
            return obj?.ToArray();
        }

        [RpcSend(nameof(FixedReceive)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<FixedRpcSerializable> InvokeFromClient(int param1, FixedRpcSerializable obj, int param2)
        {
            return RpcTask<FixedRpcSerializable>.NotImplemented;
        }

        [RpcSend(nameof(FixedReceive)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<FixedRpcSerializable> InvokeFromServer(int param1, FixedRpcSerializable obj, int param2,
            IModularRpcRemoteConnection connection)
        {
            return RpcTask<FixedRpcSerializable>.NotImplemented;
        }

        [RpcSend(nameof(FixedReceiveArray)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<FixedRpcSerializable[]> InvokeFromClient(int param1, IEnumerable<FixedRpcSerializable> obj, int param2)
        {
            return RpcTask<FixedRpcSerializable[]>.NotImplemented;
        }

        [RpcSend(nameof(FixedReceiveArray)), RpcTimeout(15 * Timeouts.Seconds)]
        public virtual RpcTask<FixedRpcSerializable[]> InvokeFromServer(int param1, IEnumerable<FixedRpcSerializable> obj, int param2,
            IModularRpcRemoteConnection connection)
        {
            return RpcTask<FixedRpcSerializable[]>.NotImplemented;
        }

        [RpcReceive]
        private FixedRpcSerializable FixedReceive(int param1, FixedRpcSerializable obj, int param2)
        {
            _wasInvoked = true;

            Assert.That(param1, Is.EqualTo(Param1));
            Assert.That(param2, Is.EqualTo(Param2));

            Assert.That(obj.Int32, Is.EqualTo(Int32));
            Assert.That(obj.Character, Is.EqualTo(Character));
            return obj;
        }

        [RpcReceive]
        private FixedRpcSerializable[] FixedReceiveArray(int param1, IEnumerable<FixedRpcSerializable> obj, int param2)
        {
            _wasInvoked = true;

            Assert.That(param1, Is.EqualTo(Param1));
            Assert.That(param2, Is.EqualTo(Param2));
            return obj?.ToArray();
        }
    }
}