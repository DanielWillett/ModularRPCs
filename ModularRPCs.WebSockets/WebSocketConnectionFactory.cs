using DanielWillett.ModularRpcs.Connections;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Protocol;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Creates connections using <see cref="WebSocket"/> objects for communication.
/// </summary>
public class WebSocketConnectionFactory : IConnectionFactory
{
    private readonly bool _isServer;

    private readonly Uri? _clientUri;
    private readonly Action<ClientWebSocketOptions>? _clientOptionsCallback;
    private readonly bool _clientsShouldAutoReconnect;

    private PlateauingDelay _delaySettings;

    /// <summary>
    /// Settings for reconnect delays on client connections. See <see cref="PlateauingDelay"/> for more info.
    /// </summary>
    /// <remarks>This does nothing for endpoints serverside connection factories.</remarks>
    public ref PlateauingDelay DelaySettings => ref _delaySettings;

    /// <summary>
    /// Size of the buffer used to send data from a stream. Not used for implementations that send data from raw binary data.
    /// </summary>
    public int StreamSendBufferSize { get; set; } = 4096;

    /// <summary>
    /// Size of the buffer used to receive data.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// Sets both <see cref="StreamSendBufferSize"/> and <see cref="ReceiveBufferSize"/>.
    /// </summary>
    public int BufferSize
    {
        get => (StreamSendBufferSize + ReceiveBufferSize) / 2;
        set
        {
            StreamSendBufferSize = value;
            ReceiveBufferSize = value;
        }
    }

    /// <inheritdoc />
    public RpcConnectionRelationship ConnectionType => _isServer ? RpcConnectionRelationship.Server : RpcConnectionRelationship.Client;

    internal WebSocketConnectionFactory(
        Uri uri,
        bool shouldAutoReconnect = false,
        Action<ClientWebSocketOptions>? configureOptions = null)
    {
        _isServer = false;

        _clientUri = uri;
        _clientOptionsCallback = configureOptions;
        _clientsShouldAutoReconnect = shouldAutoReconnect;

        _delaySettings = new PlateauingDelay(amplifier: 6, climb: 2.5, maximum: 300, start: 10);
    }

    /// <inheritdoc />
    public ValueTask<IRpcConnection> CreateConnectionAsync(CancellationToken token = default)
    {
        Type connectionType = _isServer ? typeof(WebSocketServerConnection) : typeof(WebSocketClientConnection);



        throw new RpcConnectionFailedException(connectionType);
    }
}