using System;
using System.Runtime.CompilerServices;

namespace LazyRedpaw.Utilities
{
    public static class CollectionThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowArgumentNull(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowArgumentOutOfRange(string paramName = "index")
        {
            throw new ArgumentOutOfRangeException(paramName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowArgumentOutOfRange(string paramName, string message)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowArgumentException(string message)
        {
            throw new ArgumentException(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowInvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}