using System;
using System.Threading.Tasks;

namespace Lagoon.Benchmarks
{
    public sealed class Foo : IFoo
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void Dispose()
        {

        }

        public async Task Initialize() => await Task.Delay(TimeSpan.FromMilliseconds(500));
    }
}