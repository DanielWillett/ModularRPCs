namespace DanielWillett.ModularRpcs.Connections;

/// <summary>
/// Describes the relationship between a connection and it's endpoint.
/// </summary>
public enum RpcConnectionRelationship
{
    /// <summary>
    /// A server with control over multiple clients.
    /// </summary>
    Server,

    /// <summary>
    /// A client connected to a server, who can only communicate with said server.
    /// </summary>
    Client,

    /// <summary>
    /// A peer to another endpoint, who both have equal authority over each other.
    /// </summary>
    Peer
}
