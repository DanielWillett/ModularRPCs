using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using NUnit.Framework;
using System;
using System.IO;

namespace ModularRPCs.Test;

public class ParserTests
{
    internal const int Buffer = 2;
    internal const int BufferSize = sizeof(int);

    internal static void WriteBuffer(Stream stream)
    {
        int sz = new Int32Parser().WriteObject(Buffer, stream);
        Assert.That(sz, Is.EqualTo(BufferSize));
    }

    internal static void CheckBuffer(Stream stream)
    {
        int buffer = new Int32Parser().ReadObject(stream, out int bytesRead);
        Assert.That(bytesRead, Is.EqualTo(BufferSize));
        Assert.That(buffer, Is.EqualTo(Buffer));
    }
    internal static unsafe void WriteBuffer(ref byte* ptr, ref uint maxSize)
    {
        int sz = new Int32Parser().WriteObject(Buffer, ptr, maxSize);
        Assert.That(sz, Is.EqualTo(BufferSize));
        ptr += sz;
        maxSize -= (uint)sz;
    }
    internal static unsafe void CheckBuffer(byte* ptr, uint maxSize)
    {
        int buffer = new Int32Parser().ReadObject(ptr - BufferSize, maxSize + BufferSize, out int bytesRead);
        Assert.That(bytesRead, Is.EqualTo(BufferSize));
        Assert.That(buffer, Is.EqualTo(Buffer));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestBooleanStream(bool value)
    {
        BooleanParser parser = new BooleanParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 1));

        memStream.Seek(0, SeekOrigin.Begin);

        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(1));

        CheckBuffer(buffer, maxSize);
        bool readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 1));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(1));

        CheckBuffer(buffer, maxSize);
        sbyte readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 1));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(1));

        CheckBuffer(buffer, maxSize);
        byte readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 2));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(2));

        CheckBuffer(buffer, maxSize);
        ushort readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 2));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(2));

        CheckBuffer(buffer, maxSize);
        short readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 4));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(4));

        CheckBuffer(buffer, maxSize);
        uint readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 4));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(4));

        CheckBuffer(buffer, maxSize);
        int readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        ulong readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        long readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject((nuint)value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject((nuint)value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        nuint readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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
        WriteBuffer(memStream);
        parser.WriteObject((nint)value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
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

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject((nint)value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        nint readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

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

#if NET5_0_OR_GREATER
    [Test]
    [TestCase(1f)]
    [TestCase(13f)]
    [TestCase(-9999f)]
    [TestCase(0f)]
    [TestCase(float.NaN)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.MaxValue)]
    [TestCase(float.MinValue)]
    public void TestHalfStream(float value)
    {
        Half half = (Half)value;

        HalfParser parser = new HalfParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(half, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 2));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        Half readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(2));
        Assert.That(readValue, Is.EqualTo(half));
    }

    [Test]
    [TestCase(1f)]
    [TestCase(13f)]
    [TestCase(-9999f)]
    [TestCase(0f)]
    [TestCase(float.NaN)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.MaxValue)]
    [TestCase(float.MinValue)]
    public unsafe void TestHalfBytes(float value)
    {
        Half half = (Half)value;

        HalfParser parser = new HalfParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject((Half)value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(2));

        CheckBuffer(buffer, maxSize);
        Half readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(2));
        Assert.That(readValue, Is.EqualTo(half));
    }

    [Test]
    [TestCase(1f)]
    [TestCase(13f)]
    [TestCase(-9999f)]
    [TestCase(0f)]
    [TestCase(float.NaN)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.MaxValue)]
    [TestCase(float.MinValue)]
    public unsafe void TestHalfBytesThrowsOutOfRangeError(float value)
    {
        Half half = (Half)value;

        HalfParser parser = new HalfParser();

        byte* buffer = stackalloc byte[1];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(half, buffer, 1));
    }
