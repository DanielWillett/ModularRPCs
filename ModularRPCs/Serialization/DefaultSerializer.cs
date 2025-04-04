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
using System.Collections;
using System.Globalization;
using System.Text;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Reflection;
using JetBrains.Annotations;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_0_OR_GREATER
using System.Collections;
#endif
#if !NET8_0_OR_GREATER
using DanielWillett.ModularRpcs.Data;
#endif

namespace DanielWillett.ModularRpcs.Serialization;
public class DefaultSerializer : IRpcSerializer
{
    internal const int MaxArrayPoolSize = 19;
    internal const int MaxBufferSize = 4096;
    internal static ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create(MaxArrayPoolSize, 6);
    protected readonly ConcurrentDictionary<uint, Type> KnownTypes = new ConcurrentDictionary<uint, Type>();
    protected readonly ConcurrentDictionary<Type, uint> KnownTypeIds = new ConcurrentDictionary<Type, uint>();
    protected readonly ConcurrentDictionary<Type, SerializableTypeInfo> SerializableTypeInfoCache = new ConcurrentDictionary<Type, SerializableTypeInfo>();

    // nullable/enum caches
    private readonly ConcurrentDictionary<Type, NullableHasValueTypeRef> _getNullableHasValueByRefAny = new ConcurrentDictionary<Type, NullableHasValueTypeRef>();
    private readonly ConcurrentDictionary<Type, NullableValueTypeRef> _getNullableValueByRefAny = new ConcurrentDictionary<Type, NullableValueTypeRef>();
    private readonly ConcurrentDictionary<Type, ReadNullableBytes> _nullableReadBytes = new ConcurrentDictionary<Type, ReadNullableBytes>();
    private readonly ConcurrentDictionary<Type, ReadNullableStream> _nullableReadStream = new ConcurrentDictionary<Type, ReadNullableStream>();
    private readonly ConcurrentDictionary<Type, ReadNullableBytesRefAny> _nullableReadBytesRefAny = new ConcurrentDictionary<Type, ReadNullableBytesRefAny>();
    private readonly ConcurrentDictionary<Type, ReadNullableStreamRefAny> _nullableReadStreamRefAny = new ConcurrentDictionary<Type, ReadNullableStreamRefAny>();
    private readonly ConcurrentDictionary<Type, AssignEnumByRefAny> _assignEnumRefAny = new ConcurrentDictionary<Type, AssignEnumByRefAny>();

    private readonly SerializationConfiguration _config;
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
    private static readonly MethodInfo MtdAssignEnumRefAny = typeof(DefaultSerializer).GetMethod(nameof(AssignEnumRefAny), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                          ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(AssignEnumRefAny))
                                                                              .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                              .WithGenericParameterDefinition("TEnum")
                                                                              .WithGenericParameterDefinition("TValue")
                                                                              .WithParameter(typeof(TypedReference), "byref")
                                                                              .WithParameter<object>("value")
                                                                              .ReturningVoid()
                                                                          );
    private static readonly MethodInfo MtdGetNullableHasValueRefAny = typeof(DefaultSerializer).GetMethod(nameof(GetNullableHasValueRefAny), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                      ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(GetNullableHasValueRefAny))
                                                                          .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                          .WithGenericParameterDefinition("T")
                                                                          .WithParameter(typeof(TypedReference), "value")
                                                                          .Returning<bool>()
                                                                      );
    private static readonly MethodInfo MtdGetNullableValueRefAny = typeof(DefaultSerializer).GetMethod(nameof(GetNullableValueRefAny), BindingFlags.NonPublic | BindingFlags.Instance)
                                                                   ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(GetNullableValueRefAny))
                                                                       .DeclaredIn<DefaultSerializer>(isStatic: false)
                                                                       .WithGenericParameterDefinition("T")
                                                                       .WithParameter(typeof(TypedReference), "value")
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
#if NET5_0_OR_GREATER
        _primitiveParsers.AddManySerializer(new HalfParser.Many(_config));
#endif
        _primitiveParsers.AddManySerializer(new GuidParser.Many(_config));
        _primitiveParsers.AddManySerializer(new TimeSpanParser.Many(_config));
        _primitiveParsers.AddManySerializer(new DateTimeParser.Many(_config));
        _primitiveParsers.AddManySerializer(new DateTimeOffsetParser.Many(_config));

        Type? unityRegistrationType = Type.GetType("DanielWillett.ModularRpcs.Serialization.UnitySerializationRegistrationHelper, DanielWillett.ModularRPCs.Unity");
        if (unityRegistrationType != null)
            AddUnityParserTypes(unityRegistrationType);
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

    private void AddUnityParserTypes(Type unityRegistrationType)
    {
        MethodInfo? apply = unityRegistrationType.GetMethod("ApplyUnityParsers", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        if (apply == null)
            return;

        apply.Invoke(null, [ _primitiveParsers, _primitiveSizes, _config]);
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
    /// <param name="type">The type being parsed.</param>
    private IBinaryTypeParser? LookForParser(bool isIn, Type type)
    {
        if (type.IsValueType || !isIn && type.IsSealed || isIn && type.BaseType == typeof(object) || type == typeof(object) || type == typeof(ValueType) || type == typeof(string))
            return null;
        ConcurrentDictionary<Type, IBinaryTypeParser?> dict = isIn ? _discoveredInParsers : _discoveredOutParsers;
        if (dict.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            parser = dict.GetOrAdd(type, isIn ? LookForInParserFactory : LookForOutParserFactory);
        }
        
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
        if (type.IsDefinedSafe<RpcSerializableAttribute>() || typeof(IRpcSerializable).IsAssignableFrom(type))
        {
            throw new RpcInvalidParameterException(
                string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoParamInfo,
                    Accessor.ExceptionFormatter.Format(type),
                    Properties.Exceptions.RpcInvalidParameterExceptionNoParserFoundSerializableComponentMissing)
            );
        }

        throw new RpcInvalidParameterException(
            string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoParamInfo,
                Accessor.ExceptionFormatter.Format(type),
                Properties.Exceptions.RpcInvalidParameterExceptionNoParserFound)
        );
    }
    private static void ThrowNoParserFound(Type type, Exception inner)
    {
        throw new RpcInvalidParameterException(
            string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfoNoParamInfo,
                Accessor.ExceptionFormatter.Format(type),
                Properties.Exceptions.RpcInvalidParameterExceptionNoParserFound),
            inner
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
    public int GetMinimumSize<T>() => GetMinimumSize<T>(out _);

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

        if (type.IsEnum)
        {
            return GetMinimumSize(type.GetEnumUnderlyingType(), out isFixedSize);
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
    public int GetMinimumSize<T>(out bool isFixedSize)
    {
        Type type = typeof(T);
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

        if (typeof(T).IsEnum)
        {
            return GetMinimumSize(typeof(T).GetEnumUnderlyingType(), out isFixedSize);
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
    public int GetSerializableMinimumSize<TSerializable>(out bool isFixedSize) where TSerializable : IRpcSerializable
    {
        Type type = typeof(TSerializable);

        if (SerializableTypeInfoCache.TryGetValue(type, out SerializableTypeInfo v))
        {
            isFixedSize = v.IsFixedSize;
            return !typeof(TSerializable).IsValueType ? sizeof(int) : Math.Min(sizeof(int), v.MinimumSize);
        }

        RpcSerializableAttribute? attribute = type.GetAttributeSafe<RpcSerializableAttribute>();
        if (attribute == null)
            ThrowNoParserFound(type);

        SerializableTypeInfo info = default;
        info.MinimumSize = attribute!.MinimumSize;
        info.IsFixedSize = attribute.IsFixedSize;

        SerializableTypeInfoCache.TryAdd(type, info);

        isFixedSize = info.IsFixedSize;
        return !typeof(TSerializable).IsValueType ? sizeof(int) : Math.Min(sizeof(int), info.MinimumSize);
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

        if (typeof(T).IsEnum)
        {
            return GetMinimumSize(typeof(T).GetEnumUnderlyingType()) + 1;
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

        if (typeof(T).IsEnum)
        {
            return GetMinimumSize(typeof(T).GetEnumUnderlyingType());
        }

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GetNullableSize(value!, underlyingType);
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
    public int GetSerializableSize<TSerializable>(in TSerializable value) where TSerializable : IRpcSerializable
    {
        ref TSerializable s = ref Unsafe.AsRef(in value);
        if (s == null)
            return sizeof(int);
        return s.GetSize(this) + sizeof(int);
    }

    /// <inheritdoc />
    public int GetSerializablesSize<TSerializable>(IEnumerable<TSerializable?> value) where TSerializable : IRpcSerializable
    {
        if (value == null)
        {
            return SerializationHelper.GetHeaderSize(0, true);
        }

        int size = GetSerializableMinimumSize<TSerializable>(out bool isFixedSize);
        isFixedSize &= typeof(TSerializable).IsValueType;
        int count;
        if (isFixedSize)
        {
            if (value is ICollection c)
            {
                count = c.Count;
                return sizeof(int) + count * (size + sizeof(int)) + SerializationHelper.GetHeaderSize(count, false);
            }
            if (value is ICollection<TSerializable?> c2)
            {
                count = c2.Count;
                return sizeof(int) + count * (size + sizeof(int)) + SerializationHelper.GetHeaderSize(count, false);
            }
            if (value is IReadOnlyCollection<TSerializable?> c3)
            {
                count = c3.Count;
                return sizeof(int) + count * (size + sizeof(int)) + SerializationHelper.GetHeaderSize(count, false);
            }
        }

        int ttlSize = 0;
        count = 0;

        switch (value)
        {
            case TSerializable?[] array:
                count = array.Length;
                if (count == 0)
                {
                    return SerializationHelper.GetHeaderSize(0, false);
                }

                for (int i = 0; i < array.Length; ++i)
                {
                    ref TSerializable? s = ref array[i];
                    if (s != null)
                        ttlSize += s.GetSize(this);
                }

                break;

            case ArraySegment<TSerializable?> arraySegment:
                if (arraySegment.Count == 0)
                {
                    return SerializationHelper.GetHeaderSize(0, arraySegment.Array == null);
                }

                TSerializable?[] segmentArray = arraySegment.Array!;
                int maxCt = arraySegment.Offset + arraySegment.Count;
                count = arraySegment.Count;
                for (int i = arraySegment.Offset; i < maxCt; ++i)
                {
                    ref TSerializable? s = ref segmentArray[i];
                    if (s != null)
                        ttlSize += s.GetSize(this);
                }

                break;

            case List<TSerializable?> list:
                count = list.Count;
                if (count == 0)
                {
                    return SerializationHelper.GetHeaderSize(0, false);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    TSerializable? s = list[i];
                    if (s != null)
                        ttlSize += s.GetSize(this);
                }

                break;

            default:
                foreach (TSerializable? s in value)
                {
                    if (isFixedSize)
                        ttlSize += size;
                    else if (s != null)
                        ttlSize += s.GetSize(this);
                    ++count;
                }


                break;
        }

        return ttlSize + SerializationHelper.GetHeaderSize(count, false) + (count == 0 ? 0 : sizeof(int)) + count * sizeof(int);
    }

    /// <inheritdoc />
    public int GetSerializableSize<TSerializable>(Type type, IRpcSerializable? value) where TSerializable : IRpcSerializable
    {
        if (s == null)
            return sizeof(int);
        return s.GetSize(this) + sizeof(int);
    }

    /// <inheritdoc />
    public int GetSerializablesSize<TSerializable>(IEnumerable<TSerializable?> value) where TSerializable : IRpcSerializable
    {
        if (value == null)
        {
            return SerializationHelper.GetHeaderSize(0, true);
        }

        int size = GetSerializableMinimumSize<TSerializable>(out bool isFixedSize);
        isFixedSize &= typeof(TSerializable).IsValueType;
        int count;
        if (isFixedSize)
        {
            if (value is ICollection c)
            {
                count = c.Count;
                return sizeof(int) + count * (size + sizeof(int)) + SerializationHelper.GetHeaderSize(count, false);
            }
            if (value is ICollection<TSerializable?> c2)
            {
                count = c2.Count;
                return sizeof(int) + count * (size + sizeof(int)) + SerializationHelper.GetHeaderSize(count, false);
            }
            if (value is IReadOnlyCollection<TSerializable?> c3)
            {
                count = c3.Count;
                return sizeof(int) + count * (size + sizeof(int)) + SerializationHelper.GetHeaderSize(count, false);
            }
        }

        int ttlSize = 0;
        count = 0;

        switch (value)
        {
            case TSerializable?[] array:
                count = array.Length;
                if (count == 0)
                {
                    return SerializationHelper.GetHeaderSize(0, false);
                }

                for (int i = 0; i < array.Length; ++i)
                {
                    ref TSerializable? s = ref array[i];
                    if (s != null)
                        ttlSize += s.GetSize(this);
                }

                break;

            case ArraySegment<TSerializable?> arraySegment:
                if (arraySegment.Count == 0)
                {
                    return SerializationHelper.GetHeaderSize(0, arraySegment.Array == null);
                }

                TSerializable?[] segmentArray = arraySegment.Array!;
                int maxCt = arraySegment.Offset + arraySegment.Count;
                count = arraySegment.Count;
                for (int i = arraySegment.Offset; i < maxCt; ++i)
                {
                    ref TSerializable? s = ref segmentArray[i];
                    if (s != null)
                        ttlSize += s.GetSize(this);
                }

                break;

            case List<TSerializable?> list:
                count = list.Count;
                if (count == 0)
                {
                    return SerializationHelper.GetHeaderSize(0, false);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    TSerializable? s = list[i];
                    if (s != null)
                        ttlSize += s.GetSize(this);
                }

                break;

            default:
                foreach (TSerializable? s in value)
                {
                    if (isFixedSize)
                        ttlSize += size;
                    else if (s != null)
                        ttlSize += s.GetSize(this);
                    ++count;
                }


                break;
        }

        return ttlSize + SerializationHelper.GetHeaderSize(count, false) + (count == 0 ? 0 : sizeof(int)) + count * sizeof(int);
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
            return GetNullableSize(value, nullableType);
        }

        if (valueType.IsEnum)
        {
            return GetMinimumSize(valueType.GetEnumUnderlyingType());
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

        if (type.IsEnum)
        {
            return GetMinimumSize(type.GetEnumUnderlyingType());
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
    private int GetNullableSize(object? value, Type underlyingType)
    {
        if (value == null)
            return 1;

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            return (!parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value)) + 1;
        }

        if (_primitiveSizes.TryGetValue(underlyingType, out int size))
            return size + 1;

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            return (!parser.IsVariableSize ? parser.MinimumSize : parser.GetSize(value)) + 1;
        }

        if (underlyingType.IsEnum)
        {
            return GetNullableSize(value, underlyingType.GetEnumUnderlyingType());
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

        if (underlyingType.IsEnum)
        {
            return GetNullableSize(value, underlyingType.GetEnumUnderlyingType());
        }

        ThrowNoNullableParserFound(underlyingType);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe int WriteObject<T>(in T? value, byte* bytes, uint maxSize) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
                return genParser.WriteObject(value, bytes, maxSize);

            return parser.WriteObject(__makeref(Unsafe.AsRef(in value)), bytes, maxSize);
        }

        if (maxSize < 1)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        if (!value.HasValue)
        {
            bytes[0] = 0;
            return 1;
        }

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            T v = value.Value;
            *bytes = 1;
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, bytes + 1, maxSize - 1) + 1;

            return parser.WriteObject(__makeref(v), bytes + 1, maxSize - 1) + 1;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            T v = value.Value;
            *bytes = 1;
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, bytes + 1, maxSize - 1) + 1;

            return parser.WriteObject(__makeref(v), bytes + 1, maxSize - 1) + 1;
        }

        if (typeof(T).IsEnum)
        {
            T v = value.Value;
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, int>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, uint>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, byte>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, sbyte>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, short>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, ushort>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, long>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, ulong>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, nint>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, nuint>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, char>(ref v), bytes + 1, maxSize - 1) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                *bytes = 1;
                return WriteObject(Unsafe.As<T, bool>(ref v), bytes + 1, maxSize - 1) + 1;
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, bool>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.Char:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, char>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.SByte:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, sbyte>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.Byte:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, byte>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.Int16:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, short>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.UInt16:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, ushort>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.Int32:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, int>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.UInt32:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, uint>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.Int64:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, long>(ref v), bytes + 1, maxSize - 1) + 1;

                case TypeCode.UInt64:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, ulong>(ref v), bytes + 1, maxSize - 1) + 1;

                case LegacyEnumCache<T>.NativeInt:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, nint>(ref v), bytes + 1, maxSize - 1) + 1;

                case LegacyEnumCache<T>.NativeUInt:
                    *bytes = 1;
                    return WriteObject(Unsafe.As<T, nuint>(ref v), bytes + 1, maxSize - 1) + 1;
            }
