using System;
using System.Net.WebSockets;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// A <see cref="WebSocketEndpoint"/> that uses a <see cref="IServiceProvider"/>.
/// </summary>
public class DependencyInjectionWebSocketEndpoint : WebSocketEndpoint
{
    /// <summary>
    /// The service provider used to get services for this endpoint.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
    internal DependencyInjectionWebSocketEndpoint(IServiceProvider serviceProvider, Uri uri, Action<ClientWebSocketOptions>? configureOptions, bool isClient)
        : base(uri, configureOptions, isClient)
    {
        ServiceProvider = serviceProvider;
    }
}