#endif

    [Test]
    [TestCase(1f)]
    [TestCase(13f)]
    [TestCase(-9999f)]
    [TestCase(0f)]
    [TestCase(float.NaN)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.MaxValue)]
    [TestCase(float.MinValue)]
    public void TestSingleStream(float value)
    {
        SingleParser parser = new SingleParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 4));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        float readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1f)]
    [TestCase(13f)]
    [TestCase(-9999f)]
    [TestCase(0f)]
    [TestCase(float.NaN)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.MaxValue)]
    [TestCase(float.MinValue)]
    public unsafe void TestSingleBytes(float value)
    {
        SingleParser parser = new SingleParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(4));

        CheckBuffer(buffer, maxSize);
        float readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1f)]
    [TestCase(13f)]
    [TestCase(-9999f)]
    [TestCase(0f)]
    [TestCase(float.NaN)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.MaxValue)]
    [TestCase(float.MinValue)]
    public unsafe void TestSingleBytesThrowsOutOfRangeError(float value)
    {
        SingleParser parser = new SingleParser();

        byte* buffer = stackalloc byte[3];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 3));
    }

    [Test]
    [TestCase(1d)]
    [TestCase(13d)]
    [TestCase(-9999d)]
    [TestCase(0d)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.MaxValue)]
    [TestCase(double.MinValue)]
    public void TestDoubleStream(double value)
    {
        DoubleParser parser = new DoubleParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        double readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1d)]
    [TestCase(13d)]
    [TestCase(-9999d)]
    [TestCase(0d)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.MaxValue)]
    [TestCase(double.MinValue)]
    public unsafe void TestDoubleBytes(double value)
    {
        DoubleParser parser = new DoubleParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(value, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        double readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(value));
    }

    [Test]
    [TestCase(1d)]
    [TestCase(13d)]
    [TestCase(-9999d)]
    [TestCase(0d)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.MaxValue)]
    [TestCase(double.MinValue)]
    public unsafe void TestDoubleBytesThrowsOutOfRangeError(double value)
    {
        DoubleParser parser = new DoubleParser();

        byte* buffer = stackalloc byte[7];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(value, buffer, 7));
    }

    [Test]
    [TestCase(1d)]
    [TestCase(13d)]
    [TestCase(-9999d)]
    [TestCase(0d)]
    public void TestDecimalStream(double value)
    {
        decimal v2 = new decimal(value);
        DecimalParser parser = new DecimalParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 16));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        decimal readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    [TestCase(1d)]
    [TestCase(13d)]
    [TestCase(-9999d)]
    [TestCase(0d)]
    public unsafe void TestDecimalBytes(double value)
    {
        decimal v2 = new decimal(value);
        DecimalParser parser = new DecimalParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(16));

        CheckBuffer(buffer, maxSize);
        decimal readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    [TestCase(1d)]
    [TestCase(13d)]
    [TestCase(-9999d)]
    [TestCase(0d)]
    public unsafe void TestDecimalBytesThrowsOutOfRangeError(double value)
    {
        decimal v2 = new decimal(value);
        DecimalParser parser = new DecimalParser();

        byte* buffer = stackalloc byte[15];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(v2, buffer, 15));
    }

    [Test]
    [TestCase("05/12/2024 05:44:05", DateTimeKind.Local)]
    [TestCase("01/01/0001 00:00:00", DateTimeKind.Unspecified)]
    [TestCase("12/31/9999 23:59:59", DateTimeKind.Utc)]
    public void TestDateTimeStream(string value, DateTimeKind kind)
    {
        DateTime dt = DateTime.Parse(value);
        dt = DateTime.SpecifyKind(dt, kind);

        DateTimeParser parser = new DateTimeParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(dt, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        DateTime readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(dt));
    }

    [Test]
    [TestCase("05/12/2024 05:44:05", DateTimeKind.Local)]
    [TestCase("01/01/0001 00:00:00", DateTimeKind.Unspecified)]
    [TestCase("12/31/9999 23:59:59", DateTimeKind.Utc)]
    public unsafe void TestDateTimeBytes(string value, DateTimeKind kind)
    {
        DateTime dt = DateTime.Parse(value);
        dt = DateTime.SpecifyKind(dt, kind);

        DateTimeParser parser = new DateTimeParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(dt, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        DateTime readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(dt));
    }

    [Test]
    [TestCase("05/12/2024 05:44:05", DateTimeKind.Local)]
    [TestCase("01/01/0001 00:00:00", DateTimeKind.Unspecified)]
    [TestCase("12/31/9999 23:59:59", DateTimeKind.Utc)]
    public unsafe void TestDateTimeBytesThrowsOutOfRangeError(string value, DateTimeKind kind)
    {
        DateTime dt = DateTime.Parse(value);
        dt = DateTime.SpecifyKind(dt, kind);

        DateTimeParser parser = new DateTimeParser();

        byte* buffer = stackalloc byte[7];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(dt, buffer, 7));
    }

    [Test]
    [TestCase("1.02:03:04.0050060")]
    [TestCase("4.01:06:07.1030000")]
    [TestCase("10675199.02:48:05.4775807")]
    [TestCase("-10675199.02:48:05.4775808")]
    public void TestTimeSpanStream(string value)
    {
        TimeSpan ts = TimeSpan.Parse(value);

        TimeSpanParser parser = new TimeSpanParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(ts, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        TimeSpan readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(ts));
    }

    [Test]
    [TestCase("1.02:03:04.0050060")]
    [TestCase("4.01:06:07.1030000")]
    [TestCase("10675199.02:48:05.4775807")]
    [TestCase("-10675199.02:48:05.4775808")]
    public unsafe void TestTimeSpanBytes(string value)
    {
        TimeSpan ts = TimeSpan.Parse(value);

        TimeSpanParser parser = new TimeSpanParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(ts, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        CheckBuffer(buffer, maxSize);
        TimeSpan readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(ts));
    }

    [Test]
    [TestCase("1.02:03:04.0050060")]
    [TestCase("4.01:06:07.1030000")]
    [TestCase("10675199.02:48:05.4775807")]
    [TestCase("-10675199.02:48:05.4775808")]
    public unsafe void TestTimeSpanBytesThrowsOutOfRangeError(string value)
    {
        TimeSpan ts = TimeSpan.Parse(value);

        TimeSpanParser parser = new TimeSpanParser();

        byte* buffer = stackalloc byte[7];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(ts, buffer, 7));
    }

    [Test]
    [TestCase("05/12/2024 05:50:59 +05:00")]
    [TestCase("01/01/0001 00:00:00 +00:00")]
    [TestCase("12/31/9999 23:59:59 +08:30")]
    public void TestDateTimeOffsetStream(string value)
    {
        DateTimeOffset dt = DateTimeOffset.Parse(value);

        DateTimeOffsetParser parser = new DateTimeOffsetParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(dt, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 10));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        DateTimeOffset readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(10));
        Assert.That(readValue, Is.EqualTo(dt));
    }

    [Test]
    [TestCase("05/12/2024 05:50:59 +05:00")]
    [TestCase("01/01/0001 00:00:00 +00:00")]
    [TestCase("12/31/9999 23:59:59 +08:30")]
    public unsafe void TestDateTimeOffsetBytes(string value)
    {
        DateTimeOffset dt = DateTimeOffset.Parse(value);

        DateTimeOffsetParser parser = new DateTimeOffsetParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(dt, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(10));

        CheckBuffer(buffer, maxSize);
        DateTimeOffset readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(10));
        Assert.That(readValue, Is.EqualTo(dt));
    }

    [Test]
    [TestCase("05/12/2024 05:50:59 +05:00")]
    [TestCase("01/01/0001 00:00:00 +00:00")]
    [TestCase("12/31/9999 23:59:59 +08:30")]
    public unsafe void TestDateTimeOffsetBytesThrowsOutOfRangeError(string value)
    {
        DateTimeOffset dt = DateTimeOffset.Parse(value);

        DateTimeOffsetParser parser = new DateTimeOffsetParser();

        byte* buffer = stackalloc byte[9];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(dt, buffer, 9));
    }

    [Test]
    [TestCase("8f0641ee281347a1aeae5a384dfa9f67")]
    [TestCase("182c56942f6f465d8e356b983b0a6dc7")]
    [TestCase("c5078ac52f6e4397a5873398002a84a8")]
    [TestCase("00000000000000000000000000000000")]
    [TestCase("ffffffffffffffffffffffffffffffff")]
    public void TestGuidStream(string value)
    {
        Guid guid = Guid.Parse(value);

        GuidParser parser = new GuidParser();
        using Stream memStream = new MemoryStream();
        WriteBuffer(memStream);
        parser.WriteObject(guid, memStream);

        Assert.That(memStream.Length, Is.EqualTo(BufferSize + 16));

        memStream.Seek(0, SeekOrigin.Begin);
        
        CheckBuffer(memStream);
        Guid readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(readValue, Is.EqualTo(guid));
    }

    [Test]
    [TestCase("8f0641ee281347a1aeae5a384dfa9f67")]
    [TestCase("182c56942f6f465d8e356b983b0a6dc7")]
    [TestCase("c5078ac52f6e4397a5873398002a84a8")]
    [TestCase("00000000000000000000000000000000")]
    [TestCase("ffffffffffffffffffffffffffffffff")]
    public unsafe void TestGuidBytes(string value)
    {
        Guid guid = Guid.Parse(value);

        GuidParser parser = new GuidParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(guid, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(16));

        CheckBuffer(buffer, maxSize);
        Guid readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(readValue, Is.EqualTo(guid));
    }

    [Test]
    [TestCase("8f0641ee281347a1aeae5a384dfa9f67")]
    [TestCase("182c56942f6f465d8e356b983b0a6dc7")]
    [TestCase("c5078ac52f6e4397a5873398002a84a8")]
    [TestCase("00000000000000000000000000000000")]
    [TestCase("ffffffffffffffffffffffffffffffff")]
    public unsafe void TestGuidBytesThrowsOutOfRangeError(string value)
    {
        Guid guid = Guid.Parse(value);

        GuidParser parser = new GuidParser();

        byte* buffer = stackalloc byte[15];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(guid, buffer, 15));
    }
}