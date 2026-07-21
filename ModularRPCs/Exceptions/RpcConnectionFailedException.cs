using DanielWillett.ModularRpcs.Connections;
using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when an <see cref="IConnectionFactory"/> fails to establish a connection to another client.
/// </summary>
[Serializable]
public sealed class RpcConnectionFailedException : RpcException
{
    /// <summary>
    /// The type of <see cref="IRpcConnection"/> that was trying to be established.
    /// </summary>
    public Type ConnectionType { get; }

    /// <inheritdoc />
    public RpcConnectionFailedException(Type connectionType) : base(string.Format(Properties.Exceptions.RpcConnectionFailedException_Basic, connectionType.Name))
    {
        ConnectionType = connectionType;
    }

    /// <inheritdoc />
    public RpcConnectionFailedException(string message, Type connectionType) : base(message)
    {
        ConnectionType = connectionType;
    }

    /// <inheritdoc />
    public RpcConnectionFailedException(string message, Type connectionType, Exception inner) : base(message, inner)
    {
        ConnectionType = connectionType;
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcConnectionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        string? fullTypeName = info.GetString("ConnectionType");
        if (fullTypeName == null)
        {
            ConnectionType = typeof(IRpcConnection);
        }
        else
        {
            ConnectionType = Type.GetType(fullTypeName, throwOnError: false, ignoreCase: false)!;
            if (ConnectionType == null || !typeof(IRpcConnection).IsAssignableFrom(ConnectionType))
                ConnectionType = typeof(IRpcConnection);
        }
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("ConnectionType", ConnectionType.AssemblyQualifiedName);
        base.GetObjectData(info, context);
    }
}
