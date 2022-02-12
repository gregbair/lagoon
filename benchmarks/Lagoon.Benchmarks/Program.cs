using BenchmarkDotNet.Running;

namespace Lagoon.Benchmarks
{
    internal static class Program
    {
        private static void Main()
        {
            var unused = BenchmarkRunner.Run<Benchmarks>();
        }
    }
}