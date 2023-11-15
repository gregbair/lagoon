// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using System;

namespace Lagoon
{
    /// <summary>
    /// A proxy to wrap objects of type <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">The type of object to wrap.</typeparam>
    public sealed class PooledObjectWrapper<TObject> : IInterceptor, IDisposable
        where TObject : class, IDisposable
    {
        private static readonly ProxyGenerator Generator = new ProxyGenerator();

        private readonly IObjectPool<TObject> _pool;
        private readonly TObject _actual;

        /// <summary>
        /// Gets the ID of this proxy.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the actual object that's wrapped in this proxy.
        /// </summary>
        public TObject Proxy { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledObjectWrapper{TObject}"/> class.
        /// </summary>
        /// <param name="pool">The pool this wrapper belongs to.</param>
        /// <param name="actual">The object to be wrapped.</param>
        public PooledObjectWrapper(IObjectPool<TObject> pool, TObject actual)
        {
            ArgumentNullException.ThrowIfNull(actual);
            ArgumentNullException.ThrowIfNull(pool);
            _actual = actual;
            _pool = pool;
            var proxy = Generator.CreateInterfaceProxyWithTarget(actual, this);
            Proxy = proxy;
            Id = Guid.NewGuid();
        }

        /// <inheritdoc />
        public void Intercept(IInvocation invocation)
        {
            ArgumentNullException.ThrowIfNull(invocation);

            if (!invocation.Method.Name.Equals("Dispose", StringComparison.OrdinalIgnoreCase))
            {
                invocation.Proceed();
            }
            else
            {
                _pool.ReturnObject(this);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _actual.Dispose();
        }
    }
}