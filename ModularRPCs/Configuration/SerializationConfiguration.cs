using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DanielWillett.ModularRpcs.Configuration;

/// <summary>
/// Configures the operation of an <see cref="IRpcSerializer"/>.
/// Making changes to this object after the serializer has been created produces undefined behavior.
/// </summary>
public class SerializationConfiguration
{
    internal bool Locked;
    private int _maximumGlobalArraySize = -1;
    private int _maximumStringLength = -1;
    private IDictionary<Type, int> _maximumArraySizes = new Dictionary<Type, int>();
    private int _maximumBufferSize = 4096;
    private int _maximumStackAllocationSize = 512;
    private Encoding _stringEncoding = Encoding.UTF8;

    /// <summary>
    /// The maximum collection size in elements that can be read by local deserializers before a <see cref="RpcParseException"/> is thrown. Any value less than zero implies an infinite limit.
    /// <para>
    /// To set the maximum size for a specific collection type, see <see cref="MaximumArraySizes"/>.
    /// </para>
    /// </summary>
    /// <remarks>This property is set to infinite (-1) by default.</remarks>
    public int MaximumGlobalArraySize
    {
        get => _maximumGlobalArraySize;
        set
        {
            lock (this)
            {
                if (Locked)
                    throw new InvalidOperationException(Properties.Exceptions.InvalidOperationExceptionConfigLocked);

                _maximumGlobalArraySize = value;
            }
        }
    }

    /// <summary>
    /// Override maximum collection size in characters for strings that can be read before a <see cref="RpcParseException"/> is thrown.
    /// <para>
    /// To set the value for all types, see <see cref="MaximumGlobalArraySize"/>.
    /// </para>
    /// </summary>
    /// <remarks>This property is set to fall back to <see cref="MaximumGlobalArraySize"/> (-1) by default.</remarks>
    public int MaximumStringLength
    {
        get => _maximumStringLength;
        set
        {
            lock (this)
            {
                if (Locked)
                    throw new InvalidOperationException(Properties.Exceptions.InvalidOperationExceptionConfigLocked);

                _maximumStringLength = value;
            }
        }
    }

    /// <summary>
    /// Override the maximum collection size in elements for specific types before a <see cref="RpcParseException"/> is thrown. Any value less than zero implies an infinite limit.
    /// <para>
    /// To set the value for all types, see <see cref="MaximumGlobalArraySize"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// By default there are no overrides, meaning all types will fall back to <see cref="MaximumGlobalArraySize"/>. The key of this dictionary should be the collection's element type, not the collection type itself.
    /// Example: for an integer collection the key would be <c>typeof(int)</c>.
    /// For <see cref="string"/>'s, use <see cref="MaximumStringLength"/> instead, but this dictionary can still be used for <see cref="string"/> collections.
    /// </remarks>
    public IDictionary<Type, int> MaximumArraySizes => _maximumArraySizes;

    /// <summary>
    /// The maximum size in bytes a temporary read or write buffer can be, especially when reading to or writing from streams.
    /// </summary>
    /// <remarks>This property is set to 4096 B by default.</remarks>
    public int MaximumBufferSize
    {
        get => _maximumBufferSize;
        set
        {
            lock (this)
            {
                if (Locked)
                    throw new InvalidOperationException(Properties.Exceptions.InvalidOperationExceptionConfigLocked);

                _maximumBufferSize = value;
            }
        }
    }

    /// <summary>
    /// The maximum size in bytes a temporary read or write buffer can be to be allocated on the stack (using <c>stackalloc</c>).
    /// </summary>
    /// <remarks>This property is set to 512 B by default.</remarks>
    public int MaximumStackAllocationSize
    {
        get => _maximumStackAllocationSize;
        set
        {
            lock (this)
            {
                if (Locked)
                    throw new InvalidOperationException(Properties.Exceptions.InvalidOperationExceptionConfigLocked);

                _maximumStackAllocationSize = value;
            }
        }
    }

    /// <summary>
    /// The encoding to use when reading or writing a <see cref="string"/>.
    /// </summary>
    /// <remarks>This property is set to <see cref="Encoding.UTF8"/> by default. BOM's are not used.</remarks>
    public Encoding StringEncoding
    {
        get => _stringEncoding;
        set
        {
            lock (this)
            {
                if (Locked)
                    throw new InvalidOperationException(Properties.Exceptions.InvalidOperationExceptionConfigLocked);

                _stringEncoding = value;
            }
        }
    }

    internal void Lock()
    {
        lock (this)
        {
            if (Locked)
                return;

            Locked = true;
            _maximumArraySizes = new ReadOnlyDictionary<Type, int>(new Dictionary<Type, int>(_maximumArraySizes));
        }
    }

    internal bool CanCreateArrayOfType(Type type, int length)
    {
        if (!MaximumArraySizes.TryGetValue(type, out int maxSize))
            maxSize = MaximumGlobalArraySize;

        return maxSize < 0 || maxSize >= length;
    }

    internal void AssertCanCreateArrayOfType(Type? type, int length, object parser)
    {
        int maxSize;
        if (type == null)
        {
            maxSize = MaximumStringLength;
            if (maxSize < 0)
                maxSize = MaximumGlobalArraySize;
        }
        else if (!MaximumArraySizes.TryGetValue(type, out maxSize))
        {
            if (Nullable.GetUnderlyingType(type) is not { } nullableUnderlyingType
                || !MaximumArraySizes.TryGetValue(nullableUnderlyingType, out maxSize))
            {
                maxSize = MaximumGlobalArraySize;
            }
        }

        if (maxSize < 0 || maxSize >= length)
            return;

        throw new RpcParseException(
            string.Format(
                Properties.Exceptions.RpcParseExceptionArrayTooLongIBinaryTypeParser,
                Accessor.ExceptionFormatter.Format(parser.GetType()),
                Accessor.ExceptionFormatter.Format(type == null ? typeof(string) : typeof(IEnumerable<>).MakeGenericType(type)),
                maxSize
        )) { ErrorCode = 10 };
    }
}