using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs;

/// <summary>
/// Represents either a send or receive method.
/// </summary>
public class RpcEndpoint : IRpcInvocationPoint
{
    private static MethodInfo? _fromResultMethod;
    private int _sizeWithoutIdentifier;
    protected int SignatureHash;

    /// <summary>
    /// Unique ID corresponding to this endpoint set by the server.
    /// </summary>
    public uint? EndpointId { get; set; }

    /// <summary>
    /// Is the invocation point expecting a target object?
    /// </summary>
    public bool IsStatic { get; }

    /// <summary>
    /// The identifier, if any, to identify the instance with.
    /// </summary>
    public object? Identifier { get; }

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
    bool IRpcInvocationPoint.CanCache => true;
    protected RpcEndpoint(IRpcSerializer serializer, RpcEndpoint other, object? identifier)
    {
        if (identifier is DBNull)
            identifier = null;

        Identifier = identifier;

        EndpointId = other.EndpointId;
        IsStatic = other.IsStatic;
        DeclaringTypeName = other.DeclaringTypeName;
        MethodName = other.MethodName;
        ArgumentTypeNames = other.ArgumentTypeNames;
        ArgumentTypes = other.ArgumentTypes;
        Method = other.Method;
        DeclaringType = other.DeclaringType;
        Size = _sizeWithoutIdentifier = other._sizeWithoutIdentifier;
        if (identifier != null)
        {
            Size += CalculateIdentifierSize(serializer, identifier);
        }
    }

    internal RpcEndpoint(IRpcSerializer serializer, MethodInfo method, object? identifier)
    {
        if (identifier is DBNull)
            identifier = null;

        if (method.DeclaringType == null)
            throw new ArgumentException(Properties.Exceptions.MethodHasNoDeclaringType, nameof(method));

        Identifier = identifier;
        ParameterInfo[] parameters = method.GetParameters();
        ArgumentTypes = parameters.Length == 0 ? Type.EmptyTypes : new Type[parameters.Length];
        ArgumentTypeNames = parameters.Length == 0 ? Array.Empty<string>() : new string[parameters.Length];
        IsStatic = method.IsStatic;
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
        if (identifier != null)
        {
            Size += CalculateIdentifierSize(serializer, identifier);
        }
    }

    internal RpcEndpoint(uint knownId, string declaringTypeName, string methodName, string[]? argumentTypeNames, int signatureHash, bool isStatic, Assembly? expectedAssembly = null, Type? expectedType = null)
        : this(declaringTypeName, methodName, argumentTypeNames, signatureHash, isStatic, expectedAssembly, expectedType)
    {
        EndpointId = knownId;
    }

    private static readonly ConstructorInfo MainConstructor = typeof(RpcEndpoint).GetConstructor(
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
        [ typeof(string), typeof(string), typeof(string[]), typeof(int), typeof(bool), typeof(Assembly), typeof(Type) ],
        null)!;

