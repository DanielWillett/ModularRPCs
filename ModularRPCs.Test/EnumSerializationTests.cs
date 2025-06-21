using System.IO;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Serialization;
using NUnit.Framework;

namespace ModularRPCs.Test
{
    public class EnumSerializationTests
    {
        private readonly IRpcSerializer _serializer = new DefaultSerializer(new SerializationConfiguration());

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumGeneric()
        {
            byte* ptr = stackalloc byte[64];

            int ct = _serializer.WriteObject(Int8Enum.C, ptr, 64);

            Assert.That(ct, Is.EqualTo(1));

            Int8Enum value = _serializer.ReadObject<Int8Enum>(ptr, 64, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumBoxed()
        {
            byte* ptr = stackalloc byte[64];

            int ct = _serializer.WriteObject((object)Int8Enum.C, ptr, 64);

            Assert.That(ct, Is.EqualTo(1));

            Int8Enum value = (Int8Enum)_serializer.ReadObject(typeof(Int8Enum), ptr, 64, out int bytesRead)!;

            Assert.That(bytesRead, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumRefAny()
        {
            byte* ptr = stackalloc byte[64];

            Int8Enum valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(__makeref(valueToWrite), ptr, 64);

            Assert.That(ct, Is.EqualTo(1));

            Int8Enum value = default;

            _serializer.ReadObject(__makeref(value), ptr, 64, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }
    
        [Test]
        public void TestReadWriteWriteStreamInt8EnumGeneric()
        {
            MemoryStream stream = new MemoryStream();

            int ct = _serializer.WriteObject(Int8Enum.C, stream);

            Assert.That(ct, Is.EqualTo(1));

            stream.Seek(0, SeekOrigin.Begin);
            Int8Enum value = _serializer.ReadObject<Int8Enum>(stream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public void TestReadWriteWriteStreamInt8EnumBoxed()
        {
            MemoryStream stream = new MemoryStream();

            int ct = _serializer.WriteObject((object)Int8Enum.C, stream);

            Assert.That(ct, Is.EqualTo(1));

            stream.Seek(0, SeekOrigin.Begin);
            Int8Enum value = (Int8Enum)_serializer.ReadObject(typeof(Int8Enum), stream, out int bytesRead)!;

            Assert.That(bytesRead, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public void TestReadWriteWriteStreamInt8EnumRefAny()
        {
            MemoryStream stream = new MemoryStream();

            Int8Enum valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(__makeref(valueToWrite), stream);

            Assert.That(ct, Is.EqualTo(1));

            Int8Enum value = default;

            stream.Seek(0, SeekOrigin.Begin);
            _serializer.ReadObject(__makeref(value), stream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumGenericNullableNotNull()
        {
            byte* ptr = stackalloc byte[64];

            Int8Enum? toWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(toWrite, ptr, 64);

            Assert.That(ct, Is.EqualTo(2));

            Int8Enum? value = _serializer.ReadNullable<Int8Enum>(ptr, 64, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumBoxedNullableNotNull()
        {
            byte* ptr = stackalloc byte[64];

            Int8Enum? toWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(typeof(Int8Enum?), toWrite, ptr, 64);

            Assert.That(ct, Is.EqualTo(2));

            Int8Enum? value = (Int8Enum?)_serializer.ReadObject(typeof(Int8Enum?), ptr, 64, out int bytesRead)!;

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumRefAnyNullableNotNull()
        {
            byte* ptr = stackalloc byte[64];

            Int8Enum? valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(__makeref(valueToWrite), ptr, 64);

            Assert.That(ct, Is.EqualTo(2));

            Int8Enum? value = default;

            _serializer.ReadObject(__makeref(value), ptr, 64, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public unsafe void TestReadWriteWriteInt8EnumRefAnyNullableAsNullableNotNull()
        {
            byte* ptr = stackalloc byte[64];

            Int8Enum? valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(__makeref(valueToWrite), ptr, 64);

            Assert.That(ct, Is.EqualTo(2));

            Int8Enum? value = default;

            _serializer.ReadNullable<Int8Enum>(__makeref(value), ptr, 64, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }
    
        [Test]
        public void TestReadWriteWriteStreamInt8EnumGenericNullableNotNull()
        {
            MemoryStream stream = new MemoryStream();

            Int8Enum? valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(valueToWrite, stream);

            Assert.That(ct, Is.EqualTo(2));

            stream.Seek(0, SeekOrigin.Begin);
            Int8Enum? value = _serializer.ReadNullable<Int8Enum>(stream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public void TestReadWriteWriteStreamInt8EnumBoxedNullableNotNull()
        {
            MemoryStream stream = new MemoryStream();

            Int8Enum? valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(typeof(Int8Enum?), valueToWrite, stream);

            Assert.That(ct, Is.EqualTo(2));

            stream.Seek(0, SeekOrigin.Begin);
            Int8Enum? value = (Int8Enum?)_serializer.ReadObject(typeof(Int8Enum?), stream, out int bytesRead)!;

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public void TestReadWriteWriteStreamInt8EnumRefAnyNullableNotNull()
        {
            MemoryStream stream = new MemoryStream();

            Int8Enum? valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(__makeref(valueToWrite), stream);

            Assert.That(ct, Is.EqualTo(2));

            Int8Enum? value = default;

            stream.Seek(0, SeekOrigin.Begin);
            _serializer.ReadObject(__makeref(value), stream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public void TestReadWriteWriteStreamInt8EnumRefAnyNullableAsNullableNotNull()
        {
            MemoryStream stream = new MemoryStream();

            Int8Enum? valueToWrite = Int8Enum.C;

            int ct = _serializer.WriteObject(__makeref(valueToWrite), stream);

            Assert.That(ct, Is.EqualTo(2));

            Int8Enum? value = default;

            stream.Seek(0, SeekOrigin.Begin);
            _serializer.ReadNullable<Int8Enum>(__makeref(value), stream, out int bytesRead);

            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(Int8Enum.C));
        }

        [Test]
        public void TestEnumSizesInt8()
        {
            int minSize = _serializer.GetMinimumSize(typeof(Int8Enum));
            int size = _serializer.GetSize(Int8Enum.A);

            Assert.That(minSize, Is.EqualTo(1));
            Assert.That(size, Is.EqualTo(1));
        }
    
        [Test]
        public void TestEnumSizesUInt8()
        {
            int minSize = _serializer.GetMinimumSize(typeof(UInt8Enum));
            int size = _serializer.GetSize(UInt8Enum.A);

            Assert.That(minSize, Is.EqualTo(1));
            Assert.That(size, Is.EqualTo(1));
        }
    
        [Test]
        public void TestEnumSizesInt16()
        {
            int minSize = _serializer.GetMinimumSize(typeof(Int16Enum));
            int size = _serializer.GetSize(Int16Enum.A);

            Assert.That(minSize, Is.EqualTo(2));
            Assert.That(size, Is.EqualTo(2));
        }
    
        [Test]
        public void TestEnumSizesUInt16()
        {
            int minSize = _serializer.GetMinimumSize(typeof(UInt16Enum));
            int size = _serializer.GetSize(UInt16Enum.A);

            Assert.That(minSize, Is.EqualTo(2));
            Assert.That(size, Is.EqualTo(2));
        }
    
        [Test]
        public void TestEnumSizesInt32()
        {
            int minSize = _serializer.GetMinimumSize(typeof(Int32Enum));
            int size = _serializer.GetSize(Int32Enum.A);

            Assert.That(minSize, Is.EqualTo(4));
            Assert.That(size, Is.EqualTo(4));
        }
    
        [Test]
        public void TestEnumSizesUInt32()
        {
            int minSize = _serializer.GetMinimumSize(typeof(UInt32Enum));
            int size = _serializer.GetSize(UInt32Enum.A);

            Assert.That(minSize, Is.EqualTo(4));
            Assert.That(size, Is.EqualTo(4));
        }
    
        [Test]
        public void TestEnumSizesInt64()
        {
            int minSize = _serializer.GetMinimumSize(typeof(Int64Enum));
            int size = _serializer.GetSize(Int64Enum.A);

            Assert.That(minSize, Is.EqualTo(8));
            Assert.That(size, Is.EqualTo(8));
        }
    
        [Test]
        public void TestEnumSizesUInt64()
        {
            int minSize = _serializer.GetMinimumSize(typeof(UInt64Enum));
            int size = _serializer.GetSize(UInt64Enum.A);

            Assert.That(minSize, Is.EqualTo(8));
            Assert.That(size, Is.EqualTo(8));
        }
    
        [Test]
        public void TestNullableEnumSizesInt8()
        {
            Int8Enum? v = Int8Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(2));
        }
    
        [Test]
        public void TestNullableEnumSizesUInt8()
        {
            UInt8Enum? v = UInt8Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(2));
        }
    
        [Test]
        public void TestNullableEnumSizesInt16()
        {
            Int16Enum? v = Int16Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(3));
        }
    
        [Test]
        public void TestNullableEnumSizesUInt16()
        {
            UInt16Enum? v = UInt16Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(3));
        }
    
        [Test]
        public void TestNullableEnumSizesInt32()
        {
            Int32Enum? v = Int32Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(5));
        }
    
        [Test]
        public void TestNullableEnumSizesUInt32()
        {
            UInt32Enum? v = UInt32Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(5));
        }
    
        [Test]
        public void TestNullableEnumSizesInt64()
        {
            Int64Enum? v = Int64Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(9));
        }
    
        [Test]
        public void TestNullableEnumSizesUInt64()
        {
            UInt64Enum? v = UInt64Enum.A;
            int size = _serializer.GetSize(v);
            Assert.That(size, Is.EqualTo(9));
        }

        public enum Int8Enum : sbyte { A, B, C }
        public enum UInt8Enum : byte { A, B, C }
        public enum Int16Enum : short { A, B, C }
        public enum UInt16Enum : ushort { A, B, C }
        public enum Int32Enum { A, B, C }
        public enum UInt32Enum : uint { A, B, C }
        public enum Int64Enum : long { A, B, C }
        public enum UInt64Enum : ulong { A, B, C }
    }
}
