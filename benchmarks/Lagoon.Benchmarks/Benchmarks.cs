using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Lagoon.Benchmarks
{
    public class Benchmarks : IDisposable
    {
        private IObjectPool<IFoo> _pool;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _pool = new DefaultObjectPool<IFoo>(new FooFactory());
        }

        [Benchmark]
        public async Task<Guid> NoPool()
        {
            using var foo = new Foo();
            await foo.Initialize();
            return foo.Id;
        }

        [Benchmark]
        public async Task<Guid> WithPool()
        {
            using var foo = await _pool.GetObjectAsync();
            return foo.Id;
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _pool.Dispose();
        }
    }
}