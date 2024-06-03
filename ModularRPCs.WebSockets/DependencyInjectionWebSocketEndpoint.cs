using System;
using System.Net.WebSockets;

namespace DanielWillett.ModularRpcs.WebSockets;
public class DependencyInjectionWebSocketEndpoint : WebSocketEndpoint
{
    public IServiceProvider ServiceProvider { get; }
    internal DependencyInjectionWebSocketEndpoint(IServiceProvider serviceProvider, Uri uri, Action<ClientWebSocketOptions>? configureOptions, bool isClient)
        : base(uri, configureOptions, isClient)
    {
        ServiceProvider = serviceProvider;
    }
}
