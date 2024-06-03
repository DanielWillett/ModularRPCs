using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    [RpcTimeout(10 * Timeouts.Seconds)]
    [RpcSend(nameof(RpcOne))]
    internal virtual RpcTask<int> CallRpcOne(IModularRpcRemoteConnection connection, List<string> testArray) => RpcTask<int>.NotImplemented;

    [RpcReceive]
    private async Task<int> RpcOne(IModularRpcRemoteConnection connection, string[] testArray, CancellationToken token)
    {
        Console.WriteLine($"Value: {string.Join(", ", testArray)}.");
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
        Console.WriteLine("sample finalizer ran");
    }
}