using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using ModularRPCs.Test.Many;
using NUnit.Framework;
using System.IO;
using UnityEngine;
using Random = System.Random;

namespace ModularRPCs.Test.Unity
{
    [TestFixture]
    public class UnityUnityVector4ParserTests
    {
        [Test]
        public void TestVector4Stream()
        {
            Random r = new Random();

            float x = (float)r.NextDouble();
            Vector4 v2 = new Vector4(x, x + 1, x - 1, x * 2);

            UnityVector4Parser parser = new UnityVector4Parser();
            using Stream memStream = new MemoryStream();
            ParserTests.WriteBuffer(memStream);
            parser.WriteObject(v2, memStream);

            Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 16));

            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            Vector4 readValue = parser.ReadObject(memStream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(16));
            Assert.That(readValue, Is.EqualTo(v2));
        }

        [Test]
        public unsafe void TestVector4Bytes()
        {
            Random r = new Random();

            float x = (float)r.NextDouble();
            Vector4 v2 = new Vector4(x, x + 1, x - 1, x * 2);

            UnityVector4Parser parser = new UnityVector4Parser();

            uint maxSize = 64;
            byte* buffer = stackalloc byte[(int)maxSize];
            ParserTests.WriteBuffer(ref buffer, ref maxSize);
            int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

            Assert.That(bytesWritten, Is.EqualTo(16));

            ParserTests.CheckBuffer(buffer, maxSize);
            Vector4 readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(16));
            Assert.That(readValue, Is.EqualTo(v2));
        }

        [Test]
        public unsafe void TestVector4BytesThrowsOutOfRangeError()
        {
            Random r = new Random();

            float x = (float)r.NextDouble();
            Vector4 v2 = new Vector4(x, x + 1, x - 1, x * 2);

            UnityVector4Parser parser = new UnityVector4Parser();

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
        public void TestVector4Many(int count)
        {
            Random r = new Random();

            Vector4[] arr = new Vector4[count];
            for (int i = 0; i < count; ++i)
            {
                float x = (float)r.NextDouble();
                arr[i] = new Vector4(x, x + 1, x - 1, x * 2);
            }

            UnityVector4Parser.Many parser = new UnityVector4Parser.Many(new SerializationConfiguration());
            ParserManyTests.TestManyParserBytes(arr, parser);
            ParserManyTests.TestManyParserStream(arr, parser);
        }
    }
}
