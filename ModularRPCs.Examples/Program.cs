using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Examples;
using DanielWillett.ModularRpcs.Examples.Samples;
using DanielWillett.ModularRpcs.WebSockets;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;

// ReSharper disable LocalizableElement

public class Program
{
    public static async Task Main(string[] args)
    {
        await Run(args);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

        Thread.Sleep(10000);

        Console.ReadLine();
    }

    public static async Task Run(string[] args)
    {
        ServiceCollection collection = new ServiceCollection();

        Accessor.LogILTraceMessages = true;
        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        collection.AddLogging();
        collection.AddReflectionTools();
        collection.AddModularRpcs(isServer: true);
        collection.AddRpcSingleton<TestClass>();

        IServiceProvider serverProvider = collection.BuildServiceProvider();

        collection = new ServiceCollection();

        collection.AddLogging();
        collection.AddReflectionTools();
        collection.AddModularRpcs(isServer: false);
        collection.AddRpcSingleton<TestClass>();

        IServiceProvider clientProvider = collection.BuildServiceProvider();

        ProxyGenerator.Instance.DefaultTimeout = TimeSpan.FromMinutes(2d);

        LoopbackEndpoint endpoint = new LoopbackEndpoint(false, false);

        LoopbackRpcClientsideRemoteConnection remote =
            (LoopbackRpcClientsideRemoteConnection)await endpoint.RequestConnectionAsync(clientProvider, serverProvider);


        var server = remote.Server;

        TestClass proxy = clientProvider.GetRequiredService<TestClass>();

        try
        {
            SerializableTypeClassFixed rtnValue = await proxy.InvokeTaskFromClientSerializableTypeClassFixed();
        }
        finally
        {
            ProxyGenerator.Instance.AssemblyBuilder.Save("toAsm.dll");
        }
    }
}


[RpcClass]
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
        

        return new SerializableTypeClassFixed { Value = 3 };
    }

    [RpcReceive]
    private async Task<SerializableTypeClassFixed> ReceiveSerializableTypeClassFixedTask()
    {
        

        await Task.Delay(1);

        return new SerializableTypeClassFixed { Value = 3 };
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
        

        return new SerializableTypeClassVariable { Value = "test" };
    }

    [RpcReceive]
    private async Task<SerializableTypeClassVariable> ReceiveSerializableTypeClassVariableTask()
    {
        

        await Task.Delay(1);

        return new SerializableTypeClassVariable { Value = "test" };
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
        

        return new SerializableTypeStructFixed { Value = 3 };
    }

    [RpcReceive]
    private async Task<SerializableTypeStructFixed> ReceiveSerializableTypeStructFixedTask()
    {
        

        await Task.Delay(1);

        return new SerializableTypeStructFixed { Value = 3 };
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
        

        return new SerializableTypeStructVariable { Value = "test" };
    }

    [RpcReceive]
    private async Task<SerializableTypeStructVariable> ReceiveSerializableTypeStructVariableTask()
    {
        

        await Task.Delay(1);

        return new SerializableTypeStructVariable { Value = "test" };
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
        MemoryMarshal.Write(writeTo, ref Value);
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
        MemoryMarshal.Write(writeTo, ref Value);
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
