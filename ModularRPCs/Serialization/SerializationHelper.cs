using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Serialization.Parsers;

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Helper functions for finding and registering <see cref="IBinaryTypeParser"/> objects.
/// </summary>
public static class SerializationHelper
{
    /// <summary>
    /// The minimum size of an array written with the standard array header format.
    /// </summary>
    public const int MinimumArraySize = 1;

    /// <summary>
    /// The minimum size of a string written with a <see cref="StringParser"/>.
    /// </summary>
    public const int MinimumStringSize = 1;

    /// <summary>
    /// Adds or updates a serializer in a dictionary for all the supported array/list types.
    /// <para>These collection types are <typeparamref name="TElementType"/>[], <see cref="IList{T}"/> of <typeparamref name="TElementType"/>, <see cref="IReadOnlyList{T}"/> of <typeparamref name="TElementType"/>, <see cref="Span{T}"/> of <typeparamref name="TElementType"/>, and <see cref="ReadOnlySpan{T}"/> of <typeparamref name="TElementType"/>. If <typeparamref name="TElementType"/> is <see cref="bool"/>, it also includes <see cref="BitArray"/>.</para>
    /// </summary>
    /// <typeparam name="TElementType">The element type of the array.</typeparam>
    public static void AddManySerializer<TElementType>(this IDictionary<Type, IBinaryTypeParser> dict, IArrayBinaryTypeParser<TElementType> parser)
    {
        if (!dict.ContainsKey(typeof(TElementType[])))
            dict.Add(typeof(TElementType[]), parser);
        if (!dict.ContainsKey(typeof(IList<TElementType>)))
            dict.Add(typeof(IList<TElementType>), parser);
        if (!dict.ContainsKey(typeof(IReadOnlyList<TElementType>)))
            dict.Add(typeof(IReadOnlyList<TElementType>), parser);
        if (!dict.ContainsKey(typeof(ICollection<TElementType>)))
            dict.Add(typeof(ICollection<TElementType>), parser);
        if (!dict.ContainsKey(typeof(IReadOnlyCollection<TElementType>)))
            dict.Add(typeof(IReadOnlyCollection<TElementType>), parser);
        if (!dict.ContainsKey(typeof(IEnumerable<TElementType>)))
            dict.Add(typeof(IEnumerable<TElementType>), parser);
        if (!dict.ContainsKey(typeof(Span<TElementType>)))
            dict.Add(typeof(Span<TElementType>), parser);
        if (!dict.ContainsKey(typeof(ReadOnlySpan<TElementType>)))
            dict.Add(typeof(ReadOnlySpan<TElementType>), parser);

        if (typeof(TElementType) == typeof(bool) && parser is IBinaryTypeParser<BitArray> && !dict.ContainsKey(typeof(BitArray)))
            dict.Add(typeof(BitArray), parser);
    }

