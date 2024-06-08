using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ModularRpcs.WebSockets;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Examples;
public class WebSocketsConnector
{
    private readonly ILogger _logger;
    private readonly IRpcRouter _router;
    private readonly IRpcConnectionLifetime _lifetime;
    private readonly IRpcSerializer _serializer;
    public WebSocketsConnector(ILogger<WebSocketsConnector> logger, IRpcRouter router, IRpcConnectionLifetime lifetime, IRpcSerializer serializer)
    {
        _logger = logger;
        _router = router;
        _lifetime = lifetime;
        _serializer = serializer;
    }

    public async Task<WebSocketClientsideRemoteRpcConnection?> ConnectAsync(CancellationToken token = default)
    {
        Uri? connectUri = await GetConnectUri(token);
        if (connectUri == null)
            return null;

        _logger.LogDebug($"Connecting to web socket at: {connectUri}.");

        WebSocketEndpoint endpoint = WebSocketEndpoint.AsClient(connectUri);
        endpoint.ShouldAutoReconnect = true;
        endpoint.DelaySettings = new PlateauingDelay(amplifier: 3.6d, climb: 1.8d, maximum: 60d, start: 10d);
        WebSocketClientsideRemoteRpcConnection connection;
        try
        {
            connection = await endpoint.RequestConnectionAsync(_router, _lifetime, _serializer, token).ConfigureAwait(false);
            connection.OnReconnect += ReconnectHandler;
            connection.Local.SetLogger(Accessor.Active);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open WebSocket client.");
            return null;
        }

        return connection;
    }

    private async Task<Uri?> GetConnectUri(CancellationToken token = default)
    {
        const string path = @"C:\Users\danny\OneDrive\Desktop\webSocketText.txt";

        string[] lines = File.ReadAllLines(path);

        string authEndpoint = lines[0];
        string authKey = lines[1];
        string connectEndpoint = lines[2];
        string? authJwt = null;
        if (!string.IsNullOrEmpty(authEndpoint))
        {
            if (string.IsNullOrEmpty(authKey))
            {
                _logger.LogWarning("Authentication key not configured.");
                return null;
            }

            Uri authUri = new Uri(authEndpoint);

            HttpRequestMessage reqAuth = new HttpRequestMessage(HttpMethod.Get, authUri);
            reqAuth.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authKey);

            _logger.LogDebug($"Authenticating for web socket at: {authUri} with key {authKey}.");

            HttpResponseMessage response;
            try
            {
                using HttpClient client = new HttpClient();
                response = await client.SendAsync(reqAuth, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate WebSocket client.");
                return null;
            }

            authJwt = await response.Content.ReadAsStringAsync();
        }

        Uri connectUri = new Uri(connectEndpoint);

        if (authJwt != null)
        {
            connectUri = new Uri(connectUri, "?token=" + Uri.EscapeDataString(authJwt));
        }

        return connectUri;
    }

    private async Task<Uri?> ReconnectHandler(WebSocketClientsideRemoteRpcConnection connection)
    {
        Uri? connectUri = await GetConnectUri();

        _logger.LogDebug($"Connecting to web socket at: {connectUri}.");

        return connectUri;
    }
}