#endif
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

        parser = LookForParser(true, type);
        if (parser != null)
        {
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(value, bytes, maxSize);

            return parser.WriteObject(value, bytes, maxSize);
        }

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                return WriteObject(Unsafe.As<T, int>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                return WriteObject(Unsafe.As<T, uint>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                return WriteObject(Unsafe.As<T, byte>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                return WriteObject(Unsafe.As<T, sbyte>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                return WriteObject(Unsafe.As<T, short>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                return WriteObject(Unsafe.As<T, ushort>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                return WriteObject(Unsafe.As<T, long>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                return WriteObject(Unsafe.As<T, ulong>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                return WriteObject(Unsafe.As<T, nint>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                return WriteObject(Unsafe.As<T, nuint>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                return WriteObject(Unsafe.As<T, char>(ref value!), bytes, maxSize);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                return WriteObject(Unsafe.As<T, bool>(ref value!), bytes, maxSize);
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                    return WriteObject(Unsafe.As<T, bool>(ref value!), bytes, maxSize);

                case TypeCode.Char:
                    return WriteObject(Unsafe.As<T, char>(ref value!), bytes, maxSize);

                case TypeCode.SByte:
                    return WriteObject(Unsafe.As<T, sbyte>(ref value!), bytes, maxSize);

                case TypeCode.Byte:
                    return WriteObject(Unsafe.As<T, byte>(ref value!), bytes, maxSize);

                case TypeCode.Int16:
                    return WriteObject(Unsafe.As<T, short>(ref value!), bytes, maxSize);

                case TypeCode.UInt16:
                    return WriteObject(Unsafe.As<T, ushort>(ref value!), bytes, maxSize);

                case TypeCode.Int32:
                    return WriteObject(Unsafe.As<T, int>(ref value!), bytes, maxSize);

                case TypeCode.UInt32:
                    return WriteObject(Unsafe.As<T, uint>(ref value!), bytes, maxSize);

                case TypeCode.Int64:
                    return WriteObject(Unsafe.As<T, long>(ref value!), bytes, maxSize);

                case TypeCode.UInt64:
                    return WriteObject(Unsafe.As<T, ulong>(ref value!), bytes, maxSize);

                case LegacyEnumCache<T>.NativeInt:
                    return WriteObject(Unsafe.As<T, nint>(ref value!), bytes, maxSize);

                case LegacyEnumCache<T>.NativeUInt:
                    return WriteObject(Unsafe.As<T, nuint>(ref value!), bytes, maxSize);
            }
#endif
        }

        ThrowNoParserFound(type);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe int WriteSerializableObject<TSerializable>(in TSerializable serializable, byte* bytes, uint maxSize) where TSerializable : IRpcSerializable
    {
        ref TSerializable value = ref Unsafe.AsRef(in serializable);

        Span<byte> span = new Span<byte>(bytes, (int)Math.Min(int.MaxValue, maxSize));

        if (serializable == null)
        {
            WriteToBuffer(span, int.MinValue);
            return sizeof(int);
        }

        int write = value.Write(span.Slice(sizeof(int)), this);
        WriteToBuffer(span, write);
        return write + sizeof(int);
    }

    /// <inheritdoc />
    public unsafe int WriteSerializableObjects<TSerializable>(IEnumerable<TSerializable?> serializable, byte* bytes, uint maxSize) where TSerializable : IRpcSerializable
    {
        uint index = 0;
        if (serializable == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        uint maxSizeIndex;
        int rollingMaxSize;

        switch (serializable)
        {
            case TSerializable?[] array:
                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, array.Length, false, this);
                if (array.Length == 0)
                    return (int)index;

                rollingMaxSize = 0;
                maxSizeIndex = index;
                index += sizeof(int);

                for (int i = 0; i < array.Length; ++i)
                {
                    ref TSerializable? s = ref array[i];
                    long size = (long)maxSize - index;
                    if (size < sizeof(int))
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOut)) { ErrorCode = 1 };

                    if (s == null)
                    {
                        WriteToBuffer(bytes + index, int.MinValue);
                        index += sizeof(int);
                        continue;
                    }

                    int write = s.Write(new Span<byte>(bytes + index + sizeof(int), (int)Math.Min(int.MaxValue, size)), this);
                    if (write < 0)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOut)) { ErrorCode = 1 };

                    WriteToBuffer(bytes + index, write);
                    index += checked( (uint)write ) + sizeof(int);

                    rollingMaxSize = Math.Max(rollingMaxSize, write);
                }
                break;

            case ArraySegment<TSerializable?> arraySegment:
                int segCt = arraySegment.Count;
                int segOf = arraySegment.Offset;
                TSerializable?[]? segArr = arraySegment.Array;
                if (segCt == 0 || segArr == null)
                {
                    return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, segArr == null, this);
                }

                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, segCt, false, this);

                rollingMaxSize = 0;
                maxSizeIndex = index;
                index += sizeof(int);

                int segMax = segOf + segCt;
                for (int i = segOf; i < segMax; ++i)
                {
                    ref TSerializable? s = ref segArr[i];
                    long size = (long)maxSize - index;
                    if (size < sizeof(int))
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOut)) { ErrorCode = 1 };

                    if (s == null)
                    {
                        WriteToBuffer(bytes + index, int.MinValue);
                        index += sizeof(int);
                        continue;
                    }

                    int write = s.Write(new Span<byte>(bytes + index + sizeof(int), (int)Math.Min(int.MaxValue, size)), this);
                    if (write < 0)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOut)) { ErrorCode = 1 };

                    WriteToBuffer(bytes + index, write);
                    index += checked((uint)write) + sizeof(int);

                    rollingMaxSize = Math.Max(rollingMaxSize, write);
                }
                break;

            default:
                
                int count = 0;
                using (IEnumerator<TSerializable?> enumerator = serializable.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                    {
                        SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, false, this, forceFull: true);
                        return (int)index;
                    }

                    index = (uint)SerializationHelper.GetHeaderSize(0, false, forceFull: true);
                    maxSizeIndex = index;
                    rollingMaxSize = 0;
                    index += sizeof(int);

                    do
                    {
                        long size = (long)maxSize - index;
                        if (size < sizeof(int))
                            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOut)) { ErrorCode = 1 };

                        ++count;
                        TSerializable? s = enumerator.Current;
                        if (s == null)
                        {
                            WriteToBuffer(bytes + index, int.MinValue);
                            index += sizeof(int);
                            continue;
                        }

                        int write = s.Write(new Span<byte>(bytes + index + sizeof(int), (int)Math.Min(int.MaxValue, size)), this);
                        if (write < 0)
                            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOut)) { ErrorCode = 1 };

                        WriteToBuffer(bytes + index, write);
                        index += checked( (uint)write ) + sizeof(int);

                        rollingMaxSize = Math.Max(rollingMaxSize, write);
                    }
                    while (enumerator.MoveNext());
                }

                uint index2 = 0;
                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index2, count, false, this, forceFull: true);
                break;
        }

        WriteToBuffer(bytes + maxSizeIndex, rollingMaxSize);

        return (int)index;
    }

    /// <inheritdoc />
    public unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        Type type = __reftype(value);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
            return parser.WriteObject(value, bytes, maxSize);

        if (_primitiveParsers.TryGetValue(type, out parser))
            return parser.WriteObject(value, bytes, maxSize);

        Type? underlyingType;
        if (type.IsEnum)
        {
            IConvertible val = (IConvertible)TypedReference.ToObject(value);
            underlyingType = type.GetEnumUnderlyingType();
            if (underlyingType == typeof(int))
            {
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(uint))
            {
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(byte))
            {
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(sbyte))
            {
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(short))
            {
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(ushort))
            {
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(long))
            {
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(ulong))
            {
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(nint))
            {
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(nuint))
            {
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(char))
            {
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(bool))
            {
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), bytes, maxSize);
            }
        }

        underlyingType = Nullable.GetUnderlyingType(type);
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

        Type? underlyingType;
        if (valueType.IsEnum)
        {
            IConvertible val = (IConvertible)value!;
            underlyingType = valueType.GetEnumUnderlyingType();
            if (underlyingType == typeof(int))
            {
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(uint))
            {
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(byte))
            {
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(sbyte))
            {
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(short))
            {
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(ushort))
            {
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(long))
            {
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(ulong))
            {
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(nint))
            {
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(nuint))
            {
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(char))
            {
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), bytes, maxSize);
            }
            if (underlyingType == typeof(bool))
            {
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), bytes, maxSize);
            }
        }

        underlyingType = Nullable.GetUnderlyingType(valueType);
        if (underlyingType != null)
        {
            return WriteNullable(value, bytes, maxSize, underlyingType);
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

        type = typeof(T);
        if (_parsers.TryGetValue(type, out parser))
        {
            T v = value.Value;

            stream.WriteByte(1);
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, stream) + 1;

            return parser.WriteObject(__makeref(v), stream) + 1;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            T v = value.Value;

            stream.WriteByte(1);
            if (parser is IBinaryTypeParser<T> genParser)
                return genParser.WriteObject(v, stream) + 1;

            return parser.WriteObject(__makeref(v), stream) + 1;
        }

        if (typeof(T).IsEnum)
        {
            T v = value.Value;
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, int>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, uint>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, byte>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, sbyte>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, short>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, ushort>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, long>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, ulong>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, nint>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, nuint>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, char>(ref v), stream) + 1;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                stream.WriteByte(1);
                return WriteObject(Unsafe.As<T, bool>(ref v), stream) + 1;
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, bool>(ref v), stream) + 1;

                case TypeCode.Char:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, char>(ref v), stream) + 1;

                case TypeCode.SByte:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, sbyte>(ref v), stream) + 1;

                case TypeCode.Byte:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, byte>(ref v), stream) + 1;

                case TypeCode.Int16:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, short>(ref v), stream) + 1;

                case TypeCode.UInt16:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, ushort>(ref v), stream) + 1;

                case TypeCode.Int32:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, int>(ref v), stream) + 1;

                case TypeCode.UInt32:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, uint>(ref v), stream) + 1;

                case TypeCode.Int64:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, long>(ref v), stream) + 1;

                case TypeCode.UInt64:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, ulong>(ref v), stream) + 1;

                case LegacyEnumCache<T>.NativeInt:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, nint>(ref v), stream) + 1;

                case LegacyEnumCache<T>.NativeUInt:
                    stream.WriteByte(1);
                    return WriteObject(Unsafe.As<T, nuint>(ref v), stream) + 1;
            }
