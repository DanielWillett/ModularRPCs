using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using NUnit.Framework;
using System.IO;

namespace ModularRPCs.Test;

public class ParserTests
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestBooleanStream(bool value)
    {
        BooleanParser parser = new BooleanParser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(1));

        memStream.Seek(0, SeekOrigin.Begin);
        bool readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public unsafe void TestBooleanBytes(bool value)
    {
        BooleanParser parser = new BooleanParser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(1));

        bool readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public unsafe void TestBooleanBytesThrowsOutOfRangeError(bool value)
    {
        BooleanParser parser = new BooleanParser();

        byte* buffer = stackalloc byte[0];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 0));
    }

    [Test]
    [TestCase((sbyte)1)]
    [TestCase(sbyte.MaxValue)]
    [TestCase((sbyte)-3)]
    public void TestInt8Stream(sbyte value)
    {
        Int8Parser parser = new Int8Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(1));

        memStream.Seek(0, SeekOrigin.Begin);
        sbyte readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((sbyte)1)]
    [TestCase(sbyte.MaxValue)]
    [TestCase((sbyte)-3)]
    public unsafe void TestInt8Bytes(sbyte value)
    {
        Int8Parser parser = new Int8Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(1));

        sbyte readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((sbyte)1)]
    [TestCase(sbyte.MaxValue)]
    [TestCase((sbyte)-3)]
    public unsafe void TestInt8BytesThrowsOutOfRangeError(sbyte value)
    {
        Int8Parser parser = new Int8Parser();

        byte* buffer = stackalloc byte[0];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 0));
    }

    [Test]
    [TestCase((byte)1)]
    [TestCase(byte.MaxValue)]
    public void TestUInt8Stream(byte value)
    {
        UInt8Parser parser = new UInt8Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(1));

        memStream.Seek(0, SeekOrigin.Begin);
        byte readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((byte)1)]
    [TestCase(byte.MaxValue)]
    public unsafe void TestUInt8Bytes(byte value)
    {
        UInt8Parser parser = new UInt8Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(1));

        byte readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((byte)1)]
    [TestCase(byte.MaxValue)]
    public unsafe void TestUInt8BytesThrowsOutOfRangeError(byte value)
    {
        UInt8Parser parser = new UInt8Parser();

        byte* buffer = stackalloc byte[0];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 0));
    }

    [Test]
    [TestCase((ushort)1)]
    [TestCase(ushort.MaxValue)]
    public void TestUInt16Stream(ushort value)
    {
        UInt16Parser parser = new UInt16Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(2));

        memStream.Seek(0, SeekOrigin.Begin);
        ushort readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(2));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((ushort)1)]
    [TestCase(ushort.MaxValue)]
    public unsafe void TestUInt16Bytes(ushort value)
    {
        UInt16Parser parser = new UInt16Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(2));

        ushort readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(2));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((ushort)1)]
    [TestCase(ushort.MaxValue)]
    public unsafe void TestUInt16BytesThrowsOutOfRangeError(ushort value)
    {
        UInt16Parser parser = new UInt16Parser();

        byte* buffer = stackalloc byte[1];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 1));
    }

    [Test]
    [TestCase((short)1)]
    [TestCase(short.MaxValue)]
    [TestCase((short)-3)]
    public void TestInt16Stream(short value)
    {
        Int16Parser parser = new Int16Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(2));

        memStream.Seek(0, SeekOrigin.Begin);
        short readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(2));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((short)1)]
    [TestCase(short.MaxValue)]
    [TestCase((short)-3)]
    public unsafe void TestInt16Bytes(short value)
    {
        Int16Parser parser = new Int16Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(2));

        short readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(2));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((short)1)]
    [TestCase(short.MaxValue)]
    [TestCase((short)-3)]
    public unsafe void TestInt16BytesThrowsOutOfRangeError(short value)
    {
        Int16Parser parser = new Int16Parser();

        byte* buffer = stackalloc byte[1];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 1));
    }

    [Test]
    [TestCase((uint)1)]
    [TestCase(uint.MaxValue)]
    public void TestUInt32Stream(uint value)
    {
        UInt32Parser parser = new UInt32Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(4));

        memStream.Seek(0, SeekOrigin.Begin);
        uint readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((uint)1)]
    [TestCase(uint.MaxValue)]
    public unsafe void TestUInt32Bytes(uint value)
    {
        UInt32Parser parser = new UInt32Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(4));

        uint readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((uint)1)]
    [TestCase(uint.MaxValue)]
    public unsafe void TestUInt32BytesThrowsOutOfRangeError(uint value)
    {
        UInt32Parser parser = new UInt32Parser();

        byte* buffer = stackalloc byte[3];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 3));
    }

    [Test]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    [TestCase(-3)]
    public void TestInt32Stream(int value)
    {
        Int32Parser parser = new Int32Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(4));

        memStream.Seek(0, SeekOrigin.Begin);
        int readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    [TestCase(-3)]
    public unsafe void TestInt32Bytes(int value)
    {
        Int32Parser parser = new Int32Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(4));

        int readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    [TestCase(-3)]
    public unsafe void TestInt32BytesThrowsOutOfRangeError(int value)
    {
        Int32Parser parser = new Int32Parser();

        byte* buffer = stackalloc byte[3];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 3));
    }

    [Test]
    [TestCase((ulong)1)]
    [TestCase(ulong.MaxValue)]
    public void TestUInt64Stream(ulong value)
    {
        UInt64Parser parser = new UInt64Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(8));

        memStream.Seek(0, SeekOrigin.Begin);
        ulong readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((ulong)1)]
    [TestCase(ulong.MaxValue)]
    public unsafe void TestUInt64Bytes(ulong value)
    {
        UInt64Parser parser = new UInt64Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(8));

        ulong readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase((ulong)1)]
    [TestCase(ulong.MaxValue)]
    public unsafe void TestUInt64BytesThrowsOutOfRangeError(ulong value)
    {
        UInt64Parser parser = new UInt64Parser();

        byte* buffer = stackalloc byte[7];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 3));
    }

    [Test]
    [TestCase(1)]
    [TestCase(long.MaxValue)]
    [TestCase(-3)]
    public void TestInt64Stream(long value)
    {
        Int64Parser parser = new Int64Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(8));

        memStream.Seek(0, SeekOrigin.Begin);
        long readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1)]
    [TestCase(long.MaxValue)]
    [TestCase(-3)]
    public unsafe void TestInt64Bytes(long value)
    {
        Int64Parser parser = new Int64Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(8));

        long readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1)]
    [TestCase(long.MaxValue)]
    [TestCase(-3)]
    public unsafe void TestInt64BytesThrowsOutOfRangeError(long value)
    {
        Int64Parser parser = new Int64Parser();

        byte* buffer = stackalloc byte[3];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 3));
    }

    [Test]
    [TestCase((ulong)1)]
    [TestCase(ulong.MaxValue)]
    public void TestUIntPtrStream(ulong value)
    {
        UIntPtrParser parser = new UIntPtrParser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject((nuint)value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(8));

        memStream.Seek(0, SeekOrigin.Begin);
        nuint readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo((nuint)value));
    }

    [Test]
    [TestCase((ulong)1)]
    [TestCase(ulong.MaxValue)]
    public unsafe void TestUIntPtrBytes(ulong value)
    {
        UIntPtrParser parser = new UIntPtrParser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject((nuint)value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(8));

        nuint readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo((nuint)value));
    }

    [Test]
    [TestCase((ulong)1)]
    [TestCase(ulong.MaxValue)]
    public unsafe void TestUIntPtrBytesThrowsOutOfRangeError(ulong value)
    {
        UIntPtrParser parser = new UIntPtrParser();

        byte* buffer = stackalloc byte[7];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject((nuint)value, buffer, 3));
    }

    [Test]
    [TestCase(1)]
    [TestCase(long.MaxValue)]
    [TestCase(-3)]
    public void TestIntPtrStream(long value)
    {
        IntPtrParser parser = new IntPtrParser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject((nint)value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(8));

        memStream.Seek(0, SeekOrigin.Begin);
        nint readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo((nint)value));
    }

    [Test]
    [TestCase(1)]
    [TestCase(long.MaxValue)]
    [TestCase(-3)]
    public unsafe void TestIntPtrBytes(long value)
    {
        IntPtrParser parser = new IntPtrParser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject((nint)value, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(8));

        nint readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo((nint)value));
    }

    [Test]
    [TestCase(1)]
    [TestCase(long.MaxValue)]
    [TestCase(-3)]
    public unsafe void TestIntPtrBytesThrowsOutOfRangeError(long value)
    {
        IntPtrParser parser = new IntPtrParser();

        byte* buffer = stackalloc byte[3];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject((nint)value, buffer, 3));
    }
}