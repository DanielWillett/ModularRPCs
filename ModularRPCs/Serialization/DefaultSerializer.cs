using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Buffers;
using System.Text;
using DanielWillett.ModularRpcs.Configuration;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_0_OR_GREATER
using System.Collections;
#endif

namespace DanielWillett.ModularRpcs.Serialization;
public class DefaultSerializer : IRpcSerializer
{
    internal const int MaxArrayPoolSize = 19;
    internal const int MaxBufferSize = 4096;
    internal static ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create(MaxArrayPoolSize, 6);
    protected readonly ConcurrentDictionary<uint, Type> KnownTypes = new ConcurrentDictionary<uint, Type>();
    protected readonly ConcurrentDictionary<Type, uint> KnownTypeIds = new ConcurrentDictionary<Type, uint>();
    private readonly ConcurrentDictionary<Type, InstanceGetter<object, bool>> _getNullableHasValueByBox = new ConcurrentDictionary<Type, InstanceGetter<object, bool>>();
    private readonly ConcurrentDictionary<Type, InstanceGetter<object, object>> _getNullableValueByBox = new ConcurrentDictionary<Type, InstanceGetter<object, object>>();
    private readonly ConcurrentDictionary<Type, NullableHasValueTypeRef> _getNullableHasValueByRefAny = new ConcurrentDictionary<Type, NullableHasValueTypeRef>();
    private readonly ConcurrentDictionary<Type, NullableValueTypeRef> _getNullableValueByRefAny = new ConcurrentDictionary<Type, NullableValueTypeRef>();
    private readonly ConcurrentDictionary<Type, object> _nullableDefaults = new ConcurrentDictionary<Type, object>();
    private readonly ConcurrentDictionary<Type, ReadNullableBytes> _nullableReadBytes = new ConcurrentDictionary<Type, ReadNullableBytes>();
    private readonly ConcurrentDictionary<Type, ReadNullableStream> _nullableReadStream = new ConcurrentDictionary<Type, ReadNullableStream>();
    private readonly ConcurrentDictionary<Type, ReadNullableBytesRefAny> _nullableReadBytesRefAny = new ConcurrentDictionary<Type, ReadNullableBytesRefAny>();
    private readonly ConcurrentDictionary<Type, ReadNullableStreamRefAny> _nullableReadStreamRefAny = new ConcurrentDictionary<Type, ReadNullableStreamRefAny>();
    private readonly SerializationConfiguration _config;
    private delegate bool NullableHasValueTypeRef(TypedReference value);
    private delegate object NullableValueTypeRef(TypedReference value);
    private unsafe delegate object ReadNullableBytes(byte* bytes, uint maxSize, out int bytesRead);
    private delegate object ReadNullableStream(Stream stream, out int bytesRead);
    private unsafe delegate void ReadNullableBytesRefAny(TypedReference value, byte* bytes, uint maxSize, out int bytesRead);
    private delegate void ReadNullableStreamRefAny(TypedReference value, Stream stream, out int bytesRead);
    private readonly Dictionary<Type, IBinaryTypeParser> _parsers;
    private static readonly MethodInfo MtdReadBoxedNullableBytes = typeof(DefaultSerializer).GetMethod(nameof(ReadBoxedNullableBytes), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                   ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ReadBoxedNullableBytes))
                                                                       .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                       .WithGenericParameterDefinition("T")
                                                                       .WithParameter(typeof(byte*), "bytes")
                                                                       .WithParameter<uint>("maxSize")
                                                                       .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                       .Returning<object>()
                                                                   );
    private static readonly MethodInfo MtdReadBoxedNullableStream = typeof(DefaultSerializer).GetMethod(nameof(ReadBoxedNullableStream), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                    ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ReadBoxedNullableStream))
                                                                        .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                        .WithGenericParameterDefinition("T")
                                                                        .WithParameter<Stream>("stream")
                                                                        .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                        .Returning<object>()
                                                                    );
    private static readonly MethodInfo MtdReadBoxedNullableBytesRefAny = typeof(DefaultSerializer).GetMethod(nameof(ReadBoxedNullableBytesRefAny), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                         ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ReadBoxedNullableBytesRefAny))
                                                                             .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                             .WithGenericParameterDefinition("T")
                                                                             .WithParameter(typeof(TypedReference), "value")
                                                                             .WithParameter(typeof(byte*), "bytes")
                                                                             .WithParameter<uint>("maxSize")
                                                                             .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                             .Returning<object>()
                                                                         );
    private static readonly MethodInfo MtdReadBoxedNullableStreamRefAny = typeof(DefaultSerializer).GetMethod(nameof(ReadBoxedNullableStreamRefAny), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                          ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ReadBoxedNullableStreamRefAny))
                                                                              .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                              .WithGenericParameterDefinition("T")
                                                                              .WithParameter(typeof(TypedReference), "value")
                                                                              .WithParameter<Stream>("stream")
                                                                              .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                              .Returning<object>()
                                                                          );

    private readonly ConcurrentDictionary<Type, IBinaryTypeParser?> _discoveredInParsers = new ConcurrentDictionary<Type, IBinaryTypeParser?>();
    private readonly ConcurrentDictionary<Type, IBinaryTypeParser?> _discoveredOutParsers = new ConcurrentDictionary<Type, IBinaryTypeParser?>();
    private readonly List<IBinaryParserFactory> _parserFactories;
    private readonly Dictionary<Type, IBinaryTypeParser> _primitiveParsers = new Dictionary<Type, IBinaryTypeParser>(117);

    private readonly Dictionary<Type, int> _primitiveSizes = new Dictionary<Type, int>(20)
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
    public SerializationConfiguration Configuration => _config;
    
    /// <inheritdoc />
    public bool TryGetKnownType(uint knownTypeId,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]
