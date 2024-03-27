using System;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;

namespace DanielWillett.ModularRpcs.Examples;

[RpcClass]
public class SampleClass
{
    public SampleClass()
    {
        Console.WriteLine("Called base ctor");
    }
    public SampleClass(string test1)
    {
        Console.WriteLine($"Called base ctor ({test1})");
    }
    public SampleClass(string test1, bool test2)
    {
        Console.WriteLine($"Called base ctor ({test1}, {test2})");
    }

    [RpcTimeout(10 * RpcTimeoutAttribute.Seconds)]
    [RpcSend, RpcFireAndForget]
    internal virtual RpcTask CallRpcOne() => throw new NotImplementedException();

    [RpcReceive]
    private async Task RpcOne()
    {
        Console.WriteLine("Start");
        await Task.Delay(TimeSpan.FromSeconds(5d));
        Console.WriteLine("Done");
    }
}