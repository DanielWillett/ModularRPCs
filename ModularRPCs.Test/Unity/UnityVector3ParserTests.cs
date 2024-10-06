using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityVector3ParserTests
{
    [Test]
    public void TestVector3Stream()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Vector3 v2 = new Vector3(x, x + 1, x - 1);

        UnityVector3Parser parser = new UnityVector3Parser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 12));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Vector3 readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(12));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestVector3Bytes()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Vector3 v2 = new Vector3(x, x + 1, x - 1);

        UnityVector3Parser parser = new UnityVector3Parser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(12));

        ParserTests.CheckBuffer(buffer, maxSize);
        Vector3 readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(12));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestVector3BytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Vector3 v2 = new Vector3(x, x + 1, x - 1);

        UnityVector3Parser parser = new UnityVector3Parser();

        byte* buffer = stackalloc byte[11];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(v2, buffer, 11));
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
    public void TestVector3Many(int count)
    {
        Random r = new Random();

        Vector3[] arr = new Vector3[count];
        for (int i = 0; i < count; ++i)
        {
            float x = (float)r.NextDouble();
            arr[i] = new Vector3(x, x + 1, x - 1);
        }

        UnityVector3Parser.Many parser = new UnityVector3Parser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser);
        ParserManyTests.TestManyParserStream(arr, parser);
    }
}
