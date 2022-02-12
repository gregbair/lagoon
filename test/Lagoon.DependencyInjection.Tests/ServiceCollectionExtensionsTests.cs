using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lagoon.DependencyInjection.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void ThrowsNullServices()
        {
            Action act = () => ServiceCollectionExtensions.AddObjectPool<SomeClass>(null!);
            act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("services");
        }

        [Fact]
        public void RegistersPool()
        {
            var sc = new ServiceCollection();

            Func<SomeClass, bool> activator = _ => true;
            Func<SomeClass, bool> passivator = _ => true;
            sc.AddSingleton<IObjectPoolFactory<SomeClass>, SomeFactory>();

            sc.AddObjectPool(options =>
                {
                    options.MaxObjects = 42;
                },
                activator,
                passivator);

            var sp = sc.BuildServiceProvider();

            var pool = sp.GetRequiredService<IObjectPool<SomeClass>>();
            pool.ObjectActivator.Should().Be(activator);
            pool.ObjectPassivator.Should().Be(passivator);
        }

        [Fact]
        public void RealizationThrowsNoFactory()
        {
            var sc = new ServiceCollection();

            sc.AddObjectPool<SomeClass>();

            var sp = sc.BuildServiceProvider();
            Action act = () => sp.GetRequiredService<IObjectPool<SomeClass>>();
            act.Should().ThrowExactly<InvalidOperationException>().Which.Message.Should()
                .ContainEquivalentOf("No service for type 'Lagoon.IObjectPoolFactory");
        }

        public interface ISomeInterface : IDisposable
        {

        }

        public class SomeFactory : IObjectPoolFactory<SomeClass>
        {
            public SomeClass Create() => new SomeClass();

            public Task ActivateAsync(SomeClass obj, CancellationToken token = default) => Task.CompletedTask;
        }

        public class SomeClass : ISomeInterface
        {
            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}