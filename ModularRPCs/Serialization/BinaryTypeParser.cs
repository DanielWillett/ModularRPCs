using System;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;

/// <inheritdoc />
public abstract class BinaryTypeParser<T> : IBinaryTypeParser<T?>
{
    /// <inheritdoc />
    public abstract bool IsVariableSize { get; }

    /// <inheritdoc />
    public abstract int MinimumSize { get; }

    /// <inheritdoc />
    public virtual int GetSize(T? value)
    {
        if (IsVariableSize)
            throw new NotImplementedException(string.Format(Properties.Exceptions.BinaryTypeParserNotVariableSizeGetSizeNotImplemented, GetType().Name));

        return MinimumSize;
    }

    /// <inheritdoc />
    public abstract unsafe int WriteObject(T? value, byte* bytes, uint maxSize);

    /// <inheritdoc />
    public abstract int WriteObject(T? value, Stream stream);

    /// <inheritdoc />
    public abstract unsafe T? ReadObject(byte* bytes, uint maxSize, out int bytesRead);

    /// <inheritdoc />
    public abstract T? ReadObject(Stream stream, out int bytesRead);

    int IBinaryTypeParser.GetSize(TypedReference value)
    {
        return IsVariableSize ? GetSize(__refvalue(value, T)) : MinimumSize;
    }
    int IBinaryTypeParser.GetSize(object? value)
    {
        return IsVariableSize ? GetSize((T?)value) : MinimumSize;
    }
    unsafe int IBinaryTypeParser.WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        return WriteObject(__refvalue(value, T?), bytes, maxSize);
    }
    int IBinaryTypeParser.WriteObject(TypedReference value, Stream stream)
    {
        return WriteObject(__refvalue(value, T?), stream);
    }
    unsafe void IBinaryTypeParser.ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj)
    {
        __refvalue(outObj, T?) = ReadObject(bytes, maxSize, out bytesRead)!;
    }
    void IBinaryTypeParser.ReadObject(Stream stream, out int bytesRead, TypedReference outObj)
    {
        __refvalue(outObj, T?) = ReadObject(stream, out bytesRead)!;
    }
    unsafe int IBinaryTypeParser.WriteObject(object? value, byte* bytes, uint maxSize)
    {
        return WriteObject((T?)value, bytes, maxSize);
    }
    int IBinaryTypeParser.WriteObject(object? value, Stream stream)
    {
        return WriteObject((T?)value, stream);
    }
    unsafe object IBinaryTypeParser.ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
    {
        return ReadObject(bytes, maxSize, out bytesRead)!;
    }
    object IBinaryTypeParser.ReadObject(Type type, Stream stream, out int bytesRead)
    {
        return ReadObject(stream, out bytesRead)!;
    }
}
