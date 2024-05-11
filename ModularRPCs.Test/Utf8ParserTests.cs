using DanielWillett.ModularRpcs.Serialization.Parsers;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace ModularRPCs.Test;
public class Utf8ParserTests
{
    private static int CalcLength(string value)
    {
        int byteCt = Encoding.UTF8.GetByteCount(value);
        int expectedLength = byteCt;
        if (expectedLength > ushort.MaxValue)
            expectedLength += 4;
        else if (expectedLength > byte.MaxValue)
            expectedLength += 2;
        else ++expectedLength;
        if (value.Length != byteCt)
        {
            if (value.Length > ushort.MaxValue)
                expectedLength += 4;
            else if (value.Length > byte.MaxValue)
                expectedLength += 2;
            else ++expectedLength;
        }
        ++expectedLength;

        Utf8Parser parser = new Utf8Parser();
        
        int size = parser.GetSize(value);
        Assert.That(size, Is.EqualTo(expectedLength));

        return expectedLength;
    }
    private static void TestStringStream(string value)
    {
        int expectedLength = CalcLength(value);

        Utf8Parser parser = new Utf8Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(value, memStream);

        Assert.That(memStream.Length, Is.EqualTo(expectedLength));

        memStream.Seek(0, SeekOrigin.Begin);
        string readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(expectedLength));
        Assert.That(readValue, Is.EqualTo(value));
    }
    private static unsafe void TestStringBytes(string value)
    {
        int expectedLength = CalcLength(value);

        Utf8Parser parser = new Utf8Parser();

        scoped Span<byte> buffer = expectedLength > 512 ? new byte[expectedLength + 128] : stackalloc byte[expectedLength + 128];

        fixed (byte* ptr = buffer)
        {
            int bytesWritten = parser.WriteObject(value, ptr, (uint)buffer.Length);

            Assert.That(bytesWritten, Is.EqualTo(expectedLength));

            string readValue = parser.ReadObject(ptr, (uint)buffer.Length, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(expectedLength));
            Assert.That(readValue, Is.EqualTo(value));
        }
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCase7BitSmall1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitSmall2)]
    public void TestShort7BitStringStream(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCase7BitSmall1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitSmall2)]
    public void TestShort7BitStringBytes(string value)
    {
        TestStringBytes(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCase7BitMed1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitMed2)]
    public void TestMed7BitStringStream(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCase7BitMed1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitMed2)]
    public void TestMed7BitStringBytes(string value)
    {
        TestStringBytes(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong2)]
    public void TestLong7BitStringStream(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong2)]
    public void TestLong7BitStringBytes(string value)
    {
        TestStringBytes(value);
    }

    [Test]
    public void TestEmptyStringStream()
    {
        Utf8Parser parser = new Utf8Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(string.Empty, memStream);

        Assert.That(memStream.Length, Is.EqualTo(1));

        memStream.Seek(0, SeekOrigin.Begin);
        string readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(string.Empty));
    }

    [Test]
    public unsafe void TestEmptyStringBytes()
    {
        Utf8Parser parser = new Utf8Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(string.Empty, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(1));

        string readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TestNullStringStream()
    {
        Utf8Parser parser = new Utf8Parser();
        using Stream memStream = new MemoryStream();
        parser.WriteObject(null!, memStream);

        Assert.That(memStream.Length, Is.EqualTo(1));

        memStream.Seek(0, SeekOrigin.Begin);
        string readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(string.Empty));
    }

    [Test]
    public unsafe void TestNullStringBytes()
    {
        Utf8Parser parser = new Utf8Parser();

        byte* buffer = stackalloc byte[64];
        int bytesWritten = parser.WriteObject(null!, buffer, 64);

        Assert.That(bytesWritten, Is.EqualTo(1));

        string readValue = parser.ReadObject(buffer, 64, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(1));
        Assert.That(readValue, Is.EqualTo(string.Empty));
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseSmall1)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall2)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall3)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall4)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall5)]
    public void TestShortMultiByteStringStream(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseSmall1)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall2)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall3)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall4)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall5)]
    public void TestShortMultiByteStringBytes(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseMed1)]
    [TestCase(Utf8ParserTestCases.TestCaseMed2)]
    [TestCase(Utf8ParserTestCases.TestCaseMed3)]
    [TestCase(Utf8ParserTestCases.TestCaseMed4)]
    [TestCase(Utf8ParserTestCases.TestCaseMed5)]
    [TestCase(Utf8ParserTestCases.TestCaseMed6)]
    public void TestMediumMultiByteStringStream(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseMed1)]
    [TestCase(Utf8ParserTestCases.TestCaseMed2)]
    [TestCase(Utf8ParserTestCases.TestCaseMed3)]
    [TestCase(Utf8ParserTestCases.TestCaseMed4)]
    [TestCase(Utf8ParserTestCases.TestCaseMed5)]
    [TestCase(Utf8ParserTestCases.TestCaseMed6)]
    public void TestMediumMultiByteStringBytes(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseLong1)]
    [TestCase(Utf8ParserTestCases.TestCaseLong2)]
    [TestCase(Utf8ParserTestCases.TestCaseLong3)]
    public void TestLongMultiByteStringStream(string value)
    {
        TestStringStream(value);
    }

    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseLong1)]
    [TestCase(Utf8ParserTestCases.TestCaseLong2)]
    [TestCase(Utf8ParserTestCases.TestCaseLong3)]
    public void TestLongMultiByteStringBytes(string value)
    {
        TestStringStream(value);
    }
}
