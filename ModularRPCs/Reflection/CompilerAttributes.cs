using System;
// ReSharper disable once CheckNamespace

namespace DanielWillett.ModularRpcs.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute
    {
        public string AssemblyName { get; } = assemblyName;
    }
}