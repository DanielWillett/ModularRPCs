using DanielWillett.ModularRpcs.Serialization;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace ModularRPCs.Test.Many;

[Ignore("these take forever"), TestFixture, Timeout(60000)]
public partial class ParserManyTests
{
    public static unsafe void TestManyParserBytes<T>(T[] values, IArrayBinaryTypeParser<T> parser, Func<T, T, bool> equality = null)
    {
        EqualConstraint IsEqualToCustom(IEnumerable<T> value)
        {
            if (equality != null)
                return Is.EqualTo(value).Using<T>(equality);

            return Is.EqualTo(value);
        }

        uint maxSize = 604385280U;
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
                    arrCtNoBit = 8;
                    ct = parser.WriteObject((object)values, buffer, maxSize);
                    int lastCount = ct;
                    int thisCount = parser.WriteObject((object)new ArraySegment<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((object)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((object)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((object)new ReadOnlyArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((object)new CollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((object)new ReadOnlyCollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((object)new EnumerableWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;

                    if (bitArrayParser != null)
                    {
                        thisCount = bitArrayParser.WriteObject((object)new BitArray((bool[])(object)values), buffer + ct, maxSize - (uint)ct);
                        Assert.That(thisCount, Is.EqualTo(lastCount));
                        ct += thisCount;
                    }
                }
                else
                {
                    arrCtNoBit = 31;
                    ct = ((IBinaryTypeParser<T[]>)parser).WriteObject(values, buffer, maxSize);
                    int lastCount = ct;
                    int thisCount = parser.WriteObject((IList<T>)values, buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IList<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IList<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IList<T>)new ArraySegment<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyList<T>)values, buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyList<T>)new ArraySegment<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyList<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyList<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject(new ReadOnlyArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((ICollection<T>)values, buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((ICollection<T>)new ArraySegment<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((ICollection<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((ICollection<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((ICollection<T>)new CollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyCollection<T>)values, buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyCollection<T>)new ArraySegment<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyCollection<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyCollection<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyCollection<T>)new ReadOnlyArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IReadOnlyCollection<T>)new CollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject(new ReadOnlyCollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)values, buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)new ArraySegment<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)new List<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)new ArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)new ReadOnlyArrayWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)new CollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject((IEnumerable<T>)new ReadOnlyCollectionWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject(new EnumerableWrapper<T>(values), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                    thisCount = parser.WriteObject(values.AsSpan(), buffer + ct, maxSize - (uint)ct);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;

                    if (bitArrayParser != null)
                    {
                        thisCount = bitArrayParser.WriteObject(new BitArray((bool[])(object)values), buffer + ct, maxSize - (uint)ct);
                        Assert.That(thisCount, Is.EqualTo(lastCount));
                        ct += thisCount;
                    }
                }

                int arrCt = bitArrayParser != null ? arrCtNoBit + 1 : arrCtNoBit;

                Assert.That(((IBinaryTypeParser<T[]>)parser).GetSize(values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize(new ReadOnlyArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((ICollection<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((ICollection<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((ICollection<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((ICollection<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((ICollection<T>)new CollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyCollection<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyCollection<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyCollection<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyCollection<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyCollection<T>)new ReadOnlyArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IReadOnlyCollection<T>)new CollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize(new ReadOnlyCollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)values) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)new ReadOnlyArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)new CollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize((IEnumerable<T>)new ReadOnlyCollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
                Assert.That(parser.GetSize(new EnumerableWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
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
                    T[] readArray = ((IBinaryTypeParser<T[]>)parser).ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(readArray, IsEqualToCustom(values));
                }

                if (bitArrayParser != null)
                {
                    BitArray readArray9 = bitArrayParser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;

                    bool[] output = new bool[readArray9.Count];
                    readArray9.CopyTo(output, 0);
                    Assert.That(output, IsEqualToCustom(values));
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
                        Assert.That((IEnumerable)readArray, IsEqualToCustom(values));
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
                    Assert.That(arr1, IsEqualToCustom(values));
                    arr1 = null;
                }

                Assert.That(pos, Is.EqualTo(ct));

                pos = 0;
                IList<T> arr2 = null!;
                tr = __makeref(arr2);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr2, IsEqualToCustom(values));
                    arr2 = null;
                }

                Assert.That(pos, Is.EqualTo(ct));

                pos = 0;
                IReadOnlyList<T> arr3 = null!;
                tr = __makeref(arr3);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr3, IsEqualToCustom(values));
                    arr3 = null;
                }

                Assert.That(pos, Is.EqualTo(ct));

                if (typeof(T).IsValueType)
                {
                    pos = 0;
                    Span<T> arr4 = null!;
                    Span<T>* spanPtr = &arr4;
                    tr = __makeref(spanPtr);
                    for (int i = 0; i < arrCt; ++i)
                    {
                        parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                        pos += bytesRead;
                        Assert.That(arr4.ToArray(), IsEqualToCustom(values));
                        if (i > arrCt / 2)
                        {
                            arr4 = new T[values.Length];
                        }
                        else
                        {
                            arr4 = null;
                        }
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
                        Assert.That(arr5.ToArray(), IsEqualToCustom(values));
                        arr5 = null;
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }
                else if (typeof(T) == typeof(string))
                {
                    pos = 0;
                    Span<string> arr4 = null!;
                    Span<string>* spanPtr = &arr4;
                    tr = __makeref(spanPtr);
                    for (int i = 0; i < arrCt; ++i)
                    {
                        parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                        pos += bytesRead;
                        Assert.That(arr4.ToArray(), IsEqualToCustom(values));
                        if (i > arrCt / 2)
                        {
                            arr4 = new string[values.Length];
                        }
                        else
                        {
                            arr4 = null;
                        }
                    }

                    Assert.That(pos, Is.EqualTo(ct));

                    pos = 0;
                    ReadOnlySpan<string> arr5 = null!;
                    ReadOnlySpan<string>* roSpanPtr = &arr5;
                    tr = __makeref(roSpanPtr);
                    for (int i = 0; i < arrCt; ++i)
                    {
                        parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                        pos += bytesRead;
                        Assert.That(arr5.ToArray(), IsEqualToCustom(values));
                        arr5 = null;
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }

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
                        Assert.That(output, IsEqualToCustom(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }

                pos = 0;
                ArraySegment<T> arr7 = default!;
                tr = __makeref(arr7);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(buffer + pos, maxSize - (uint)pos, out int bytesRead, tr);
                    pos += bytesRead;
                    T[] toArray = new T[arr7.Count];
                    Array.Copy(arr7.Array, arr7.Offset, toArray, 0, toArray.Length);
                    Assert.That(toArray, IsEqualToCustom(values));
                    arr7 = default;
                }

                Assert.That(pos, Is.EqualTo(ct));

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
                    Assert.That(output.ToArray(), IsEqualToCustom(values));
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
                    Assert.That(output.ToArray(), IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
                }

                /*
                 * read to array segment (read len, set)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    int size = parser.ReadArrayLength(buffer + pos, maxSize - (uint)pos, out int bytesRead);
                    pos += bytesRead;
                    Assert.That(size, Is.EqualTo(values.Length));

                    ArraySegment<T> arr = new ArraySegment<T>(new T[size + 2], 1, size);
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, arr, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(arr.Count));
                    pos += bytesRead;
                    Assert.That(arr, IsEqualToCustom(values));
                }

                /*
                 * read to array segment (dont read len, set)
                 */
                ParserTests.CheckBuffer(buffer, maxSize);
                pos = 0;

                for (int i = 0; i < arrCt; ++i)
                {
                    ArraySegment<T> arr = new ArraySegment<T>(new T[values.Length + 2], 1, values.Length);
                    int readCt = parser.ReadObject(buffer + pos, maxSize - (uint)pos, arr, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                    Assert.That(readCt, Is.EqualTo(arr.Count));
                    pos += bytesRead;
                    Assert.That(arr, IsEqualToCustom(values));
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
                        Assert.That(outputList, IsEqualToCustom(values));
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
                        Assert.That(outputList, IsEqualToCustom(values));
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
                        Assert.That(outputList, IsEqualToCustom(values));
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
                        Assert.That(outputList, IsEqualToCustom(values));
                    }

                    Assert.That(pos, Is.EqualTo(ct));
                }
            }
        }
    }
    public static unsafe void TestManyParserStream<T>(T[] values, IArrayBinaryTypeParser<T> parser, Func<T, T, bool> equality = null)
    {
        EqualConstraint IsEqualToCustom(IEnumerable<T> value)
        {
            if (equality != null)
                return Is.EqualTo(value).Using<T>(equality);

            return Is.EqualTo(value);
        }

        IBinaryTypeParser<BitArray> bitArrayParser = parser as IBinaryTypeParser<BitArray>;
        for (bool useObjToWrite = false;; useObjToWrite = true)
        {
            using Stream memStream = new MemoryStream();
            ParserTests.WriteBuffer(memStream);
            int arrCtNoBit;
            int ct;
            if (useObjToWrite)
            {
                arrCtNoBit = 8;
                ct = parser.WriteObject((object)values, memStream);
                int lastCount = ct;
                int thisCount = parser.WriteObject((object)new ArraySegment<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((object)new List<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((object)new ArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((object)new ReadOnlyArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((object)new CollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((object)new ReadOnlyCollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((object)new EnumerableWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                if (bitArrayParser != null)
                {
                    thisCount = bitArrayParser.WriteObject((object)new BitArray((bool[])(object)values), memStream);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                }
            }
            else
            {
                arrCtNoBit = 31;
                ct = ((IBinaryTypeParser<T[]>)parser).WriteObject(values, memStream);
                int lastCount = ct;
                int thisCount = parser.WriteObject((IList<T>)values, memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IList<T>)new List<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IList<T>)new ArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IList<T>)new ArraySegment<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyList<T>)values, memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyList<T>)new ArraySegment<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyList<T>)new List<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyList<T>)new ArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject(new ReadOnlyArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((ICollection<T>)values, memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((ICollection<T>)new ArraySegment<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((ICollection<T>)new List<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((ICollection<T>)new ArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((ICollection<T>)new CollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyCollection<T>)values, memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyCollection<T>)new ArraySegment<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyCollection<T>)new List<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyCollection<T>)new ArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyCollection<T>)new ReadOnlyArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IReadOnlyCollection<T>)new CollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject(new ReadOnlyCollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)values, memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)new ArraySegment<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)new List<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)new ArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)new ReadOnlyArrayWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)new CollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject((IEnumerable<T>)new ReadOnlyCollectionWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject(new EnumerableWrapper<T>(values), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;
                thisCount = parser.WriteObject(values.AsSpan(), memStream);
                Assert.That(thisCount, Is.EqualTo(lastCount));
                ct += thisCount;

                if (bitArrayParser != null)
                {
                    thisCount = bitArrayParser.WriteObject(new BitArray((bool[])(object)values), memStream);
                    Assert.That(thisCount, Is.EqualTo(lastCount));
                    ct += thisCount;
                }
            }

            int arrCt = bitArrayParser != null ? arrCtNoBit + 1 : arrCtNoBit;
            Assert.That(memStream.Position, Is.EqualTo(ct + ParserTests.BufferSize));

            Assert.That(((IBinaryTypeParser<T[]>)parser).GetSize(values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyList<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize(new ReadOnlyArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((ICollection<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((ICollection<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((ICollection<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((ICollection<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((ICollection<T>)new CollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyCollection<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyCollection<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyCollection<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyCollection<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyCollection<T>)new ReadOnlyArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IReadOnlyCollection<T>)new CollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize(new ReadOnlyCollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)values) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)new ArraySegment<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)new List<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)new ArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)new ReadOnlyArrayWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)new CollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize((IEnumerable<T>)new ReadOnlyCollectionWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
            Assert.That(parser.GetSize(new EnumerableWrapper<T>(values)) * arrCt, Is.EqualTo(ct));
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
                T[] readArray = ((IBinaryTypeParser<T[]>)parser).ReadObject(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(readArray, IsEqualToCustom(values));
            }

            if (bitArrayParser != null)
            {
                BitArray readArray9 = bitArrayParser.ReadObject(memStream, out int bytesRead);
                pos += bytesRead;

                bool[] output = new bool[readArray9.Count];
                readArray9.CopyTo(output, 0);
                Assert.That(output, IsEqualToCustom(values));
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
                    Assert.That((IEnumerable)readArray, IsEqualToCustom(values));
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
                Assert.That(arr1, IsEqualToCustom(values));
                arr1 = null;
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
                Assert.That(arr2, IsEqualToCustom(values));
                arr2 = null;
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
                Assert.That(arr3, IsEqualToCustom(values));
                arr3 = null;
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            if (typeof(T).IsValueType)
            {
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
                    Assert.That(arr4.ToArray(), IsEqualToCustom(values));
                    if (i > arrCt / 2)
                    {
                        arr4 = new T[values.Length];
                    }
                    else
                    {
                        arr4 = null;
                    }
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
                    Assert.That(arr5.ToArray(), IsEqualToCustom(values));
                    arr5 = null;
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }
            else if (typeof(T) == typeof(string))
            {
                memStream.Seek(0, SeekOrigin.Begin);
                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;
                Span<string> arr4 = null!;
                Span<string>* spanPtr = &arr4;
                tr = __makeref(spanPtr);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(memStream, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr4.ToArray(), IsEqualToCustom(values));
                    if (i > arrCt / 2)
                    {
                        arr4 = new string[values.Length];
                    }
                    else
                    {
                        arr4 = null;
                    }
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

                memStream.Seek(0, SeekOrigin.Begin);
                ParserTests.CheckBuffer(memStream);
                pos = ParserTests.BufferSize;
                ReadOnlySpan<string> arr5 = null!;
                ReadOnlySpan<string>* roSpanPtr = &arr5;
                tr = __makeref(roSpanPtr);
                for (int i = 0; i < arrCt; ++i)
                {
                    parser.ReadObject(memStream, out int bytesRead, tr);
                    pos += bytesRead;
                    Assert.That(arr5.ToArray(), IsEqualToCustom(values));
                    arr5 = null;
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }


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
                    Assert.That(output, IsEqualToCustom(values));
                    arr6 = null;
                }

                Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
                Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream
            }

            memStream.Seek(0, SeekOrigin.Begin);
            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;
            ArraySegment<T> arr7 = default!;
            tr = __makeref(arr7);
            for (int i = 0; i < arrCt; ++i)
            {
                parser.ReadObject(memStream, out int bytesRead, tr);
                pos += bytesRead;
                T[] toArray = new T[arr7.Count];
                Array.Copy(arr7.Array, arr7.Offset, toArray, 0, toArray.Length);
                Assert.That(toArray, IsEqualToCustom(values));
                arr7 = default;
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

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
                Assert.That(output.ToArray(), IsEqualToCustom(values));
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
                Assert.That(output.ToArray(), IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
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
                Assert.That(outputList, IsEqualToCustom(values));
            }

            /*
             * read to array segment (read len, set)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                int size = parser.ReadArrayLength(memStream, out int bytesRead);
                pos += bytesRead;
                Assert.That(size, Is.EqualTo(values.Length));

                ArraySegment<T> arr = new ArraySegment<T>(new T[size + 2], 1, size);
                int readCt = parser.ReadObject(memStream, arr, out bytesRead, measuredCount: size, hasReadLength: true, setInsteadOfAdding: true);
                Assert.That(readCt, Is.EqualTo(arr.Count));
                pos += bytesRead;
                Assert.That(arr, IsEqualToCustom(values));
            }

            Assert.That(pos, Is.EqualTo(ct + ParserTests.BufferSize));
            Assert.That(memStream.ReadByte(), Is.EqualTo(-1)); // end of stream

            /*
             * read to array segment (dont read len, set)
             */
            memStream.Seek(0, SeekOrigin.Begin);

            ParserTests.CheckBuffer(memStream);
            pos = ParserTests.BufferSize;

            for (int i = 0; i < arrCt; ++i)
            {
                ArraySegment<T> arr = new ArraySegment<T>(new T[values.Length + 2], 1, values.Length);
                int readCt = parser.ReadObject(memStream, arr, out int bytesRead, hasReadLength: false, setInsteadOfAdding: true);
                Assert.That(readCt, Is.EqualTo(arr.Count));
                pos += bytesRead;
                Assert.That(arr, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
                    Assert.That(outputList, IsEqualToCustom(values));
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
internal class EnumerableWrapper<T> : IEnumerable<T>
{
    public IEnumerable<T> Enumerable { get; }
    public EnumerableWrapper(IEnumerable<T> enu)
    {
        Enumerable = enu;
    }

    public IEnumerator<T> GetEnumerator() => Enumerable.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Enumerable.GetEnumerator();
}
internal class CollectionWrapper<T> : ICollection<T>, IReadOnlyCollection<T>
{
    public ICollection<T> Collection { get; }
    public CollectionWrapper(ICollection<T> enu)
    {
        Collection = enu;
    }

    public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Collection.GetEnumerator();
    public void Add(T item) => Collection.Add(item);
    public void Clear() => Collection.Clear();
    public bool Contains(T item) => Collection.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => Collection.CopyTo(array, arrayIndex);
    public bool Remove(T item) => Collection.Remove(item);
    public int Count => Collection.Count;
    public bool IsReadOnly => Collection.IsReadOnly;
}
internal class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
{
    public IReadOnlyCollection<T> Collection { get; }
    public ReadOnlyCollectionWrapper(IReadOnlyCollection<T> enu)
    {
        Collection = enu;
    }

    public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Collection.GetEnumerator();
    public int Count => Collection.Count;
}
internal class ReadOnlyArrayWrapper<T> : IReadOnlyList<T>
{
    public T[] Array { get; }
    public ReadOnlyArrayWrapper(T[] array)
    {
        Array = array;
    }
    public IEnumerator<T> GetEnumerator() => ((IList<T>)Array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Array.GetEnumerator();
    public int Count => ((IList<T>)Array).Count;
    public T this[int index]
    {
        get => Array[index];
        set => Array[index] = value;
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
