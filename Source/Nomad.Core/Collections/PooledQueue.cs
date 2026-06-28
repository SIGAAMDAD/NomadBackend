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
    public sealed class PooledQueue<T> : IDisposable
    {
        private T[]? _items;
        private readonly ArrayPool<T> _pool;
        private readonly bool _clearOnReturn;
        private int _head;
        private int _tail;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public int Capacity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _items == null ? 0 : _items.Length; } }

        public PooledQueue(int initialCapacity = 16, bool clearOnReturn = true, ArrayPool<T>? pool = null)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _pool = pool ?? ArrayPool<T>.Shared;
            _clearOnReturn = clearOnReturn;
            _items = _pool.Rent(initialCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if (_count == Items.Length)
            {
                Grow();
            }
            T[] items = Items;
            items[_tail] = item;
            _tail = WrapIncrement(_tail, items.Length);
            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            Debug.Assert(_count > 0);
            T[] items = Items;
            int head = _head;
            T item = items[head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                items[head] = default!;
            }

            _head = WrapIncrement(head, items.Length);
            _count--;
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }
            item = Dequeue();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T PeekRef()
        {
            Debug.Assert(_count > 0);
            return ref Items[_head];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetFromOldest(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            T[] items = Items;
            int physical = _head + index;
            if (physical >= items.Length)
            {
                physical -= items.Length;
            }

            return ref items[physical];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetFromNewest(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            T[] items = Items;
            int physical = _tail - 1 - index;
            if (physical < 0)
            {
                physical += items.Length;
            }

            return ref items[physical];
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && _count > 0)
            {
                T[] items = Items;
                if (_head < _tail)
                {
                    Array.Clear(items, _head, _count);
                }
                else
                {
                    Array.Clear(items, _head, items.Length - _head);
                    Array.Clear(items, 0, _tail);
                }
            }
            _head = 0;
            _tail = 0;
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
            _head = 0;
            _tail = 0;
            _count = 0;
            _pool.Return(items, _clearOnReturn);
        }

        private void Grow()
        {
            T[] oldItems = Items;
            int oldCapacity = oldItems.Length;
            T[] newItems = _pool.Rent(oldCapacity << 1);
            if (_count > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(oldItems, _head, newItems, 0, _count);
                }
                else
                {
                    int rightCount = oldCapacity - _head;
                    Array.Copy(oldItems, _head, newItems, 0, rightCount);
                    Array.Copy(oldItems, 0, newItems, rightCount, _tail);
                }
            }
            _pool.Return(oldItems, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _items = newItems;
            _head = 0;
            _tail = _count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WrapIncrement(int value, int length)
        {
            value++;
            return value == length ? 0 : value;
        }

        private T[] Items
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                T[]? items = _items;
                if (items == null)
                {
                    throw new ObjectDisposedException(nameof(PooledQueue<T>));
                }

                return items!;
            }
        }
    }
}
