using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity;
public class UnityUnityVector2ParserTests
{
    [Test]
    public void TestVector2Stream()
    {
        Random r = new Random();

        Vector2 v2 = new Vector2((float)r.NextDouble(), (float)r.NextDouble());

        UnityVector2Parser parser = new UnityVector2Parser();
        using Stream memStream = new MemoryStream();
        ParserTests.WriteBuffer(memStream);
        parser.WriteObject(v2, memStream);

        Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 8));

        memStream.Seek(0, SeekOrigin.Begin);

        ParserTests.CheckBuffer(memStream);
        Vector2 readValue = parser.ReadObject(memStream, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestVector2Bytes()
    {
        Random r = new Random();

        Vector2 v2 = new Vector2((float)r.NextDouble(), (float)r.NextDouble());

        UnityVector2Parser parser = new UnityVector2Parser();

        uint maxSize = 64;
        byte* buffer = stackalloc byte[(int)maxSize];
        ParserTests.WriteBuffer(ref buffer, ref maxSize);
        int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

        Assert.That(bytesWritten, Is.EqualTo(8));

        ParserTests.CheckBuffer(buffer, maxSize);
        Vector2 readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

        Assert.That(bytesRead, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(v2));
    }

    [Test]
    public unsafe void TestVector2BytesThrowsOutOfRangeError()
    {
        Random r = new Random();

        Vector2 v2 = new Vector2((float)r.NextDouble(), (float)r.NextDouble());

        UnityVector2Parser parser = new UnityVector2Parser();

        byte* buffer = stackalloc byte[7];

        Assert.Throws(Is.TypeOf<RpcOverflowException>(), () => parser.WriteObject(v2, buffer, 3));
    }

}
