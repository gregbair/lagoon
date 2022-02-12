// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Lagoon
{
    /// <summary>
    /// Options for an <see cref="DefaultObjectPool{TObject}"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ObjectPoolOptions
    {
        /// <summary>
        /// Gets or sets the min number of objects allowed in the pool.
        /// </summary>
        public int MinObjects { get; set; } = 3;

        /// <summary>
        /// Gets or sets the max number of objects allowed in the pool.
        /// </summary>
        public int MaxObjects { get; set; } = 10;

        /// <summary>
        /// Gets or sets the frequency with which pruning and growing are checked.
        /// </summary>
        public TimeSpan SweepFrequency { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the amount of time to wait for acquisition before throwing an exception.
        /// </summary>
        public TimeSpan AcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}