#nullable enable
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Serialization.Parsers;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text;

namespace ModularRPCs.Test.Many;
partial class ParserManyTests
{
    [Test]
    [TestCase(Utf8ParserTestCases.TestCaseSmall1)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall2)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall3)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall4)]
    [TestCase(Utf8ParserTestCases.TestCaseSmall5)]
    [TestCase(Utf8ParserTestCases.TestCaseMed1)]
    [TestCase(Utf8ParserTestCases.TestCaseMed2)]
    [TestCase(Utf8ParserTestCases.TestCaseMed3)]
    [TestCase(Utf8ParserTestCases.TestCaseMed4)]
    [TestCase(Utf8ParserTestCases.TestCaseMed5)]
    [TestCase(Utf8ParserTestCases.TestCaseMed6)]
    [TestCase(Utf8ParserTestCases.TestCaseLong1)]
    [TestCase(Utf8ParserTestCases.TestCaseLong2)]
    [TestCase(Utf8ParserTestCases.TestCaseLong3)]
    [TestCase(Utf8ParserTestCases.TestCase7BitSmall1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitSmall2)]
    [TestCase(Utf8ParserTestCases.TestCase7BitMed1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitMed2)]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong1)]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong2)]
    [TestCase(Utf8ParserTestCases.TestCaseLong3 + "," + Utf8ParserTestCases.TestCaseLong2)]
    [TestCase(Utf8ParserTestCases.TestCaseLong3 + "," + Utf8ParserTestCases.TestCaseLong1)]
    [TestCase(Utf8ParserTestCases.TestCaseLong3 + "," + Utf8ParserTestCases.TestCaseSmall5)]
    [TestCase(Utf8ParserTestCases.TestCase7BitLong2 + "," + Utf8ParserTestCases.TestCaseMed3)]
    [TestCase("test string")]
    [TestCase("")]
    [TestCase(280)]
    [TestCase(65580)]
    public void TestString(object ctOrString)
    {
        if (ctOrString is not string stri)
        {
            int ct = (int)ctOrString;
            StringBuilder str = new StringBuilder(ct);
            Random r = new Random();
            for (int i = 0; i < ct; ++i)
            {
                if (i != 0)
                {
                    str.Append(",");
                }
                bool isNull = r.NextDouble() > 0.95d;
                if (isNull)
                {
                    str.Append("null");
                }
                else
                {
                    int len = r.Next(0, 65581);
                    len /= r.Next(1, 5001);
                    char[] arr = new char[len];
                    for (int j = 0; j < len; ++j)
                    {
                        char c;
                        do
                        {
                            c = (char)r.Next(32, ushort.MaxValue);
                        } while (!char.IsLetterOrDigit(c) && !char.IsPunctuation(c));
                        arr[j] = c;
                    }
                    string str2 = new string(arr).Replace(",", " ");
                    str.Append(str2);
                }
            }
            stri = str.ToString();
        }

        string?[] stringArr = stri.Length == 0 ? Array.Empty<string>() : stri.Split(',').Select(x => x == "null" ? null : x).ToArray();

        Utf8Parser.Many parser = new Utf8Parser.Many(new SerializationConfiguration());
        TestManyParserBytes(stringArr, parser);
        TestManyParserStream(stringArr, parser);
    }}
