using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace OMODFramework.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