#endif
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

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                return WriteObject(Unsafe.As<T, int>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                return WriteObject(Unsafe.As<T, uint>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                return WriteObject(Unsafe.As<T, byte>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                return WriteObject(Unsafe.As<T, sbyte>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                return WriteObject(Unsafe.As<T, short>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                return WriteObject(Unsafe.As<T, ushort>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                return WriteObject(Unsafe.As<T, long>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                return WriteObject(Unsafe.As<T, ulong>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                return WriteObject(Unsafe.As<T, nint>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                return WriteObject(Unsafe.As<T, nuint>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                return WriteObject(Unsafe.As<T, char>(ref value!), stream);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                return WriteObject(Unsafe.As<T, bool>(ref value!), stream);
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                    return WriteObject(Unsafe.As<T, bool>(ref value!), stream);

                case TypeCode.Char:
                    return WriteObject(Unsafe.As<T, char>(ref value!), stream);

                case TypeCode.SByte:
                    return WriteObject(Unsafe.As<T, sbyte>(ref value!), stream);

                case TypeCode.Byte:
                    return WriteObject(Unsafe.As<T, byte>(ref value!), stream);

                case TypeCode.Int16:
                    return WriteObject(Unsafe.As<T, short>(ref value!), stream);

                case TypeCode.UInt16:
                    return WriteObject(Unsafe.As<T, ushort>(ref value!), stream);

                case TypeCode.Int32:
                    return WriteObject(Unsafe.As<T, int>(ref value!), stream);

                case TypeCode.UInt32:
                    return WriteObject(Unsafe.As<T, uint>(ref value!), stream);

                case TypeCode.Int64:
                    return WriteObject(Unsafe.As<T, long>(ref value!), stream);

                case TypeCode.UInt64:
                    return WriteObject(Unsafe.As<T, ulong>(ref value!), stream);

                case LegacyEnumCache<T>.NativeInt:
                    return WriteObject(Unsafe.As<T, nint>(ref value!), stream);

                case LegacyEnumCache<T>.NativeUInt:
                    return WriteObject(Unsafe.As<T, nuint>(ref value!), stream);
            }
#endif
        }

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return WriteNullable(value!, stream, underlyingType);
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
    public int WriteSerializableObject<TSerializable>(in TSerializable serializable, Stream stream) where TSerializable : IRpcSerializable
    {
        ref TSerializable s = ref Unsafe.AsRef(in serializable);
        int size = s != null ? s.GetSize(this) : 0;
        int fullSize = size + sizeof(int);

        int write = 0;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        scoped Span<byte> buffer = fullSize > Configuration.MaximumStackAllocationSize
            ? new byte[fullSize]
            : stackalloc byte[fullSize];

        if (s != null)
        {
            write = s.Write(buffer.Slice(sizeof(int)), this);
            WriteToBuffer(buffer, write);
        }
        else
        {
            WriteToBuffer(buffer, int.MinValue);
        }
        write += sizeof(int);
        stream.Write(buffer[..write]);
        return write;
#else
        byte[] buffer = fullSize > MaxArrayPoolSize
            ? new byte[fullSize]
            : ArrayPool.Rent(fullSize);
        try
        {
            if (s != null)
            {
                write = s.Write(buffer.AsSpan(sizeof(int)), this);
                WriteToBuffer(buffer, write);
            }
            else
            {
                WriteToBuffer(buffer, int.MinValue);
            }
            stream.Write(buffer, 0, write + sizeof(int));
            return write + sizeof(int);
        }
        finally
        {
            if (fullSize <= MaxArrayPoolSize)
                ArrayPool.Return(buffer);
        }
#endif
    }

    private static unsafe int ReadSerializableSizeFromBuffer(byte* buffer, uint maxSize, bool stream, bool checkError)
    {
        if (maxSize < sizeof(int))
        {
            throw new RpcParseException(stream
                ? Properties.Exceptions.RpcParseExceptionStreamRunOut
                : Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = stream ? 2 : 1 };
        }

        int size = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<int>(ref buffer[0])
            : buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

        if (size < 0)
        {
            if (size == int.MinValue)
                return int.MinValue;

            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
        }

        if (checkError && maxSize - sizeof(int) < size)
        {
            throw new RpcParseException(stream
                    ? Properties.Exceptions.RpcParseExceptionStreamRunOut
                    : Properties.Exceptions.RpcParseExceptionBufferRunOut)
                { ErrorCode = stream ? 2 : 1 };
        }

        return size;
    }
    private static int ReadSerializableSizeFromStream(Stream stream)
    {
        int size;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Span<byte> intBuffer = stackalloc byte[sizeof(int)];
        int amt = stream.Read(intBuffer);
#else
        byte[] intBuffer = ArrayPool.Rent(sizeof(int));
        try
        {
            int amt = stream.Read(intBuffer, 0, sizeof(int));
#endif
        if (amt < sizeof(int))
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
        }

        size = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<int>(ref intBuffer[0])
            : intBuffer[0] << 24 | intBuffer[1] << 16 | intBuffer[2] << 8 | intBuffer[3];
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        }
        finally
        {
            ArrayPool.Return(intBuffer);
        }
#endif

        if (size < 0)
        {
            if (size == int.MinValue)
                return int.MinValue;

            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
        }

        return size;
    }
    private static int ReadSerializableSizeFromStreamToBuffer(Stream stream,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Span<byte> buffer
#else
        byte[] buffer
#endif
        , ref int bytesRead
    )
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        int amt = stream.Read(buffer.Slice(0, sizeof(int)));
#else
        int amt = stream.Read(buffer, 0, sizeof(int));
#endif
        bytesRead += amt;
        if (amt < sizeof(int))
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
        }

        int size = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<int>(ref buffer[0])
            : buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

        if (size < 0)
        {
            if (size == int.MinValue)
                return int.MinValue;

            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
        }

        if (buffer.Length < size)
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOutInvalidSize) { ErrorCode = 10 };
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        amt = stream.Read(buffer.Slice(0, size));
#else
        amt = stream.Read(buffer, 0, size);
#endif

        bytesRead += amt;
        if (amt < size)
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
        }

        return size;
    }

    private static unsafe void WriteToBuffer(byte* buffer, int write)
    {
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(buffer, write);
        }
        else
        {
            buffer[3] = unchecked((byte)write);
            buffer[2] = unchecked((byte)(write >>> 8));
            buffer[1] = unchecked((byte)(write >>> 16));
            buffer[0] = unchecked((byte)(write >>> 24));
        }
    }
    private static void WriteToBuffer(Span<byte> buffer, int write)
    {
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref buffer[0], write);
        }
        else
        {
            buffer[3] = unchecked((byte)write);
            buffer[2] = unchecked((byte)(write >>> 8));
            buffer[1] = unchecked((byte)(write >>> 16));
            buffer[0] = unchecked((byte)(write >>> 24));
        }
    }

    /// <inheritdoc />
    public int WriteSerializableObjects<TSerializable>(IEnumerable<TSerializable?> serializable, Stream stream) where TSerializable : IRpcSerializable
    {
        uint index = 0;
        if (serializable == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }

        int size = GetSerializableMinimumSize<TSerializable>(out bool isFixedSize);
        isFixedSize &= typeof(TSerializable).IsValueType;

        switch (serializable)
        {
            case TSerializable?[] array:
                if (array.Length == 0)
                {
                    return SerializationHelper.WriteStandardArrayHeader(stream, 0, false);
                }

                int maxSize = 0;
                if (isFixedSize)
                {
                    maxSize = size;
                }
                else
                {
                    for (int i = 0; i < array.Length; ++i)
                    {
                        ref TSerializable? s = ref array[i];
                        if (s != null)
                            maxSize = Math.Max(s.GetSize(this), maxSize);
                    }
                }

                int bufferSize = maxSize + sizeof(int);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                scoped Span<byte> buffer = bufferSize > Configuration.MaximumStackAllocationSize
                    ? new byte[bufferSize]
                    : stackalloc byte[bufferSize];
#else
                byte[] buffer = bufferSize > MaxArrayPoolSize
                    ? new byte[bufferSize]
                    : ArrayPool.Rent(bufferSize);
                try
                {
#endif
                index += (uint)SerializationHelper.WriteStandardArrayHeader(stream, array.Length, false);
                WriteToBuffer(buffer, maxSize);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                stream.Write(buffer[..sizeof(int)]);
#else
                stream.Write(buffer, 0, sizeof(int));
#endif
                index += sizeof(int);
                for (int i = 0; i < array.Length; ++i)
                {
                    ref TSerializable? s = ref array[i];
                    int write = 0;
                    if (s != null)
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        write = s.Write(buffer.Slice(sizeof(int)), this);
#else
                        write = s.Write(buffer.AsSpan(sizeof(int)), this);
#endif
                        WriteToBuffer(buffer, write);
                    }
                    else
                    {
                        WriteToBuffer(buffer, int.MinValue);
                    }
                    int amt = write + sizeof(int);
                    index += (uint)amt;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    stream.Write(buffer[..amt]);
#else
                    stream.Write(buffer, 0, amt);
#endif
                }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (bufferSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif
                break;
                
            case ArraySegment<TSerializable?> arraySegment:
                if (arraySegment.Count == 0)
                {
                    return SerializationHelper.WriteStandardArrayHeader(stream, 0, arraySegment.Array == null);
                }

                TSerializable[] segmentArray = arraySegment.Array!;
                int maxCt = arraySegment.Count + arraySegment.Offset;

                maxSize = 0;
                if (isFixedSize)
                {
                    maxSize = size;
                }
                else
                {
                    for (int i = arraySegment.Offset; i < maxCt; ++i)
                    {
                        ref TSerializable s = ref segmentArray[i];
                        if (s != null)
                            maxSize = Math.Max(s.GetSize(this), maxSize);
                    }
                }
                
                bufferSize = maxSize + sizeof(int);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                buffer = bufferSize > Configuration.MaximumStackAllocationSize
                    ? new byte[bufferSize]
                    : stackalloc byte[bufferSize];
#else
                buffer = bufferSize > MaxArrayPoolSize
                    ? new byte[bufferSize]
                    : ArrayPool.Rent(bufferSize);
                try
                {
#endif
                index += (uint)SerializationHelper.WriteStandardArrayHeader(stream, arraySegment.Count, false);
                WriteToBuffer(buffer, maxSize);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                stream.Write(buffer[..sizeof(int)]);
#else
                stream.Write(buffer, 0, sizeof(int));
#endif
                index += sizeof(int);
                for (int i = arraySegment.Offset; i < maxCt; ++i)
                {
                    ref TSerializable? s = ref segmentArray[i];
                    int write = 0;
                    if (s != null)
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        write = s.Write(buffer.Slice(sizeof(int)), this);
#else
                        write = s.Write(buffer.AsSpan(sizeof(int)), this);
#endif
                        WriteToBuffer(buffer, write);
                    }
                    else
                    {
                        WriteToBuffer(buffer, int.MinValue);
                    }
                    int amt = write + sizeof(int);
                    index += (uint)amt;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    stream.Write(buffer[..amt]);
#else
                    stream.Write(buffer, 0, amt);
#endif
                }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (bufferSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif
                break;

            case ICollection<TSerializable?> collection:
                if (collection.Count == 0)
                {
                    return SerializationHelper.WriteStandardArrayHeader(stream, 0, false);
                }

                maxSize = 0;
                if (isFixedSize)
                {
                    maxSize = size;
                }
                else
                {
                    foreach (TSerializable? s in collection)
                    {
                        if (s != null)
                            maxSize = Math.Max(s.GetSize(this), maxSize);
                    }
                }
                
                bufferSize = maxSize + sizeof(int);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                buffer = bufferSize > Configuration.MaximumStackAllocationSize
                    ? new byte[bufferSize]
                    : stackalloc byte[bufferSize];
#else
                buffer = bufferSize > MaxArrayPoolSize
                    ? new byte[bufferSize]
                    : ArrayPool.Rent(bufferSize);
                try
                {
#endif
                index += (uint)SerializationHelper.WriteStandardArrayHeader(stream, collection.Count, false);
                WriteToBuffer(buffer, maxSize);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                stream.Write(buffer[..sizeof(int)]);
#else
                stream.Write(buffer, 0, sizeof(int));
#endif
                index += sizeof(int);
                foreach (TSerializable? s in collection)
                {
                    int write = 0;
                    if (s != null)
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        write = s.Write(buffer.Slice(sizeof(int)), this);
#else
                        write = s.Write(buffer.AsSpan(sizeof(int)), this);
#endif
                        WriteToBuffer(buffer, write);
                    }
                    else
                    {
                        WriteToBuffer(buffer, int.MinValue);
                    }
                    int amt = write + sizeof(int);
                    index += (uint)amt;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    stream.Write(buffer[..amt]);
#else
                    stream.Write(buffer, 0, amt);
#endif
                }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (bufferSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif
                break;

            case IReadOnlyCollection<TSerializable> collection:
                if (collection.Count == 0)
                {
                    return SerializationHelper.WriteStandardArrayHeader(stream, 0, false);
                }

                maxSize = 0;
                if (isFixedSize)
                {
                    maxSize = size;
                }
                else
                {
                    foreach (TSerializable s in collection)
                    {
                        if (s != null)
                            maxSize = Math.Max(s.GetSize(this), maxSize);
                    }
                }
                
                bufferSize = maxSize + sizeof(int);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                buffer = bufferSize > Configuration.MaximumStackAllocationSize
                    ? new byte[bufferSize]
                    : stackalloc byte[bufferSize];
#else
                buffer = bufferSize > MaxArrayPoolSize
                    ? new byte[bufferSize]
                    : ArrayPool.Rent(bufferSize);
                try
                {
#endif
                index += (uint)SerializationHelper.WriteStandardArrayHeader(stream, collection.Count, false);
                WriteToBuffer(buffer, maxSize);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                stream.Write(buffer[..sizeof(int)]);
#else
                stream.Write(buffer, 0, sizeof(int));
#endif
                index += sizeof(int);
                foreach (TSerializable s in collection)
                {
                    int write = 0;
                    if (s != null)
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        write = s.Write(buffer.Slice(sizeof(int)), this);
#else
                        write = s.Write(buffer.AsSpan(sizeof(int)), this);
#endif
                        WriteToBuffer(buffer, write);
                    }
                    else
                    {
                        WriteToBuffer(buffer, int.MinValue);
                    }
                    int amt = write + sizeof(int);
                    index += (uint)amt;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    stream.Write(buffer[..amt]);
#else
                    stream.Write(buffer, 0, amt);
#endif
                }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (bufferSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif
                break;
                
            default:
            {
                IEnumerator<TSerializable?> enumerator = serializable.GetEnumerator();
                try
                {
                    List<TSerializable?>? list = null;
                    try
                    {
                        enumerator.Reset();
                    }
                    catch (NotSupportedException)
                    {
                        list = new List<TSerializable?>(16);
                        while (enumerator.MoveNext())
                        {
                            list.Add(enumerator.Current);
                        }
                    }

                    if (list != null)
                    {
                        if (list.Count == 0)
                        {
                            return SerializationHelper.WriteStandardArrayHeader(stream, 0, false);
                        }

                        maxSize = 0;
                        if (isFixedSize)
                        {
                            maxSize = size;
                        }
                        else
                        {
                            foreach (TSerializable? s in list)
                            {
                                if (s != null)
                                    maxSize = Math.Max(s!.GetSize(this), maxSize);
                            }
                        }
                        
                        bufferSize = maxSize + sizeof(int);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        buffer = bufferSize > Configuration.MaximumStackAllocationSize
                            ? new byte[bufferSize]
                            : stackalloc byte[bufferSize];
#else
                        buffer = bufferSize > MaxArrayPoolSize
                            ? new byte[bufferSize]
                            : ArrayPool.Rent(bufferSize);
                        try
                        {
#endif
                        index += (uint)SerializationHelper.WriteStandardArrayHeader(stream, list.Count, false);
                        WriteToBuffer(buffer, maxSize);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        stream.Write(buffer[..sizeof(int)]);
#else
                        stream.Write(buffer, 0, sizeof(int));
#endif
                        index += sizeof(uint);
                        foreach (TSerializable? s in list)
                        {
                            int write = 0;
                            if (s != null)
                            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                                write = s.Write(buffer.Slice(sizeof(int)), this);
#else
                                write = s.Write(buffer.AsSpan(sizeof(int)), this);
#endif
                                WriteToBuffer(buffer, write);
                            }
                            else
                            {
                                WriteToBuffer(buffer, int.MinValue);
                            }
                            int amt = write + sizeof(int);
                            index += (uint)amt;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                            stream.Write(buffer[..amt]);
#else
                            stream.Write(buffer, 0, amt);
#endif
                        }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                        }
                        finally
                        {
                            if (bufferSize <= MaxArrayPoolSize)
                                ArrayPool.Return(buffer);
                        }
#endif
                        break;
                    }
                    

                    int count = 0;
                    maxSize = 0;
                    if (isFixedSize)
                    {
                        maxSize = size;
                        while (enumerator.MoveNext())
                            ++count;
                    }
                    else
                    {
                        while (enumerator.MoveNext())
                        {
                            TSerializable s = enumerator.Current!;
                            if (s != null)
                                maxSize = Math.Max(s.GetSize(this), maxSize);
                        }
                    }

                    if (count == 0)
                    {
                        return SerializationHelper.WriteStandardArrayHeader(stream, 0, false);
                    }

                    enumerator.Reset();
                        
                    bufferSize = maxSize + sizeof(int);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    buffer = bufferSize > Configuration.MaximumStackAllocationSize
                        ? new byte[bufferSize]
                        : stackalloc byte[bufferSize];
#else
                    buffer = bufferSize > MaxArrayPoolSize
                        ? new byte[bufferSize]
                        : ArrayPool.Rent(bufferSize);
                    try
                    {
#endif
                    index += (uint)SerializationHelper.WriteStandardArrayHeader(stream, count, false);
                    WriteToBuffer(buffer, maxSize);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    stream.Write(buffer[..sizeof(int)]);
#else
                    stream.Write(buffer, 0, sizeof(int));
#endif
                    index += sizeof(int);
                    while (enumerator.MoveNext())
                    {
                        --count;
                        TSerializable? s = enumerator.Current;
                        int write = 0;
                        if (s != null)
                        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                            write = s.Write(buffer.Slice(sizeof(int)), this);
#else
                            write = s.Write(buffer.AsSpan(sizeof(int)), this);
#endif
                            WriteToBuffer(buffer, write);
                        }
                        else
                        {
                            WriteToBuffer(buffer, int.MinValue);
                        }
                        int amt = write + sizeof(int);
                        index += (uint)amt;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                        stream.Write(buffer[..amt]);
#else
                        stream.Write(buffer, 0, amt);
#endif
                    }
                    if (count > 0)
                        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.InvalidOperationExceptionInconsistantEnumerator, Accessor.ExceptionFormatter.Format(serializable.GetType())));
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                    }
                    finally
                    {
                        if (bufferSize <= MaxArrayPoolSize)
                            ArrayPool.Return(buffer);
                    }
#endif
                }
                finally
                {
                    enumerator.Dispose();
                }
                break;
            }
        }

        return (int)index;
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

        if (type.IsEnum)
        {
            IConvertible val = (IConvertible)TypedReference.ToObject(value);
            underlyingType = type.GetEnumUnderlyingType();
            if (underlyingType == typeof(int))
            {
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(uint))
            {
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(byte))
            {
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(sbyte))
            {
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(short))
            {
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(ushort))
            {
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(long))
            {
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(ulong))
            {
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(nint))
            {
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(nuint))
            {
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(char))
            {
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(bool))
            {
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), stream);
            }
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

        Type? underlyingType;
        if (valueType.IsEnum)
        {
            underlyingType = valueType.GetEnumUnderlyingType();
            IConvertible val = (IConvertible?)value!;
            if (underlyingType == typeof(int))
            {
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(uint))
            {
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(byte))
            {
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(sbyte))
            {
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(short))
            {
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(ushort))
            {
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(long))
            {
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(ulong))
            {
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(nint))
            {
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(nuint))
            {
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(char))
            {
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), stream);
            }
            if (underlyingType == typeof(bool))
            {
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), stream);
            }
        }

        underlyingType = Nullable.GetUnderlyingType(valueType);
        if (underlyingType != null)
        {
            return WriteNullable(value, stream, underlyingType);
        }

        parser = LookForParser(true, valueType);
        if (parser != null)
        {
            return parser.WriteObject(value, stream);
        }

        ThrowNoParserFound(valueType);
        return 0; // not reached
    }
    private unsafe int WriteNullable(object? value, byte* bytes, uint maxSize, Type underlyingType)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        if (value == null)
        {
            bytes[0] = 0;
            return 1;
        }

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            bytes[0] = 1;
            return parser.WriteObject(value, bytes + 1, maxSize - 1) + 1;
        }

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            bytes[0] = 1;
            return parser.WriteObject(value, bytes + 1, maxSize - 1) + 1;
        }

        if (underlyingType.IsEnum)
        {
            Type underlyingType2 = underlyingType.GetEnumUnderlyingType();
            IConvertible val = (IConvertible?)value!;
            if (underlyingType2 == typeof(int))
            {
                bytes[0] = 1;
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(uint))
            {
                bytes[0] = 1;
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(byte))
            {
                bytes[0] = 1;
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(sbyte))
            {
                bytes[0] = 1;
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(short))
            {
                bytes[0] = 1;
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(ushort))
            {
                bytes[0] = 1;
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(long))
            {
                bytes[0] = 1;
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(ulong))
            {
                bytes[0] = 1;
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(nint))
            {
                bytes[0] = 1;
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(nuint))
            {
                bytes[0] = 1;
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(char))
            {
                bytes[0] = 1;
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(bool))
            {
                bytes[0] = 1;
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
        }

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

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            bytes[0] = 1;
            return parser.WriteObject(GetNullableValue(value), bytes + 1, maxSize - 1) + 1;
        }

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            bytes[0] = 1;
            return parser.WriteObject(GetNullableValue(value), bytes + 1, maxSize - 1) + 1;
        }

        if (underlyingType.IsEnum)
        {
            Type underlyingType2 = underlyingType.GetEnumUnderlyingType();
            IConvertible val = (IConvertible?)TypedReference.ToObject(value)!;
            if (underlyingType2 == typeof(int))
            {
                bytes[0] = 1;
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(uint))
            {
                bytes[0] = 1;
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(byte))
            {
                bytes[0] = 1;
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(sbyte))
            {
                bytes[0] = 1;
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(short))
            {
                bytes[0] = 1;
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(ushort))
            {
                bytes[0] = 1;
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(long))
            {
                bytes[0] = 1;
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(ulong))
            {
                bytes[0] = 1;
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(nint))
            {
                bytes[0] = 1;
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(nuint))
            {
                bytes[0] = 1;
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(char))
            {
                bytes[0] = 1;
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
            if (underlyingType2 == typeof(bool))
            {
                bytes[0] = 1;
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), bytes + 1, maxSize - 1) + 1;
            }
        }

        ThrowNoParserFound(underlyingType);
        return 0; // not reached
    }
    private int WriteNullable(object? value, Stream stream, Type underlyingType)
    {
        if (value == null)
        {
            stream.WriteByte(0);
            return 1;
        }

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            stream.WriteByte(1);
            return parser.WriteObject(value, stream) + 1;
        }

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            stream.WriteByte(1);
            return parser.WriteObject(value, stream) + 1;
        }

        if (underlyingType.IsEnum)
        {
            Type underlyingType2 = underlyingType.GetEnumUnderlyingType();
            IConvertible val = (IConvertible?)value!;
            if (underlyingType2 == typeof(int))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(uint))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(byte))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(sbyte))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(short))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(ushort))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(long))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(ulong))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(nint))
            {
                stream.WriteByte(1);
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(nuint))
            {
                stream.WriteByte(1);
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(char))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(bool))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), stream) + 1;
            }
        }

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

        if (_parsers.TryGetValue(underlyingType, out IBinaryTypeParser? parser))
        {
            stream.WriteByte(1);
            return parser.WriteObject(GetNullableValue(value), stream) + 1;
        }

        if (_primitiveParsers.TryGetValue(underlyingType, out parser))
        {
            stream.WriteByte(1);
            return parser.WriteObject(GetNullableValue(value), stream) + 1;
        }

        if (underlyingType.IsEnum)
        {
            Type underlyingType2 = underlyingType.GetEnumUnderlyingType();
            IConvertible val = (IConvertible?)TypedReference.ToObject(value)!;
            if (underlyingType2 == typeof(int))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToInt32(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(uint))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToUInt32(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(byte))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToByte(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(sbyte))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToSByte(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(short))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToInt16(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(ushort))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToUInt16(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(long))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(ulong))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToUInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(nint))
            {
                stream.WriteByte(1);
                return WriteObject((nint)val.ToInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(nuint))
            {
                stream.WriteByte(1);
                return WriteObject((nint)val.ToUInt64(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(char))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToChar(CultureInfo.InvariantCulture), stream) + 1;
            }
            if (underlyingType2 == typeof(bool))
            {
                stream.WriteByte(1);
                return WriteObject(val.ToBoolean(CultureInfo.InvariantCulture), stream) + 1;
            }
        }

        ThrowNoParserFound(underlyingType);
        return 0; // not reached
    }

    /// <inheritdoc />
    public unsafe T? ReadNullable<T>(byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
                return genParser.ReadObject(bytes, maxSize, out bytesRead);

            T? value = default;
            parser.ReadObject(bytes, maxSize, out bytesRead, __makeref(value));
            return value;
        }

        if (maxSize < 1)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

        if (*bytes == 0)
        {
            bytesRead = 1;
            return default;
        }

        ++bytes;
        --maxSize;

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

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<int, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<uint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<byte, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<sbyte, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<short, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<ushort, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<long, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<ulong, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<nint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<nuint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<char, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                return Unsafe.As<bool, T>(ref val);
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                {
                    bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<bool, T>(ref val);
                }

                case TypeCode.Char:
                {
                    char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<char, T>(ref val);
                }

                case TypeCode.SByte:
                {
                    sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<sbyte, T>(ref val);
                }

                case TypeCode.Byte:
                {
                    byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<byte, T>(ref val);
                }

                case TypeCode.Int16:
                {
                    short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<short, T>(ref val);
                }

                case TypeCode.UInt16:
                {
                    ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<ushort, T>(ref val);
                }

                case TypeCode.Int32:
                {
                    int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<int, T>(ref val);
                }

                case TypeCode.UInt32:
                {
                    uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<uint, T>(ref val);
                }

                case TypeCode.Int64:
                {
                    long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<long, T>(ref val);
                }

                case TypeCode.UInt64:
                {
                    ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<ulong, T>(ref val);
                }

                case LegacyEnumCache<T>.NativeInt:
                {
                    nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<nint, T>(ref val);
                }

                case LegacyEnumCache<T>.NativeUInt:
                {
                    nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<nuint, T>(ref val);
                }
            }
