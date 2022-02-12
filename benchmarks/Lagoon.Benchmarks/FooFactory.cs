using System.Threading;
using System.Threading.Tasks;

namespace Lagoon.Benchmarks
{
    public class FooFactory : IObjectPoolFactory<IFoo>
    {
        public IFoo Create()
        {
            return new Foo();
        }

        public async Task ActivateAsync(IFoo obj, CancellationToken token = default) => await obj.Initialize();
    }
}