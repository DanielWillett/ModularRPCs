using System;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityRayParserTests
{
    private static bool RaysEqual(ref Ray r1, ref Ray r2)
    {
        Vector3 or1 = r1.origin, or2 = r2.origin;
        Vector3 di1 = r1.direction, di2 = r2.direction;

        return Math.Abs(or1.x - or2.x) < 0.001f && Math.Abs(or1.y - or2.y) < 0.001f && Math.Abs(or1.z - or2.z) < 0.001f &&
               Math.Abs(di1.x - di2.x) < 0.001f && Math.Abs(di1.y - di2.y) < 0.001f && Math.Abs(di1.z - di2.z) < 0.001f;
    }

    [Test]
    public void TestRayStream()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Ray v2 = new Ray(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));

        UnityRayParser parser = new UnityRayParser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 24));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Ray readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(24));
        Assert.That(RaysEqual(ref readValue, ref v2), Is.True);
    }

    [Test]
    public unsafe void TestRayBytes()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Ray v2 = new Ray(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));

        UnityRayParser parser = new UnityRayParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(24));

        ParserTests.CheckBuffer(buffer, maxSize);
        Ray readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(24));
        Assert.That(RaysEqual(ref readValue, ref v2), Is.True);
    }

    [Test]
    public unsafe void TestRayBytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Ray v2 = new Ray(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));

        UnityRayParser parser = new UnityRayParser();

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
    public void TestRayMany(int count)
    {
        Random r = new Random();

        Ray[] arr = new Ray[count];
        for (int i = 0; i < count; ++i)
        {
            float x = (float)r.NextDouble();
            arr[i] = new Ray(new Vector3(x, x + 1, x - 1), new Vector3(x * 2, x * 2 + 1, x * 2 - 1));
        }

        UnityRayParser.Many parser = new UnityRayParser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser, (a, b) => RaysEqual(ref a, ref b));
        ParserManyTests.TestManyParserStream(arr, parser, (a, b) => RaysEqual(ref a, ref b));
    }
}