#endif
        }

        bytesRead = 1; // not reached
        ThrowNoParserFound(type);
        return default!;
    }

    /// <inheritdoc />
    public unsafe void ReadNullable<T>(TypedReference refOut, byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        Type type = typeof(T?);
        if (_parsers.TryGetValue(type, out IBinaryTypeParser? parser))
        {
            if (parser is IBinaryTypeParser<T?> genParser)
                __refvalue(refOut, T?) = genParser.ReadObject(bytes, maxSize, out bytesRead);
            else
                parser.ReadObject(bytes, maxSize, out bytesRead, refOut);
            return;
        }

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

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<int, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<uint, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<byte, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<sbyte, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<short, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<ushort, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<long, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<ulong, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<nint, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<nuint, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<char, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<bool, T>(ref val);
                return;
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                {
                    bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<bool, T>(ref val);
                    return;
                }

                case TypeCode.Char:
                {
                    char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<char, T>(ref val);
                    return;
                }

                case TypeCode.SByte:
                {
                    sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<sbyte, T>(ref val);
                    return;
                }

                case TypeCode.Byte:
                {
                    byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<byte, T>(ref val);
                    return;
                }

                case TypeCode.Int16:
                {
                    short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<short, T>(ref val);
                    return;
                }

                case TypeCode.UInt16:
                {
                    ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<ushort, T>(ref val);
                    return;
                }

                case TypeCode.Int32:
                {
                    int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<int, T>(ref val);
                    return;
                }

                case TypeCode.UInt32:
                {
                    uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<uint, T>(ref val);
                    return;
                }

                case TypeCode.Int64:
                {
                    long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<long, T>(ref val);
                    return;
                }

                case TypeCode.UInt64:
                {
                    ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<ulong, T>(ref val);
                    return;
                }

                case LegacyEnumCache<T>.NativeInt:
                {
                    nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<nint, T>(ref val);
                    return;
                }

                case LegacyEnumCache<T>.NativeUInt:
                {
                    nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<nuint, T>(ref val);
                    return;
                }
            }
