using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityRay2DParserTests
{
    private static bool Ray2DsEqual(ref Ray2D r1, ref Ray2D r2)
    {
        Vector2 or1 = r1.origin, or2 = r2.origin;
        Vector2 di1 = r1.direction, di2 = r2.direction;

        return Math.Abs(or1.x - or2.x) < 0.001f && Math.Abs(or1.y - or2.y) < 0.001f &&
               Math.Abs(di1.x - di2.x) < 0.001f && Math.Abs(di1.y - di2.y) < 0.001f;
    }

    [Test]
    public void TestRay2DStream()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Ray2D v2 = new Ray2D(new Vector2(x, x + 1), new Vector2(x * 2, x * 2 + 1));

        UnityRay2DParser parser = new UnityRay2DParser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 16));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Ray2D readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(Ray2DsEqual(ref readValue, ref v2), Is.True);
    }

    [Test]
    public unsafe void TestRay2DBytes()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Ray2D v2 = new Ray2D(new Vector2(x, x + 1), new Vector2(x * 2, x * 2 + 1));

        UnityRay2DParser parser = new UnityRay2DParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(16));

        ParserTests.CheckBuffer(buffer, maxSize);
        Ray2D readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(Ray2DsEqual(ref readValue, ref v2), Is.True);
    }

    [Test]
    public unsafe void TestRay2DBytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Ray2D v2 = new Ray2D(new Vector2(x, x + 1), new Vector2(x * 2, x * 2 + 1));

        UnityRay2DParser parser = new UnityRay2DParser();

        byte* buffer = stackalloc byte[15];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(v2, buffer, 15));
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
    public void TestRay2DMany(int count)
    {
        Random r = new Random();

        Ray2D[] arr = new Ray2D[count];
        for (int i = 0; i < count; ++i)
        {
            float x = (float)r.NextDouble();
            arr[i] = new Ray2D(new Vector2(x, x + 1), new Vector2(x * 2, x * 2 + 1));
        }

        UnityRay2DParser.Many parser = new UnityRay2DParser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser, (a, b) => Ray2DsEqual(ref a, ref b));
        ParserManyTests.TestManyParserStream(arr, parser, (a, b) => Ray2DsEqual(ref a, ref b));
    }
}