#endif
            out Type knownType) => KnownTypes.TryGetValue(knownTypeId, out knownType);

    /// <inheritdoc />
    public bool TryGetKnownTypeId(Type knownType, out uint knownTypeId) => KnownTypeIds.TryGetValue(knownType, out knownTypeId);

    /// <inheritdoc />
    public void SaveKnownType(uint knownTypeId, Type knownType) => KnownTypes[knownTypeId] = knownType;

    private void AddDefaultNonPrimitiveSerializers()
    {
        _primitiveParsers.AddManySerializer(new BooleanParser.Many(_config));
        _primitiveParsers.AddManySerializer(new CharParser.Many(_config));
        _primitiveParsers.AddManySerializer(new UIntPtrParser.Many(_config));
        _primitiveParsers.AddManySerializer(new UInt8Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new UInt16Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new UInt32Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new UInt64Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new Int8Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new IntPtrParser.Many(_config));
        _primitiveParsers.AddManySerializer(new Int16Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new Int32Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new Int64Parser.Many(_config));
        _primitiveParsers.AddManySerializer(new SingleParser.Many(_config));
        _primitiveParsers.AddManySerializer(new DoubleParser.Many(_config));

        _primitiveParsers.AddManySerializer(new DecimalParser.Many(_config));
        _primitiveParsers.AddManySerializer(Encoding.UTF8.Equals(_config.StringEncoding) ? new Utf8Parser.Many(_config) : new StringParser.Many(_config, _config.StringEncoding));
        _primitiveParsers.AddManySerializer(new Utf8Parser.Many(_config));
#if NET5_0_OR_GREATER
        _primitiveParsers.AddManySerializer(new HalfParser.Many(_config));
#endif
        _primitiveParsers.AddManySerializer(new GuidParser.Many(_config));
        _primitiveParsers.AddManySerializer(new TimeSpanParser.Many(_config));
        _primitiveParsers.AddManySerializer(new DateTimeParser.Many(_config));
        _primitiveParsers.AddManySerializer(new DateTimeOffsetParser.Many(_config));
    }
    private void SetupPrimitiveParsers()
    {
        _primitiveParsers.Add(typeof(byte), new UInt8Parser());
        _primitiveParsers.Add(typeof(sbyte), new Int8Parser());
        _primitiveParsers.Add(typeof(bool), new BooleanParser());
        _primitiveParsers.Add(typeof(ushort), new UInt16Parser());
        _primitiveParsers.Add(typeof(short), new Int16Parser());
        _primitiveParsers.Add(typeof(char), new CharParser());
        _primitiveParsers.Add(typeof(uint), new UInt32Parser());
        _primitiveParsers.Add(typeof(int), new Int32Parser());
        _primitiveParsers.Add(typeof(ulong), new UInt64Parser());
        _primitiveParsers.Add(typeof(long), new Int64Parser());
        _primitiveParsers.Add(typeof(nuint), new UIntPtrParser());
        _primitiveParsers.Add(typeof(nint), new IntPtrParser());
        _primitiveParsers.Add(typeof(string), Encoding.UTF8.Equals(_config.StringEncoding) ? new Utf8Parser(_config) : new StringParser(_config, _config.StringEncoding));
#if NET5_0_OR_GREATER
        _primitiveParsers.Add(typeof(Half), new HalfParser());
#endif
        _primitiveParsers.Add(typeof(float), new SingleParser());
        _primitiveParsers.Add(typeof(double), new DoubleParser());
        _primitiveParsers.Add(typeof(decimal), new DecimalParser());
        _primitiveParsers.Add(typeof(DateTime), new DateTimeParser());
        _primitiveParsers.Add(typeof(DateTimeOffset), new DateTimeOffsetParser());
        _primitiveParsers.Add(typeof(TimeSpan), new TimeSpanParser());
        _primitiveParsers.Add(typeof(Guid), new GuidParser());
    }

    /// <summary>
    /// Create a serializer and register custom parsers.
    /// </summary>
    /// <exception cref="ArgumentNullException">The key of a pair is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key was added more than once.</exception>
    public DefaultSerializer(Action<SerializationConfiguration, IDictionary<Type, IBinaryTypeParser>, IList<IBinaryParserFactory>> registerParsers, SerializationConfiguration? configuration = null)
    {
        _config = configuration ?? new SerializationConfiguration();
        IDictionary<Type, IBinaryTypeParser> dict = new Dictionary<Type, IBinaryTypeParser>();
        IList<IBinaryParserFactory> factories = new List<IBinaryParserFactory>();
        registerParsers(_config, dict, factories);
        _config.Lock();
        _parsers = new Dictionary<Type, IBinaryTypeParser>(dict);
        _parserFactories = new List<IBinaryParserFactory>(factories);
        SetupPrimitiveParsers();
        CanFastReadPrimitives = !IntlAnyCustomPrimitiveParsers();
        AddDefaultNonPrimitiveSerializers();
    }

    /// <summary>
    /// Create a serializer with custom parsers.
    /// </summary>
    /// <exception cref="ArgumentNullException">The key of a pair is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key was added more than once.</exception>
    public DefaultSerializer(IEnumerable<KeyValuePair<Type, IBinaryTypeParser>> parsers, IEnumerable<IBinaryParserFactory> parserFactories, SerializationConfiguration? configuration = null)
    {
        _config = configuration ?? new SerializationConfiguration();
        _config.Lock();
        SetupPrimitiveParsers();
        _parserFactories = new List<IBinaryParserFactory>(parserFactories);
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
        AddDefaultNonPrimitiveSerializers();
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
    public DefaultSerializer(SerializationConfiguration? configuration = null)
    {
        _config = configuration ?? new SerializationConfiguration();
        _config.Lock();
        _parsers = new Dictionary<Type, IBinaryTypeParser>();
        _parserFactories = new List<IBinaryParserFactory>();
        SetupPrimitiveParsers();
        CanFastReadPrimitives = true;
        AddDefaultNonPrimitiveSerializers();
    }
    
    /// <summary>
    /// Find a parser that could be assigned but aren't stored as the exact type, or from existing factories.
    /// </summary>
    /// <param name="isIn">Would this be treated as an 'in' (contravariant) parameter, or an 'out' (covariant) parameter.</param>
    private IBinaryTypeParser? LookForParser(bool isIn, Type type)
    {
        if (type.IsValueType || type == typeof(string))
            return null;
        ConcurrentDictionary<Type, IBinaryTypeParser?> dict = isIn ? _discoveredInParsers : _discoveredOutParsers;
        IBinaryTypeParser? parser = dict.GetOrAdd(type, isIn ? LookForInParserFactory : LookForOutParserFactory);
        if (parser != null)
            return parser;

        for (int i = 0; i < _parserFactories.Count; ++i)
        {
            if (!_parserFactories[i].TryGetParser(type, _config, isIn, out parser, out bool canCacheParser))
                continue;

            if (!canCacheParser || parser == null)
                return parser;

            IBinaryTypeParser? existingParser;
            while ((existingParser = dict.GetOrAdd(type, parser)) == null)
            {
                dict[type] = parser;
            }

            return existingParser;
        }

        return null;
    }

    private IBinaryTypeParser? LookForInParserFactory(Type arg)
    {
        foreach (KeyValuePair<Type, IBinaryTypeParser> parserSet in _parsers)
        {
            if (parserSet.Key.IsAssignableFrom(arg))
            {
                return parserSet.Value;
            }
        }
        foreach (KeyValuePair<Type, IBinaryTypeParser> parserSet in _primitiveParsers)
        {
            if (parserSet.Key.IsAssignableFrom(arg))
            {
                return parserSet.Value;
            }
        }

        return null;
    }
    private IBinaryTypeParser? LookForOutParserFactory(Type arg)
    {
        foreach (KeyValuePair<Type, IBinaryTypeParser> parserSet in _parsers)
        {
            if (arg.IsAssignableFrom(parserSet.Key))
            {
                return parserSet.Value;
            }
        }
        foreach (KeyValuePair<Type, IBinaryTypeParser> parserSet in _primitiveParsers)
        {
            if (arg.IsAssignableFrom(parserSet.Key))
            {
                return parserSet.Value;
            }
        }

        return null;
    }

    private static void ThrowNoParserFound(Type type)
    {
        throw new RpcInvalidParameterException(
            string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoParamInfo,
                Accessor.ExceptionFormatter.Format(type),
                Properties.Exceptions.RpcInvalidParameterExceptionNoParserFound)
        );
    }
    private static void ThrowNoNullableParserFound(Type type)
    {
        throw new RpcInvalidParameterException(
            string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoNullableParamInfo,
                Accessor.ExceptionFormatter.Format(type),
                Properties.Exceptions.RpcInvalidParameterExceptionNoParserFound)
        );
    }

    /// <inheritdoc />
    public int GetMinimumSize(Type type) => GetMinimumSize(type, out _);

    /// <inheritdoc />
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

        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            isFixedSize = false;
            if (!_parsers.ContainsKey(nullableType) && !_primitiveSizes.ContainsKey(nullableType) && !_primitiveParsers.ContainsKey(nullableType))
                ThrowNoParserFound(type);
            return 1;
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            isFixedSize = !parser.IsVariableSize;
            return parser.MinimumSize;
        }

        ThrowNoParserFound(type);
        isFixedSize = false;
        return 0; // not reached
    }

    /// <inheritdoc />
    public int GetSize<T>(in T? value) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (!parser.IsVariableSize)
                return parser.MinimumSize + 1;

            if (parser is IBinaryTypeParser<T?> genParser)
                return genParser.GetSize(value) + 1;

            return parser.GetSize(__makeref(Unsafe.AsRef(in value))) + 1;
        }

        if (!value.HasValue)
        {
            return 1;
        }

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            if (!parser.IsVariableSize)
                return parser.MinimumSize + 1;

            T v = value.Value;
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.GetSize(v) + 1;

            return parser.GetSize(__makeref(v)) + 1;
        }

        if (_primitiveSizes.TryGetValue(type, out int size))
            return size + 1;

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (!parser.IsVariableSize)
                return parser.MinimumSize + 1;

            T v = value.Value;
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.GetSize(v) + 1;

            return parser.GetSize(__makeref(v)) + 1;
        }

        ThrowNoParserFound(type);
        return 0;
    }

    /// <inheritdoc />
    public int GetSize<T>(T? value)
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

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GetNullableSize(value!, underlyingType, type);
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.GetSize(value);

            return parser.GetSize(value);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public int GetSize(object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return GetSize(value.GetType(), value);
    }

    /// <inheritdoc />
    public int GetSize(Type valueType, object? value)
    {
        if (_parsers.TryGetValue(valueType, out IBinaryTypeParser? parser))
        {
            return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
        }

        if (_primitiveSizes.TryGetValue(valueType, out int size))
            return size;

        if (_primitiveParsers.TryGetValue(valueType, out parser))
        {
            return !parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value);
        }

        Type? nullableType = Nullable.GetUnderlyingType(valueType);
        if (nullableType != null)
        {
            return GetNullableSize(value, nullableType, valueType);
        }

        parser = LookForParser(true, valueType);
        if (parser != null)
        {
            return parser.GetSize(value);
        }

        ThrowNoParserFound(valueType);
        return 0; // not reached
    }

    /// <inheritdoc />
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

        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            return GetNullableSize(value, nullableType);
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            return parser.GetSize(value);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }
    private int GetNullableSize(object? value, Type underlyingType, Type type)
    {
        if (value == null || !GetNullableHasValue(value, type))
            return 1;

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            return (!parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(GetNullableValue(value, type))) + 1;
        }

        if (_primitiveSizes.TryGetValue(underlyingType, out int size))
            return size + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            return (!parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(GetNullableValue(value, type))) + 1;
        }

        ThrowNoNullableParserFound(underlyingType);
        return 0; // not reached
    }
    private int GetNullableSize(TypedReference value, Type underlyingType)
    {
        if (!GetNullableHasValue(value))
            return 1;

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            return (!parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(GetNullableValue(value))) + 1;
        }

        if (_primitiveSizes.TryGetValue(underlyingType, out int size))
            return size + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            return (!parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(GetNullableValue(value))) + 1;
        }

        ThrowNoNullableParserFound(underlyingType);
        return 0; // not reached
    }
    protected bool GetNullableHasValue(object value, Type nullableType)
    {
        InstanceGetter<object, bool> getter = _getNullableHasValueByBox.GetOrAdd(nullableType, CreateNullableHasValueGetterByBox);
        return getter(value);
    }

    protected object GetNullableValue(object value, Type nullableType)
    {
        InstanceGetter<object, object> getter = _getNullableValueByBox.GetOrAdd(nullableType, CreateNullableValueGetterByBox);
        return getter(value);
    }

    protected bool GetNullableHasValue(TypedReference value)
    {
        NullableHasValueTypeRef getter = _getNullableHasValueByRefAny.GetOrAdd(__reftype(value), CreateNullableHasValueGetterByRefAny);
        return getter(value);
    }

    protected object GetNullableValue(TypedReference value)
    {
        NullableValueTypeRef getter = _getNullableValueByRefAny.GetOrAdd(__reftype(value), CreateNullableValueGetterByRefAny);
        return getter(value);
    }

    private InstanceGetter<object, bool> CreateNullableHasValueGetterByBox(Type nullableType)
    {
        MethodInfo getter = nullableType.GetProperty(nameof(Nullable<int>.HasValue), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                            ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.HasValue))
                                .DeclaredIn(nullableType, isStatic: false)
                                .WithPropertyType<bool>()
                                .WithNoSetter()
                            );

        return Accessor.GenerateInstanceCaller<InstanceGetter<object, bool>>(getter, true, allowUnsafeTypeBinding: true)!;
    }
    private InstanceGetter<object, object> CreateNullableValueGetterByBox(Type nullableType)
    {
        MethodInfo getter = nullableType.GetProperty(nameof(Nullable<int>.Value), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                            ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.Value))
                                .DeclaredIn(nullableType, isStatic: false)
                                .WithPropertyType(Nullable.GetUnderlyingType(nullableType)!)
                                .WithNoSetter()
                            );

        return Accessor.GenerateInstanceCaller<InstanceGetter<object, object>>(getter, true, allowUnsafeTypeBinding: true)!;
    }
    private NullableHasValueTypeRef CreateNullableHasValueGetterByRefAny(Type nullableType)
    {
        MethodInfo getter = nullableType.GetProperty(nameof(Nullable<int>.HasValue), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                            ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.HasValue))
                                .DeclaredIn(nullableType, isStatic: false)
                                .WithPropertyType<bool>()
                                .WithNoSetter()
                            );

        return Accessor.GenerateInstanceCaller<NullableHasValueTypeRef>(getter, true, allowUnsafeTypeBinding: true)!;
    }
    private NullableValueTypeRef CreateNullableValueGetterByRefAny(Type nullableType)
    {
        MethodInfo getter = nullableType.GetProperty(nameof(Nullable<int>.Value), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                            ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.Value))
                                .DeclaredIn(nullableType, isStatic: false)
                                .WithPropertyType(Nullable.GetUnderlyingType(nullableType)!)
                                .WithNoSetter()
                            );

        return Accessor.GenerateInstanceCaller<NullableValueTypeRef>(getter, true, allowUnsafeTypeBinding: true)!;
    }


    /// <inheritdoc />
    public unsafe int WriteObject<T>(in T? value, byte* bytes, uint maxSize) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
                return genParser.WriteObject(value, bytes + 1, maxSize - 1) + 1;

            return parser.WriteObject(__makeref(Unsafe.AsRef(in value)), bytes + 1, maxSize - 1) + 1;
        }

        if (maxSize < 1)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        if (!value.HasValue)
        {
            bytes[0] = 0;
            return 1;
        }

        *bytes = 1;

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            T v = value.Value;
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, bytes + 1, maxSize - 1) + 1;

            return parser.WriteObject(__makeref(v), bytes + 1, maxSize - 1) + 1;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            T v = value.Value;
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, bytes + 1, maxSize - 1) + 1;

            return parser.WriteObject(__makeref(v), bytes + 1, maxSize - 1) + 1;
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe int WriteObject<T>(T? value, byte* bytes, uint maxSize)
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

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return WriteNullable(value!, bytes, maxSize, underlyingType, type);
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, bytes, maxSize);

            return parser.WriteObject(value, bytes, maxSize);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        Type type = __reftype(value);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, bytes, maxSize);

        if (_primitiveParsers.TryGetValue(type, out parser))
            return parser.WriteObject(value, bytes, maxSize);

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return WriteNullable(value, bytes, maxSize, underlyingType);
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            return parser.WriteObject(value, bytes, maxSize);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe int WriteObject(object value, byte* bytes, uint maxSize)
    {
        if (value == null)
            throw new ArgumentNullException();

        return WriteObject(value.GetType(), value, bytes, maxSize);
    }

    /// <inheritdoc />
    public unsafe int WriteObject(Type valueType, object? value, byte* bytes, uint maxSize)
    {
        if (_parsers.TryGetValue(valueType, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, bytes, maxSize);

        if (_primitiveParsers.TryGetValue(valueType, out parser))
            return parser.WriteObject(value, bytes, maxSize);

        Type? underlyingType = Nullable.GetUnderlyingType(valueType);
        if (underlyingType != null)
        {
            return WriteNullable(value, bytes, maxSize, underlyingType, valueType);
        }

        parser = LookForParser(true, valueType);
        if (parser != null)
        {
            return parser.WriteObject(value, bytes, maxSize);
        }

        ThrowNoParserFound(valueType);
        return 0; // not reached
    }

    /// <inheritdoc />
    public int WriteObject<T>(in T? value, Stream stream) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
                return genParser.WriteObject(value, stream);

            return parser.WriteObject(__makeref(Unsafe.AsRef(in value)), stream);
        }

        if (!value.HasValue)
        {
            stream.WriteByte(0);
            return 1;
        }

        stream.WriteByte(1);

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            T v = value.Value;

            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, stream);

            return parser.WriteObject(__makeref(v), stream);
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            T v = value.Value;

            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, stream);

            return parser.WriteObject(__makeref(v), stream);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public int WriteObject<T>(T? value, Stream stream)
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

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return WriteNullable(value!, stream, underlyingType, type);
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, stream);

            return parser.WriteObject(value, stream);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public int WriteObject(TypedReference value, Stream stream)
    {
        Type type = __reftype(value);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, stream);

        if (_primitiveParsers.TryGetValue(type, out parser))
            return parser.WriteObject(value, stream);

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return WriteNullable(value, stream, underlyingType);
        }

        parser = LookForParser(true, type);
        if (parser != null)
        {
            return parser.WriteObject(value, stream);
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public int WriteObject(object value, Stream stream)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return WriteObject(value.GetType(), value, stream);
    }

    /// <inheritdoc />
    public int WriteObject(Type valueType, object? value, Stream stream)
    {
        if (_parsers.TryGetValue(valueType, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, stream);

        if (_primitiveParsers.TryGetValue(valueType, out parser))
            return parser.WriteObject(value, stream);

        Type? underlyingType = Nullable.GetUnderlyingType(valueType);
        if (underlyingType != null)
        {
            return WriteNullable(value, stream, underlyingType, valueType);
        }

        parser = LookForParser(true, valueType);
        if (parser != null)
        {
            return parser.WriteObject(value, stream);
        }

        ThrowNoParserFound(valueType);
        return 0; // not reached
    }
    private unsafe int WriteNullable(object? value, byte* bytes, uint maxSize, Type underlyingType, Type nullableType)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        if (value == null || !GetNullableHasValue(value, nullableType))
        {
            bytes[0] = 0;
            return 1;
        }

        bytes[1] = 1;

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, bytes + 1, maxSize - 1) + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
            return parser.WriteObject(value, bytes + 1, maxSize - 1) + 1;

        ThrowNoParserFound(underlyingType);
        return 0; // not reached
    }
    private unsafe int WriteNullable(TypedReference value, byte* bytes, uint maxSize, Type underlyingType)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        if (!GetNullableHasValue(value))
        {
            bytes[0] = 0;
            return 1;
        }

        bytes[1] = 1;

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
            return parser.WriteObject(GetNullableValue(value), bytes + 1, maxSize - 1) + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
            return parser.WriteObject(GetNullableValue(value), bytes + 1, maxSize - 1) + 1;

        ThrowNoParserFound(underlyingType);
        return 0; // not reached
    }
    private int WriteNullable(object? value, Stream stream, Type underlyingType, Type nullableType)
    {
        if (value == null || !GetNullableHasValue(value, nullableType))
        {
            stream.WriteByte(0);
            return 1;
        }

        stream.WriteByte(1);

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, stream) + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
            return parser.WriteObject(value, stream) + 1;

        ThrowNoParserFound(underlyingType);
        return 0; // not reached
    }
    private int WriteNullable(TypedReference value, Stream stream, Type underlyingType)
    {
        if (!GetNullableHasValue(value))
        {
            stream.WriteByte(0);
            return 1;
        }

        stream.WriteByte(1);

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
            return parser.WriteObject(GetNullableValue(value), stream) + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
            return parser.WriteObject(GetNullableValue(value), stream) + 1;

        ThrowNoParserFound(underlyingType);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe T? ReadNullable<T>(byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        if (maxSize < 1)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

        if (*bytes == 0)
        {
            bytesRead = 1;
            return default;
        }

        ++bytes;
        --maxSize;

        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
            {
                T? v = genParser.ReadObject(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return v;
            }

            T? value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            ++bytesRead;
            return value;
        }

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                T? v = genParser.ReadObject(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return v;
            }

            T value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            ++bytesRead;
            return value;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                T? v = genParser.ReadObject(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return v;
            }

            T value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            ++bytesRead;
            return value;
        }

        ThrowNoParserFound(type);
        bytesRead = 1; // not reached
        return default!;
    }

    /// <inheritdoc />
    public unsafe void ReadNullable<T>(TypedReference refOut, byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        if (maxSize < 1)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

        if (*bytes == 0)
        {
            bytesRead = 1;
            __refvalue(refOut, T?) = default;
            return;
        }

        ++bytes;
        --maxSize;

        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            parser.ReadObject(bytes, maxSize, out bytesRead, refOut);
            ++bytesRead;
            return;
        }

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                __refvalue(refOut, T?) = genParser.ReadObject(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return;
            }

            T value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            ++bytesRead;
            __refvalue(refOut, T?) = value;
            return;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                __refvalue(refOut, T?) = genParser.ReadObject(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return;
            }

            T value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            ++bytesRead;
            __refvalue(refOut, T?) = value;
            return;
        }

        ThrowNoParserFound(type);
        bytesRead = 1; // not reached
    }

    /// <inheritdoc />
    public unsafe T? ReadObject<T>(byte* bytes, uint maxSize, out int bytesRead)
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

        if (!type.IsValueType && type.BaseType != typeof(object))
        {
            foreach (KeyValuePair<Type, IBinaryTypeParser> parserPair in _parsers)
            {
                if (type.IsAssignableFrom(parserPair.Key))
                {
                    return (T?)parserPair.Value.ReadObject(parserPair.Key, bytes, maxSize, out bytesRead);
                }
            }
            foreach (KeyValuePair<Type, IBinaryTypeParser> parserPair in _primitiveParsers)
            {
                if (type.IsAssignableFrom(parserPair.Key))
                {
                    return (T?)parserPair.Value.ReadObject(parserPair.Key, bytes, maxSize, out bytesRead);
                }
            }
        }
        else
        {
            Type? underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return (T)ReadNullable(type, underlyingType, bytes, maxSize, out bytesRead);
            }
        }

        parser = LookForParser(false, type);
        if (parser != null)
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(bytes, maxSize, out bytesRead);

            return (T?)parser.ReadObject(type, bytes, maxSize, out bytesRead);
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
        return default!;
    }

    /// <inheritdoc />
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

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            ReadNullable(refValue, type, underlyingType, bytes, maxSize, out bytesRead);
            return;
        }

        parser = LookForParser(false, type);
        if (parser != null)
        {
            parser.ReadObject(bytes, maxSize, out bytesRead, refValue);
            return;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
    }

    /// <inheritdoc />
    public unsafe object? ReadObject(Type objectType, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (_parsers.TryGetValue(objectType, out IBinaryTypeParser? parser))
        {
            return parser.ReadObject(objectType, bytes, maxSize, out bytesRead);
        }

        if (_primitiveParsers.TryGetValue(objectType, out parser))
        {
            return parser.ReadObject(objectType, bytes, maxSize, out bytesRead);
        }

        Type? underlyingType = Nullable.GetUnderlyingType(objectType);
        if (underlyingType != null)
        {
            return ReadNullable(objectType, underlyingType, bytes, maxSize, out bytesRead);
        }

        parser = LookForParser(false, objectType);
        if (parser != null)
        {
            return parser.ReadObject(objectType, bytes, maxSize, out bytesRead);
        }

        ThrowNoParserFound(objectType);
        bytesRead = 0; // not reached
        return default!;
    }

    /// <inheritdoc />
    public T? ReadNullable<T>(Stream stream, out int bytesRead) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
                return genParser.ReadObject(stream, out bytesRead);

            T? value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            return value;
        }
        
        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(stream, out bytesRead);

            T value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            return value;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(stream, out bytesRead);

            T value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            return value;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
        return default!;
    }

    /// <inheritdoc />
    public void ReadNullable<T>(TypedReference refOut, Stream stream, out int bytesRead) where T : struct
    {
        if (stream.ReadByte() == 0)
        {
            bytesRead = 1;
            __refvalue(refOut, T?) = default;
            return;
        }

        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            parser.ReadObject(stream, out bytesRead, refOut);
            ++bytesRead;
            return;
        }

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                __refvalue(refOut, T?) = genParser.ReadObject(stream, out bytesRead);
                ++bytesRead;
                return;
            }

            T value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            ++bytesRead;
            __refvalue(refOut, T?) = value;
            return;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                __refvalue(refOut, T?) = genParser.ReadObject(stream, out bytesRead);
                ++bytesRead;
                return;
            }

            T value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            ++bytesRead;
            __refvalue(refOut, T?) = value;
            return;
        }

        ThrowNoParserFound(type);
        bytesRead = 1; // not reached
    }

    /// <inheritdoc />
    public T? ReadObject<T>(Stream stream, out int bytesRead)
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

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return (T)ReadNullable(type, underlyingType, stream, out bytesRead);
        }

        parser = LookForParser(false, type);
        if (parser != null)
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.ReadObject(stream, out bytesRead);

            return (T?)parser.ReadObject(type, stream, out bytesRead);
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
        return default!;
    }

    /// <inheritdoc />
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

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            ReadNullable(refValue, type, underlyingType, stream, out bytesRead);
            return;
        }

        parser = LookForParser(false, type);
        if (parser != null)
        {
            parser.ReadObject(stream, out bytesRead, refValue);
            return;
        }

        ThrowNoParserFound(type);
        bytesRead = 0; // not reached
    }

    /// <inheritdoc />
    public object? ReadObject(Type objectType, Stream stream, out int bytesRead)
    {
        if (_parsers.TryGetValue(objectType, out IBinaryTypeParser? parser))
        {
            return parser.ReadObject(objectType, stream, out bytesRead);
        }

        if (_primitiveParsers.TryGetValue(objectType, out parser))
        {
            return parser.ReadObject(objectType, stream, out bytesRead);
        }

        Type? underlyingType = Nullable.GetUnderlyingType(objectType);
        if (underlyingType != null)
        {
            return ReadNullable(objectType, underlyingType, stream, out bytesRead);
        }

        parser = LookForParser(false, objectType);
        if (parser != null)
        {
            return parser.ReadObject(objectType, stream, out bytesRead);
        }

        ThrowNoParserFound(objectType);
        bytesRead = 0; // not reached
        return default!;
    }
    protected object GetDefaultNullable(Type nullableType)
    {
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        return _nullableDefaults.GetOrAdd(nullableType, Activator.CreateInstance!);
    }
    protected unsafe object ReadNullable(Type nullableType, Type underlyingType, byte* bytes, uint maxSize, out int bytesRead)
    {
        return _nullableReadBytes.GetOrAdd(nullableType,
            _ => (ReadNullableBytes)MtdReadBoxedNullableBytes.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableBytes))
        )(bytes, maxSize, out bytesRead);
    }
    protected object ReadNullable(Type nullableType, Type underlyingType, Stream stream, out int bytesRead)
    {
        return _nullableReadStream.GetOrAdd(nullableType,
            _ => (ReadNullableStream)MtdReadBoxedNullableStream.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableStream))
        )(stream, out bytesRead);
    }
    protected unsafe void ReadNullable(TypedReference value, Type nullableType, Type underlyingType, byte* bytes, uint maxSize, out int bytesRead)
    {
        _nullableReadBytesRefAny.GetOrAdd(nullableType,
            _ => (ReadNullableBytesRefAny)MtdReadBoxedNullableBytesRefAny.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableBytes))
        )(value, bytes, maxSize, out bytesRead);
    }
    protected void ReadNullable(TypedReference value, Type nullableType, Type underlyingType, Stream stream, out int bytesRead)
    {
        _nullableReadStreamRefAny.GetOrAdd(nullableType,
            _ => (ReadNullableStreamRefAny)MtdReadBoxedNullableStreamRefAny.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableStream))
        )(value, stream, out bytesRead);
    }
    private unsafe object ReadBoxedNullableBytes<T>(byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        return ReadNullable<T>(bytes, maxSize, out bytesRead)!;
    }
    private object ReadBoxedNullableStream<T>(Stream stream, out int bytesRead) where T : struct
    {
        return ReadNullable<T>(stream, out bytesRead)!;
    }
    private unsafe void ReadBoxedNullableBytesRefAny<T>(TypedReference value, byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        ReadNullable<T>(value, bytes, maxSize, out bytesRead);
    }
    private void ReadBoxedNullableStreamRefAny<T>(TypedReference value, Stream stream, out int bytesRead) where T : struct
    {
        ReadNullable<T>(value, stream, out bytesRead);
    }
}
