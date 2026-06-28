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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class DenseIdMap<T>
    {
        private ulong[] _occupied;
        private T[] _values;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public int Capacity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _values.Length; } }

        public DenseIdMap(int initialCapacity = 64)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _values = new T[initialCapacity];
            _occupied = new ulong[(initialCapacity + 63) >> 6];
        }

        public ref T this[int id]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(Contains(id));
                return ref _values[id];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int id)
        {
            Debug.Assert(id >= 0);
            int word = id >> 6;
            return (uint)id < (uint)_values.Length && (_occupied[word] & (1UL << (id & 63))) != 0UL;
        }

        public void Set(int id, T value)
        {
            Debug.Assert(id >= 0);
            EnsureCapacity(id + 1);
            int word = id >> 6;
            ulong mask = 1UL << (id & 63);
            if ((_occupied[word] & mask) == 0UL)
            {
                _occupied[word] |= mask;
                _count++;
            }
            _values[id] = value;
        }

        public bool TryGetValue(int id, out T value)
        {
            if (Contains(id))
            {
                value = _values[id];
                return true;
            }
            value = default!;
            return false;
        }

        public bool Remove(int id)
        {
            Debug.Assert(id >= 0);
            if ((uint)id >= (uint)_values.Length)
            {
                return false;
            }

            int word = id >> 6;
            ulong mask = 1UL << (id & 63);
            if ((_occupied[word] & mask) == 0UL)
            {
                return false;
            }

            _occupied[word] &= ~mask;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _values[id] = default!;
            }

            _count--;
            return true;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity <= _values.Length)
            {
                return;
            }

            int newCapacity = CollectionMath.NextPowerOfTwo(capacity);
            Array.Resize(ref _values, newCapacity);
            Array.Resize(ref _occupied, (newCapacity + 63) >> 6);
        }

        public void Clear()
        {
            Array.Clear(_occupied, 0, _occupied.Length);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_values, 0, _values.Length);
            }

            _count = 0;
        }
    }
}
