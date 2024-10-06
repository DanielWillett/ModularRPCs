using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityPlaneParserTests
{
    [Test]
    public void TestPlaneStream()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Plane v2 = new Plane(new Vector3(x, x + 1, x - 1), x * 2);

        UnityPlaneParser parser = new UnityPlaneParser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 16));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Plane readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestPlaneBytes()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Plane v2 = new Plane(new Vector3(x, x + 1, x - 1), x * 2);

        UnityPlaneParser parser = new UnityPlaneParser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(16));

        ParserTests.CheckBuffer(buffer, maxSize);
        Plane readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(16));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestPlaneBytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        float x = (float)r.NextDouble();
        Plane v2 = new Plane(new Vector3(x, x + 1, x - 1), x * 2);

        UnityPlaneParser parser = new UnityPlaneParser();

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
    public void TestPlaneMany(int count)
    {
        Random r = new Random();

        Plane[] arr = new Plane[count];
        for (int i = 0; i < count; ++i)
        {
            float x = (float)r.NextDouble();
            arr[i] = new Plane(new Vector3(x, x + 1, x - 1), x * 2);
        }

        UnityPlaneParser.Many parser = new UnityPlaneParser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser);
        ParserManyTests.TestManyParserStream(arr, parser);
    }
}