    /// <summary>
    /// Adds or updates a serializer in a dictionary for all the supported array/list types from a factory taking in the collection type. Return <see langword="null"/> to skip the type.
    /// <para>These collection types are <typeparamref name="TElementType"/>[], <see cref="IList{T}"/> of <typeparamref name="TElementType"/>, <see cref="IReadOnlyList{T}"/> of <typeparamref name="TElementType"/>, <see cref="Span{T}"/> of <typeparamref name="TElementType"/>, and <see cref="ReadOnlySpan{T}"/> of <typeparamref name="TElementType"/>. If <typeparamref name="TElementType"/> is <see cref="bool"/>, it also includes <see cref="BitArray"/>.</para>
    /// </summary>
    /// <typeparam name="TElementType">The element type of the array.</typeparam>
    public static void AddManySerializer<TElementType>(this IDictionary<Type, IBinaryTypeParser> dict, Func<Type, IBinaryTypeParser?> parserFactory)
    {
        if (!dict.ContainsKey(typeof(TElementType[])))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(TElementType[]));
            if (parser != null)
                dict.Add(typeof(TElementType[]), parser);
        }
        
        if (!dict.ContainsKey(typeof(IList<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(IList<TElementType>));
            if (parser != null)
                dict.Add(typeof(IList<TElementType>), parser);
        }
        
        if (!dict.ContainsKey(typeof(IReadOnlyList<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(IReadOnlyList<TElementType>));
            if (parser != null)
                dict.Add(typeof(IReadOnlyList<TElementType>), parser);
        }
        
        if (!dict.ContainsKey(typeof(ICollection<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(ICollection<TElementType>));
            if (parser != null)
                dict.Add(typeof(ICollection<TElementType>), parser);
        }
        
        if (!dict.ContainsKey(typeof(IReadOnlyCollection<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(IReadOnlyCollection<TElementType>));
            if (parser != null)
                dict.Add(typeof(IReadOnlyCollection<TElementType>), parser);
        }
        
        if (!dict.ContainsKey(typeof(IEnumerable<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(IEnumerable<TElementType>));
            if (parser != null)
                dict.Add(typeof(IEnumerable<TElementType>), parser);
        }
        
        if (!dict.ContainsKey(typeof(Span<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(Span<TElementType>));
            if (parser != null)
                dict.Add(typeof(Span<TElementType>), parser);
        }
        
        if (!dict.ContainsKey(typeof(ReadOnlySpan<TElementType>)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(ReadOnlySpan<TElementType>));
            if (parser != null)
                dict.Add(typeof(ReadOnlySpan<TElementType>), parser);
        }
        
        if (typeof(TElementType) == typeof(bool) && !dict.ContainsKey(typeof(BitArray)))
        {
            IBinaryTypeParser? parser = parserFactory(typeof(BitArray));
            if (parser != null)
                dict.Add(typeof(BitArray), parser);
        }
    }

    /// <summary>
    /// Finds all <see cref="RpcParserAttribute"/>'s from all types declared in the calling assembly and all assemblies it directly references. Order with the <see cref="PriorityAttribute"/>.
    /// </summary>
    /// <remarks>Serializers can either have an empty constructor or a constructor with only a <see cref="SerializationConfiguration"/> parameter.</remarks>
    /// <param name="dict">A dictionary mapping types to parsers. Use this in the <see cref="DefaultSerializer"/> configuration callback.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterParserAttributes(this IDictionary<Type, IBinaryTypeParser> dict, SerializationConfiguration configuration)
    {
        Assembly caller = Assembly.GetCallingAssembly();

        dict.RegisterParserAttributes(configuration, caller);

        foreach (AssemblyName asmName in caller.GetReferencedAssemblies())
        {
            try
            {
                dict.RegisterParserAttributes(configuration, Assembly.Load(asmName));
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }
        }
    }

    /// <summary>
    /// Finds all <see cref="RpcParserAttribute"/>'s from all types declared in the given <paramref name="assemblies"/>. Order with the <see cref="PriorityAttribute"/>.
    /// </summary>
    /// <remarks>Serializers can either have an empty constructor or a constructor with only a <see cref="SerializationConfiguration"/> parameter.</remarks>
    /// <param name="dict">A dictionary mapping types to parsers. Use this in the <see cref="DefaultSerializer"/> configuration callback.</param>
    public static void RegisterParserAttributes(this IDictionary<Type, IBinaryTypeParser> dict, SerializationConfiguration configuration, params Assembly[] assemblies)
        => dict.RegisterParserAttributes(configuration, (IEnumerable<Assembly>)assemblies);

    /// <summary>
    /// Finds all <see cref="RpcParserAttribute"/>'s from all types declared in the given <paramref name="assemblies"/>. Order with the <see cref="PriorityAttribute"/>.
    /// </summary>
    /// <remarks>Serializers can either have an empty constructor or a constructor with only a <see cref="SerializationConfiguration"/> parameter.</remarks>
    /// <param name="dict">A dictionary mapping types to parsers. Use this in the <see cref="DefaultSerializer"/> configuration callback.</param>
    public static void RegisterParserAttributes(this IDictionary<Type, IBinaryTypeParser> dict, SerializationConfiguration configuration, IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly asm in assemblies)
        {
            dict.RegisterParserAttributes(configuration, asm);
        }
    }

    /// <summary>
    /// Finds all <see cref="RpcParserAttribute"/>'s from all types declared in the given <paramref name="assembly"/>. Order with the <see cref="PriorityAttribute"/>.
    /// </summary>
    /// <remarks>Serializers can either have an empty constructor or a constructor with only a <see cref="SerializationConfiguration"/> parameter.</remarks>
    /// <param name="dict">A dictionary mapping types to parsers. Use this in the <see cref="DefaultSerializer"/> configuration callback.</param>
    public static void RegisterParserAttributes(this IDictionary<Type, IBinaryTypeParser> dict, SerializationConfiguration configuration, Assembly assembly)
    {
        foreach (Type type in Accessor.GetTypesSafe(assembly, removeIgnored: false))
        {
            Type valueType = type;
            if (!valueType.TryGetAttributeSafe(out RpcParserAttribute parserAttribute))
                continue;

            Type? parserType = parserAttribute.Type;
            if (parserType == null)
                continue;

            // parserType is the target type instead of the parser
            if (typeof(IBinaryTypeParser).IsAssignableFrom(valueType))
            {
                (valueType, parserType) = (parserType, valueType);
            }

            if (parserType.IsAbstract || !typeof(IBinaryTypeParser).IsAssignableFrom(parserType))
                continue;

            foreach (Type nestedParserType in parserType.GetNestedTypes())
            {
                // look for nested parsers
                if (nestedParserType.IsAbstract || !typeof(IBinaryTypeParser).IsAssignableFrom(nestedParserType) || nestedParserType.IsIgnored())
                    continue;

                if (TryCreateParser(nestedParserType, configuration, out IBinaryTypeParser arrayParser))
                {
                    RegisterParser(valueType, nestedParserType, dict, arrayParser);
                }
            }

            if (!TryCreateParser(parserType, configuration, out IBinaryTypeParser parser))
                continue;

            RegisterParser(valueType, parserType, dict, parser);
        }
    }

    private static void RegisterParser(Type valueType, Type parserType, IDictionary<Type, IBinaryTypeParser> dict, IBinaryTypeParser parser)
    {
        Type[] valueTypeArray = [ valueType ];

        if (!typeof(IArrayBinaryTypeParser<>).MakeGenericType(valueTypeArray).IsAssignableFrom(parserType))
        {
            dict[valueType] = parser;
            return;
        }

        dict[valueType.MakeArrayType()] = parser;
        dict[typeof(IList<>).MakeGenericType(valueTypeArray)] = parser;
        dict[typeof(IReadOnlyList<>).MakeGenericType(valueTypeArray)] = parser;
        dict[typeof(ICollection<>).MakeGenericType(valueTypeArray)] = parser;
        dict[typeof(IReadOnlyCollection<>).MakeGenericType(valueTypeArray)] = parser;
        dict[typeof(IEnumerable<>).MakeGenericType(valueTypeArray)] = parser;
        dict[typeof(Span<>).MakeGenericType(valueTypeArray)] = parser;
        dict[typeof(ReadOnlySpan<>).MakeGenericType(valueTypeArray)] = parser;
        if (valueType == typeof(bool) && typeof(IBinaryTypeParser<BitArray>).IsAssignableFrom(parserType))
        {
            dict[typeof(BitArray)] = parser;
        }
    }

    private static bool TryCreateParser(Type parserType, SerializationConfiguration configuration, out IBinaryTypeParser parser)
    {
        ConstructorInfo? configCtor = parserType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null,
            CallingConventions.Any, [typeof(SerializationConfiguration)], null);

        if (configCtor != null)
        {
            parser = (IBinaryTypeParser)configCtor.Invoke([ configuration ]);
            return true;
        }

        ConstructorInfo? emptyCtor = parserType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null,
            CallingConventions.Any, Type.EmptyTypes, null);

        if (emptyCtor != null)
        {
            parser = (IBinaryTypeParser)emptyCtor.Invoke(Array.Empty<object>());
            return true;
        }

        parser = null!;
        return false;
    }

    /*
     * Header format:
     * [ 1 byte - flags                                                                                     ] [ byte count                ] [ data...            ]
     * | 100000BB            mask   meaning                                                                 | | variable sizes, see flags | | length: byte count |
     * | ^     11 elem count 0b0011 0 = empty array, 1 = 8 bit length, 2 = 16 bit length, 3 = 32 bit length | |                           | |                    |
     * | null                                                                                               | |                           | |                    |
     */

    /// <summary>
    /// Write the format of a standard array header (see comment in source code) given the <paramref name="length"/> and if the array <paramref name="isNull"/>.
    /// </summary>
    /// <param name="bytes">Write destination.</param>
    /// <param name="maxSize">Maximum amount of bytes left in <paramref name="bytes"/>, not including what was taken up by <paramref name="index"/>.</param>
    /// <param name="index">Current position in <paramref name="bytes"/>.</param>
    /// <param name="length">Length of the array in elements.</param>
    /// <param name="isNull">If the array is <see langword="null"/>.</param>
    /// <param name="parser">Used to display the parser type in errors when the buffer runs out. Can be <c>this</c>.</param>
    /// <returns>Number of bytes written to <paramref name="bytes"/>. <paramref name="index"/> will also be incremented by this value.</returns>
    /// <exception cref="RpcOverflowException">Error code 1, buffer overflowed.</exception>
    public static unsafe int WriteStandardArrayHeader(byte* bytes, uint maxSize, ref uint index, int length, bool isNull, object parser, bool forceFull = false)
    {
        byte lenFlag = GetLengthFlag(length, isNull, forceFull);

        int hdrSize = GetHeaderSize(lenFlag);
        if (maxSize - index < hdrSize)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };

        bytes[index] = lenFlag;
        ++index;
        if ((lenFlag & 0b10000000) == 0)
        {
            switch (lenFlag & 3)
            {
                case 1:
                    bytes[index] = (byte)length;
                    ++index;
                    break;

                case 2:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(bytes + index, (ushort)length);
                    }
                    else
                    {
                        bytes[index + 1] = unchecked((byte)length);
                        bytes[index] = unchecked((byte)(length >>> 8));
                    }

                    index += 2;
                    break;

                default:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(bytes + index, length);
                    }
                    else
                    {
                        bytes[index + 3] = unchecked((byte)length);
                        bytes[index + 2] = unchecked((byte)(length >>> 8));
                        bytes[index + 1] = unchecked((byte)(length >>> 16));
                        bytes[index] = unchecked((byte)(length >>> 24));
                    }

                    index += 4;
                    break;
            }
        }
        else
        {
            index += (uint)hdrSize - 1u;
        }

        return hdrSize;
    }

    /// <summary>
    /// Write the format of a standard array header (see comment in source code) given the <paramref name="length"/> and if the array <paramref name="isNull"/>.
    /// </summary>
    /// <param name="stream">Stream to write the header to.</param>
    /// <param name="length">Length of the array in elements.</param>
    /// <param name="isNull">If the array is <see langword="null"/>.</param>
    /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
    public static int WriteStandardArrayHeader(Stream stream, int length, bool isNull, bool forceFull = false)
    {
        byte lenFlag = GetLengthFlag(length, isNull, forceFull);
        int hdrSize = GetHeaderSize(lenFlag);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(hdrSize);
        try
        {
#else
        Span<byte> span = stackalloc byte[hdrSize];
#endif
        span[0] = lenFlag;
        if ((lenFlag & 0b10000000) == 0)
        {
            switch (lenFlag & 3)
            {
                case 1:
                    span[1] = (byte)length;
                    break;

                case 2:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(ref span[1], (ushort)length);
                    }
                    else
                    {
                        span[2] = unchecked( (byte) length );
                        span[1] = unchecked( (byte)(length >>> 8) );
                    }
                    break;

                default:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(ref span[1], length);
                    }
                    else
                    {
                        span[4] = unchecked( (byte) length );
                        span[3] = unchecked( (byte)(length >>> 8) );
                        span[2] = unchecked( (byte)(length >>> 16) );
                        span[1] = unchecked( (byte)(length >>> 24) );
                    }
                    break;
            }
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, hdrSize);
#else
        stream.Write(span);
