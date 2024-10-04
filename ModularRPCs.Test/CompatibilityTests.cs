using System;
using DanielWillett.ModularRpcs.Reflection;
using NUnit.Framework;

namespace ModularRPCs.Test;

[NonParallelizable]
public class CompatibilityTests
{
    [Test]
    [TestCase("2023.4.0f1"  , true)]
    [TestCase("2021.2.0f1"  , true)]
    [TestCase("2020.3.19f1" , false)]
    [TestCase("2017.3.29f1" , false)]
    [TestCase("6000.0.3f1"  , true)]
    [TestCase("5.6.7f1"     , false)]
    public void TestCanUnityVersionUseMemoryCopyOnOverlappedBuffers(string version, bool expectedResult)
    {
        Assert.That(Compatibility.CanUnityVersionUseMemoryCopyOnOverlappedBuffers(version), Is.EqualTo(expectedResult));
    }

    [Test]
    [TestCase("3.12.0 (tarball Sat Feb  7 19:13:43 UTC 2015)", "3.12.0")]
    [TestCase("6.13.0 (Visual Studio built mono)", "6.13.0")]
    [TestCase("6.12.0 (Visual Studio built mono)", "6.12.0")]
    [TestCase(" 6.12.0 ()", "6.12.0")]
    [TestCase(" 6.12.0 (", "6.12.0")]
    [TestCase(" 6.12.0 ", "6.12.0")]
    [TestCase(" 6.12.0 )", "6.12.0")]
    [TestCase("6.12.0", "6.12.0")]
    [TestCase(" 6.10.0", "6.10.0")]
    public void TestParseMonoVersion(string rawVersion, string expectedVersion)
    {
        Version exVsn = Version.Parse(expectedVersion);
        Version parsedVersion = MonoImpl.ParseMonoVersion(rawVersion);

        Assert.That(parsedVersion, Is.EqualTo(exVsn));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void TestNoParseMonoVersion(string rawVersion)
    {
        Version parsedVersion = MonoImpl.ParseMonoVersion(rawVersion);

        Assert.That(parsedVersion, Is.Null);
    }

    [Test]
    [TestCase("7.0.0", true)]
    [TestCase("6.13.0", true)]
    [TestCase("6.12.0", true)]
    [TestCase("6.11.9", false)]
    [TestCase("6.11.0", false)]
    [TestCase("3.12.0", false)]
    public void TestCanMonoVersionUseMemoryCopyOnOverlappedBuffers(string monoVersion, bool expectedResult)
    {
        Version vsn = Version.Parse(monoVersion);
        Assert.That(Compatibility.CanMonoVersionUseMemoryCopyOnOverlappedBuffers(vsn), Is.EqualTo(expectedResult));
    }
}