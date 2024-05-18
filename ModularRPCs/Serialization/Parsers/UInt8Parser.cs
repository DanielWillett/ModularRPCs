using DanielWillett.ModularRpcs.Exceptions;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UInt8Parser : BinaryTypeParser<byte>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(byte value, byte* bytes, uint maxSize)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UInt8Parser))) { ErrorCode = 1 };

        *bytes = value;
        return 1;
    }
    public override int WriteObject(byte value, Stream stream)
    {
        stream.WriteByte(value);
        return 1;
    }
    public override unsafe byte ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UInt8Parser))) { ErrorCode = 1 };

        bytesRead = 1;
        return *bytes;
    }
    public override byte ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UInt8Parser))) { ErrorCode = 2 };

        bytesRead = 1;
        return (byte)b;
    }
}
