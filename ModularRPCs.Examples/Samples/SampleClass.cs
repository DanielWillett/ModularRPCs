using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using System;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Abstractions;

// ReSharper disable LocalizableElement

namespace DanielWillett.ModularRpcs.Examples.Samples;

[RpcClass]
public class SampleClass : IRpcObject<int>
{
    private static int _identifier;
    public int Identifier { get; set; }
    public SampleClass()
    {
        Identifier = Interlocked.Increment(ref _identifier);
        Console.WriteLine("Called base ctor");
    }
    public SampleClass(string test1)
    {
        Identifier = Interlocked.Increment(ref _identifier);
        Console.WriteLine($"Called base ctor ({test1})");
    }
    public SampleClass(string test1, bool test2)
    {
        Identifier = Interlocked.Increment(ref _identifier);
        Console.WriteLine($"Called base ctor ({test1}, {test2})");
    }

    [RpcTimeout(10 * RpcTimeoutAttribute.Seconds)]
    [RpcSend(nameof(RpcOne))]
    internal virtual RpcTask<int> CallRpcOne(IModularRpcRemoteConnection connection, int arg1, nint arg2, string arg3, DateTime arg4) => RpcTask<int>.NotImplemented;

    [RpcReceive]
    private async Task<int> RpcOne(IModularRpcRemoteConnection connection, int value, CancellationToken token)
    {
        Console.WriteLine($"Value: {value}");
        Console.WriteLine("Start");
        await Task.Delay(TimeSpan.FromSeconds(5d), token);
        Console.WriteLine("Done");
        return 4;
    }
    protected virtual bool Release()
    {
        Console.WriteLine("base release called");
        return false;
    }

    ~SampleClass()
    {
        Console.WriteLine("test");
    }
}