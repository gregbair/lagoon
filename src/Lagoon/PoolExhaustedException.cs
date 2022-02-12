// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Lagoon
{
    /// <summary>
    /// An exception for when the pool has no available objects.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PoolExhaustedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoolExhaustedException"/> class.
        /// </summary>
        /// <param name="message">The message to present.</param>
        public PoolExhaustedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolExhaustedException"/> class.
        /// </summary>
        /// <param name="message">The message to present.</param>
        /// <param name="innerException">Any inner exception thrown.</param>
        public PoolExhaustedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolExhaustedException"/> class.
        /// </summary>
        public PoolExhaustedException()
        {
        }
    }
}