#endif

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        return hdrSize;
    }

    /// <summary>
    /// Read the format of a standard array header (see comment in source code) returning the <paramref name="length"/> and if the array was null when it was written.
    /// </summary>
    /// <param name="bytes">Read source.</param>
    /// <param name="maxSize">Maximum amount of bytes left in <paramref name="bytes"/>, not including what was taken up by <paramref name="index"/>.</param>
    /// <param name="index">Current position in <paramref name="bytes"/>. Will be incremented by the number of bytes read.</param>
    /// <param name="length">Length of the array in elements.</param>
    /// <param name="parser">Used to display the parser type in errors when the buffer runs out. Can be <c>this</c>.</param>
    /// <returns><see langword="false"/> if the array read as <see langword="null"/>, otherwise <see langword="true"/>.</returns>
    /// <exception cref="RpcParseException">Error code 1, buffer ran out.</exception>
    public static unsafe bool ReadStandardArrayHeader(byte* bytes, uint maxSize, ref uint index, out int length, object parser)
    {
        if (maxSize - index < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };

        byte lenFlag = bytes[index];
        ++index;
        if (lenFlag == 0)
        {
            length = 0;
            return true;
        }

        if ((lenFlag & 0b10000000) != 0)
        {
            if ((lenFlag & 3) == 3)
            {
                index += 4;
            }
            length = -1;
            return false;
        }

        switch (lenFlag & 3)
        {
            case 1:
                if (maxSize < 2)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };
                length = bytes[index];
                ++index;
                break;

            case 2:
                if (maxSize < 3)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(bytes + index)
                    : bytes[index] << 8 | bytes[index + 1];
                index += 2;
                break;

            default:
                if (maxSize < 5)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(bytes + index)
                    : bytes[index] << 24 | bytes[index + 1] << 16 | bytes[index + 2] << 8 | bytes[index + 3];
                index += 4;
                break;
        }

        return true;
    }

    /// <summary>
    /// Read the format of a standard array header (see comment in source code) returning the <paramref name="length"/> and if the array was null when it was written.
    /// </summary>
    /// <param name="stream">The stream to read data from.</param>
    /// <param name="bytesRead">Number of bytes read from the stream.</param>
    /// <param name="length">Length of the array in elements.</param>
    /// <param name="parser">Used to display the parser type in errors when the buffer runs out. Can be <c>this</c>.</param>
    /// <returns><see langword="false"/> if the array read as <see langword="null"/>, otherwise <see langword="true"/>.</returns>
    /// <exception cref="RpcParseException">Error code 2, stream ran out.</exception>
    public static bool ReadStandardArrayHeader(Stream stream, out int length, out int bytesRead, object parser)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 2 };

        byte lenFlag = (byte)b;
        if (lenFlag == 0)
        {
            length = 0;
            bytesRead = 1;
            return true;
        }

        bool isNull = (lenFlag & 0b10000000) != 0;
        if (isNull && (lenFlag & 3) != 3)
        {
            length = -1;
            bytesRead = 1;
            return false;
        }

        int hdrSize = GetHeaderSize(lenFlag) - 1;
        
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(hdrSize);
        try
        {
#else
        Span<byte> span = stackalloc byte[hdrSize];
#endif

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        int ct = stream.Read(span, 0, hdrSize);
#else
        int ct = stream.Read(span);
#endif

        if (ct != hdrSize)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 2 };

        switch (lenFlag & 3)
        {
            case 1:
                length = span[0];
                bytesRead = 2;
                break;

            case 2:
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref span[0])
                    : span[0] << 8 | span[1];
                bytesRead = 3;
                break;

            default:
                if (!isNull)
                {
                    length = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<int>(ref span[0])
                        : span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3];
                }
                else
                {
                    length = -1;
                }
                bytesRead = 5;
                break;
        }
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        return !isNull;
    }

    internal static byte GetLengthFlag(int length, bool isNull, bool forceFull = false)
    {
        if (isNull)
        {
            return forceFull ? (byte)0b10000011 : (byte)0b10000000;
        }

        if (forceFull)
            return 3;

        byte f = length switch
        {
            > ushort.MaxValue => 3,
            > byte.MaxValue => 2,
            0 => 0,
            _ => 1
        };
        return f;
    }

    internal static int GetHeaderSize(byte lenFlag) =>
        (lenFlag & 3) switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            _ => 5
        };

    /// <summary>
    /// Get the size of the standard array header (see comment in source code) given the array <paramref name="length"/> and if the array <paramref name="isNull"/>.
    /// </summary>
    /// <param name="length">The number of elements in the array.</param>
    /// <param name="isNull">If the array is <see langword="null"/>.</param>
    /// <returns>Size of the header in bytes.</returns>
    public static int GetHeaderSize(int length, bool isNull, bool forceFull = false) => forceFull ? 5 : GetHeaderSize(GetLengthFlag(length, isNull, forceFull));

    /// <summary>
    /// Manually try to advance a stream a number of bytes to make sure a stream ends up where it should, even if a parser has to throw an error. It may not actually advance that much or at all, depending on how much data is left and what type of stream it is.
    /// </summary>
    /// <param name="stream">The stream to advance.</param>
    /// <param name="bytesRead">Incremented by the number of bytes that the stream was actually advanced.</param>
    /// <param name="length">Number of bytes to try to advance the stream.</param>
    public static void TryAdvanceStream(Stream stream, SerializationConfiguration config, ref int bytesRead, int length)
    {
        if (stream.CanSeek)
        {
            try
            {
                long oldPos = stream.Position;
                long newPos = stream.Seek(length, SeekOrigin.Current);
                bytesRead += (int)(newPos - oldPos);

                return;
            }
            catch (NotSupportedException)
            {
                // ignored
            }
        }

        if (!stream.CanRead || length < 0)
            return;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        if (length <= config.MaximumStackAllocationSize)
        {
            Span<byte> span = stackalloc byte[length];
            bytesRead += stream.Read(span);
        }
        else
#endif
        if (length <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
            try
            {
                bytesRead += stream.Read(buffer, 0, length);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (length <= config.MaximumBufferSize)
        {
            byte[] buffer = new byte[config.MaximumBufferSize];
            bytesRead += stream.Read(buffer, 0, length);
        }
        else
        {
            byte[] buffer = new byte[config.MaximumBufferSize];
            int bytesLeft = length;
            int ct;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                ct = stream.Read(buffer, 0, sizeToCopy);
                bytesRead += ct;
                bytesLeft -= ct;
            } while (bytesLeft > 0 && ct > 0);
        }
    }
}