    internal RpcEndpoint(string declaringTypeName, string methodName, string[]? argumentTypeNames, int signatureHash, bool isStatic, Assembly? expectedAssembly = null, Type? expectedType = null)
    {
        IsStatic = isStatic;
        DeclaringTypeName = declaringTypeName;
        MethodName = methodName;
        SignatureHash = signatureHash;
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

    private protected virtual unsafe object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerBytes handlerBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, byte* bytes, uint maxSize, CancellationToken token)
    {
        return handlerBytes(null, targetObject, overhead, router, serializer, bytes, maxSize, token);
    }
    private protected virtual object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        return handlerStream(null, targetObject, overhead, router, serializer, stream, token);
    }

    public unsafe ValueTask Invoke(RpcOverhead overhead, IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token = default)
    {
        if (Method == null || !Method.IsDefinedSafe<RpcReceiveAttribute>())
            throw new RpcEndpointNotFoundException(this);

        int paramHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(Method);

        if (SignatureHash != paramHash)
        {
            throw new RpcEndpointNotFoundException(this, Properties.Exceptions.RpcEndpointNotFoundExceptionMismatchHash);
        }

        object? targetObject = null;
        if (!IsStatic)
        {
            targetObject = GetTargetObject();
        }

        ProxyGenerator.RpcInvokeHandlerBytes invokeMethod = ProxyGenerator.Instance.GetInvokeBytesMethod(Method);

        object? returnedValue;
        fixed (byte* ptr = rawData)
        {
            returnedValue = InvokeInvokeMethod(invokeMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, ptr, (uint)rawData.Length, token);
        }
        return ConvertReturnedValueToValueTask(returnedValue);
    }
    public ValueTask Invoke(RpcOverhead overhead, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        if (Method == null || !Method.IsDefinedSafe<RpcReceiveAttribute>())
            throw new RpcEndpointNotFoundException(this);

        object? targetObject = null;
        if (!IsStatic)
        {
            targetObject = GetTargetObject();
        }

        ProxyGenerator.RpcInvokeHandlerStream invokeMethod = ProxyGenerator.Instance.GetInvokeStreamMethod(Method);

        object? returnedValue = InvokeInvokeMethod(invokeMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, stream, token);
        return ConvertReturnedValueToValueTask(returnedValue);
    }
    protected virtual ValueTask ConvertReturnedValueToValueTask(object? returnedValue)
    {
        switch (returnedValue)
        {
            case null:
                return default;
            case Task task:
                return new ValueTask(task);
            case ValueTask vt:
                return vt;
        }

        Type returnedType = returnedValue.GetType();
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
            || _fromResultMethod.MakeGenericMethod(returnedType).Invoke(null, [returnedValue]) is not Task newTask)
        {
            return default;
        }

        return new ValueTask(newTask);

        return default;
    }
    protected virtual object? GetTargetObject()
    {
        if (Identifier == null || IsStatic)
            return null;

        if (DeclaringType == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierDeclaringTypeNotFound) { ErrorCode = 4 };

        WeakReference? weakRef = ProxyGenerator.Instance.GetObjectByIdentifier(DeclaringType, Identifier);
        object? target;

        try
        {
            target = weakRef?.Target;
        }
        catch (InvalidOperationException)
        {
            target = null;
        }

        if (target == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierNotExists) { ErrorCode = 5 };

        return target;
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

        _sizeWithoutIdentifier = size;
        Size = size;
    }

    internal static unsafe IRpcInvocationPoint ReadFromBytes(IRpcSerializer serializer, IRpcRouter router, byte* bytes, uint maxCt, out int bytesRead)
    {
        if (maxCt < 9)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        bool isLittleEndian = BitConverter.IsLittleEndian;

        byte* originalPtr = bytes;
        EndpointFlags flags1 = (EndpointFlags)(*bytes);
        ++bytes;

        uint knownRpcShortcutId = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(bytes)
            : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

        bytes += sizeof(uint);

        int signatureHash = isLittleEndian
            ? Unsafe.ReadUnaligned<int>(bytes)
            : *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];

        bytes += sizeof(int);

        int rpcTypeLength = isLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(bytes)
            : (ushort)(*bytes << 8 | bytes[1]);

        bytes += sizeof(ushort);

        int rpcMethodLength = isLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(bytes)
            : (ushort)(*bytes << 8 | bytes[1]);

        bytes += sizeof(ushort);
        int size = 10 + rpcTypeLength + rpcMethodLength;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

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
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            int argCt = isLittleEndian
                ? Unsafe.ReadUnaligned<ushort>(bytes)
                : (ushort)(*bytes << 8 | bytes[1]);
            bytes += 2;
            size += argCt * 2;

            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            args = new string[argCt];
            Span<int> argLens = stackalloc int[argCt];
            int ttlLen = 0;
            for (int i = 0; i < argCt; ++i)
            {
                int len = isLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(bytes)
                    : (ushort)(*bytes << 8 | bytes[1]);
                argLens[i] = len;
                ttlLen += len;
                bytes += sizeof(ushort);
            }

            size += ttlLen;
            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            for (int i = 0; i < argCt; ++i)
            {
                args[i] = Encoding.UTF8.GetString(bytes, argLens[i]);
                bytes += argLens[i];
            }
        }
        else args = null!;

        object? identifier = null;
        if ((flags1 & EndpointFlags.HasIdentifier) != 0)
        {
            identifier = ReadIdentifierFromBytes(serializer, bytes, maxCt - (uint)size, out int bytesReadIdentifier);
            // size += bytesReadIdentifier;
            bytes += bytesReadIdentifier;
        }

        bytesRead = checked( (int)(bytes - originalPtr) );
        return router.ResolveEndpoint(serializer, knownRpcShortcutId, typeName, methodName, signatureHash, (flags1 & EndpointFlags.IsStatic) != 0, args, bytesRead, identifier);
    }

    internal static unsafe IRpcInvocationPoint ReadFromStream(IRpcSerializer serializer, IRpcRouter router, Stream stream, out int bytesRead)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byte[] bytes = new byte[64];

        int byteCt = stream.Read(bytes, 0, 13);
