using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityMatrix4x4ParserTests
{
    private static void MakeMatrix(Random r, out Matrix4x4 matrix)
    {
        matrix = default;
        float x = (float)r.NextDouble();
        matrix.m00 = x;
        matrix.m10 = x + 1;
        matrix.m20 = x + 2;
        matrix.m30 = x + 3;
        matrix.m01 = x + 4;
        matrix.m11 = x + 5;
        matrix.m21 = x + 6;
        matrix.m31 = x + 7;
        matrix.m02 = x + 8;
        matrix.m12 = x + 9;
        matrix.m22 = x + 10;
        matrix.m32 = x + 11;
        matrix.m03 = x + 12;
        matrix.m13 = x + 13;
        matrix.m23 = x + 14;
        matrix.m33 = x + 15;
    }

    [Test]
    public void TestMatrix4x4Stream()
    {
        Random r = new Random();

        MakeMatrix(r, out Matrix4x4 matrix);

        UnityMatrix4x4Parser parser = new UnityMatrix4x4Parser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(matrix, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 64));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Matrix4x4 readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(64));
        Assert.That(readValue, Is.EqualTo(matrix));
    }

    [Test]
    public unsafe void TestMatrix4x4Bytes()
    {
        Random r = new Random();

        MakeMatrix(r, out Matrix4x4 matrix);

        UnityMatrix4x4Parser parser = new UnityMatrix4x4Parser();

        uint maxSize = 128;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(matrix, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(64));

        ParserTests.CheckBuffer(buffer, maxSize);
        Matrix4x4 readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(64));
        Assert.That(readValue, Is.EqualTo(matrix));
    }

    [Test]
    public unsafe void TestMatrix4x4BytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        MakeMatrix(r, out Matrix4x4 matrix);

        UnityMatrix4x4Parser parser = new UnityMatrix4x4Parser();

        byte* buffer = stackalloc byte[63];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(matrix, buffer, 63));
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
    public void TestMatrix4x4Many(int count)
    {
        Random r = new Random();

        Matrix4x4[] arr = new Matrix4x4[count];
        for (int i = 0; i < count; ++i)
        {
            MakeMatrix(r, out arr[i]);
        }

        UnityMatrix4x4Parser.Many parser = new UnityMatrix4x4Parser.Many(new SerializationConfiguration());
        ParserManyTests.TestManyParserBytes(arr, parser);
        ParserManyTests.TestManyParserStream(arr, parser);
    }
}
