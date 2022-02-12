using System;
using FluentAssertions;
using Moq;
using Xunit;

namespace Lagoon.Tests
{
    public class PooledObjectWrapperTests
    {
        [Fact]
        public void ConstructorThrowsNullActual()
        {
            Action act = () =>
                new PooledObjectWrapper<ISomeInterface>(new Mock<IObjectPool<ISomeInterface>>().Object, null!);
            act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("actual");
        }

        [Fact]
        public void ConstructorThrowsNullPool()
        {
            Action act = () => new PooledObjectWrapper<ISomeInterface>(null!, new Mock<ISomeInterface>().Object);
            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pool");
        }

        [Fact]
        public void MethodsPassThrough()
        {
            var mockPool = new Mock<IObjectPool<ISomeInterface>>();
            var mockObj = new Mock<ISomeInterface>();
            var sut = new PooledObjectWrapper<ISomeInterface>(mockPool.Object, mockObj.Object);
            sut.Proxy.Foo();

            mockObj.Verify(x => x.Foo(), Times.Once);
        }

        [Fact]
        public void DisposePassesToPool()
        {
            var mockPool = new Mock<IObjectPool<ISomeInterface>>();
            var mockObj = new Mock<ISomeInterface>();
            var sut = new PooledObjectWrapper<ISomeInterface>(mockPool.Object, mockObj.Object);
            sut.Proxy.Dispose();

            mockObj.Verify(x => x.Dispose(), Times.Never);
            mockPool.Verify(x => x.ReturnObject(sut), Times.Once);
        }

        [Fact]
        public void InterceptThrowsNullInvocation()
        {
            var sut = new PooledObjectWrapper<ISomeInterface>(new Mock<IObjectPool<ISomeInterface>>().Object,
                new Mock<ISomeInterface>().Object);

            Action act = () => sut.Intercept(null!);
            act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("invocation");
        }



        public interface ISomeInterface : IDisposable
        {
            public void Foo() { }
        }
    }
}