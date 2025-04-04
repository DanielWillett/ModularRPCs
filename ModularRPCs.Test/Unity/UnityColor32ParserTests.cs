using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityColor32ParserTests
{
    [Test]
    public void TestColor32Stream()
    {
        Random r = new Random();

        byte x = (byte)r.Next(2, 256);
        Color32 v2 = new Color32(x, (byte)(x + 1), (byte)(x - 1), (byte)(x * 2));

        UnityColor32Parser parser = new UnityColor32Parser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 4));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Color32 readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestColor32Bytes()
    {
        Random r = new Random();

        byte x = (byte)r.Next(2, 256);
        Color32 v2 = new Color32(x, (byte)(x + 1), (byte)(x - 1), (byte)(x * 2));

        UnityColor32Parser parser = new UnityColor32Parser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(4));

        ParserTests.CheckBuffer(buffer, maxSize);
        Color32 readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestColor32BytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        byte x = (byte)r.Next(2, 256);
        Color32 v2 = new Color32(x, (byte)(x + 1), (byte)(x - 1), (byte)(x * 2));

        UnityColor32Parser parser = new UnityColor32Parser();

        byte* buffer = stackalloc byte[3];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(v2, buffer, 3));
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(255)]
    [TestCase(256)]
    [TestCase(280)]
    [TestCase(65535)]
    [TestCase(65536)]
    [TestCase(65570)]
    [Ignore("these take forever")]
    public void TestColor32Many(int count)
    {
        Random r = new Random();

        Color32[] arr = new Color32[count];
        for (int i = 0; i < count; ++i)
        {
            byte x = (byte)r.Next(2, 256);
            arr[i] = new Color32(x, (byte)(x + 1), (byte)(x - 1), (byte)(x * 2));
        }

        UnityColor32Parser.Many parser = new UnityColor32Parser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser);
        ParserManyTests.TestManyParserStream(arr, parser);
    }
}
