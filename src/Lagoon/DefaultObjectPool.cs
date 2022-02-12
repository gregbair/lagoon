// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lagoon
{
    /// <summary>
    /// Standard implementation of <see cref="IObjectPool{TObject}"/>.
    /// </summary>
    /// <typeparam name="TObject">The type of object to pool.</typeparam>
    public sealed class DefaultObjectPool<TObject> : IObjectPool<TObject>
        where TObject : class, IDisposable
    {
        private readonly IObjectPoolFactory<TObject> _factory;

        private readonly ObjectPoolOptions _options;

        private readonly ConcurrentStack<PooledObjectWrapper<TObject>> _available =
            new ConcurrentStack<PooledObjectWrapper<TObject>>();

        private readonly ConcurrentDictionary<Guid, PooledObjectWrapper<TObject>> _active =
            new ConcurrentDictionary<Guid, PooledObjectWrapper<TObject>>();

        private readonly CancellationTokenSource _backgroundTokenSource = new CancellationTokenSource();

        private readonly Timer _pruneTimer;

        private readonly Timer _growTimer;

        private bool _isDisposed;

        /// <inheritdoc />
        public Func<TObject, bool> ObjectActivator { get; }

        /// <inheritdoc/>
        public Func<TObject, bool> ObjectPassivator { get; }

        /// <inheritdoc />
        public int AvailableCount => _available.Count;

        /// <inheritdoc/>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultObjectPool{TObject}"/> class.
        /// </summary>
        /// <param name="factory">The factory to produce objects of type <typeparamref name="TObject"/>.</param>
        /// <param name="options">Configuration options for the pool.</param>
        /// <param name="objectActivator">The activator for the object.</param>
        /// <param name="objectPassivator">The passivator for the object.</param>
        public DefaultObjectPool(IObjectPoolFactory<TObject> factory, ObjectPoolOptions? options = null, Func<TObject, bool>? objectActivator = null, Func<TObject, bool>? objectPassivator = null)
        {
            ObjectActivator = objectActivator ?? (_ => true);
            ObjectPassivator = objectPassivator ?? (_ => true);
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _options = options ?? new ObjectPoolOptions();

            _pruneTimer = new Timer(Prune, null, TimeSpan.Zero, _options.SweepFrequency);
            _growTimer = new Timer(Grow, null, TimeSpan.Zero, _options.SweepFrequency);
        }

        /// <param name="token"></param>
        /// <inheritdoc/>
        public async Task<TObject> GetObjectAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (_available.TryPop(out var existing))
            {
                _active.TryAdd(existing.Id, existing);
                return existing.Proxy;
            }

            if (_active.Count >= _options.MaxObjects)
            {
                return await BlockAcquisition(token).ConfigureAwait(false);
            }

            var wrapper = await CreateObject(token).ConfigureAwait(false);
            _active.TryAdd(wrapper.Id, wrapper);

            return wrapper.Proxy;
        }

        private async Task<PooledObjectWrapper<TObject>> CreateObject(CancellationToken token = default)
        {
            TObject? obj;
            try
            {
                obj = _factory.Create();
            }
            catch (Exception ex)
            {
                throw new PoolException("Exception thrown when instantiating object", ex);
            }

            if (ObjectActivator(obj))
            {
                try
                {
                    await _factory.ActivateAsync(obj, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new PoolException("Exception thrown when activating object", ex);
                }
            }

            var wrapper = new PooledObjectWrapper<TObject>(this, obj);
            return wrapper;
        }

        private void Prune(object? state)
        {
            var currentSize = _active.Count + _available.Count;
            var minPoolSize = _options.MinObjects;

            if (currentSize <= minPoolSize || !_available.Any())
            {
                return;
            }

            var numObjToPrune = _available.Count;

            for (var i = 0; i < numObjToPrune && currentSize > minPoolSize; i++)
            {
                if (!_available.TryPop(out var obj))
                {
                    break;
                }

                obj.Dispose();
                currentSize--;
            }
        }

        private void Grow(object? state)
        {
            _ = GrowAsync(_options.MinObjects);
        }

        private async Task GrowAsync(int size)
        {
            var count = 0;
            while (_active.Count + _available.Count < size && count < size * 2)
            {
                try
                {
#pragma warning disable CA2000
                    var obj = await CreateObject().ConfigureAwait(false);
#pragma warning restore CA2000
                    _available.Push(obj);
                }
                catch (Exception ex)
                {
                    throw new PoolException("Some exception", ex);
                }
            }
        }

        private async Task<TObject> BlockAcquisition(CancellationToken token)
        {
            var timer = Stopwatch.StartNew();
            var timeout = _options.AcquisitionTimeout;
            while (true)
            {
                if (_available.TryPop(out var existing))
                {
                    _active.TryAdd(existing.Id, existing);
                    return existing.Proxy;
                }

                if (timer.Elapsed > timeout)
                {
                    throw new PoolExhaustedException("Pool empty and timeout exceeded");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10), token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public void ReturnObject(PooledObjectWrapper<TObject> wrapper)
        {
            if (wrapper is null)
            {
                throw new ArgumentNullException(nameof(wrapper));
            }

            if (_active.TryGetValue(wrapper.Id, out var proxy) && _active.TryRemove(proxy.Id, out _))
            {
                _available.Push(wrapper);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all managed references.
        /// </summary>
        /// <param name="disposing">Whether we're actually disposing.</param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                if (!_active.IsEmpty)
                {
                    throw new InvalidOperationException($"There are still {_active.Count} active object(s)");
                }

                foreach (var proxy in _available)
                {
                    if (ObjectPassivator(proxy.Proxy))
                    {
                        proxy.Dispose();
                    }
                }

                _pruneTimer.Dispose();
                _growTimer.Dispose();
                _backgroundTokenSource.Cancel();
                _backgroundTokenSource.Dispose();
            }

            _isDisposed = true;
        }
    }
}