#endif
        }

        bytesRead = 1;
        ThrowNoParserFound(type);
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

        if (type.IsValueType)
        {
            if (typeof(T).IsEnum)
            {
#if NET8_0_OR_GREATER
                // this gets optimized away
                if (typeof(T).GetEnumUnderlyingType() == typeof(int))
                {
                    int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<int, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
                {
                    uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<uint, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
                {
                    byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<byte, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
                {
                    sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<sbyte, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(short))
                {
                    short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<short, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
                {
                    ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<ushort, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(long))
                {
                    long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<long, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
                {
                    ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<ulong, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
                {
                    nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<nint, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
                {
                    nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<nuint, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(char))
                {
                    char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<char, T>(ref val);
                }
                if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
                {
                    bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                    return Unsafe.As<bool, T>(ref val);
                }
#else
                switch (LegacyEnumCache<T>.UnderlyingType)
                {
                    case TypeCode.Boolean:
                    {
                        bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<bool, T>(ref val);
                    }

                    case TypeCode.Char:
                    {
                        char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<char, T>(ref val);
                    }

                    case TypeCode.SByte:
                    {
                        sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<sbyte, T>(ref val);
                    }

                    case TypeCode.Byte:
                    {
                        byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<byte, T>(ref val);
                    }

                    case TypeCode.Int16:
                    {
                        short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<short, T>(ref val);
                    }

                    case TypeCode.UInt16:
                    {
                        ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<ushort, T>(ref val);
                    }

                    case TypeCode.Int32:
                    {
                        int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<int, T>(ref val);
                    }

                    case TypeCode.UInt32:
                    {
                        uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<uint, T>(ref val);
                    }

                    case TypeCode.Int64:
                    {
                        long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<long, T>(ref val);
                    }

                    case TypeCode.UInt64:
                    {
                        ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<ulong, T>(ref val);
                    }

                    case LegacyEnumCache<T>.NativeInt:
                    {
                        nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<nint, T>(ref val);
                    }

                    case LegacyEnumCache<T>.NativeUInt:
                    {
                        nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                        return Unsafe.As<nuint, T>(ref val);
                    }
                }
#endif
            }

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
        
        bytesRead = 0;
        ThrowNoParserFound(type);
        return default!;
    }

    /// <inheritdoc />
    public unsafe TSerializable ReadSerializableObject<TSerializable>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable
    {
        TSerializable s;
        if (typeof(TSerializable).IsValueType)
        {
            s = default!;
        }
        else
        {
            CreateSerializable(out s);
        }

        int size = ReadSerializableSizeFromBuffer(bytes, maxSize, stream: false, checkError: true);
        if (size == int.MinValue)
        {
            bytesRead = sizeof(int);
            return default!;
        }

        int read = s.Read(new Span<byte>(bytes + sizeof(int), size), this);
        if (read < 0)
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };
        }

        bytesRead = size + sizeof(int);
        return s;
    }

    /// <inheritdoc />
    public unsafe IRpcSerializable ReadSerializableObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
    {
        CreateSerializable(type, out IRpcSerializable s);

        int size = ReadSerializableSizeFromBuffer(bytes, maxSize, stream: false, checkError: true);
        if (size == int.MinValue)
        {
            bytesRead = sizeof(int);
            return default!;
        }

        int read = s.Read(new Span<byte>(bytes + sizeof(int), size), this);
        if (read < 0)
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };
        }

        bytesRead = size + sizeof(int);
        return s;
    }

    /// <inheritdoc />
    public unsafe TSerializable[]? ReadSerializableObjects<TSerializable>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable
    {
        uint index = 0;
        if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
        {
            bytesRead = (int)index;
            return null;
        }

        if (length == 0)
        {
            bytesRead = (int)index;
            return Array.Empty<TSerializable>();
        }

        index += sizeof(int);

        Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

        TSerializable[] array = new TSerializable[length];
        for (int i = 0; i < length; ++i)
        {
            ref TSerializable s = ref array[i];

            int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
            index += sizeof(int);
            if (size == int.MinValue)
            {
                continue;
            }

            Span<byte> span = new Span<byte>(bytes + index, size);

            if (!typeof(TSerializable).IsValueType)
                CreateSerializable(out s);

            int read = s.Read(span, this);
            if (read < 0)
                throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

            index += (uint)size;
        }

        bytesRead = (int)index;
        return array;
    }

    /// <inheritdoc />
    public unsafe TCollectionType? ReadSerializableObjects<TSerializable, TCollectionType>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable
    {
        Type collectionType = typeof(TCollectionType);
        if (collectionType.IsValueType)
        {
            if (collectionType == typeof(ArraySegment<TSerializable?>))
            {
                TSerializable?[]? arr = ReadSerializableObjects<TSerializable>(bytes, maxSize, out bytesRead);
                ArraySegment<TSerializable?> seg = arr == null ? default : new ArraySegment<TSerializable?>(arr, 0, arr.Length);
                return Unsafe.As<ArraySegment<TSerializable?>, TCollectionType>(ref seg);
            }

            if (collectionType == typeof(Memory<TSerializable?>))
            {
                TSerializable?[]? arr = ReadSerializableObjects<TSerializable>(bytes, maxSize, out bytesRead);
                Memory<TSerializable?> seg = arr == null ? default : new Memory<TSerializable?>(arr, 0, arr.Length);
                return Unsafe.As<Memory<TSerializable?>, TCollectionType>(ref seg);
            }

            if (collectionType == typeof(ReadOnlyMemory<TSerializable?>))
            {
                TSerializable?[]? arr = ReadSerializableObjects<TSerializable>(bytes, maxSize, out bytesRead);
                ReadOnlyMemory<TSerializable?> seg = arr == null ? default : new ReadOnlyMemory<TSerializable?>(arr, 0, arr.Length);
                return Unsafe.As<ReadOnlyMemory<TSerializable?>, TCollectionType>(ref seg);
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType.IsArray)
        {
            if (collectionType == typeof(TSerializable?[]))
            {
                return (TCollectionType?)(object?)ReadSerializableObjects<TSerializable>(bytes, maxSize, out bytesRead);
            }

            Type elementType = collectionType.GetElementType()!;
            if (elementType.IsAssignableFrom(typeof(TSerializable)))
            {
                uint index = 0;
                if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
                {
                    bytesRead = (int)index;
                    return default;
                }

                if (length == 0)
                {
                    bytesRead = (int)index;
                    return (TCollectionType)(object)Array.CreateInstance(elementType, 0);
                }

                index += sizeof(int);

                Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

                object?[] array = (object?[])Array.CreateInstance(elementType, length);
                for (int i = 0; i < length; ++i)
                {
                    int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
                    index += sizeof(int);
                    if (size == int.MinValue)
                    {
                        continue;
                    }

                    Span<byte> span = new Span<byte>(bytes + index, size);

                    TSerializable s = default!;
                    if (!typeof(TSerializable).IsValueType)
                        CreateSerializable(out s);

                    int read = s.Read(span, this);
                    if (read < 0)
                        throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

                    array[i] = s;
                    index += (uint)size;
                }

                bytesRead = (int)index;
                return (TCollectionType?)(object?)array;
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType.IsInterface)
        {
            if (collectionType.IsAssignableFrom(typeof(IReadOnlyCollection<TSerializable>))
                || collectionType.IsAssignableFrom(typeof(IReadOnlyList<TSerializable>))
                || collectionType.IsAssignableFrom(typeof(IEnumerable<TSerializable>))
                || collectionType == typeof(IEnumerable)
               )
            {
                return (TCollectionType?)(object?)ReadSerializableObjects<TSerializable>(bytes, maxSize, out bytesRead);
            }

            if (collectionType == typeof(IEnumerator))
            {
                return (TCollectionType?)ReadSerializableObjects<TSerializable>(bytes, maxSize, out bytesRead)?.GetEnumerator();
            }

            if (collectionType.IsAssignableFrom(typeof(IEnumerator<TSerializable>)))
            {
                return (TCollectionType?)(object?)ReadSerializableList<TSerializable>(bytes, maxSize, out bytesRead)?.GetEnumerator();
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType == typeof(List<TSerializable>))
        {
            return (TCollectionType?)(object?)ReadSerializableList<TSerializable>(bytes, maxSize, out bytesRead);
        }

        bool coll1;
        if (collectionType.IsClass && ((coll1 = typeof(ICollection<TSerializable>).IsAssignableFrom(collectionType)) || typeof(IList).IsAssignableFrom(collectionType)))
        {
            uint index = 0;
            if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
            {
                bytesRead = (int)index;
                return default;
            }

            Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

            if (coll1)
            {
                ICollection<TSerializable> collection;
                try
                {
                    collection = (ICollection<TSerializable>)Activator.CreateInstance(collectionType, length);
                }
                catch
                {
                    try
                    {
                        collection = (ICollection<TSerializable>)Activator.CreateInstance(collectionType);
                    }
                    catch (Exception ex2)
                    {
                        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)), ex2);
                    }
                }

                if (length == 0)
                {
                    bytesRead = (int)index;
                    return (TCollectionType?)collection;
                }

                index += sizeof(int);

                Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

                for (int i = 0; i < length; ++i)
                {
                    int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
                    index += sizeof(int);
                    if (size == int.MinValue)
                    {
                        collection.Add(default!);
                        continue;
                    }

                    Span<byte> span = new Span<byte>(bytes + index, size);

                    TSerializable s = default!;
                    if (!typeof(TSerializable).IsValueType)
                        CreateSerializable(out s);

                    int read = s.Read(span, this);
                    if (read < 0)
                        throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

                    index += (uint)size;
                    collection.Add(s);
                }

                bytesRead = (int)index;
                return (TCollectionType?)collection;
            }

            IList list;
            try
            {
                list = (IList)Activator.CreateInstance(collectionType, length);
            }
            catch
            {
                try
                {
                    list = (IList)Activator.CreateInstance(collectionType);
                }
                catch (Exception ex2)
                {
                    throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)), ex2);
                }
            }

            if (length == 0)
            {
                bytesRead = (int)index;
                return (TCollectionType?)list;
            }

            index += sizeof(int);

            Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

            for (int i = 0; i < length; ++i)
            {
                int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
                index += sizeof(int);
                if (size == int.MinValue)
                {
                    list.Add(default(TSerializable));
                    continue;
                }

                Span<byte> span = new Span<byte>(bytes + index, size);

                TSerializable s = default!;
                if (!typeof(TSerializable).IsValueType)
                    CreateSerializable(out s);

                int read = s.Read(span, this);
                if (read < 0)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

                index += (uint)size;
                list.Add(s);
            }

            bytesRead = (int)index;
            return (TCollectionType?)list;
        }

        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
    }

    /// <inheritdoc />
    public unsafe object? ReadSerializableObjects(Type type, Type collectionType, byte* bytes, uint maxSize, out int bytesRead)
    {
        Type? genericTypeDef = collectionType.IsConstructedGenericType ? collectionType.GetGenericTypeDefinition() : null;
        if (collectionType.IsValueType)
        {
            if (genericTypeDef != null && (genericTypeDef == typeof(ArraySegment<>) || genericTypeDef == typeof(Memory<>) || genericTypeDef == typeof(ReadOnlyMemory<>)))
            {
                object? arr = ReadSerializableObjects(type, type.MakeArrayType(), bytes, maxSize, out bytesRead);
                return arr == null ? Activator.CreateInstance(collectionType) : Activator.CreateInstance(collectionType, arr);
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType.IsArray)
        {
            Type elementType = collectionType.GetElementType()!;
            if (!elementType.IsAssignableFrom(type))
            {
                throw new RpcInvalidParameterException(string.Format(
                    Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType,
                    Accessor.ExceptionFormatter.Format(collectionType)));
            }

            uint index = 0;
            if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
            {
                bytesRead = (int)index;
                return default;
            }

            if (length == 0)
            {
                bytesRead = (int)index;
                return Array.CreateInstance(elementType, 0);
            }

            index += sizeof(int);

            Configuration.AssertCanCreateArrayOfType(elementType, length, this);

            Array array = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; ++i)
            {
                int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
                index += sizeof(int);
                if (size == int.MinValue)
                {
                    continue;
                }

                Span<byte> span = new Span<byte>(bytes + index, size);

                CreateSerializable(out IRpcSerializable s);

                int read = s.Read(span, this);
                if (read < 0)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

                array.SetValue(s, i);
                index += (uint)size;
            }

            bytesRead = (int)index;
            return array;
        }

        if (collectionType.IsInterface)
        {
            if (collectionType == typeof(IEnumerable))
            {
                return ReadSerializableObjects(type, type.MakeArrayType(), bytes, maxSize, out bytesRead);
            }

            if (genericTypeDef == typeof(IReadOnlyCollection<>)
                || genericTypeDef == typeof(IReadOnlyList<>)
                || genericTypeDef == typeof(IEnumerable<>)
               )
            {
                Type baseType = genericTypeDef.MakeGenericType(type);
                if (collectionType.IsAssignableFrom(baseType))
                    return ReadSerializableObjects(type, type.MakeArrayType(), bytes, maxSize, out bytesRead);
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType.IsClass && typeof(IList).IsAssignableFrom(collectionType))
        {
            uint index = 0;
            if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
            {
                bytesRead = (int)index;
                return default;
            }

            Configuration.AssertCanCreateArrayOfType(type, length, this);

            IList list;
            try
            {
                list = (IList)Activator.CreateInstance(collectionType, length);
            }
            catch
            {
                try
                {
                    list = (IList)Activator.CreateInstance(collectionType);
                }
                catch (Exception ex2)
                {
                    throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)), ex2);
                }
            }

            if (length == 0)
            {
                bytesRead = (int)index;
                return list;
            }

            index += sizeof(int);

            Configuration.AssertCanCreateArrayOfType(type, length, this);

            for (int i = 0; i < length; ++i)
            {
                int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
                index += sizeof(int);
                if (size == int.MinValue)
                {
                    list.Add(CreateDefaultSerializable(type));
                    continue;
                }

                Span<byte> span = new Span<byte>(bytes + index, size);
                CreateSerializable(type, out IRpcSerializable s);

                int read = s.Read(span, this);
                if (read < 0)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

                index += (uint)size;
                list.Add(s);
            }

            bytesRead = (int)index;
            return list;
        }

        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
    }

    private unsafe List<TSerializable?>? ReadSerializableList<TSerializable>(byte* bytes, uint maxSize, out int bytesRead) where TSerializable : IRpcSerializable
    {
        uint index = 0;
        if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
        {
            bytesRead = (int)index;
            return null;
        }

        if (length == 0)
        {
            bytesRead = (int)index;
            return new List<TSerializable?>(0);
        }

        Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

        index += sizeof(int);

        List<TSerializable?> list = new List<TSerializable?>(length);

        for (int i = 0; i < length; ++i)
        {
            int size = ReadSerializableSizeFromBuffer(bytes + index, maxSize - index, stream: false, checkError: true);
            index += sizeof(int);
            if (size == int.MinValue)
            {
                list.Add(default(TSerializable));
                continue;
            }

            Span<byte> span = new Span<byte>(bytes + index, size);

            TSerializable s = default!;
            if (!typeof(TSerializable).IsValueType)
                CreateSerializable(out s);

            int read = s.Read(span, this);
            if (read < 0)
                throw new RpcParseException(Properties.Exceptions.RpcParseExceptionBufferRunOut) { ErrorCode = 1 };

            index += (uint)size;
            list.Add(s);
        }

        bytesRead = (int)index;
        return list;
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

        Type? underlyingType;
        if (type.IsEnum)
        {
            underlyingType = type.GetEnumUnderlyingType();
            // this gets optimized away
            if (underlyingType == typeof(int))
            {
                int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(uint))
            {
                uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(byte))
            {
                byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(short))
            {
                short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(long))
            {
                long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(nint))
            {
                nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(char))
            {
                char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(bool))
            {
                bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
        }

        underlyingType = Nullable.GetUnderlyingType(type);
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

        Type? underlyingType;
        if (objectType.IsEnum)
        {
            underlyingType = objectType.GetEnumUnderlyingType();
            if (underlyingType == typeof(int))
            {
                int val = ReadObject<int>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(uint))
            {
                uint val = ReadObject<uint>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(byte))
            {
                byte val = ReadObject<byte>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(short))
            {
                short val = ReadObject<short>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(long))
            {
                long val = ReadObject<long>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(nint))
            {
                nint val = ReadObject<nint>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(char))
            {
                char val = ReadObject<char>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(bool))
            {
                bool val = ReadObject<bool>(bytes, maxSize, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
        }

        underlyingType = Nullable.GetUnderlyingType(objectType);
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

        int b = stream.ReadByte();
        if (b < 0)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };

        if (b == 0)
        {
            bytesRead = 1;
            return null;
        }

        if (_parsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                T v = genParser.ReadObject(stream, out bytesRead);
                ++bytesRead;
                return v;
            }

            T value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            ++bytesRead;
            return value;
        }

        if (_primitiveParsers.TryGetValue(type, out parser))
        {
            if (parser is IBinaryTypeParser<T> genParser)
            {
                T v = genParser.ReadObject(stream, out bytesRead);
                ++bytesRead;
                return v;
            }

            T value = default;
            parser.ReadObject(stream, out bytesRead, __makeref(value));
            ++bytesRead;
            return value;
        }

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                int val = ReadObject<int>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<int, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                uint val = ReadObject<uint>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<uint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                byte val = ReadObject<byte>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<byte, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<sbyte, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                short val = ReadObject<short>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<short, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<ushort, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                long val = ReadObject<long>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<long, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<ulong, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                nint val = ReadObject<nint>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<nint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<nuint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                char val = ReadObject<char>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<char, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                bool val = ReadObject<bool>(stream, out bytesRead);
                ++bytesRead;
                return Unsafe.As<bool, T>(ref val);
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                {
                    bool val = ReadObject<bool>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<bool, T>(ref val);
                }

                case TypeCode.Char:
                {
                    char val = ReadObject<char>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<char, T>(ref val);
                }

                case TypeCode.SByte:
                {
                    sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<sbyte, T>(ref val);
                }

                case TypeCode.Byte:
                {
                    byte val = ReadObject<byte>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<byte, T>(ref val);
                }

                case TypeCode.Int16:
                {
                    short val = ReadObject<short>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<short, T>(ref val);
                }

                case TypeCode.UInt16:
                {
                    ushort val = ReadObject<ushort>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<ushort, T>(ref val);
                }

                case TypeCode.Int32:
                {
                    int val = ReadObject<int>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<int, T>(ref val);
                }

                case TypeCode.UInt32:
                {
                    uint val = ReadObject<uint>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<uint, T>(ref val);
                }

                case TypeCode.Int64:
                {
                    long val = ReadObject<long>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<long, T>(ref val);
                }

                case TypeCode.UInt64:
                {
                    ulong val = ReadObject<ulong>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<ulong, T>(ref val);
                }

                case LegacyEnumCache<T>.NativeInt:
                {
                    nint val = ReadObject<nint>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<nint, T>(ref val);
                }

                case LegacyEnumCache<T>.NativeUInt:
                {
                    nuint val = ReadObject<nuint>(stream, out bytesRead);
                    ++bytesRead;
                    return Unsafe.As<nuint, T>(ref val);
                }
            }
#endif
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

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                int val = ReadObject<int>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<int, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                uint val = ReadObject<uint>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<uint, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                byte val = ReadObject<byte>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<byte, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<sbyte, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                short val = ReadObject<short>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<short, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<ushort, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                long val = ReadObject<long>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<long, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<ulong, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                nint val = ReadObject<nint>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<nint, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<nuint, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                char val = ReadObject<char>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<char, T>(ref val);
                return;
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                bool val = ReadObject<bool>(stream, out bytesRead);
                ++bytesRead;
                __refvalue(refOut, T?) = Unsafe.As<bool, T>(ref val);
                return;
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                {
                    bool val = ReadObject<bool>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<bool, T>(ref val);
                    return;
                }

                case TypeCode.Char:
                {
                    char val = ReadObject<char>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<char, T>(ref val);
                    return;
                }

                case TypeCode.SByte:
                {
                    sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<sbyte, T>(ref val);
                    return;
                }

                case TypeCode.Byte:
                {
                    byte val = ReadObject<byte>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<byte, T>(ref val);
                    return;
                }

                case TypeCode.Int16:
                {
                    short val = ReadObject<short>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<short, T>(ref val);
                    return;
                }

                case TypeCode.UInt16:
                {
                    ushort val = ReadObject<ushort>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<ushort, T>(ref val);
                    return;
                }

                case TypeCode.Int32:
                {
                    int val = ReadObject<int>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<int, T>(ref val);
                    return;
                }

                case TypeCode.UInt32:
                {
                    uint val = ReadObject<uint>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<uint, T>(ref val);
                    return;
                }

                case TypeCode.Int64:
                {
                    long val = ReadObject<long>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<long, T>(ref val);
                    return;
                }

                case TypeCode.UInt64:
                {
                    ulong val = ReadObject<ulong>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<ulong, T>(ref val);
                    return;
                }

                case LegacyEnumCache<T>.NativeInt:
                {
                    nint val = ReadObject<nint>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<nint, T>(ref val);
                    return;
                }

                case LegacyEnumCache<T>.NativeUInt:
                {
                    nuint val = ReadObject<nuint>(stream, out bytesRead);
                    ++bytesRead;
                    __refvalue(refOut, T?) = Unsafe.As<nuint, T>(ref val);
                    return;
                }
            }