#else
        Span<byte> bytes = stackalloc byte[64];

        int byteCt = stream.Read(bytes[..13]);
#endif
        if (byteCt < 13)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        EndpointFlags flags1 = (EndpointFlags)bytes[0];
        bool isStatic = (flags1 & EndpointFlags.IsStatic) != 0;
        bool hasIdentifier = (flags1 & EndpointFlags.HasIdentifier) != 0;

        uint knownRpcShortcutId = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref bytes[1])
            : (uint)bytes[1] << 24 | (uint)bytes[2] << 16 | (uint)bytes[3] << 8 | bytes[4];

        int index = sizeof(uint) + 1;

        int signatureHash = isLittleEndian
            ? Unsafe.ReadUnaligned<int>(ref bytes[index])
            : bytes[index] << 24 | bytes[index + 1] << 16 | bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(int);

        int rpcTypeLength = isLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(ref bytes[index])
            : (ushort)(bytes[index] << 8 | bytes[index + 1]);

        index += sizeof(ushort);

        int rpcMethodLength = isLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(ref bytes[index])
            : (ushort)(bytes[index] << 8 | bytes[index + 1]);

        index += sizeof(ushort);

        int maxSize = Math.Max(rpcTypeLength, rpcMethodLength);
        if (maxSize > 64)
        {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            bytes = new byte[maxSize];
#else
            bytes = stackalloc byte[maxSize];
#endif
        }

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byteCt = stream.Read(bytes, 0, rpcTypeLength);
#else
        byteCt = stream.Read(bytes[..rpcTypeLength]);
#endif

        if (byteCt < rpcTypeLength)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        string typeName = Encoding.UTF8.GetString(bytes, 0, rpcTypeLength);
#else
        string typeName = Encoding.UTF8.GetString(bytes[..rpcTypeLength]);
#endif
        index += rpcTypeLength;

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byteCt = stream.Read(bytes, 0, rpcMethodLength);
#else
        byteCt = stream.Read(bytes[..rpcMethodLength]);
#endif

        if (byteCt < rpcMethodLength)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        string methodName = Encoding.UTF8.GetString(bytes, 0, rpcMethodLength);
#else
        string methodName = Encoding.UTF8.GetString(bytes[..rpcMethodLength]);
#endif
        index += rpcMethodLength;

        string[] args;
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
        ++index;
        if (b != 0)
        {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, 2);
#else
            byteCt = stream.Read(bytes[..2]);
