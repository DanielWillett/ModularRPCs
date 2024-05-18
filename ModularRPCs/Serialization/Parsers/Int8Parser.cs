using DanielWillett.ModularRpcs.Exceptions;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class Int8Parser : BinaryTypeParser<sbyte>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(sbyte value, byte* bytes, uint maxSize)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(Int8Parser))) { ErrorCode = 1 };

        *bytes = unchecked( (byte)value );
        return 1;
    }
    public override int WriteObject(sbyte value, Stream stream)
    {
        stream.WriteByte(unchecked( (byte)value ));
        return 1;
    }
    public override unsafe sbyte ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(Int8Parser))) { ErrorCode = 1 };

        bytesRead = 1;
        return unchecked( (sbyte)*bytes );
    }
    public override sbyte ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(Int8Parser))) { ErrorCode = 2 };

        bytesRead = 1;
        return unchecked( (sbyte)(byte)b );
    }
}