#endif
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

        if (typeof(T).IsEnum)
        {
#if NET8_0_OR_GREATER
            // this gets optimized away
            if (typeof(T).GetEnumUnderlyingType() == typeof(int))
            {
                int val = ReadObject<int>(stream, out bytesRead);
                return Unsafe.As<int, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(uint))
            {
                uint val = ReadObject<uint>(stream, out bytesRead);
                return Unsafe.As<uint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(byte))
            {
                byte val = ReadObject<byte>(stream, out bytesRead);
                return Unsafe.As<byte, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                return Unsafe.As<sbyte, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(short))
            {
                short val = ReadObject<short>(stream, out bytesRead);
                return Unsafe.As<short, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(stream, out bytesRead);
                return Unsafe.As<ushort, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(long))
            {
                long val = ReadObject<long>(stream, out bytesRead);
                return Unsafe.As<long, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(stream, out bytesRead);
                return Unsafe.As<ulong, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nint))
            {
                nint val = ReadObject<nint>(stream, out bytesRead);
                return Unsafe.As<nint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(stream, out bytesRead);
                return Unsafe.As<nuint, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(char))
            {
                char val = ReadObject<char>(stream, out bytesRead);
                return Unsafe.As<char, T>(ref val);
            }
            if (typeof(T).GetEnumUnderlyingType() == typeof(bool))
            {
                bool val = ReadObject<bool>(stream, out bytesRead);
                return Unsafe.As<bool, T>(ref val);
            }
