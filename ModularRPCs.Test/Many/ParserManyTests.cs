using DanielWillett.ModularRpcs.Serialization;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace ModularRPCs.Test.Many;
public partial class ParserManyTests
{
    private unsafe void TestManyParserBytes<T>(T[] values, IArrayBinaryTypeParser<T> parser)
    {
        uint maxSize = 8394240u;
        fixed (byte* bufferSrc = new byte[maxSize])
        {
            for (bool useObjToWrite = false; ; useObjToWrite = true)
            {
                if (useObjToWrite)
                    Unsafe.InitBlock(bufferSrc, 0, maxSize);

                byte* buffer = bufferSrc;
                IBinaryTypeParser<BitArray> bitArrayParser = parser as IBinaryTypeParser<BitArray>;
                int arrCtNoBit;
                int ct;
                ParserTests.WriteBuffer(ref buffer, ref maxSize);

                if (useObjToWrite)
                {
                    arrCtNoBit = 4;
                    ct = parser.WriteObject((object)values, buffer, maxSize);
                    ct += parser.WriteObject((object)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((object)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((object)values, buffer + ct, maxSize - (uint)ct);

                    if (bitArrayParser != null)
                    {
                        ct += bitArrayParser.WriteObject((object)new BitArray((bool[])(object)values), buffer + ct, maxSize - (uint)ct);
                    }
                }
                else
                {
                    arrCtNoBit = 8;
                    ct = ((IBinaryTypeParser<T[]>)parser).WriteObject(values, buffer, maxSize);
                    ct += parser.WriteObject((IList<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((IList<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((IList<T>)values, buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((IReadOnlyList<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((IReadOnlyList<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject((IReadOnlyList<T>)values, buffer + ct, maxSize - (uint)ct);
                    ct += parser.WriteObject(values.AsSpan(), buffer + ct, maxSize - (uint)ct);

                    if (bitArrayParser != null)
                    {
                        ct += bitArrayParser.WriteObject(new BitArray((bool[])(object)values), buffer + ct, maxSize - (uint)ct);
                    }
                }

                int arrCt = bitArrayParser != null ? arrCtNoBit + 1 : arrCtNoBit;

                Assert.That(((IBinaryTypeParser<T[]>)parser).GetSize(values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize(values.AsSpan()) * arrCt, Is.EqualTo(ct));
                if (bitArrayParser != null)
                    Assert.That(parser.GetSize(new BitArray((bool[])(object)values)) * arrCt, Is.EqualTo(ct));

                /*
                 * read to array
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                int pos = 0;
                for (int i = 0; i < arrCtNoBit; ++i)
                {
                    T[] readArray = parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(readArray, Is.EqualTo(values));
                }

                if (bitArrayParser != null)
                {
                    BitArray readArray9 = bitArrayParser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;

                    bool[] output = new bool[readArray9.Count];
                    readArray9.CopyTo(output, 0);
                    Assert.That(output, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                if (useObjToWrite)
                    break;

                /*
                 * read to other types
                 */

                List<Type> types = [typeof(T[]), typeof(IList<T>), typeof(IReadOnlyList<T>)];
                if (bitArrayParser != null)
                    types.Add(typeof(BitArray));

                foreach (Type readType in types)
                {
                    ParserTests.CheckBuffer(buffer, maxSize);
                    pos = 0;
                    for (int i = 0; i < arrCt; ++i)
                    {
                        object readArray = parser.ReadObject(readType, buffer + pos, maxSize - (uint)pos, out int bytesRead);
                        pos += bytesRead;
                        Assert.That((IEnumerable)readArray, Is.EqualTo(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }

                /*
                 * read to typed reference types
                 */

                // array
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;
                T[] arr1 = null!;
                TypedReference tr = __makeref(arr1);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr1, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                pos = 0;
                IList<T> arr2 = null!;
                tr = __makeref(arr2);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr2, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                pos = 0;
                IReadOnlyList<T> arr3 = null!;
                tr = __makeref(arr3);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr3, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                pos = 0;
                Span<T> arr4 = null!;
                Span<T>* spanPtr = &arr4;
                tr = __makeref(spanPtr);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr4.ToArray(), Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                pos = 0;
                ReadOnlySpan<T> arr5 = null!;
                ReadOnlySpan<T>* roSpanPtr = &arr5;
                tr = __makeref(roSpanPtr);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr5.ToArray(), Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                if (bitArrayParser != null)
                {
                    pos = 0;
                    BitArray arr6 = null!;
                    tr = __makeref(arr6);
                    for (int i = 0; i < arrCt; ++i)
                    {
                        parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                        pos += bytesRead;
                        bool[] output = new bool[arr6.Count];
                        arr6.CopyTo(output, 0);
                        Assert.That(output, Is.EqualTo(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }

                /*
                 * read to span (read len first)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    Span<T> output = new T[size];
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, output, out bytesRead, hasReadLength: true);
                    Assert.That(readCt, Is.EqualTo(output.Length));
                    pos += bytesRead;
                    Assert.That(output.ToArray(), Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to span (dont read len)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    Span<T> output = new T[values.Length];
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, output, out int bytesRead, hasReadLength: false);
                    Assert.That(readCt, Is.EqualTo(output.Length));
                    pos += bytesRead;
                    Assert.That(output.ToArray(), Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                if (values.Length != 0)
                {
                    /*
                     * read to span (throws too short exception)
                     */
                    ParserTests.CheckBuffer(buffer, maxSize);
                    pos = 0;

                    for (int i = 0; i < arrCt; ++i)
                    {
                        int bytesRead = 0;
                        // check to see if bytesRead still gets updated correctly
                        int* ptr = &bytesRead;
                        Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
                        {
                            Span<T> output = new T[values.Length - 1];
                            ref int pos2 = ref Unsafe.AsRef<int>(ptr);
                            parser.ReadObject(buffer + pos, maxSize - (uint)pos, output, out pos2, hasReadLength: false);
                        });
                        pos += bytesRead;
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }

                /*
                 * read to list (read len, add)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    List<T> outputList = new List<T>(size);
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out bytesRead, measuredCount: size, hasReadLength: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to list (dont read len, add)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    List<T> outputList = new List<T>(values.Length);
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out int bytesRead, hasReadLength: false);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to list (read len, set)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    List<T> outputList = [.. new T[size]];
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to list (dont read len, set)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    List<T> outputList = [.. new T[values.Length]];
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                /*
                 * read to list wrapper (read len, add)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    ListWrapper<T> outputList = new ListWrapper<T>(new List<T>(size));
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out bytesRead, measuredCount: size, hasReadLength: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to list wrapper (dont read len, add)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    ListWrapper<T> outputList = new ListWrapper<T>(new List<T>(values.Length));
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out int bytesRead, hasReadLength: false);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to list wrapper (read len, set)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    ListWrapper<T> outputList = new ListWrapper<T>([.. new T[size]]);
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct));

                /*
                 * read to list wrapper (dont read len, set)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    ListWrapper<T> outputList = new ListWrapper<T>([.. new T[values.Length]]);
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                if (values.Length != 0)
                {
                    /*
                     * read to list (expand, read len, set)
                     */
                    ParserTests.CheckBuffer(buffer, maxSize);
                    pos = 0;

                    for (int i = 0; i < arrCt; ++i)
                    {
                        int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                        pos += bytesRead;
                        Assert.That(size, Is.EqualTo(values.Length));

                        List<T> outputList = [.. new T[size - 1]];
                        int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                        Assert.That(readCt, Is.EqualTo(outputList.Count));
                        pos += bytesRead;
                        Assert.That(outputList, Is.EqualTo(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));

                    /*
                     * read to list (expand, dont read len, set)
                     */
                    ParserTests.CheckBuffer(buffer, maxSize);
                    pos = 0;

                    for (int i = 0; i < arrCt; ++i)
                    {
                        List<T> outputList = [.. new T[values.Length - 1]];
                        int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                        Assert.That(readCt, Is.EqualTo(outputList.Count));
                        pos += bytesRead;
                        Assert.That(outputList, Is.EqualTo(values));
                    }

                    /*
                     * read to list wrapper (expand, read len, set)
                     */
                    ParserTests.CheckBuffer(buffer, maxSize);
                    pos = 0;

                    for (int i = 0; i < arrCt; ++i)
                    {
                        int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                        pos += bytesRead;
                        Assert.That(size, Is.EqualTo(values.Length));

                        ListWrapper<T> outputList = new ListWrapper<T>([.. new T[size - 1]]);
                        int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                        Assert.That(readCt, Is.EqualTo(outputList.Count));
                        pos += bytesRead;
                        Assert.That(outputList, Is.EqualTo(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));

                    /*
                     * read to list wrapper (expand, dont read len, set)
                     */
                    ParserTests.CheckBuffer(buffer, maxSize);
                    pos = 0;

                    for (int i = 0; i < arrCt; ++i)
                    {
                        ListWrapper<T> outputList = new ListWrapper<T>([.. new T[values.Length - 1]]);
                        int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                        Assert.That(readCt, Is.EqualTo(outputList.Count));
                        pos += bytesRead;
                        Assert.That(outputList, Is.EqualTo(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }
            }
        }
    }
    private unsafe void TestManyParserStream<T>(T[] values, IArrayBinaryTypeParser<T> parser)
    {
        IBinaryTypeParser<BitArray> bitArrayParser = parser as IBinaryTypeParser<BitArray>;
        for (bool useObjToWrite = false;; useObjToWrite = true)
        {
            using Stream memStream = new MemoryStream();
            ParserTests.WriteBuffer(memStream);
            int arrCtNoBit;
            int ct;
            if (useObjToWrite)
            {
                arrCtNoBit = 4;
                ct = parser.WriteObject((object)values, memStream);
                ct += parser.WriteObject((object)new List<T>(values), memStream);
                ct += parser.WriteObject((object)new ArrayWrapper<T>(values), memStream);
                ct += parser.WriteObject((object)values, memStream);

                if (bitArrayParser != null)
                {
                    ct += bitArrayParser.WriteObject((object)new BitArray((bool[])(object)values), memStream);
                }
            }
            else
            {
                arrCtNoBit = 8;
                ct = ((IBinaryTypeParser<T[]>)parser).WriteObject(values, memStream);
                ct += parser.WriteObject((IList<T>)new List<T>(values), memStream);
                ct += parser.WriteObject((IList<T>)new ArrayWrapper<T>(values), memStream);
                ct += parser.WriteObject((IList<T>)values, memStream);
                ct += parser.WriteObject((IReadOnlyList<T>)new List<T>(values), memStream);
                ct += parser.WriteObject((IReadOnlyList<T>)new ArrayWrapper<T>(values), memStream);
                ct += parser.WriteObject((IReadOnlyList<T>)values, memStream);
                ct += parser.WriteObject(values.AsSpan(), memStream);

                if (bitArrayParser != null)
                {
                    ct += bitArrayParser.WriteObject(new BitArray((bool[])(object)values), memStream);
                }
            }

            int arrCt = bitArrayParser != null ? arrCtNoBit + 1 : arrCtNoBit;
            Assert.That(memStream.Position, Is.EqualTo(ct + ParserTests.BufferSize));

            Assert.That(((IBinaryTypeParser<T[]>)parser).GetSize(values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize(values.AsSpan()) * arrCt, Is.EqualTo(ct));
            if (bitArrayParser != null)
                Assert.That(parser.GetSize(new BitArray((bool[])(object)values)) * arrCt, Is.EqualTo(ct));

            /*
             * read to array
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            int pos = ParserTests.BufferSize;
            for (int i = 0; i < arrCtNoBit; ++i)
            {
                T[] readArray = parser.ReadObject(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(readArray, Is.EqualTo(values));
            }

            if (bitArrayParser != null)
            {
                BitArray readArray9 = bitArrayParser.ReadObject(memStream, out int bytesRead);
                pos += bytesRead;

                bool[] output = new bool[readArray9.Count];
                readArray9.CopyTo(output, 0);
                Assert.That(output, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            if (useObjToWrite)
                break;

            /*
             * read to other types
             */

            List<Type> types = [typeof(T[]), typeof(IList<T>), typeof(IReadOnlyList<T>)];
            if (bitArrayParser != null)
                types.Add(typeof(BitArray));

            foreach (Type readType in types)
            {
                memStream.Seek(0, SeekOrigin.Begin);
                ParserTests.CheckBuffer(memStream);

                pos = ParserTests.BufferSize;
                for (int i = 0; i < arrCt; ++i)
                {
                    object readArray = parser.ReadObject(readType, memStream, out int bytesRead);
                    pos += bytesRead;
                    Assert.That((IEnumerable)readArray, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }

            /*
             * read to typed reference types
             */

            memStream.Seek(0, SeekOrigin.Begin);
            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;
            T[] arr1 = null!;
            TypedReference tr = __makeref(arr1);
            for (int i = 0; i < arrCt; ++i)
            {
                parser.ReadObject(memStream, out int bytesRead, tr);
                pos += bytesRead;
                Assert.That(arr1, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            memStream.Seek(0, SeekOrigin.Begin);
            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;
            IList<T> arr2 = null!;
            tr = __makeref(arr2);
            for (int i = 0; i < arrCt; ++i)
            {
                parser.ReadObject(memStream, out int bytesRead, tr);
                pos += bytesRead;
                Assert.That(arr2, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            memStream.Seek(0, SeekOrigin.Begin);
            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;
            IReadOnlyList<T> arr3 = null!;
            tr = __makeref(arr3);
            for (int i = 0; i < arrCt; ++i)
            {
                parser.ReadObject(memStream, out int bytesRead, tr);
                pos += bytesRead;
                Assert.That(arr3, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            memStream.Seek(0, SeekOrigin.Begin);
            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;
            Span<T> arr4 = null!;
            Span<T>* spanPtr = &arr4;
            tr = __makeref(spanPtr);
            for (int i = 0; i < arrCt; ++i)
            {
                parser.ReadObject(memStream, out int bytesRead, tr);
                pos += bytesRead;
                Assert.That(arr4.ToArray(), Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            memStream.Seek(0, SeekOrigin.Begin);
            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;
            ReadOnlySpan<T> arr5 = null!;
            ReadOnlySpan<T>* roSpanPtr = &arr5;
            tr = __makeref(roSpanPtr);
            for (int i = 0; i < arrCt; ++i)
            {
                parser.ReadObject(memStream, out int bytesRead, tr);
                pos += bytesRead;
                Assert.That(arr5.ToArray(), Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            if (bitArrayParser != null)
            {
                memStream.Seek(0, SeekOrigin.Begin);
                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;
                BitArray arr6 = null!;
                tr = __makeref(arr6);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(memStream, out int bytesRead, tr);
                    pos += bytesRead;
                    bool[] output = new bool[arr6.Count];
                    arr6.CopyTo(output, 0);
                    Assert.That(output, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }

            /*
             * read to span (read len first)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                int size = parser.ReadArrayLength(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(size, Is.EqualTo(values.Length));

                Span<T> output = new T[size];
                int readCt = parser.ReadObject(memStream, output, out bytesRead, hasReadLength: true);
                Assert.That(readCt, Is.EqualTo(output.Length));
                pos += bytesRead;
                Assert.That(output.ToArray(), Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to span (dont read len)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                Span<T> output = new T[values.Length];
                int readCt = parser.ReadObject(memStream, output, out int bytesRead, hasReadLength: false);
                Assert.That(readCt, Is.EqualTo(output.Length));
                pos += bytesRead;
                Assert.That(output.ToArray(), Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            if (values.Length != 0)
            {
                /*
                 * read to span (throws too short exception)
                 */
                memStream.Seek(0, SeekOrigin.Begin);

                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;

                for (int i = 0; i < arrCt; ++i)
                {
                    int bytesRead = 0;
                    // check to see if bytesRead still gets updated correctly
                    int* ptr = &bytesRead;
                    Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
                    {
                        Span<T> output = new T[values.Length - 1];
                        ref int pos2 = ref Unsafe.AsRef<int>(ptr);
                        parser.ReadObject(memStream, output, out pos2, hasReadLength: false);
                    });
                    pos += bytesRead;
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }

            /*
             * read to list (read len, add)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                int size = parser.ReadArrayLength(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(size, Is.EqualTo(values.Length));

                List<T> outputList = new List<T>(size);
                int readCt = parser.ReadObject(memStream, outputList, out bytesRead, measuredCount: size, hasReadLength: true);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to list (dont read len, add)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                List<T> outputList = new List<T>(values.Length);
                int readCt = parser.ReadObject(memStream, outputList, out int bytesRead, hasReadLength: false);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to list (read len, set)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                int size = parser.ReadArrayLength(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(size, Is.EqualTo(values.Length));

                List<T> outputList = [.. new T[size]];
                int readCt = parser.ReadObject(memStream, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to list (dont read len, set)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                List<T> outputList = [.. new T[values.Length]];
                int readCt = parser.ReadObject(memStream, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            /*
             * read to list wrapper (read len, add)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                int size = parser.ReadArrayLength(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(size, Is.EqualTo(values.Length));

                ListWrapper<T> outputList = new ListWrapper<T>(new List<T>(size));
                int readCt = parser.ReadObject(memStream, outputList, out bytesRead, measuredCount: size, hasReadLength: true);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to list wrapper (dont read len, add)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                ListWrapper<T> outputList = new ListWrapper<T>(new List<T>(values.Length));
                int readCt = parser.ReadObject(memStream, outputList, out int bytesRead, hasReadLength: false);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to list wrapper (read len, set)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                int size = parser.ReadArrayLength(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(size, Is.EqualTo(values.Length));

                ListWrapper<T> outputList = new ListWrapper<T>([.. new T[size]]);
                int readCt = parser.ReadObject(memStream, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to list wrapper (dont read len, set)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                ListWrapper<T> outputList = new ListWrapper<T>([.. new T[values.Length]]);
                int readCt = parser.ReadObject(memStream, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                Assert.That(readCt, Is.EqualTo(outputList.Count));
                pos += bytesRead;
                Assert.That(outputList, Is.EqualTo(values));
            }

            if (values.Length != 0)
            {
                /*
                 * read to list (expand, read len, set)
                 */
                memStream.Seek(0, SeekOrigin.Begin);

                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(memStream, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    List<T> outputList = [.. new T[size - 1]];
                    int readCt = parser.ReadObject(memStream, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

                /*
                 * read to list (expand, dont read len, set)
                 */
                memStream.Seek(0, SeekOrigin.Begin);

                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;

                for (int i = 0; i < arrCt; ++i)
                {
                    List<T> outputList = [.. new T[values.Length - 1]];
                    int readCt = parser.ReadObject(memStream, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                /*
                 * read to list wrapper (expand, read len, set)
                 */
                memStream.Seek(0, SeekOrigin.Begin);

                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(memStream, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    ListWrapper<T> outputList = new ListWrapper<T>([.. new T[size - 1]]);
                    int readCt = parser.ReadObject(memStream, outputList, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

                /*
                 * read to list wrapper (expand, dont read len, set)
                 */
                memStream.Seek(0, SeekOrigin.Begin);

                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;

                for (int i = 0; i < arrCt; ++i)
                {
                    ListWrapper<T> outputList = new ListWrapper<T>([.. new T[values.Length - 1]]);
                    int readCt = parser.ReadObject(memStream, outputList, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(outputList.Count));
                    pos += bytesRead;
                    Assert.That(outputList, Is.EqualTo(values));
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }
        }
    }
}

// used to circumvent optimization checks for arrays and lists
internal class ListWrapper<T> : IList<T>, IReadOnlyList<T>
{
    public List<T> List { get; }
    public ListWrapper(List<T> list)
    {
        List = list;
    }
    public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)List).GetEnumerator();
    public void Add(T item)
    {
        List.Add(item);
    }
    public void Clear()
    {
        List.Clear();
    }
    public bool Contains(T item) => List.Contains(item);
    public void CopyTo(T[] array, int arrayIndex)
    {
        List.CopyTo(array, arrayIndex);
    }
    public bool Remove(T item) => List.Remove(item);
    public int Count => List.Count;
    public bool IsReadOnly => ((IList<T>)List).IsReadOnly;
    public int IndexOf(T item) => List.IndexOf(item);
    public void Insert(int index, T item)
    {
        List.Insert(index, item);
    }
    public void RemoveAt(int index)
    {
        List.RemoveAt(index);
    }
    public T this[int index]
    {
        get => List[index];
        set => List[index] = value;
    }
}

internal class ArrayWrapper<T> : IList<T>, IReadOnlyList<T>
{
    public T[] Array { get; }
    public ArrayWrapper(T[] array)
    {
        Array = array;
    }
    public IEnumerator<T> GetEnumerator() => ((IList<T>)Array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Array.GetEnumerator();
    public void Add(T item)
    {
        ((IList<T>)Array).Add(item);
    }
    public void Clear()
    {
        ((IList<T>)Array).Clear();
    }
    public bool Contains(T item) => ((IList<T>)Array).Contains(item);
    public void CopyTo(T[] array, int arrayIndex)
    {
        ((IList<T>)Array).CopyTo(array, arrayIndex);
    }
    public bool Remove(T item) => ((IList<T>)Array).Remove(item);
    public int Count => ((IList<T>)Array).Count;
    public bool IsReadOnly => ((IList<T>)Array).IsReadOnly;

    public int IndexOf(T item) => ((IList<T>)Array).IndexOf(item);

    public void Insert(int index, T item)
    {
        ((IList<T>)Array).Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        ((IList<T>)Array).RemoveAt(index);
    }

    public T this[int index]
    {
        get => Array[index];
        set => Array[index] = value;
    }
}
