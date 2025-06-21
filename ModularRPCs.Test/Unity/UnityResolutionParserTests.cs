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
    public class UnityResolutionParserTests
    {
        [Test]
        public void TestResolutionStream()
        {
            Random r = new Random();

            int x = r.Next(1, 2048);
            Resolution v2 = new Resolution { width = x, height = x + 1, refreshRate = x + 2 };

            UnityResolutionParser parser = new UnityResolutionParser();
            using Stream memStream = new MemoryStream();
            ParserTests.WriteBuffer(memStream);
            parser.WriteObject(v2, memStream);

            Assert.That(memStream.Length, Is.EqualTo(ParserTests.BufferSize + 12));

            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            Resolution readValue = parser.ReadObject(memStream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(12));
            Assert.That(readValue, Is.EqualTo(v2));
        }

        [Test]
        public unsafe void TestResolutionBytes()
        {
            Random r = new Random();

            int x = r.Next(1, 2048);
            Resolution v2 = new Resolution { width = x, height = x + 1, refreshRate = x + 2 };

            UnityResolutionParser parser = new UnityResolutionParser();

            uint maxSize = 64;
            byte* buffer = stackalloc byte[(int)maxSize];
            ParserTests.WriteBuffer(ref buffer, ref maxSize);
            int bytesWritten = parser.WriteObject(v2, buffer, maxSize);

            Assert.That(bytesWritten, Is.EqualTo(12));

            ParserTests.CheckBuffer(buffer, maxSize);
            Resolution readValue = parser.ReadObject(buffer, maxSize, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(12));
            Assert.That(readValue, Is.EqualTo(v2));
        }

        [Test]
        public unsafe void TestResolutionBytesThrowsOutOfRangeError()
        {
            Random r = new Random();

            int x = r.Next(1, 2048);
            Resolution v2 = new Resolution { width = x, height = x + 1, refreshRate = x + 2 };

            UnityResolutionParser parser = new UnityResolutionParser();

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

        [Ignore("these take forever")]
        public void TestResolutionMany(int count)
        {
            Random r = new Random();

            Resolution[] arr = new Resolution[count];
            for (int i = 0; i < count; ++i)
            {
                int x = r.Next(1, 2048);
                arr[i] = new Resolution { width = x, height = x + 1, refreshRate = x + 2 };
            }

            UnityResolutionParser.Many parser = new UnityResolutionParser.Many(new SerializationConfiguration());
            ParserManyTests.TestManyParserBytes(arr, parser);
            ParserManyTests.TestManyParserStream(arr, parser);
        }
    }
}