#endif

            if (byteCt < 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            int argCt = isLittleEndian
                ? Unsafe.ReadUnaligned<ushort>(ref bytes[0])
                : (ushort)(bytes[0] << 8 | bytes[1]);
            args = new string[argCt];
            Span<int> argLens = stackalloc int[argCt];
            int ttlLen = 0;
            if (argCt * 2 > bytes.Length)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                bytes = new byte[argCt * 2];
#else
                bytes = argCt > 256 ? new byte[argCt * 2] : stackalloc byte[argCt * 2];
#endif
            }
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, argCt * 2);
#else
            byteCt = stream.Read(bytes[..(argCt * 2)]);
#endif

            if (byteCt < argCt * 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            for (int i = 0; i < argCt; ++i)
            {
                int len = isLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref bytes[i * argCt])
                    : (ushort)(bytes[i * argCt] << 8 | bytes[i * argCt + 1]);
                index += sizeof(ushort);
                argLens[i] = len;
                ttlLen += len;
                index += len;
            }

            if (ttlLen > bytes.Length)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                bytes = new byte[ttlLen];
#else
                bytes = ttlLen > 512 ? new byte[ttlLen] : stackalloc byte[ttlLen];
#endif
            }

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, ttlLen);
#else
            byteCt = stream.Read(bytes[..(argCt * 2)]);
