// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

[assembly: CLSCompliant(true)]

namespace Lagoon.DependencyInjection
{
    /// <summary>
    /// Extension methods to add object pools to dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IObjectPool{TObject}"/> to dependency injection.
        /// </summary>
        /// <param name="services">The service collection to add the pool to.</param>
        /// <param name="optionsAction">An action to modify options for the pool.</param>
        /// <param name="objectActivator">An activator for the pool.</param>
        /// <param name="objectPassivator">A passivator for the pool.</param>
        /// <typeparam name="TObject">The type of object pool to create.</typeparam>
        /// <returns>The service collection passed in.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddObjectPool<TObject>(
            this IServiceCollection services,
            Action<ObjectPoolOptions>? optionsAction = null,
            Func<TObject, bool>? objectActivator = null,
            Func<TObject, bool>? objectPassivator = null)
            where TObject : class, IDisposable
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new ObjectPoolOptions();
            optionsAction?.Invoke(options);

            services.TryAddSingleton<IObjectPool<TObject>>(sp =>
                new DefaultObjectPool<TObject>(
                    sp.GetRequiredService<IObjectPoolFactory<TObject>>(),
                    options,
                    objectActivator,
                    objectPassivator));

            return services;
        }
    }
}