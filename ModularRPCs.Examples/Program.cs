using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Examples;
using DanielWillett.ModularRpcs.Reflection;
using System;

SampleClass proxy = ProxyGenerator.Instance.CreateProxy<SampleClass>(nonPublic: true, "test", true);

RpcTask task = proxy.CallRpcOne();

Console.WriteLine(task.GetAwaiter().IsCompleted);
Console.ReadLine();