#endif

            if (byteCt < argCt * 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            int pos = 0;
            for (int i = 0; i < argCt; ++i)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                args[i] = Encoding.UTF8.GetString(bytes, pos, argLens[i]);
#else
                args[i] = Encoding.UTF8.GetString(bytes.Slice(pos, argLens[i]));
#endif
                pos += argLens[i];
            }
        }
        else args = null!;

        object? identifier = null;
        if (hasIdentifier)
        {
            identifier = ReadIdentifierFromStream(serializer, stream, out int bytesReadIdentifier);
            index += bytesReadIdentifier;
        }

        bytesRead = index;
        return router.ResolveEndpoint(serializer, knownRpcShortcutId, typeName, methodName, signatureHash, isStatic, args, bytesRead, identifier);
    }
    public virtual IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier)
    {
        return new RpcEndpoint(serializer, this, identifier);
    }
    internal static int CalculateIdentifierSize(IRpcSerializer serializer, object identifier)
    {
        Type idType = identifier.GetType();
        TypeCode tc = Type.GetTypeCode(idType);
        if (tc is <= TypeCode.DBNull or > TypeUtility.MaxUsedTypeCode)
        {
            int size = 1 + serializer.GetSize(identifier);
            if (serializer.TryGetKnownTypeId(idType, out _))
                return 4 + size;

            return size + serializer.GetSize(idType.FullName + ", " + idType.Assembly.GetName().Name);
        }

        if (serializer.CanFastReadPrimitives)
            return 2 + TypeUtility.GetTypeCodeSize(tc);
        
        return 2 + serializer.GetSize(identifier);
    }
    internal static unsafe object ReadIdentifierFromBytes(IRpcSerializer serializer, byte* bytes, uint maxCt, out int bytesRead)
    {
        int size = 1;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        IdentifierFlags flags = (IdentifierFlags)(*bytes);
        ++bytes;
        if ((flags & IdentifierFlags.IsTypeCode) != 0)
        {
            ++size;
            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };
            TypeCode typeCode = (TypeCode)(*bytes);
            ++bytes;

            if (typeCode is > TypeUtility.MaxUsedTypeCode or <= TypeCode.DBNull)
                throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };

            if (typeCode == TypeCode.String)
            {
                string str = serializer.ReadObject<string>(bytes, maxCt - (uint)size, out int strBytesRead);
                bytesRead = size + strBytesRead;
                return str;
            }

            object identifier;
            if (serializer.CanFastReadPrimitives)
            {
                size += TypeUtility.GetTypeCodeSize(typeCode);
                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        identifier = *bytes != 0;
                        ++bytes;
                        break;

                    case TypeCode.SByte:
                        identifier = unchecked((sbyte)*bytes);
                        ++bytes;
                        break;

                    case TypeCode.Byte:
                        identifier = *bytes;
                        ++bytes;
                        break;

                    case TypeCode.Char:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<char>(bytes)
                            : (char)(*bytes << 8 | bytes[1]);
                        break;

                    case TypeCode.Int16:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<short>(bytes)
                            : (short)(*bytes << 8 | bytes[1]);
                        break;

                    case TypeCode.UInt16:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<ushort>(bytes)
                            : (ushort)(*bytes << 8 | bytes[1]);
                        break;

                    case TypeCode.Int32:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<int>(bytes)
                            : *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                        break;

                    case TypeCode.UInt32:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<uint>(bytes)
                            : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
                        break;

                    case TypeCode.Int64:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(bytes)
                            : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        break;

                    case TypeCode.UInt64:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<ulong>(bytes)
                            : ((ulong)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        break;

                    case TypeCode.Single:
                        int z32 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<int>(bytes)
                            : *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                        identifier = *(float*)&z32;
                        break;

                    case TypeCode.Double:
                        long z64 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(bytes)
                            : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        identifier = *(double*)&z64;
                        break;

                    case TypeCode.Decimal:
#if NET5_0_OR_GREATER
                        int* bits = stackalloc int[4];
                        if (BitConverter.IsLittleEndian)
                        {
                            Unsafe.CopyBlock(bits, bytes, sizeof(int) * 4u);
                        }
#else
                        int[] bits = new int[4];
                        if (BitConverter.IsLittleEndian)
                        {
                            Unsafe.CopyBlock(ref Unsafe.As<int, byte>(ref bits[0]), ref Unsafe.AsRef<byte>(bytes), sizeof(int) * 4u);
                        }
#endif
                        else
                        {
                            bits[0] = *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                            bytes += sizeof(int);
                            bits[1] = *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                            bytes += sizeof(int);
                            bits[2] = *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                            bytes += sizeof(int);
                            bits[3] = *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                        }

#if NET5_0_OR_GREATER
                        identifier = new decimal(new ReadOnlySpan<int>(bits, 4));
#else
                        identifier = new decimal(bits);
#endif
                        break;

                    case TypeCode.DateTime:
                        z64 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(bytes)
                            : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        DateTimeKind kind = (DateTimeKind)((z64 >> 62) & 0b11);
                        z64 &= ~(0b11L << 62);
                        identifier = new DateTime(z64, kind);
                        break;

                    case TypeUtility.TypeCodeTimeSpan:
                        z64 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(bytes)
                            : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        identifier = new TimeSpan(z64);
                        break;

                    case TypeUtility.TypeCodeGuid:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        identifier = new Guid(new ReadOnlySpan<byte>(bytes, 16));
#else
                        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
                        try
                        {
                            Unsafe.CopyBlockUnaligned(ref span[0], ref Unsafe.AsRef<byte>(bytes), 16u);
                            identifier = new Guid(span);
                        }
                        finally
                        {
                            DefaultSerializer.ArrayPool.Return(span);
                        }
#endif
                        break;

                    default:
                        // should never happen
                        throw new Exception();
                }
            }
            else
            {
                Type type = TypeUtility.GetType(typeCode);
                object obj = serializer.ReadObject(type, bytes, maxCt - (uint)size, out int identBytesRead);
                bytesRead = size + identBytesRead;
                return obj;
            }

            bytesRead = size;
            return identifier;
        }
        else
        {
            uint? knownTypeId = null;
            string? typeName = null;
            if ((flags & IdentifierFlags.IsTypeNameOnly) == 0)
            {
                size += sizeof(uint);

                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                knownTypeId = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<uint>(bytes)
                        : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

                bytes += sizeof(uint);
            }
            if ((flags & IdentifierFlags.IsKnownTypeOnly) == 0)
            {
                typeName = serializer.ReadObject<string>(bytes, maxCt - (uint)size, out int strBytesRead);
                bytes += strBytesRead;
                size += strBytesRead;
            }

            Type type = DetermineIdentifierType(serializer, typeName, knownTypeId);
            object identifier = serializer.ReadObject(type, bytes, maxCt - (uint)size, out bytesRead);

            bytesRead += size;
            return identifier;
        }
    }
    internal static unsafe object ReadIdentifierFromStream(IRpcSerializer serializer, Stream stream, out int bytesRead)
    {
        int size = 1;

        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
        IdentifierFlags flags = (IdentifierFlags)b;
        if ((flags & IdentifierFlags.IsTypeCode) != 0)
        {
            ++size;
            b = stream.ReadByte();
            if (b == -1)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
            TypeCode typeCode = (TypeCode)b;

            if (typeCode is > TypeUtility.MaxUsedTypeCode or <= TypeCode.DBNull)
                throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };

            object identifier;

            if (serializer.CanFastReadPrimitives)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                byte[]? bytes = null;
                try
                {
#else
                scoped Span<byte> bytes;
#endif
                if (typeCode == TypeCode.String)
                {
                    string value = serializer.ReadObject<string>(stream, out int strBytesRead);
                    bytesRead = size + strBytesRead;
                    return value;
                }
                int tcsz = TypeUtility.GetTypeCodeSize(typeCode);
                size += tcsz;
                if (size != 1)
                {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    bytes = DefaultSerializer.ArrayPool.Rent(tcsz);
                    int byteCt = stream.Read(bytes, 0, tcsz);
#else
                    bytes = stackalloc byte[tcsz];
                    int byteCt = stream.Read(bytes);
#endif
                    if (byteCt < tcsz)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
                }
                else
                {
                    bytes = null!;
                    b = stream.ReadByte();
                    if (b == -1)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
                }


                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        identifier = b != 0;
                        break;

                    case TypeCode.SByte:
                        identifier = unchecked((sbyte)(byte)b);
                        break;

                    case TypeCode.Byte:
                        identifier = (byte)b;
                        break;

                    case TypeCode.Char:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<char>(ref bytes[0])
                            : (char)(bytes[0] << 8 | bytes[1]);
                        break;

                    case TypeCode.Int16:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<short>(ref bytes[0])
                            : (short)(bytes[0] << 8 | bytes[1]);
                        break;

                    case TypeCode.UInt16:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<ushort>(ref bytes[0])
                            : (ushort)(bytes[0] << 8 | bytes[1]);
                        break;

                    case TypeCode.Int32:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<int>(ref bytes[0])
                            : bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                        break;

                    case TypeCode.UInt32:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<uint>(ref bytes[0])
                            : (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
                        break;

                    case TypeCode.Int64:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(ref bytes[0])
                            : ((long)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        break;

                    case TypeCode.UInt64:
                        identifier = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<ulong>(ref bytes[0])
                            : ((ulong)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        break;

                    case TypeCode.Single:
                        int z32 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<int>(ref bytes[0])
                            : bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                        identifier = *(float*)&z32;
                        break;

                    case TypeCode.Double:
                        long z64 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(ref bytes[0])
                            : ((long)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        identifier = *(double*)&z64;
                        break;

                    case TypeCode.Decimal:
#if NET5_0_OR_GREATER
                        Span<int> bits = stackalloc int[4];
#else
                        int[] bits = new int[4];
#endif
                        if (BitConverter.IsLittleEndian)
                        {
                            Unsafe.CopyBlock(ref Unsafe.As<int, byte>(ref bits[0]), ref bytes[0], sizeof(int) * 4u);
                        }
                        else
                        {
                            bits[0] = bytes[00] << 24 | bytes[01] << 16 | bytes[02] << 8 | bytes[03];
                            bits[1] = bytes[04] << 24 | bytes[05] << 16 | bytes[06] << 8 | bytes[07];
                            bits[2] = bytes[08] << 24 | bytes[09] << 16 | bytes[10] << 8 | bytes[11];
                            bits[3] = bytes[12] << 24 | bytes[13] << 16 | bytes[14] << 8 | bytes[15];
                        }

                        identifier = new decimal(bits);
                        break;

                    case TypeCode.DateTime:
                        z64 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(ref bytes[0])
                            : ((long)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        DateTimeKind kind = (DateTimeKind)((z64 >> 62) & 0b11);
                        z64 &= ~(0b11L << 62);
                        identifier = new DateTime(z64, kind);
                        break;

                    case TypeUtility.TypeCodeTimeSpan:
                        z64 = BitConverter.IsLittleEndian
                            ? Unsafe.ReadUnaligned<long>(ref bytes[0])
                            : ((long)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                        identifier = new TimeSpan(z64);
                        break;

                    case TypeUtility.TypeCodeGuid:
                        identifier = new Guid(bytes);
                        break;

                    default:
                        // should never happen
                        throw new Exception();
                }
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                }
                finally
                {
                    if (bytes != null)
                        DefaultSerializer.ArrayPool.Return(bytes);
                }
#endif
            }
            else
            {
                Type type = TypeUtility.GetType(typeCode);
                object obj = serializer.ReadObject(type, stream, out int identBytesRead);
                bytesRead = size + identBytesRead;
                return obj;
            }

            bytesRead = size;
            return identifier;
        }
        else
        {
            uint? knownTypeId = null;
            string? typeName = null;
            int sz = (flags & IdentifierFlags.IsTypeNameOnly) == 0 ? sizeof(uint) : 0;
            sz += (flags & IdentifierFlags.IsKnownTypeOnly) == 0 ? sizeof(ushort) : 0;
            int arrSize = (flags & IdentifierFlags.IsTypeNameOnly) == 0 ? sz + 32 : sz;
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byte[] bytes = new byte[arrSize];
            int byteCt = stream.Read(bytes, 0, sz);
#else
            scoped Span<byte> bytes = stackalloc byte[arrSize];
            int byteCt = stream.Read(bytes[..sz]);
#endif
            if (byteCt < sz)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            if ((flags & IdentifierFlags.IsTypeNameOnly) == 0)
            {
                size += sizeof(uint);

                knownTypeId = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref bytes[0])
                    : (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
            }
            if ((flags & IdentifierFlags.IsKnownTypeOnly) == 0)
            {
                size += sizeof(ushort);

                int strLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref bytes[0])
                    : (ushort)(bytes[0] << 8 | bytes[1]);

                size += strLen;

                if (strLen > bytes.Length)
                {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    bytes = new byte[strLen];
#else
                    bytes = stackalloc byte[strLen];
#endif
                }

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                byteCt = stream.Read(bytes, 0, strLen);
#else
                byteCt = stream.Read(bytes[..strLen]);
#endif
                if (byteCt < strLen)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                typeName = Encoding.UTF8.GetString(bytes, 0, strLen);
#else
                typeName = Encoding.UTF8.GetString(bytes[..strLen]);
#endif
            }

            Type type = DetermineIdentifierType(serializer, typeName, knownTypeId);
            object identifier = serializer.ReadObject(type, stream, out bytesRead);

            bytesRead += size;
            return identifier;
        }
    }
    private static Type DetermineIdentifierType(IRpcSerializer serializer, string? typeName, uint? knownTypeId)
    {
        Type? type = null;
        if (knownTypeId.HasValue)
            serializer.TryGetKnownType(knownTypeId.Value, out type);

        if (type != null)
            return type;

        if (typeName != null)
            type = Type.GetType(typeName, false, false);

        if (type == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionUnknownIdentifierType) { ErrorCode = 8 };

        if (knownTypeId.HasValue)
        {
            serializer.SaveKnownType(knownTypeId.Value, type);
        }

        return type;
    }
    
    [Flags]
    protected internal enum IdentifierFlags : byte
    {
        IsTypeCode = 1,
        IsKnownTypeOnly = 1 << 3,
        IsTypeNameOnly = 1 << 4
    }

    [Flags]
    protected internal enum EndpointFlags
    {
        IsStatic = 1,
        HasIdentifier = 1 << 1
    }
}
