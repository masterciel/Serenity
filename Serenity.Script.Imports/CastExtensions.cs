﻿using System.Runtime.CompilerServices;

namespace Serenity
{
    /// <summary>
    /// Provides an extension method to simulate unchecked casting, to prevent Saltarelle type checks
    /// </summary>
    [Imported]
    public static class CastExtensions
    {
        /// <summary>
        /// Casts an object to another type without checking
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="value">The value to cast</param>
        /// <returns>Value itself of requested type</returns>
        [ScriptSkip]
        public static T As<T>(this object value)
        {
            return default(T);
        }
    }
}