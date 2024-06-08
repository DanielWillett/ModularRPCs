using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using System;

namespace DanielWillett.ModularRpcs.Examples.Samples;

// ReSharper disable LocalizableElement
public class WebSocketsTestClass
{
    [RpcReceive]
    private int ReceiveTestFunc()
    {
        Console.WriteLine("=== ReceiveTestFunc ===");
        return 3;
    }

    [RpcSend, RpcTimeout((int)(2.5 * Timeouts.Seconds))]
    internal virtual RpcTask<int> SendTestFunc() => RpcTask<int>.NotImplemented;
}