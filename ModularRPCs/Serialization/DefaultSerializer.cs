using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_0_OR_GREATER
using System.Collections;
#endif
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
using System.Buffers;
#endif

namespace DanielWillett.ModularRpcs.Serialization;
public class DefaultSerializer : IRpcSerializer
{
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
    internal static ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create(16, 6);
#endif
    protected readonly ConcurrentDictionary<uint, Type> KnownTypes = new ConcurrentDictionary<uint, Type>();
    protected readonly ConcurrentDictionary<Type, uint> KnownTypeIds = new ConcurrentDictionary<Type, uint>();
    private readonly Dictionary<Type, IBinaryTypeParser> _parsers;

    private readonly Dictionary<Type, IBinaryTypeParser> _primitiveParsers = new Dictionary<Type, IBinaryTypeParser>
    {
        { typeof(byte), new UInt8Parser() },
        { typeof(sbyte), new Int8Parser() },
        { typeof(bool), new BooleanParser() },
        { typeof(ushort), new UInt16Parser() },
        { typeof(short), new Int16Parser() },
        { typeof(char), new CharParser() },
        { typeof(uint), new UInt32Parser() },
        { typeof(int), new Int32Parser() },
        { typeof(ulong), new UInt64Parser() },
        { typeof(long), new Int64Parser() },
        { typeof(nuint), new UIntPtrParser() },
        { typeof(nint), new IntPtrParser() },
        { typeof(string), new Utf8Parser() },
#if NET5_0_OR_GREATER
        { typeof(Half), new HalfParser() },
#endif
        { typeof(float), new SingleParser() },
        { typeof(double), new DoubleParser() },
        { typeof(decimal), new DecimalParser() },
        { typeof(DateTime), new DateTimeParser() },
        { typeof(DateTimeOffset), new DateTimeOffsetParser() },
        { typeof(TimeSpan), new TimeSpanParser() },
        { typeof(Guid), new GuidParser() }
    };

    private readonly Dictionary<Type, int> _primitiveSizes = new Dictionary<Type, int>
    {
        { typeof(byte), 1 },
        { typeof(sbyte), 1 },
        { typeof(bool), 1 },
        { typeof(ushort), sizeof(ushort) },
        { typeof(short), sizeof(short) },
        { typeof(char), sizeof(char) },
        { typeof(uint), sizeof(uint) },
        { typeof(int), sizeof(int) },
        { typeof(ulong), sizeof(ulong) },
        { typeof(long), sizeof(long) },
        { typeof(nuint), sizeof(ulong) }, // native ints are always read/written in 64 bit
        { typeof(nint), sizeof(long) },
#if NET5_0_OR_GREATER
        { typeof(Half), 2 },
#endif
        { typeof(float), sizeof(float) },
        { typeof(double), sizeof(double) },
        { typeof(decimal), 16 },
        { typeof(DateTime), sizeof(long) },
        { typeof(DateTimeOffset), sizeof(long) + sizeof(short) },
        { typeof(TimeSpan), sizeof(long) },
        { typeof(Guid), 16 }
    };

    /// <inheritdoc />
    public bool CanFastReadPrimitives { get; }

    /// <inheritdoc />
    public bool TryGetKnownType(uint knownTypeId, out Type knownType) => KnownTypes.TryGetValue(knownTypeId, out knownType);

    /// <inheritdoc />
    public bool TryGetKnownTypeId(Type knownType, out uint knownTypeId) => KnownTypeIds.TryGetValue(knownType, out knownTypeId);

    /// <inheritdoc />
    public void SaveKnownType(uint knownTypeId, Type knownType) => KnownTypes[knownTypeId] = knownType;

    /// <summary>
    /// Create a serializer and register custom parsers.
    /// </summary>
    /// <exception cref="ArgumentNullException">The key of a pair is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key was added more than once.</exception>
    public DefaultSerializer(Action<IDictionary<Type, IBinaryTypeParser>> registerParsers)
    {
        IDictionary<Type, IBinaryTypeParser> dict = new Dictionary<Type, IBinaryTypeParser>();
        registerParsers(dict);
        _parsers = new Dictionary<Type, IBinaryTypeParser>(dict);
        CanFastReadPrimitives = !IntlAnyCustomPrimitiveParsers();
    }

