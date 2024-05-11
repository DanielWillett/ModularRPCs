using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using DanielWillett.ReflectionTools;
using System;
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
        { typeof(float), new SingleParser() },
        { typeof(double), new DoubleParser() },
        { typeof(nuint), new UIntPtrParser() },
        { typeof(nint), new IntPtrParser() },
        { typeof(string), new Utf8Parser() }
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
        { typeof(float), sizeof(float) },
        { typeof(double), sizeof(double) },
        { typeof(nuint), sizeof(ulong) }, // native ints are always read/written in 64 bit
        { typeof(nint), sizeof(long) }
    };

    /// <inheritdoc />
    public bool PreCalculatePrimitiveSizes { get; }

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
        PreCalculatePrimitiveSizes = !IntlAnyCustomPrimitiveParsers();
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
        PreCaluclatePrimitiveSizes = !IntlAnyCustomPrimitiveParsers();
#else
        _parsers = new Dictionary<Type, IBinaryTypeParser>(parsers is ICollection c ? c.Count : 4);
        PreCalculatePrimitiveSizes = true;
        foreach (KeyValuePair<Type, IBinaryTypeParser> parsePair in parsers)
        {
            _parsers.Add(parsePair.Key, parsePair.Value);
            if (_primitiveParsers.TryGetValue(parsePair.Key, out IBinaryTypeParser defaultParser) && !defaultParser.IsVariableSize)
                PreCalculatePrimitiveSizes = false;
        }
#endif
    }

    private bool IntlAnyCustomPrimitiveParsers()
    {
        foreach (KeyValuePair<Type, IBinaryTypeParser> parser in _parsers)
        {
            if (_primitiveParsers.TryGetValue(parser.Key, out IBinaryTypeParser defaultParser) && !defaultParser.IsVariableSize)
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
        PreCalculatePrimitiveSizes = true;
    }
    private static void ThrowNoParserFound(Type type)
    {
        throw new RpcInvalidParameterException(
            string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoParamInfo,
                Accessor.ExceptionFormatter.Format(type),
                Properties.Exceptions.RpcInvalidParameterExceptionNoParserFound)
        );
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

    public unsafe void WriteObject<T>(T value, byte* bytes, uint maxSize)
    {
        throw new NotImplementedException();
    }

    public unsafe void WriteObject(object value, byte* bytes, uint maxSize)
    {
        throw new NotImplementedException();
    }

    public void WriteObject<T>(T value, Stream stream)
    {
        throw new NotImplementedException();
    }

    public void WriteObject(object value, Stream stream)
    {
        throw new NotImplementedException();
    }

    public unsafe T ReadObject<T>(byte* bytes, uint maxSize, out int bytesRead) => throw new NotImplementedException();

    public unsafe object ReadObject(Type objectType, byte* bytes, uint maxSize, out int bytesRead) => throw new NotImplementedException();

    public T ReadObject<T>(Stream stream, out int bytesRead) => throw new NotImplementedException();

    public object ReadObject(Type objectType, Stream stream, out int bytesRead) => throw new NotImplementedException();
}
