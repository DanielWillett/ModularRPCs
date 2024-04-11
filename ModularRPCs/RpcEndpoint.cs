using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs;

/// <summary>
/// Represents either a send or receive method.
/// </summary>
public class RpcEndpoint : IRpcInvocationPoint
{
    private static MethodInfo? _fromResultMethod;

    /// <summary>
    /// Unique ID corresponding to this endpoint set by the server.
    /// </summary>
    public uint? EndpointId { get; set; }

    /// <summary>
    /// The fully declared name of the declaring type.
    /// </summary>
    public string DeclaringTypeName { get; }

    /// <summary>
    /// The method name of the declaring type.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// The fully declared names of the argument types.
    /// </summary>
    public string[]? ArgumentTypeNames { get; }

    /// <summary>
    /// The argument types of the method, if available.
    /// </summary>
    public Type?[]? ArgumentTypes { get; }

    /// <summary>
    /// The method, if available.
    /// </summary>
    public MethodInfo? Method { get; }
    
    /// <summary>
    /// The declaring type, if available.
    /// </summary>
    public Type? DeclaringType { get; }

    /// <summary>
    /// Size in bytes of this endpoint if it were written to a buffer.
    /// </summary>
    public int Size { get; private set; }

    public ValueTask Invoke(object? targetObject, ArraySegment<object> parameters)
    {
        if (Method == null)
            throw new RpcEndpointNotFoundException(this);

        object returnedValue = Method.Invoke(
            targetObject,
            parameters.Offset == 0 && parameters.Count == parameters.Array!.Length ? parameters.Array : parameters.AsSpan().ToArray()
        );

        switch (returnedValue)
        {
            case null when Method.ReturnType == typeof(void):
                return default;
            case Task task:
                return new ValueTask(task);
            case ValueTask vt:
                return vt;
        }

        Type returnedType = returnedValue?.GetType() ?? Method.ReturnType;
        if (returnedType.IsGenericType && returnedType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            MethodInfo? asTaskMethod = returnedType.GetMethod("AsTask", BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null);
            if (asTaskMethod == null || asTaskMethod.Invoke(returnedValue, Array.Empty<object>()) is not Task task)
                return default;

            return new ValueTask(task);
        }

        _fromResultMethod ??= typeof(Task).GetMethod("FromResult", BindingFlags.Static | BindingFlags.Instance);
        if (_fromResultMethod == null
            || !_fromResultMethod.IsGenericMethodDefinition
            || _fromResultMethod.MakeGenericMethod(returnedType).Invoke(null, [ returnedValue ]) is not Task newTask)
        {
            return default;
        }

        return new ValueTask(newTask);
    }

    internal RpcEndpoint(MethodInfo method)
    {
        if (method.DeclaringType == null)
            throw new ArgumentException(Properties.Exceptions.MethodHasNoDeclaringType, nameof(method));

        ParameterInfo[] parameters = method.GetParameters();
        ArgumentTypes = parameters.Length == 0 ? Type.EmptyTypes : new Type[parameters.Length];
        ArgumentTypeNames = parameters.Length == 0 ? Array.Empty<string>() : new string[parameters.Length];
        for (int i = 0; i < parameters.Length; ++i)
        {
            Type parameterType = parameters[i].ParameterType;
            ArgumentTypes[i] = parameterType;
            ArgumentTypeNames[i] = parameterType.AssemblyQualifiedName!;
        }

        Method = method;
        MethodName = method.Name;
        DeclaringType = method.DeclaringType;
        DeclaringTypeName = method.DeclaringType.AssemblyQualifiedName!;
        CalculateSize();
    }

    internal RpcEndpoint(uint knownId, string declaringTypeName, string methodName, string[]? argumentTypeNames, Assembly? expectedAssembly, Type? expectedType)
        : this(declaringTypeName, methodName, argumentTypeNames, expectedAssembly, expectedType)
    {
        EndpointId = knownId;
    }

