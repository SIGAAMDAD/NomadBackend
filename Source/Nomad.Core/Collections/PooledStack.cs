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
    public sealed class PooledStack<T> : IDisposable
    {
        private T[]? _items;
        private readonly ArrayPool<T> _pool;
        private readonly bool _clearOnReturn;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public PooledStack(int initialCapacity = 16, bool clearOnReturn = true, ArrayPool<T>? pool = null)
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
        public void Push(T item)
        {
            if (_count == Items.Length)
            {
                Grow(_count + 1);
            }

            Items[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            Debug.Assert(_count > 0);
            int index = --_count;
            T item = Items[index];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Items[index] = default!;
            }

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T item)
        {
            if (_count == 0) { item = default!; return false; }
            item = Pop();
            return true;
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


        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _items == null ? 0 : _items.Length; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetFromBottom(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return ref Items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetFromTop(int offset)
        {
            Debug.Assert((uint)offset < (uint)_count);
            return ref Items[_count - 1 - offset];
        }

        public void CopyTo(T[] destination, int destinationIndex = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if ((uint)destinationIndex > (uint)destination.Length || destination.Length - destinationIndex < _count)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            }
            Array.Copy(Items, 0, destination, destinationIndex, _count);
        }

        public T[] ToArray()
        {
            T[] result = new T[_count];
            CopyTo(result, 0);
            return result;
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
                    throw new ObjectDisposedException(nameof(PooledStack<T>));
                }

                return items!;
            }
        }
    }
}
