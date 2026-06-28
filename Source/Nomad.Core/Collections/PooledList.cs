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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class PooledList<T> : IDisposable
    {
        private T[]? _items;
        private readonly ArrayPool<T> _pool;
        private readonly bool _clearOnReturn;
        private int _count;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _items == null ? 0 : _items.Length; }
        }

        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Items.AsSpan(0, _count); }
        }

        public PooledList(int initialCapacity = 16, bool clearOnReturn = true, ArrayPool<T>? pool = null)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _pool = pool ?? ArrayPool<T>.Shared;
            _clearOnReturn = clearOnReturn;
            _items = _pool.Rent(initialCapacity);
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert((uint)index < (uint)_count);
                return ref Items[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (_count == Items.Length)
            {
                Grow(_count + 1);
            }

            Items[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddUninitialized()
        {
            if (_count == Items.Length)
            {
                Grow(_count + 1);
            }

            int index = _count++;
            return ref Items[index];
        }

        public void AddRange(ReadOnlySpan<T> values)
        {
            EnsureCapacity(_count + values.Length);
            values.CopyTo(Items.AsSpan(_count));
            _count += values.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            int last = _count - 1;
            Items[index] = Items[last];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Items[last] = default!;
            }
            _count = last;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > Items.Length)
            {
                Grow(capacity);
            }
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(Items, 0, _count);
            }
            _count = 0;
        }

        public void Dispose()
        {
            T[]? items = _items;
            if (items == null)
            {
                return;
            }

            _items = null;
            _count = 0;
            _pool.Return(items, _clearOnReturn);
        }

        private void Grow(int required)
        {
            T[] oldItems = Items;
            int newCapacity = oldItems.Length << 1;
            if (newCapacity < required)
            {
                newCapacity = required;
            }

            T[] newItems = _pool.Rent(newCapacity);
            Array.Copy(oldItems, 0, newItems, 0, _count);
            _pool.Return(oldItems, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _items = newItems;
        }

        private T[] Items
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                T[]? items = _items;
                if (items == null)
                {
                    ThrowDisposed();
                }
                return items!;
            }
        }

        private static void ThrowDisposed()
        {
            throw new ObjectDisposedException(nameof(PooledList<T>));
        }
    }
}