    internal RpcEndpoint(string declaringTypeName, string methodName, string[]? argumentTypeNames, Assembly? expectedAssembly, Type? expectedType)
    {
        DeclaringTypeName = declaringTypeName;
        MethodName = methodName;
        bool foundAllTypes = false;
        if (argumentTypeNames != null)
        {
            ArgumentTypeNames = argumentTypeNames;
            ArgumentTypes = argumentTypeNames.Length == 0 ? Type.EmptyTypes : new Type[argumentTypeNames.Length];
            foundAllTypes = true;
            for (int i = 0; i < argumentTypeNames.Length; ++i)
            {
                Type? type = Type.GetType(argumentTypeNames[i], throwOnError: false) ?? expectedAssembly?.GetType(declaringTypeName);
                ArgumentTypes[i] = type;
                if (type == null)
                    foundAllTypes = false;
            }
        }
        DeclaringType = Type.GetType(declaringTypeName, throwOnError: false) ?? expectedAssembly?.GetType(declaringTypeName) ?? expectedType;
        if (DeclaringType != null && !string.IsNullOrEmpty(declaringTypeName) && !declaringTypeName.Equals(DeclaringType.Name))
        {
            DeclaringType = null;
        }

        if (DeclaringType == null)
            return;

        if (foundAllTypes)
        {
            Method = DeclaringType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null,
                CallingConventions.Any, ArgumentTypes!, null);
        }
        else
        {
            try
            {
                Method = DeclaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }
            catch (AmbiguousMatchException)
            {
                // ignored
            }
        }

        CalculateSize();
    }
    private void CalculateSize()
    {
        int size = sizeof(uint) + sizeof(ushort) + sizeof(ushort) + 1;
        size += Encoding.UTF8.GetByteCount(DeclaringTypeName)
                + Encoding.UTF8.GetByteCount(MethodName);
        if (ArgumentTypeNames != null)
        {
            size += sizeof(ushort);
            for (int i = 0; i < ArgumentTypeNames.Length; ++i)
                size += Encoding.UTF8.GetByteCount(ArgumentTypeNames[i]);
        }

        Size = size;
    }
    internal static unsafe IRpcInvocationPoint FromBytes(IRpcRouter router, byte* bytes, uint maxCt, out int bytesRead)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

        byte* originalPtr = bytes;

        if (maxCt < 8)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut);

        uint knownRpcShortcutId = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(bytes)
            : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

        bytes += sizeof(uint);

        int rpcTypeLength = isLittleEndian
            ? *bytes | bytes[1] << 8
            : *bytes << 8 | bytes[1];

        bytes += sizeof(ushort);

        int rpcMethodLength = isLittleEndian
            ? *bytes | bytes[1] << 8
            : *bytes << 8 | bytes[1];

        bytes += sizeof(ushort);
        int size = 9 + rpcTypeLength + rpcMethodLength;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut);

        string typeName = Encoding.UTF8.GetString(bytes, rpcTypeLength);
        bytes += rpcTypeLength;
        string methodName = Encoding.UTF8.GetString(bytes, rpcMethodLength);
        bytes += rpcMethodLength;

        string[] args;
        byte v = *bytes;
        ++bytes;
        if (v != 0)
        {
            size += sizeof(ushort);

            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut);

            int argCt = isLittleEndian
                ? *bytes | bytes[1] << 8
                : *bytes << 8 | bytes[1];
            size += argCt * 2;

            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut);

            args = new string[argCt];
            Span<int> argLens = stackalloc int[argCt];
            int ttlLen = 0;
            for (int i = 0; i < argCt; ++i)
            {
                int len = isLittleEndian
                    ? *bytes | bytes[1] << 8
                    : *bytes << 8 | bytes[1];
                argLens[i] = len;
                ttlLen += len;
                bytes += sizeof(ushort);
            }

            if (maxCt < size + ttlLen)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut);

            for (int i = 0; i < argCt; ++i)
            {
                args[i] = Encoding.UTF8.GetString(bytes, argLens[i]);
                bytes += argLens[i];
            }
        }
        else args = null!;

        bytesRead = checked( (int)(bytes - originalPtr) );
        return router.ResolveEndpoint(knownRpcShortcutId, typeName, methodName, args, bytesRead);
    }
    internal static unsafe IRpcInvocationPoint FromStream(IRpcRouter router, Stream stream, out int bytesRead)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

#if NETFRAMEWORK
        byte[] bytes = new byte[64];

        int byteCt = stream.Read(bytes, 0, 8);
#else
        Span<byte> bytes = stackalloc byte[64];

        int byteCt = stream.Read(bytes);
