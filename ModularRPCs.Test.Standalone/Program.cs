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
         * last run 2024-06-12 02:25:36Z
         *
         * 546/546 passed
         *
         */

        new AutoRun(Assembly.GetAssembly(typeof(ParserTests))).Execute(args, new ColorConsoleWriter(true), Console.In);
    }
}