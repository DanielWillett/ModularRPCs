using DanielWillett.ModularRpcs.Reflection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
// ReSharper disable LocalizableElement

namespace ModularRPCs.Test
{
    internal unsafe class AssemblyQualifiedNameTests
    {
#if NET7_0_OR_GREATER
        private const int OptionCt = 14;
#else
        private const int OptionCt = 16;
#endif

        // ReSharper disable once UseCollectionExpression
        // ReSharper disable once RedundantExplicitArraySize
        private static readonly Type[] Options = new Type[OptionCt]
        {
            typeof(List<string>),
            typeof(string[]),
            typeof(KeyValuePair<string[], AssemblyQualifiedNameTests>),
            typeof(AssemblyQualifiedNameTests),
            typeof(Generic<AssemblyQualifiedNameTests, int, ValueTuple<ushort, short>>),
            typeof(int*),
            typeof(Struct*),
            typeof(Generic<AssemblyQualifiedNameTests, int, ValueTuple<ushort, short>>.Nested),
            typeof(Generic<AssemblyQualifiedNameTests, int, ValueTuple<ushort, short>>.NestedGeneric<Version>),
            typeof(Generic<AssemblyQualifiedNameTests, int, ValueTuple<ushort, short>>.NestedGeneric<Version>.DoubleNestedGeneric<string>),
            typeof(Struct.Nested),
            typeof(int[][]),
            typeof(int[][,]),
            typeof(int[,][]),
            typeof(int*[,][]),
#if NET7_0_OR_GREATER
            typeof(int[,]*[]),
            typeof(int[,][]*),
#endif
            typeof(Struct.NestedGeneric<Version>)
        };

        [Test]
        public void AsmQualifiedNameNoVersion([Range(0, OptionCt - 1)] int num)
        {
            Type expected = Options[num];
            string name = TypeUtility.GetAssemblyQualifiedNameNoVersion(expected);

            Console.WriteLine($"Generated: \"{name}\".");
            Console.WriteLine($"Expected:  \"{expected.AssemblyQualifiedName}\".");

            Type foundType = Type.GetType(name, ignoreCase: true, throwOnError: true);

            Assert.That(foundType, Is.EqualTo(expected));

            Assert.That(foundType, Is.Not.Null);
            Assert.That(foundType, Is.EqualTo(expected));
            Assert.That(TypeUtility.GetAssemblyQualifiedNameNoVersion(foundType), Is.EqualTo(name));
        }

        [Test]
        public void AsmQualifiedNameNoVersionLowerBoundedArrayLen1()
        {
            Type expected = Array.CreateInstance(typeof(int), new int[] { 1 }, new int[] { 1 }).GetType();
            string name = TypeUtility.GetAssemblyQualifiedNameNoVersion(expected);

            Console.WriteLine($"Generated: \"{name}\".");
            Console.WriteLine($"Expected:  \"{expected.AssemblyQualifiedName}\".");

            Type foundType = Type.GetType(name, ignoreCase: true, throwOnError: true);

            Assert.That(foundType, Is.EqualTo(expected));

            Assert.That(foundType, Is.Not.Null);
            Assert.That(foundType, Is.EqualTo(expected));
            Assert.That(TypeUtility.GetAssemblyQualifiedNameNoVersion(foundType), Is.EqualTo(name));
        }

        [Test]
        public void AsmQualifiedNameNoVersionLowerBoundedArrayLenMore()
        {
            Type expected = Array.CreateInstance(typeof(int), new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }).GetType();
            string name = TypeUtility.GetAssemblyQualifiedNameNoVersion(expected);

            Console.WriteLine($"Generated: \"{name}\".");
            Console.WriteLine($"Expected:  \"{expected.AssemblyQualifiedName}\".");

            Type foundType = Type.GetType(name, ignoreCase: true, throwOnError: true);

            Assert.That(foundType, Is.EqualTo(expected));

            Assert.That(foundType, Is.Not.Null);
            Assert.That(foundType, Is.EqualTo(expected));
            Assert.That(TypeUtility.GetAssemblyQualifiedNameNoVersion(foundType), Is.EqualTo(name));
        }

        private struct Generic<T1, T2, T3>
        {
            public struct NestedGeneric<T4>
            {
                public struct DoubleNestedGeneric<T5> { }
            }

            public struct Nested { }
        }

        private struct Struct
        {
            public struct NestedGeneric<T4> { }
            public struct Nested { }
        }
    }
}
