// Copyright (c) Greg Bair. All rights reserved.
// Licensed under MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Lagoon
{
    /// <summary>
    /// An exception for when something goes wrong.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PoolException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoolException"/> class.
        /// </summary>
        /// <param name="message">The message to present.</param>
        public PoolException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolException"/> class.
        /// </summary>
        public PoolException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolException"/> class.
        /// </summary>
        /// <param name="message">The message to present.</param>
        /// <param name="innerException">Any inner exception thrown.</param>
        public PoolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}