using BenchmarkDotNet.Running;

namespace SharpFetch.Benchmark;

internal class Program
{
    public static void Main(string[] args)
    {
        // Run benchmarks via BenchmarkDotNet
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