#endif
        if (byteCt < 8)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);

        uint knownRpcShortcutId = isLittleEndian
            ? bytes[0] | (uint)bytes[1] << 8 | (uint)bytes[2] << 16 | (uint)bytes[3] << 24
            : (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

        int index = sizeof(uint);

        int rpcTypeLength = isLittleEndian
            ? bytes[index] | bytes[index + 1] << 8
            : bytes[index] << 8 | bytes[index + 1];

        index += sizeof(ushort);

        int rpcMethodLength = isLittleEndian
            ? bytes[index] | bytes[index + 1] << 8
            : bytes[index] << 8 | bytes[index + 1];

        index += sizeof(ushort);

        int maxSize = Math.Max(rpcTypeLength, rpcMethodLength);
        if (maxSize > 64)
        {
#if NETFRAMEWORK
            bytes = new byte[maxSize];
#else
            bytes = stackalloc byte[maxSize];
#endif
        }

#if NETFRAMEWORK
        byteCt = stream.Read(bytes, 0, rpcTypeLength);
#else
        byteCt = stream.Read(bytes[..rpcTypeLength]);
#endif

        if (byteCt < rpcTypeLength)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);

#if NETFRAMEWORK
        string typeName = Encoding.UTF8.GetString(bytes, 0, rpcTypeLength);
#else
        string typeName = Encoding.UTF8.GetString(bytes[..rpcTypeLength]);
#endif
        index += rpcTypeLength;

#if NETFRAMEWORK
        byteCt = stream.Read(bytes, 0, rpcMethodLength);
#else
        byteCt = stream.Read(bytes[..rpcMethodLength]);
#endif

        if (byteCt < rpcMethodLength)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);

#if NETFRAMEWORK
        string methodName = Encoding.UTF8.GetString(bytes, 0, rpcMethodLength);
#else
        string methodName = Encoding.UTF8.GetString(bytes[..rpcMethodLength]);
#endif
        index += rpcMethodLength;

        string[] args;
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);
        ++index;
        if (b != 0)
        {
#if NETFRAMEWORK
            byteCt = stream.Read(bytes, 0, 2);
#else
            byteCt = stream.Read(bytes[..2]);
#endif

            if (byteCt < 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);

            int argCt = isLittleEndian
                ? bytes[0] | bytes[1] << 8
                : bytes[0] << 8 | bytes[1];
            args = new string[argCt];
            Span<int> argLens = stackalloc int[argCt];
            int ttlLen = 0;
            if (argCt * 2 > bytes.Length)
            {
#if NETFRAMEWORK
                bytes = new byte[argCt * 2];
#else
                bytes = argCt > 256 ? new byte[argCt * 2] : stackalloc byte[argCt * 2];
#endif
            }
#if NETFRAMEWORK
            byteCt = stream.Read(bytes, 0, argCt * 2);
#else
            byteCt = stream.Read(bytes[..(argCt * 2)]);
#endif

            if (byteCt < argCt * 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);

            for (int i = 0; i < argCt; ++i)
            {
                int len = isLittleEndian
                    ? bytes[i * argCt] | bytes[i * argCt + 1] << 8
                    : bytes[i * argCt] << 8 | bytes[i * argCt + 1];
                index += sizeof(ushort);
                argLens[i] = len;
                ttlLen += len;
                index += len;
            }

            if (ttlLen > bytes.Length)
            {
#if NETFRAMEWORK
                bytes = new byte[ttlLen];
#else
                bytes = ttlLen > 512 ? new byte[ttlLen] : stackalloc byte[ttlLen];
#endif
            }

#if NETFRAMEWORK
            byteCt = stream.Read(bytes, 0, ttlLen);
#else
            byteCt = stream.Read(bytes[..(argCt * 2)]);
#endif

            if (byteCt < argCt * 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut);

            int pos = 0;
            for (int i = 0; i < argCt; ++i)
            {
#if NETFRAMEWORK
                args[i] = Encoding.UTF8.GetString(bytes, pos, argLens[i]);
#else
                args[i] = Encoding.UTF8.GetString(bytes.Slice(pos, argLens[i]));
#endif
                pos += argLens[i];
            }
        }
        else args = null!;

        bytesRead = index;
        return router.ResolveEndpoint(knownRpcShortcutId, typeName, methodName, args, bytesRead);
    }
}
