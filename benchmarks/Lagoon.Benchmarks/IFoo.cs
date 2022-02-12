using System;
using System.Threading.Tasks;

namespace Lagoon.Benchmarks
{
    public interface IFoo : IDisposable
    {
        Guid Id { get; }
        Task Initialize();
    }
}