    /// <summary>
    /// Create a serializer with custom parsers.
    /// </summary>
    /// <exception cref="ArgumentNullException">The key of a pair is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key was added more than once.</exception>
    public DefaultSerializer(IEnumerable<KeyValuePair<Type, IBinaryTypeParser>> parsers)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        _parsers = new Dictionary<Type, IBinaryTypeParser>(parsers);
        CanFastReadPrimitives = !IntlAnyCustomPrimitiveParsers();
#else
        _parsers = new Dictionary<Type, IBinaryTypeParser>(parsers is ICollection c ? c.Count : 4);
        CanFastReadPrimitives = true;
        foreach (KeyValuePair<Type, IBinaryTypeParser> parsePair in parsers)
        {
            _parsers.Add(parsePair.Key, parsePair.Value);
            if (_primitiveParsers.TryGetValue(parsePair.Key, out IBinaryTypeParser defaultParser) && !defaultParser.IsVariableSize)
                CanFastReadPrimitives = false;
        }
#endif
    }

    private bool IntlAnyCustomPrimitiveParsers()
    {
        foreach (KeyValuePair<Type, IBinaryTypeParser> parser in _parsers)
        {
            if (_primitiveParsers.TryGetValue(parser.Key, out IBinaryTypeParser? defaultParser) && !defaultParser.IsVariableSize)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Create a serializer.
    /// </summary>
    public DefaultSerializer()
    {
        _parsers = new Dictionary<Type, IBinaryTypeParser>();
        CanFastReadPrimitives = true;
    }
    private static void ThrowNoParserFound(Type type)
    {
        throw new RpcInvalidParameterException(
            string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoParamInfo,
                Accessor.ExceptionFormatter.Format(type),
                Properties.Exceptions.RpcInvalidParameterExceptionNoParserFound)
        );
    }
    public int GetMinimumSize(Type type) => GetMinimumSize(type, out _);
    public int GetMinimumSize(Type type, out bool isFixedSize)
    {
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            isFixedSize = !parser.IsVariableSize;
            return parser.MinimumSize;
        }

        if (_primitiveSizes.TryGetValue(type, out int size))
        {
            isFixedSize = true;
            return size;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            isFixedSize = !parser.IsVariableSize;
            return parser.MinimumSize;
        }

        ThrowNoParserFound(type);
        isFixedSize = false;
        return 0; // not reached
    }
    public int GetSize<T>(T value)
    {
        Type type = typeof(T);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (!parser.IsVariableSize)
                return parser.MinimumSize;

            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.GetSize(value);

            return parser.GetSize(__makeref(value));
        }

        if (_primitiveSizes.TryGetValue(type, out int size))
            return size;

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (!parser.IsVariableSize)
                return parser.MinimumSize;

            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.GetSize(value);

            return parser.GetSize(__makeref(value));
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    public int GetSize(object value)
    {
        Type origType = value.GetType();
        if (origType.IsValueType || origType == typeof(string))
        {
            if (_parsers.TryGetValue(origType, out IBinaryTypeParser? parser))
            {
                return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
            }

            if (_primitiveSizes.TryGetValue(origType, out int size))
                return size;

            if (_primitiveParsers.TryGetValue(origType, out parser))
            {
                return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
            }
        }
        else
        {
            for (Type? type = origType; type != typeof(object) && type != null; type = type.BaseType)
            {
                if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
                {
                    return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
                }

                if (_primitiveSizes.TryGetValue(type, out int size))
                    return size;

                if (_primitiveParsers.TryGetValue(type, out parser))
                {
                    return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
                }
            }
        }

        ThrowNoParserFound(origType);
        return 0; // not reached
    }
    public int GetSize(TypedReference value)
    {
        Type type = __reftype(value);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
        }

        if (_primitiveSizes.TryGetValue(type, out int size))
            return size;

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    public unsafe int WriteObject<T>(T value, byte* bytes, uint maxSize)
    {
        Type type = typeof(T);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, bytes, maxSize);

            return parser.WriteObject(__makeref(value), bytes, maxSize);
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, bytes, maxSize);

            return parser.WriteObject(__makeref(value), bytes, maxSize);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    public unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        Type type = __reftype(value);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, bytes, maxSize);

        if (_primitiveParsers.TryGetValue(type, out parser))
            return parser.WriteObject(value, bytes, maxSize);

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    public unsafe int WriteObject(object value, byte* bytes, uint maxSize)
    {
        Type origType = value.GetType();
        if (origType.IsValueType || origType == typeof(string))
        {
            if (_parsers.TryGetValue(origType, out IBinaryTypeParser? parser))
                return parser.WriteObject(value, bytes, maxSize);

            if (_primitiveParsers.TryGetValue(origType, out parser))
                return parser.WriteObject(value, bytes, maxSize);
        }
        else
        {
            for (Type? type = origType; type != typeof(object) && type != null; type = type.BaseType)
            {
                if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
                    return parser.WriteObject(value, bytes, maxSize);

                if (_primitiveParsers.TryGetValue(type, out parser))
                    return parser.WriteObject(value, bytes, maxSize);
            }
        }

        ThrowNoParserFound(origType);
        return 0; // not reached
    }
    public int WriteObject<T>(T value, Stream stream)
    {
        Type type = typeof(T);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, stream);

            return parser.WriteObject(__makeref(value), stream);
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, stream);

            return parser.WriteObject(__makeref(value), stream);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    public int WriteObject(TypedReference value, Stream stream)
    {
        Type type = __reftype(value);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, stream);

        if (_primitiveParsers.TryGetValue(type, out parser))
            return parser.WriteObject(value, stream);

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    public int WriteObject(object value, Stream stream)
    {
        Type origType = value.GetType();
        if (origType.IsValueType || origType == typeof(string))
        {
            if (_parsers.TryGetValue(origType, out IBinaryTypeParser? parser))
                return parser.WriteObject(value, stream);

            if (_primitiveParsers.TryGetValue(origType, out parser))
                return parser.WriteObject(value, stream);
        }
        else
        {
            for (Type? type = origType; type != typeof(object) && type != null; type = type.BaseType)
            {
                if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
                    return parser.WriteObject(value, stream);

                if (_primitiveParsers.TryGetValue(type, out parser))
                    return parser.WriteObject(value, stream);
            }
        }

        ThrowNoParserFound(origType);
        return 0; // not reached
    }
    public unsafe T ReadObject<T>(byte* bytes, uint maxSize, out int bytesRead)
    {
        Type type = typeof(T);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(bytes, maxSize, out bytesRead);

            T? value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            return value!;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(bytes, maxSize, out bytesRead);

            T? value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            return value!;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
        return default!;
    }
    public unsafe void ReadObject(TypedReference refValue, byte* bytes, uint maxSize, out int bytesRead)
    {
        Type type = __reftype(refValue);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            parser.ReadObject(bytes, maxSize, out bytesRead, refValue);
            return;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            parser.ReadObject(bytes, maxSize, out bytesRead, refValue);
            return;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
    }
    public unsafe object ReadObject(Type objectType, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (_parsers.TryGetValue(objectType, out IBinaryTypeParser? parser))
        {
            return parser.ReadObject(bytes, maxSize, out bytesRead);
        }

        if (_primitiveParsers.TryGetValue(objectType, out parser))
        {
            return parser.ReadObject(bytes, maxSize, out bytesRead);
        }

        ThrowNoParserFound(objectType);
        bytesRead = 0; // not reached
        return default!;
    }
    public T ReadObject<T>(Stream stream, out int bytesRead)
    {
        Type type = typeof(T);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(stream, out bytesRead);

            T? value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            return value!;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(stream, out bytesRead);

            T? value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            return value!;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
        return default!;
    }
    public void ReadObject(TypedReference refValue, Stream stream, out int bytesRead)
    {
        Type type = __reftype(refValue);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            parser.ReadObject(stream, out bytesRead, refValue);
            return;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            parser.ReadObject(stream, out bytesRead, refValue);
            return;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
    }
    public object ReadObject(Type objectType, Stream stream, out int bytesRead)
    {
        if (_parsers.TryGetValue(objectType, out IBinaryTypeParser? parser))
        {
            return parser.ReadObject(stream, out bytesRead);
        }

        if (_primitiveParsers.TryGetValue(objectType, out parser))
        {
            return parser.ReadObject(stream, out bytesRead);
        }

        ThrowNoParserFound(objectType);
        bytesRead = 0; // not reached
        return default!;
    }
}
