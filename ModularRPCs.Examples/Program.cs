using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Examples;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices(serviceCollection =>
{
    serviceCollection.AddReflectionTools();
    serviceCollection.AddProxyGenerator();

    serviceCollection.AddRpcTransient<SampleClass>();
});

IHost host = hostBuilder.Build();

SampleClass sc = host.Services.GetRequiredService<SampleClass>();

await sc.CallRpcOne();

Console.ReadLine();