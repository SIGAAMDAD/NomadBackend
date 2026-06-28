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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class PooledBinaryHeap<T, TComparer> : IDisposable where TComparer : struct, IComparer<T>
    {
        private T[]? _items;
        private readonly ArrayPool<T> _pool;
        private TComparer _comparer;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public PooledBinaryHeap(int initialCapacity = 32, TComparer comparer = default(TComparer), ArrayPool<T>? pool = null)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _pool = pool ?? ArrayPool<T>.Shared;
            _comparer = comparer;
            _items = _pool.Rent(initialCapacity);
        }

        public void Push(T value)
        {
            if (_count == Items.Length)
            {
                Grow();
            }

            int index = _count++;
            T[] items = Items;
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (_comparer.Compare(value, items[parent]) >= 0)
                {
                    break;
                }

                items[index] = items[parent];
                index = parent;
            }
            items[index] = value;
        }

        public T PopMin()
        {
            Debug.Assert(_count > 0);
            T[] items = Items;
            T root = items[0];
            T last = items[--_count];
            if (_count > 0)
            {
                SiftDown(0, last);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                items[_count] = default!;
            }

            return root;
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
            _pool.Return(items, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }


        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _items == null ? 0 : _items.Length; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetUnorderedByIndex(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return ref Items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T PeekMinRef()
        {
            Debug.Assert(_count > 0);
            return ref Items[0];
        }

        public T[] ToUnorderedArray()
        {
            T[] result = new T[_count];
            Array.Copy(Items, 0, result, 0, _count);
            return result;
        }

        private void SiftDown(int index, T value)
        {
            T[] items = Items;
            int half = _count >> 1;
            while (index < half)
            {
                int child = (index << 1) + 1;
                int right = child + 1;
                if (right < _count && _comparer.Compare(items[right], items[child]) < 0)
                {
                    child = right;
                }

                if (_comparer.Compare(items[child], value) >= 0)
                {
                    break;
                }

                items[index] = items[child];
                index = child;
            }
            items[index] = value;
        }

        private void Grow()
        {
            T[] old = Items;
            T[] next = _pool.Rent(old.Length << 1);
            Array.Copy(old, 0, next, 0, _count);
            _pool.Return(old, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _items = next;
        }

        private T[] Items { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { if (_items == null) { throw new ObjectDisposedException(nameof(PooledBinaryHeap<T, TComparer>)); } return _items; } }
    }
}
