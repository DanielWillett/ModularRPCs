using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Examples.Samples;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using DanielWillett.ReflectionTools;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices(serviceCollection =>
{
    serviceCollection.AddReflectionTools();
    serviceCollection.AddProxyGenerator();

    serviceCollection.AddRpcTransient<SampleKeysNullable>();
    serviceCollection.AddRpcTransient<SampleKeysNullableOtherField>();
    serviceCollection.AddRpcTransient<SampleKeysNullableNoField>();
    serviceCollection.AddRpcTransient<SampleKeysString>();
});

Accessor.LogILTraceMessages = true;
Accessor.LogDebugMessages = true;
Accessor.LogInfoMessages = true;
Accessor.LogWarningMessages = true;
Accessor.LogErrorMessages = true;

IHost host = hostBuilder.Build();

SampleKeysNullable sc1 = host.Services.GetRequiredService<SampleKeysNullable>();
SampleKeysNullableNoField sc2 = host.Services.GetRequiredService<SampleKeysNullableNoField>();
SampleKeysString sc3 = host.Services.GetRequiredService<SampleKeysString>();
SampleKeysNullableOtherField sc4 = host.Services.GetRequiredService<SampleKeysNullableOtherField>();

_ = sc1;
_ = sc2;
_ = sc3;
_ = sc4;

Console.ReadLine();