#else
            switch (LegacyEnumCache<T>.UnderlyingType)
            {
                case TypeCode.Boolean:
                {
                    bool val = ReadObject<bool>(stream, out bytesRead);
                    return Unsafe.As<bool, T>(ref val);
                }

                case TypeCode.Char:
                {
                    char val = ReadObject<char>(stream, out bytesRead);
                    return Unsafe.As<char, T>(ref val);
                }

                case TypeCode.SByte:
                {
                    sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                    return Unsafe.As<sbyte, T>(ref val);
                }

                case TypeCode.Byte:
                {
                    byte val = ReadObject<byte>(stream, out bytesRead);
                    return Unsafe.As<byte, T>(ref val);
                }

                case TypeCode.Int16:
                {
                    short val = ReadObject<short>(stream, out bytesRead);
                    return Unsafe.As<short, T>(ref val);
                }

                case TypeCode.UInt16:
                {
                    ushort val = ReadObject<ushort>(stream, out bytesRead);
                    return Unsafe.As<ushort, T>(ref val);
                }

                case TypeCode.Int32:
                {
                    int val = ReadObject<int>(stream, out bytesRead);
                    return Unsafe.As<int, T>(ref val);
                }

                case TypeCode.UInt32:
                {
                    uint val = ReadObject<uint>(stream, out bytesRead);
                    return Unsafe.As<uint, T>(ref val);
                }

                case TypeCode.Int64:
                {
                    long val = ReadObject<long>(stream, out bytesRead);
                    return Unsafe.As<long, T>(ref val);
                }

                case TypeCode.UInt64:
                {
                    ulong val = ReadObject<ulong>(stream, out bytesRead);
                    return Unsafe.As<ulong, T>(ref val);
                }

                case LegacyEnumCache<T>.NativeInt:
                {
                    nint val = ReadObject<nint>(stream, out bytesRead);
                    return Unsafe.As<nint, T>(ref val);
                }

                case LegacyEnumCache<T>.NativeUInt:
                {
                    nuint val = ReadObject<nuint>(stream, out bytesRead);
                    return Unsafe.As<nuint, T>(ref val);
                }
            }
#endif
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
    public TSerializable ReadSerializableObject<TSerializable>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable
    {
        TSerializable s;
        if (typeof(TSerializable).IsValueType)
        {
            s = default!;
        }
        else
        {
            CreateSerializable(out s);
        }

        int size = ReadSerializableSizeFromStream(stream);
        if (size == int.MinValue)
        {
            bytesRead = sizeof(int);
            return default!;
        }

        int read;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        scoped Span<byte> buffer = size > Configuration.MaximumStackAllocationSize
            ? new byte[size]
            : stackalloc byte[size];
        int readCount = stream.Read(buffer);
#else
        byte[] buffer = size > MaxArrayPoolSize
            ? new byte[size]
            : ArrayPool.Rent(size);
        try
        {
            int readCount = stream.Read(buffer, 0, size);
#endif

        if (readCount < size)
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
        }

        read = s.Read(buffer, this);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        }
        finally
        {
            if (size <= MaxArrayPoolSize)
                ArrayPool.Return(buffer);
        }
#endif

        if (read < 0)
        {
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
        }

        bytesRead = size + sizeof(int);
        return s;
    }

    /// <inheritdoc />
    public TSerializable?[]? ReadSerializableObjects<TSerializable>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable
    {
        if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
        {
            return null;
        }

        if (length == 0)
        {
            return Array.Empty<TSerializable?>();
        }

        int maxSize = ReadSerializableSizeFromStream(stream);
        if (maxSize < 0)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
        bytesRead += sizeof(int);

        Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

        TSerializable?[] array = new TSerializable?[length];
        maxSize = Math.Max(maxSize, sizeof(int));
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        scoped Span<byte> buffer = maxSize > Configuration.MaximumStackAllocationSize
            ? new byte[maxSize]
            : stackalloc byte[maxSize];
#else
        byte[] buffer = maxSize > MaxArrayPoolSize
            ? new byte[maxSize]
            : ArrayPool.Rent(maxSize);
        try
        {
#endif
        for (int i = 0; i < length; ++i)
        {
            int size = ReadSerializableSizeFromStreamToBuffer(stream, buffer, ref bytesRead);
            if (size == int.MinValue)
            {
                continue;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            Span<byte> span = buffer.Slice(0, size);
#else
            Span<byte> span = buffer.AsSpan(0, size);
#endif
            ref TSerializable s = ref array[i]!;

            if (!typeof(TSerializable).IsValueType)
                CreateSerializable(out s);

            int read = s.Read(span, this);
            if (read < 0)
            {
                throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
            }
        }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        }
        finally
        {
            if (maxSize <= MaxArrayPoolSize)
                ArrayPool.Return(buffer);
        }
#endif

        return array;
    }

    private static void CreateSerializable<TSerializable>(out TSerializable s) where TSerializable : IRpcSerializable
    {
        try
        {
            s = Activator.CreateInstance<TSerializable>();
        }
        catch (Exception ex)
        {
            ThrowNoParserFound(typeof(TSerializable), ex);
            throw; // wont be reached
        }
    }
    private static void CreateSerializable(Type type, out IRpcSerializable s)
    {
        try
        {
            object o = Activator.CreateInstance(type)!;
            if (o is not IRpcSerializable s2)
            {
                throw new ArgumentException(string.Format(Properties.Exceptions.ArgumentExpectedBaseType,
                    Accessor.Formatter.Format(typeof(IRpcSerializable))), nameof(type)
                );
            }

            s = s2;
        }
        catch (Exception ex)
        {
            ThrowNoParserFound(type, ex);
            throw; // wont be reached
        }
    }
    private static IRpcSerializable? CreateDefaultSerializable(Type type)
    {
        if (!type.IsValueType)
            return null!;
        try
        {
            object o = Activator.CreateInstance(type)!;
            if (o is not IRpcSerializable s2)
            {
                throw new ArgumentException(string.Format(Properties.Exceptions.ArgumentExpectedBaseType,
                    Accessor.Formatter.Format(typeof(IRpcSerializable))), nameof(type)
                );
            }

            return s2;
        }
        catch (Exception ex)
        {
            ThrowNoParserFound(type, ex);
            throw; // wont be reached
        }
    }

    /// <inheritdoc />
    public TCollectionType? ReadSerializableObjects<TSerializable, TCollectionType>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable
    {
        Type collectionType = typeof(TCollectionType);
        if (collectionType.IsValueType)
        {
            if (collectionType == typeof(ArraySegment<TSerializable?>))
            {
                TSerializable?[]? arr = ReadSerializableObjects<TSerializable>(stream, out bytesRead);
                ArraySegment<TSerializable?> seg = arr == null ? default : new ArraySegment<TSerializable?>(arr, 0, arr.Length);
                return Unsafe.As<ArraySegment<TSerializable?>, TCollectionType>(ref seg);
            }

            if (collectionType == typeof(Memory<TSerializable?>))
            {
                TSerializable?[]? arr = ReadSerializableObjects<TSerializable>(stream, out bytesRead);
                Memory<TSerializable?> seg = arr == null ? default : new Memory<TSerializable?>(arr, 0, arr.Length);
                return Unsafe.As<Memory<TSerializable?>, TCollectionType>(ref seg);
            }

            if (collectionType == typeof(ReadOnlyMemory<TSerializable?>))
            {
                TSerializable?[]? arr = ReadSerializableObjects<TSerializable>(stream, out bytesRead);
                ReadOnlyMemory<TSerializable?> seg = arr == null ? default : new ReadOnlyMemory<TSerializable?>(arr, 0, arr.Length);
                return Unsafe.As<ReadOnlyMemory<TSerializable?>, TCollectionType>(ref seg);
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType.IsArray)
        {
            if (collectionType == typeof(TSerializable?[]))
            {
                return (TCollectionType?)(object?)ReadSerializableObjects<TSerializable>(stream, out bytesRead);
            }

            Type elementType = collectionType.GetElementType()!;
            if (elementType.IsAssignableFrom(typeof(TSerializable)))
            {
                if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
                {
                    return default;
                }

                if (length == 0)
                {
                    return (TCollectionType)(object)Array.CreateInstance(elementType, 0);
                }

                int maxSize = ReadSerializableSizeFromStream(stream);
                if (maxSize < 0)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
                bytesRead += sizeof(int);

                Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

                object?[] array = (object?[])Array.CreateInstance(elementType, length);
                maxSize = Math.Max(maxSize, sizeof(int));
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                scoped Span<byte> buffer = maxSize > Configuration.MaximumStackAllocationSize
                    ? new byte[maxSize]
                    : stackalloc byte[maxSize];
#else
                byte[] buffer = maxSize > MaxArrayPoolSize
                    ? new byte[maxSize]
                    : ArrayPool.Rent(maxSize);
                try
                {
#endif
                for (int i = 0; i < length; ++i)
                {
                    int size = ReadSerializableSizeFromStreamToBuffer(stream, buffer, ref bytesRead);
                    if (size == int.MinValue)
                    {
                        continue;
                    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    Span<byte> span = buffer.Slice(0, size);
#else
                    Span<byte> span = buffer.AsSpan(0, size);
#endif
                    TSerializable s = default!;

                    if (!typeof(TSerializable).IsValueType)
                        CreateSerializable(out s);

                    int read = s.Read(span, this);
                    if (read < 0)
                    {
                        throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
                    }

                    array[i] = s;
                }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (maxSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif

                return (TCollectionType?)(object?)array;
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType.IsInterface)
        {
            if (collectionType.IsAssignableFrom(typeof(IReadOnlyCollection<TSerializable>))
                || collectionType.IsAssignableFrom(typeof(IReadOnlyList<TSerializable>))
                || collectionType.IsAssignableFrom(typeof(IEnumerable<TSerializable>))
                || collectionType == typeof(IEnumerable)
               )
            {
                return (TCollectionType?)(object?)ReadSerializableObjects<TSerializable>(stream, out bytesRead);
            }

            if (collectionType == typeof(IEnumerator))
            {
                return (TCollectionType?)ReadSerializableObjects<TSerializable>(stream, out bytesRead)?.GetEnumerator();
            }

            if (collectionType.IsAssignableFrom(typeof(IEnumerator<TSerializable>)))
            {
                return (TCollectionType?)(object?)ReadSerializableList<TSerializable>(stream, out bytesRead)?.GetEnumerator();
            }

            throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
        }

        if (collectionType == typeof(List<TSerializable?>))
        {
            return (TCollectionType?)(object?)ReadSerializableList<TSerializable>(stream, out bytesRead);
        }

        bool coll1;
        if (collectionType.IsClass && ((coll1 = typeof(ICollection<TSerializable?>).IsAssignableFrom(collectionType)) || typeof(IList).IsAssignableFrom(collectionType)))
        {
            if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
            {
                return default;
            }

            Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

            if (coll1)
            {
                ICollection<TSerializable?> collection;
                try
                {
                    collection = (ICollection<TSerializable?>)Activator.CreateInstance(collectionType, length);
                }
                catch
                {
                    try
                    {
                        collection = (ICollection<TSerializable?>)Activator.CreateInstance(collectionType);
                    }
                    catch (Exception ex2)
                    {
                        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)), ex2);
                    }
                }

                if (length == 0)
                {
                    return (TCollectionType?)collection;
                }

                int maxSize = ReadSerializableSizeFromStream(stream);
                if (maxSize < 0)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
                bytesRead += sizeof(int);

                Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

                maxSize = Math.Max(maxSize, sizeof(int));
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                scoped Span<byte> buffer = maxSize > Configuration.MaximumStackAllocationSize
                    ? new byte[maxSize]
                    : stackalloc byte[maxSize];
#else
                byte[] buffer = maxSize > MaxArrayPoolSize
                    ? new byte[maxSize]
                    : ArrayPool.Rent(maxSize);
                try
                {
#endif
                for (int i = 0; i < length; ++i)
                {
                    int size = ReadSerializableSizeFromStreamToBuffer(stream, buffer, ref bytesRead);
                    if (size == int.MinValue)
                    {
                        collection.Add(default);
                        continue;
                    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    Span<byte> span = buffer.Slice(0, size);
#else
                    Span<byte> span = buffer.AsSpan(0, size);
#endif
                    TSerializable s = default!;

                    if (!typeof(TSerializable).IsValueType)
                        CreateSerializable(out s);

                    int read = s.Read(span, this);
                    if (read < 0)
                    {
                        throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
                    }

                    collection.Add(s);
                }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (maxSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif

                return (TCollectionType?)collection;
            }
            else
            {
                IList list;
                try
                {
                    list = (IList)Activator.CreateInstance(collectionType, length);
                }
                catch
                {
                    try
                    {
                        list = (IList)Activator.CreateInstance(collectionType);
                    }
                    catch (Exception ex2)
                    {
                        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)), ex2);
                    }
                }
                
                if (length == 0)
                {
                    return (TCollectionType?)list;
                }

                int maxSize = ReadSerializableSizeFromStream(stream);
                if (maxSize < 0)
                    throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
                bytesRead += sizeof(int);

                Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

                maxSize = Math.Max(maxSize, sizeof(int));
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                scoped Span<byte> buffer = maxSize > Configuration.MaximumStackAllocationSize
                    ? new byte[maxSize]
                    : stackalloc byte[maxSize];
#else
                byte[] buffer = maxSize > MaxArrayPoolSize
                    ? new byte[maxSize]
                    : ArrayPool.Rent(maxSize);
                try
                {
#endif
                for (int i = 0; i < length; ++i)
                {
                    int size = ReadSerializableSizeFromStreamToBuffer(stream, buffer, ref bytesRead);
                    if (size == int.MinValue)
                    {
                        list.Add(default(TSerializable));
                        continue;
                    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                    Span<byte> span = buffer.Slice(0, size);
#else
                    Span<byte> span = buffer.AsSpan(0, size);
#endif
                    TSerializable s = default!;

                    if (!typeof(TSerializable).IsValueType)
                        CreateSerializable(out s);

                    int read = s.Read(span, this);
                    if (read < 0)
                    {
                        throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
                    }

                    list.Add(s);
                }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                }
                finally
                {
                    if (maxSize <= MaxArrayPoolSize)
                        ArrayPool.Return(buffer);
                }
#endif

                return (TCollectionType?)list;
            }
        }

        throw new RpcInvalidParameterException(string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInvalidCollectionType, Accessor.ExceptionFormatter.Format(collectionType)));
    }

    private List<TSerializable>? ReadSerializableList<TSerializable>(Stream stream, out int bytesRead) where TSerializable : IRpcSerializable
    {
        if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
        {
            return null;
        }

        if (length == 0)
        {
            return new List<TSerializable>(0);
        }

        int maxSize = ReadSerializableSizeFromStream(stream);
        if (maxSize < 0)
            throw new RpcParseException(Properties.Exceptions.RpcParseExceptionInvalidSerializableSize) { ErrorCode = 11 };
        bytesRead += sizeof(int);

        Configuration.AssertCanCreateArrayOfType(typeof(TSerializable), length, this);

        List<TSerializable> list = new List<TSerializable>(length);
        maxSize = Math.Max(maxSize, sizeof(int));
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        scoped Span<byte> buffer = maxSize > Configuration.MaximumStackAllocationSize
            ? new byte[maxSize]
            : stackalloc byte[maxSize];
#else
        byte[] buffer = maxSize > MaxArrayPoolSize
            ? new byte[maxSize]
            : ArrayPool.Rent(maxSize);
        try
        {
#endif
        for (int i = 0; i < length; ++i)
        {
            int size = ReadSerializableSizeFromStreamToBuffer(stream, buffer, ref bytesRead);
            if (size == int.MinValue)
            {
                list.Add(default(TSerializable));
                continue;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            Span<byte> span = buffer.Slice(0, size);
#else
            Span<byte> span = buffer.AsSpan(0, size);
#endif
            TSerializable s = default!;

            if (!typeof(TSerializable).IsValueType)
                CreateSerializable(out s);

            int read = s.Read(span, this);
            if (read < 0)
            {
                throw new RpcParseException(Properties.Exceptions.RpcParseExceptionStreamRunOut) { ErrorCode = 2 };
            }

            list.Add(s);
        }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        }
        finally
        {
            if (maxSize <= MaxArrayPoolSize)
                ArrayPool.Return(buffer);
        }
#endif

        return list;
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

        Type? underlyingType;
        if (type.IsEnum)
        {
            underlyingType = type.GetEnumUnderlyingType();
            if (underlyingType == typeof(int))
            {
                int val = ReadObject<int>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(uint))
            {
                uint val = ReadObject<uint>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(byte))
            {
                byte val = ReadObject<byte>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(short))
            {
                short val = ReadObject<short>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(long))
            {
                long val = ReadObject<long>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(nint))
            {
                nint val = ReadObject<nint>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(char))
            {
                char val = ReadObject<char>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
            if (underlyingType == typeof(bool))
            {
                bool val = ReadObject<bool>(stream, out bytesRead);
                AssignEnum(refValue, val, type);
                return;
            }
        }

        underlyingType = Nullable.GetUnderlyingType(type);
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

        Type? underlyingType;
        if (objectType.IsEnum)
        {
            underlyingType = objectType.GetEnumUnderlyingType();
            if (underlyingType == typeof(int))
            {
                int val = ReadObject<int>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(uint))
            {
                uint val = ReadObject<uint>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(byte))
            {
                byte val = ReadObject<byte>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(sbyte))
            {
                sbyte val = ReadObject<sbyte>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(short))
            {
                short val = ReadObject<short>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(ushort))
            {
                ushort val = ReadObject<ushort>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(long))
            {
                long val = ReadObject<long>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(ulong))
            {
                ulong val = ReadObject<ulong>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(nint))
            {
                nint val = ReadObject<nint>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(nuint))
            {
                nuint val = ReadObject<nuint>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(char))
            {
                char val = ReadObject<char>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
            if (underlyingType == typeof(bool))
            {
                bool val = ReadObject<bool>(stream, out bytesRead);
                return Enum.ToObject(objectType, val);
            }
        }

        underlyingType = Nullable.GetUnderlyingType(objectType);
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

    protected unsafe object ReadNullable(Type nullableType, Type underlyingType, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (_nullableReadBytes.TryGetValue(nullableType, out ReadNullableBytes? r))
        {
            return r(bytes, maxSize, out bytesRead);
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return _nullableReadBytes.GetOrAdd(nullableType,
            (_, underlyingType) => (ReadNullableBytes)MtdReadBoxedNullableBytes.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableBytes), this),
            underlyingType
        )(bytes, maxSize, out bytesRead);
#else
        return _nullableReadBytes.GetOrAdd(nullableType,
            _ => (ReadNullableBytes)MtdReadBoxedNullableBytes.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableBytes), this)
        )(bytes, maxSize, out bytesRead);
#endif
    }
    protected object ReadNullable(Type nullableType, Type underlyingType, Stream stream, out int bytesRead)
    {
        if (_nullableReadStream.TryGetValue(nullableType, out ReadNullableStream? r))
        {
            return r(stream, out bytesRead);
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return _nullableReadStream.GetOrAdd(nullableType,
            (_, underlyingType) => (ReadNullableStream)MtdReadBoxedNullableStream.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableStream), this),
            underlyingType
        )(stream, out bytesRead);
#else
        return _nullableReadStream.GetOrAdd(nullableType,
            _ => (ReadNullableStream)MtdReadBoxedNullableStream.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableStream), this)
        )(stream, out bytesRead);
#endif
    }
    protected unsafe void ReadNullable(TypedReference value, Type nullableType, Type underlyingType, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (_nullableReadBytesRefAny.TryGetValue(nullableType, out ReadNullableBytesRefAny? r))
        {
            r(value, bytes, maxSize, out bytesRead);
            return;
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        _nullableReadBytesRefAny.GetOrAdd(nullableType,
            (_, underlyingType) => (ReadNullableBytesRefAny)MtdReadBoxedNullableBytesRefAny.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableBytesRefAny), this)
            , underlyingType
        )(value, bytes, maxSize, out bytesRead);
#else
        _nullableReadBytesRefAny.GetOrAdd(nullableType,
            _ => (ReadNullableBytesRefAny)MtdReadBoxedNullableBytesRefAny.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableBytesRefAny), this)
        )(value, bytes, maxSize, out bytesRead);
