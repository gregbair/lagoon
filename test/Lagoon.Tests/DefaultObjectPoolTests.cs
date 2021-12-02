using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Lagoon.Tests
{
    public class DefaultObjectPoolTests
    {
        [Fact]
        public void ConstructorThrowsNullFactory()
        {
            Action act = () => new DefaultObjectPool<IDisposable>(null!);
            act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("factory");
        }

        [Fact]
        public async Task ActivateTrueActivates()
        {
            var mockFoo = new Mock<IFoo>();
            var factory = new Mock<IObjectPoolFactory<IFoo>>();
            factory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(factory.Object, new ObjectPoolOptions(), _ => true);

            await sut.GetObjectAsync();

            factory.Verify(x => x.ActivateAsync(mockFoo.Object, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ActivateFalseDoesNotActivate()
        {
            var mockFoo = new Mock<IFoo>();
            var factory = new Mock<IObjectPoolFactory<IFoo>>();
            factory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(factory.Object, new ObjectPoolOptions(), _ => false);

            await sut.GetObjectAsync();

            factory.Verify(x => x.ActivateAsync(mockFoo.Object, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PassivateTruePassivates()
        {
            var mockFoo = new Mock<IFoo>();
            var factory = new Mock<IObjectPoolFactory<IFoo>>();
            factory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(factory.Object, objectPassivator: _ => true);

            var obj = await sut.GetObjectAsync();

            obj.Dispose();

            sut.Dispose();
            mockFoo.Verify(x => x.Dispose(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task PassivateFalseDoesNotPassivate()
        {
            var mockFoo = new Mock<IFoo>();
            var factory = new Mock<IObjectPoolFactory<IFoo>>();
            factory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(factory.Object, objectPassivator: _ => false);

            var obj = await sut.GetObjectAsync();

            obj.Dispose();

            sut.Dispose();
            mockFoo.Verify(x => x.Dispose(), Times.Never);
        }

        [Fact]
        public async Task CancelledTokenThrows()
        {
            var mockFoo = new Mock<IFoo>();
            var factory = new Mock<IObjectPoolFactory<IFoo>>();
            factory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(factory.Object);

            var source = new CancellationTokenSource();
            source.Cancel();
            Func<Task> act = async () =>  await sut.GetObjectAsync(source.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GetObjectThrowsFactoryThrowsOnActivate()
        {
            var mockFoo = new Mock<IFoo>();
            var factory = new Mock<IObjectPoolFactory<IFoo>>();
            factory.Setup(x => x.Create()).Returns(mockFoo.Object);
            factory.Setup(x => x.ActivateAsync(mockFoo.Object, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("foo"));

            var sut = new DefaultObjectPool<IFoo>(factory.Object);

            Func<Task> act = async () => await sut.GetObjectAsync();
            var exc = await act.Should().ThrowExactlyAsync<PoolException>();
            exc.Which.Message.Should().ContainEquivalentOf("when activating object");
        }

        [Fact]
        public void ReturnThrowsNullProxy()
        {
            var sut = new DefaultObjectPool<IFoo>(new Mock<IObjectPoolFactory<IFoo>>().Object);
            Action act = () => sut.ReturnObject(null!);
            act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("wrapper");
        }

        [Fact]
        public async Task OneAvailableGetsOneFromPool()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            var mockFoo = new Mock<IFoo>();
            mockFactory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object);

            sut.ActiveCount.Should().Be(0);
            var first = (await sut.GetObjectAsync());
            sut.ActiveCount.Should().Be(1);
            first.Dispose();
            sut.ActiveCount.Should().Be(0);
            await sut.GetObjectAsync();
            sut.ActiveCount.Should().Be(1);
        }

        [Fact]
        public async Task DisposeThrowsActiveObjects()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            var mockFoo = new Mock<IFoo>();
            mockFactory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object);

            await sut.GetObjectAsync();

            Action act = () => sut.Dispose();

            act.Should().ThrowExactly<InvalidOperationException>().Which.Message.Should()
                .ContainEquivalentOf("1 active object(s)");
        }

        [Fact]
        public async Task DisposeDisposesObjects()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            var mockFoo = new Mock<IFoo>();
            mockFactory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object, objectPassivator: _ => true);

            var proxy = await sut.GetObjectAsync();
            proxy.Dispose();

            sut.Dispose();

            mockFoo.Verify(x => x.Dispose(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DisposePassivatorFalseDoesNotDispose()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            var mockFoo = new Mock<IFoo>();
            mockFactory.Setup(x => x.Create()).Returns(mockFoo.Object);

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object, objectPassivator: _ => false);

            var proxy = await sut.GetObjectAsync();
            proxy.Dispose();

            sut.Dispose();

            mockFoo.Verify(x => x.Dispose(), Times.Never);
        }

        [Fact]
        public async Task GetObjectThrowsWhenFactoryThrows()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            mockFactory.Setup(x => x.Create()).Throws<InvalidOperationException>();

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object);

            Func<Task> act = async () => await sut.GetObjectAsync();

            (await act.Should().ThrowExactlyAsync<PoolException>())
                .WithInnerExceptionExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task TimeoutExpires()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            var mockObj = new Mock<IFoo>();
            mockFactory.Setup(x => x.Create()).Returns(mockObj.Object);

            var options = new ObjectPoolOptions
            {
                MaxObjects = 1,
                MinObjects = 0,
                SweepFrequency = TimeSpan.FromMinutes(20),
                AcquisitionTimeout = TimeSpan.FromMilliseconds(500),
            };

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object, options);

            using var obj = await sut.GetObjectAsync();

            Func<Task> act = async () => await sut.GetObjectAsync();

            await act.Should().ThrowExactlyAsync<PoolExhaustedException>();
        }

        [Fact]
        public async Task BackgroundPrunePrunes()
        {
            var mockFactory = new Mock<IObjectPoolFactory<IFoo>>();
            var mockObj = new Mock<IFoo>();
            mockFactory.Setup(x => x.Create()).Returns(mockObj.Object);

            var options = new ObjectPoolOptions
            {
                MaxObjects = 1,
                MinObjects = 0,
                AcquisitionTimeout = TimeSpan.FromMilliseconds(500),
                SweepFrequency = TimeSpan.FromMilliseconds(100),
            };

            var sut = new DefaultObjectPool<IFoo>(mockFactory.Object, options);

            var obj = await sut.GetObjectAsync();

            obj.Dispose();

            await Task.Delay(TimeSpan.FromMilliseconds(150));

            sut.AvailableCount.Should().Be(0);
            sut.ActiveCount.Should().Be(0);
            mockObj.Verify(x => x.Dispose(), Times.Once);
        }
    }

    public interface IFoo : IDisposable
    {

    }

}
