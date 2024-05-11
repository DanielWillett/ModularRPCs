using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Reflection;

namespace DanielWillett.ModularRpcs.Routing;
public class DefaultRpcRouter : IRpcRouter
{
    private readonly IRpcSerializer _serializer;
    protected readonly ConcurrentDictionary<uint, Type> KnownTypes = new ConcurrentDictionary<uint, Type>();
    public DefaultRpcRouter(IRpcSerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// A dictionary of unique IDs to invocation points.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, IRpcInvocationPoint> CachedDescriptors = new ConcurrentDictionary<uint, IRpcInvocationPoint>();

    /// <summary>
    /// The next Id to be used, actually a <see cref="uint"/> but stored as <see cref="int"/> to be used with <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    protected int NextId;
    public virtual IRpcInvocationPoint? FindSavedRpcEndpoint(uint endpointSharedId)
    {
        return CachedDescriptors.TryGetValue(endpointSharedId, out IRpcInvocationPoint? endpoint) ? endpoint : null!;
    }

    public uint AddRpcEndpoint(IRpcInvocationPoint endPoint)
    {
        // keep trying to add if the id is taken, could've been added by a third party
        while (true)
        {
            uint id = unchecked((uint)Interlocked.Increment(ref NextId));
            if (!CachedDescriptors.TryAdd(id, endPoint))
            {
                if (NextId == 0)  // NextId rolled over. Realistically memory will run out before this gets called, but better to prevent an infinite loop.
                    throw new InvalidOperationException($"There are too many saved endpoints {CachedDescriptors.Count}.");
            }
            else
            {
                endPoint.EndpointId = id;
                return id;
            }
        }
    }

    protected virtual IRpcInvocationPoint CreateEndpoint(uint key, string typeName, string methodName, string[]? args, bool isStatic)
    {
        return new RpcEndpoint(key, typeName, methodName, args, isStatic, null, null);
    }
    public virtual IRpcInvocationPoint ResolveEndpoint(uint knownRpcShortcutId, string typeName, string methodName, bool isStatic, string[] args, int byteSize, object? identifier)
    {
        IRpcInvocationPoint cachedEndpoint = knownRpcShortcutId == 0u
            ? ValueFactory(0u)
            : CachedDescriptors.GetOrAdd(knownRpcShortcutId, ValueFactory);

        return ReferenceEquals(cachedEndpoint.Identifier, identifier)
            ? cachedEndpoint
            : cachedEndpoint.CloneWithIdentifier(this, identifier);

        IRpcInvocationPoint ValueFactory(uint key)
        {
            IRpcInvocationPoint endPoint = CreateEndpoint(key, typeName, methodName, args, isStatic);
            return endPoint;
        }
    }
    public unsafe object ReadIdentifierFromBytes(byte* bytes, uint maxCt, out int bytesRead)
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
            if (typeCode == TypeCode.String)
            {
                int strLen;
                if ((flags & IdentifierFlags.StrLen32) == IdentifierFlags.StrLen32)
                {
                    size += sizeof(uint);
                    if (maxCt < size)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                    uint strLenU = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<uint>(bytes)
                        : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
                    if (strLenU > int.MaxValue)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStringLengthOverflow) { ErrorCode = 6 };

                    bytes += sizeof(uint);
                    strLen = (int)strLenU;
                }
                else if ((flags & IdentifierFlags.StrLen16) != 0)
                {
                    size += sizeof(ushort);
                    if (maxCt < size)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                    strLen = BitConverter.IsLittleEndian
                        ? *bytes | bytes[1] << 8
                        : *bytes << 8 | bytes[1];
                    bytes += sizeof(ushort);
                }
                else
                {
                    ++size;
                    if (maxCt < size)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                    strLen = *bytes;
                    ++bytes;
                }

                size += strLen;

                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                string value = Encoding.UTF8.GetString(bytes, strLen);
                bytesRead = size;
                return value;
            }
            
