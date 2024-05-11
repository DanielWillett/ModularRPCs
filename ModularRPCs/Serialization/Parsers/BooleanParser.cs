using DanielWillett.ModularRpcs.Exceptions;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class BooleanParser : BinaryTypeParser<bool>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(bool value, byte* bytes, uint maxSize)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(BooleanParser))) { ErrorCode = 1 };

        *bytes = value ? (byte)1 : (byte)0;
        return 1;
    }
    public override int WriteObject(bool value, Stream stream)
    {
        stream.WriteByte(value ? (byte)1 : (byte)0);
        return 1;
    }
    public override unsafe bool ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOutIBinaryTypeParser, nameof(BooleanParser))) { ErrorCode = 1 };

        bytesRead = 1;
        return *bytes > 0;
    }
    public override bool ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOutIBinaryTypeParser, nameof(BooleanParser))) { ErrorCode = 2 };

        bytesRead = 1;
        return b > 0;
    }
}