#endif
    }
    protected void ReadNullable(TypedReference value, Type nullableType, Type underlyingType, Stream stream, out int bytesRead)
    {
        if (_nullableReadStreamRefAny.TryGetValue(nullableType, out ReadNullableStreamRefAny? r))
        {
            r(value, stream, out bytesRead);
            return;
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        _nullableReadStreamRefAny.GetOrAdd(nullableType,
            (_, underlyingType) => (ReadNullableStreamRefAny)MtdReadBoxedNullableStreamRefAny.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableStreamRefAny), this),
            underlyingType
        )(value, stream, out bytesRead);
#else
        _nullableReadStreamRefAny.GetOrAdd(nullableType,
            _ => (ReadNullableStreamRefAny)MtdReadBoxedNullableStreamRefAny.MakeGenericMethod(underlyingType).CreateDelegate(typeof(ReadNullableStreamRefAny), this)
        )(value, stream, out bytesRead);
#endif
    }
    protected bool GetNullableHasValue(TypedReference value)
    {
        Type type = __reftype(value);
        if (_getNullableHasValueByRefAny.TryGetValue(type, out NullableHasValueTypeRef? r))
        {
            return r(value);
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        NullableHasValueTypeRef getter = _getNullableHasValueByRefAny.GetOrAdd(type,
            static (nullableType, serializer) => (NullableHasValueTypeRef)MtdGetNullableHasValueRefAny.MakeGenericMethod([ Nullable.GetUnderlyingType(nullableType) ]).CreateDelegate(typeof(NullableHasValueTypeRef), serializer)
            , this
        );
#else
        NullableHasValueTypeRef getter = _getNullableHasValueByRefAny.GetOrAdd(type,
            nullableType => (NullableHasValueTypeRef)MtdGetNullableHasValueRefAny.MakeGenericMethod([ Nullable.GetUnderlyingType(nullableType) ]).CreateDelegate(typeof(NullableHasValueTypeRef), this)
        );
#endif
        return getter(value);
    }

    protected object GetNullableValue(TypedReference value)
    {
        Type type = __reftype(value);
        if (_getNullableValueByRefAny.TryGetValue(type, out NullableValueTypeRef? r))
        {
            return r(value);
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        NullableValueTypeRef getter = _getNullableValueByRefAny.GetOrAdd(type,
            static (nullableType, serializer) => (NullableValueTypeRef)MtdGetNullableValueRefAny.MakeGenericMethod([ Nullable.GetUnderlyingType(nullableType) ]).CreateDelegate(typeof(NullableValueTypeRef), serializer)
            , this
        );
#else
        NullableValueTypeRef getter = _getNullableValueByRefAny.GetOrAdd(type,
            nullableType => (NullableValueTypeRef)MtdGetNullableValueRefAny.MakeGenericMethod([ Nullable.GetUnderlyingType(nullableType) ]).CreateDelegate(typeof(NullableValueTypeRef), this)
        );
#endif

        return getter(value);
    }

    private void AssignEnum(TypedReference byref, object value, Type enumType)
    {
        if (_assignEnumRefAny.TryGetValue(enumType, out AssignEnumByRefAny? r))
        {
            r(byref, value);
            return;
        }

#if NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        _assignEnumRefAny.GetOrAdd(enumType,
            static (enumType, serializer) => (AssignEnumByRefAny)MtdAssignEnumRefAny.MakeGenericMethod([ enumType, enumType.GetEnumUnderlyingType() ]).CreateDelegate(typeof(AssignEnumByRefAny), serializer)
            , this
        )(byref, value);
#else
        _assignEnumRefAny.GetOrAdd(enumType,
            enumType => (AssignEnumByRefAny)MtdAssignEnumRefAny.MakeGenericMethod([ enumType, enumType.GetEnumUnderlyingType() ]).CreateDelegate(typeof(AssignEnumByRefAny), this)
        )(byref, value);
#endif
    }

    [UsedImplicitly]
    private bool GetNullableHasValueRefAny<T>(TypedReference byref) where T : struct
    {
        ref T? val = ref __refvalue(byref, T?);
        return val.HasValue;
    }

    [UsedImplicitly]
    private object GetNullableValueRefAny<T>(TypedReference byref) where T : struct
    {
        ref T? val = ref __refvalue(byref, T?);
        return val!.Value;
    }

    [UsedImplicitly]
    private unsafe object ReadBoxedNullableBytes<T>(byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        return ReadNullable<T>(bytes, maxSize, out bytesRead)!;
    }

    [UsedImplicitly]
    private object ReadBoxedNullableStream<T>(Stream stream, out int bytesRead) where T : struct
    {
        return ReadNullable<T>(stream, out bytesRead)!;
    }

    [UsedImplicitly]
    private unsafe void ReadBoxedNullableBytesRefAny<T>(TypedReference value, byte* bytes, uint maxSize, out int bytesRead) where T : struct
    {
        ReadNullable<T>(value, bytes, maxSize, out bytesRead);
    }

    [UsedImplicitly]
    private void ReadBoxedNullableStreamRefAny<T>(TypedReference value, Stream stream, out int bytesRead) where T : struct
    {
        ReadNullable<T>(value, stream, out bytesRead);
    }

    [UsedImplicitly]
    private void AssignEnumRefAny<TEnum, TValue>(TypedReference byref, object value)
    {
        TValue val = (TValue)value;
        __refvalue(byref, TEnum) = Unsafe.As<TValue, TEnum>(ref val);
    }

    private delegate bool NullableHasValueTypeRef(TypedReference value);
    private delegate object NullableValueTypeRef(TypedReference value);
    private unsafe delegate object ReadNullableBytes(byte* bytes, uint maxSize, out int bytesRead);
    private delegate object ReadNullableStream(Stream stream, out int bytesRead);
    private unsafe delegate void ReadNullableBytesRefAny(TypedReference value, byte* bytes, uint maxSize, out int bytesRead);
    private delegate void ReadNullableStreamRefAny(TypedReference value, Stream stream, out int bytesRead);
    private delegate void AssignEnumByRefAny(TypedReference byref, object value);

    protected struct SerializableTypeInfo
    {
        public int MinimumSize;
        public bool IsFixedSize;
    }
}