            if (typeCode is > TypeCode.String or <= TypeCode.DBNull)
                throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };
            
            size += GetTypeCodeSize(typeCode);
            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            object identifier;
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    identifier = *bytes != 0;
                    ++bytes;
                    break;

                case TypeCode.SByte:
                    identifier = unchecked( (sbyte) *bytes );
                    ++bytes;
                    break;

                case TypeCode.Byte:
                    identifier = *bytes;
                    ++bytes;
                    break;

                case TypeCode.Char:
                    identifier = (char)(
                        BitConverter.IsLittleEndian
                        ? *bytes | bytes[1] << 8
                        : *bytes << 8 | bytes[1]
                    );
                    break;

                case TypeCode.Int16:
                    identifier = (short)(
                        BitConverter.IsLittleEndian
                        ? *bytes | bytes[1] << 8
                        : *bytes << 8 | bytes[1]
                    );
                    break;

                case TypeCode.UInt16:
                    identifier = (ushort)(
                        BitConverter.IsLittleEndian
                        ? *bytes | bytes[1] << 8
                        : *bytes << 8 | bytes[1]
                    );
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

                case (TypeCode)17: // used as TimeSpan
                    z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(bytes)
                        : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                    identifier = new TimeSpan(z64);
                    break;

                default:
                    // should never happen
                    throw new Exception();
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
            }
            if ((flags & IdentifierFlags.IsKnownTypeOnly) == 0)
            {
                size += sizeof(ushort);

                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                int strLen = BitConverter.IsLittleEndian
                        ? *bytes | bytes[1] << 8
                        : *bytes << 8 | bytes[1];

                size += strLen;

                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                typeName = Encoding.UTF8.GetString(bytes, strLen);
            }

            Type type = DetermineIdentifierType(typeName, knownTypeId);
            object identifier = _serializer.ReadObject(type, bytes, (uint)(maxCt - size), out bytesRead);

            bytesRead += size;
            return identifier;
        }
    }
    public unsafe object ReadIdentifierFromStream(Stream stream, out int bytesRead)
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

            int byteCt;
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byte[] bytes;
#else
            scoped Span<byte> bytes;
#endif
            if (typeCode == TypeCode.String)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                bytes = new byte[32];
#else
                bytes = stackalloc byte[64];
#endif
                int strLen;
                if ((flags & IdentifierFlags.StrLen32) == IdentifierFlags.StrLen32)
                {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    byteCt = stream.Read(bytes, 0, sizeof(uint));
#else
                    byteCt = stream.Read(bytes[..sizeof(uint)]);
#endif
                    if (byteCt < sizeof(uint))
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

                    size += sizeof(uint);

                    uint strLenU = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<uint>(ref bytes[0])
                        : (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

                    if (strLenU > int.MaxValue)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStringLengthOverflow) { ErrorCode = 6 };

                    strLen = (int)strLenU;
                }
                else if ((flags & IdentifierFlags.StrLen16) != 0)
                {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    byteCt = stream.Read(bytes, 0, sizeof(ushort));
#else
                    byteCt = stream.Read(bytes[..sizeof(ushort)]);
#endif
                    if (byteCt < sizeof(ushort))
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

                    size += sizeof(ushort);

                    strLen = BitConverter.IsLittleEndian
                        ? bytes[0] | bytes[1] << 8
                        : bytes[0] << 8 | bytes[1];
                }
                else
                {
                    strLen = stream.ReadByte();
                    if (strLen == -1)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

                    ++size;
                }

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

                size += strLen;

                if (byteCt < strLen)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                string value = Encoding.UTF8.GetString(bytes, 0, strLen);
#else
                string value = Encoding.UTF8.GetString(bytes[..strLen]);
#endif
                bytesRead = size;
                return value;
            }

            if (typeCode is > TypeCode.String or <= TypeCode.DBNull)
                throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };

            int tcsz = GetTypeCodeSize(typeCode);
            size += tcsz;
            if (size != 1)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                bytes = new byte[tcsz];
                byteCt = stream.Read(bytes, 0, tcsz);
