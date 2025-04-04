using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityBoundsParserTests
{
    [Test]
    public void TestBoundsStream()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Bounds v2 = new Bounds(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));

        UnityBoundsParser parser = new UnityBoundsParser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 24));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Bounds readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(24));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestBoundsBytes()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Bounds v2 = new Bounds(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));

        UnityBoundsParser parser = new UnityBoundsParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(24));

        ParserTests.CheckBuffer(buffer, maxSize);
        Bounds readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(24));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestBoundsBytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Bounds v2 = new Bounds(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));

        UnityBoundsParser parser = new UnityBoundsParser();

        byte* buffer = stackalloc byte[23];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(v2, buffer, 23));
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
    public void TestBoundsMany(int count)
    {
        Random r = new Random();

        Bounds[] arr = new Bounds[count];
        for (int i = 0; i < count; ++i)
        {
            float x = (float)r.NextDouble();
            arr[i] = new Bounds(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));
        }

        UnityBoundsParser.Many parser = new UnityBoundsParser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser);
        ParserManyTests.TestManyParserStream(arr, parser);
    }
}
