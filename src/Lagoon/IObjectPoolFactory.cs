// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lagoon
{
    /// <summary>
    /// Creates and activates an object.
    /// </summary>
    /// <typeparam name="TObject">The object to create and activate.</typeparam>
    public interface IObjectPoolFactory<TObject>
        where TObject : class, IDisposable
    {
        /// <summary>
        /// Creates an object.
        /// </summary>
        /// <returns>The object.</returns>
        TObject Create();

        /// <summary>
        /// Activates an object.
        /// </summary>
        /// <param name="obj">The object to activate.</param>
        /// <param name="token">Token used for cancellation.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task ActivateAsync(TObject obj, CancellationToken token = default);
    }
}