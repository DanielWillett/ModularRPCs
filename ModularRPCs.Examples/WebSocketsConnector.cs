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

            // this event allows you to customize the Uri used to reconnect with.
            // this supports being able to use a temporary token that expires quickly in the query parameters
            connection.OnRequestingReconnect += ReconnectHandler;
            connection.Local.SetLogger(Accessor.Active);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open WebSocket client.");
            return null;
        }

        return connection;
    }

    private async Task<Uri?> ReconnectHandler(WebSocketClientsideRemoteRpcConnection connection)
    {
        Uri? connectUri = await GetConnectUri();

        _logger.LogDebug($"Reconnecting to web socket at: {connectUri}.");

        return connectUri;
    }

    private async Task<Uri?> GetConnectUri(CancellationToken token = default)
    {
        const string path = @"%USERPROFILE%\OneDrive\Desktop\webSocketText.txt";

        string[] lines = File.ReadAllLines(path);

        // example reads endpoints from a file but most would read from IConfiguration or similar
        string authEndpoint = lines[0];    // https://example.com/my-rpc-api/auth
        string authKey = lines[1];         // Some bearer token (ex. e5ed3cae1e5e4e75af41ec39b4d9298c)
        string connectEndpoint = lines[2]; // wss://example.com/my-rpc-api/connect
        string? authJwt = null;
        if (!string.IsNullOrEmpty(authEndpoint))
        {
            if (string.IsNullOrEmpty(authKey))
            {
                _logger.LogWarning("Authentication key not configured.");
                return null;
            }

            Uri authUri = new Uri(authEndpoint);

            // make a requrest for a temporary auth token (JWT in this case) using a bearer key
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

        // connect using ws or wss, optionally passing a pre-fetched temporary auth token
        Uri connectUri = new Uri(connectEndpoint);

        if (authJwt != null)
        {
            connectUri = new Uri(connectUri, "?token=" + Uri.EscapeDataString(authJwt));
        }

        return connectUri;
    }
}