#else
                bytes = stackalloc byte[tcsz];
                byteCt = stream.Read(bytes[..tcsz]);
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


            object identifier;
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    identifier = b != 0;
                    break;

                case TypeCode.SByte:
                    identifier = unchecked( (sbyte)(byte)b );
                    break;

                case TypeCode.Byte:
                    identifier = (byte)b;
                    break;

                case TypeCode.Char:
                    identifier = (char)(
                        BitConverter.IsLittleEndian
                        ? bytes[0] | bytes[1] << 8
                        : bytes[0] << 8 | bytes[1]
                    );
                    break;

                case TypeCode.Int16:
                    identifier = (short)(
                        BitConverter.IsLittleEndian
                        ? bytes[0] | bytes[1] << 8
                        : bytes[0] << 8 | bytes[1]
                    );
                    break;

                case TypeCode.UInt16:
                    identifier = (ushort)(
                        BitConverter.IsLittleEndian
                        ? bytes[0] | bytes[1] << 8
                        : bytes[0] << 8 | bytes[1]
                    );
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

                case (TypeCode)17: // used as TimeSpan
                    z64 = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<long>(ref bytes[0])
                        : ((long)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
                    identifier = new TimeSpan(z64);
                    break;

                default:
                    // should never happen
                    throw new Exception();
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
                    ? bytes[0] | bytes[1] << 8
                    : bytes[0] << 8 | bytes[1];

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

            Type type = DetermineIdentifierType(typeName, knownTypeId);
            object identifier = _serializer.ReadObject(type, stream, out bytesRead);

            bytesRead += size;
            return identifier;
        }
    }
    private Type DetermineIdentifierType(string? typeName, uint? knownTypeId)
    {
        Type? type = null;
        if (knownTypeId.HasValue)
            KnownTypes.TryGetValue(knownTypeId.Value, out type);

        if (type != null)
            return type;

        if (typeName != null)
            type = Type.GetType(typeName, false, false);

        if (type == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionUnknownIdentifierType) { ErrorCode = 8 };

        if (knownTypeId.HasValue)
            KnownTypes[knownTypeId.Value] = type;

        return type;
    }
    public int CalculateIdentifierSize(object identifier) => throw new NotImplementedException();
    public void GetDefaultProxyContext(Type proxyType, out ProxyContext context)
    {
        context = default;
        context.DefaultSerializer = _serializer;
        context.Router = this;
    }

    public ValueTask HandleReceivedData(RpcOverhead overhead, Stream streamData, CancellationToken token = default)
    {
        return default;
    }
    public ValueTask HandleReceivedData(RpcOverhead overhead, ReadOnlySpan<byte> byteData, CancellationToken token = default)
    {
        IRpcInvocationPoint invocationPoint = overhead.Rpc;

        
        
        return default;
    }
    private static int GetTypeCodeSize(TypeCode tc)
    {
        return tc switch
        {
            TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte => 1,
            TypeCode.Char => sizeof(char),
            TypeCode.Int16 => sizeof(short),
            TypeCode.UInt16 => sizeof(ushort),
            TypeCode.Int32 => sizeof(int),
            TypeCode.UInt32 => sizeof(uint),
            TypeCode.Int64 => sizeof(long),
            TypeCode.UInt64 => sizeof(ulong),
            TypeCode.Single => sizeof(float),
            TypeCode.Double => sizeof(double),
            TypeCode.Decimal => sizeof(int) * 4,
            TypeCode.DateTime => sizeof(long),
            // TimeSpan
            (TypeCode)17 => sizeof(long),
            _ => throw new ArgumentOutOfRangeException(nameof(tc), tc, null)
        };
    }

    [Flags]
    protected internal enum IdentifierFlags : byte
    {
        IsTypeCode = 1,
        StrLen8 = 1 << 1,
        StrLen16 = 1 << 2,
        StrLen32 = StrLen8 | StrLen16,
        IsKnownTypeOnly = 1 << 3,
        IsTypeNameOnly = 1 << 4
    }
}