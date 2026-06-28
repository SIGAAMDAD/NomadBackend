/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class PooledArray<T> : IDisposable
    {
        private T[]? _array;
        private readonly ArrayPool<T> _pool;
        private readonly bool _clearOnReturn;

        public int Length { get; private set; }

        public T[] Raw
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                T[]? array = _array;
                if (array == null)
                {
                    ThrowDisposed();
                }
                return array!;
            }
        }

        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Raw.AsSpan(0, Length); }
        }

        public PooledArray(int length, bool clearOnReturn = true, ArrayPool<T>? pool = null)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _pool = pool ?? ArrayPool<T>.Shared;
            _clearOnReturn = clearOnReturn;
            _array = _pool.Rent(length);
            Length = length;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Raw[index]; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearUsed()
        {
            Array.Clear(Raw, 0, Length);
        }

        public void Dispose()
        {
            T[]? array = _array;
            if (array == null)
            {
                return;
            }

            _array = null;
            Length = 0;
            _pool.Return(array, _clearOnReturn);
        }

        private static void ThrowDisposed()
        {
            throw new ObjectDisposedException(nameof(PooledArray<T>));
        }
    }
}
