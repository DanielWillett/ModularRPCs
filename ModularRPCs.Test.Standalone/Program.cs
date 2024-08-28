using NUnit.Common;
using NUnitLite;
using System;
using System.Reflection;

namespace ModularRPCs.Test.Standalone;

internal class Program
{
    static void Main(string[] args)
    {
        /*
         *
         * this is used to run tests in Mono
         *
         * last run 2024-08-28 02:05:53Z
         *
         * 554/554 passed
         *
         */

        new AutoRun(Assembly.GetAssembly(typeof(ParserTests))).Execute(args, new ColorConsoleWriter(true), Console.In);
    }
}