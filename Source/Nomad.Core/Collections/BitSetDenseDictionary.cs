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
    public sealed class BitSetDenseDictionary<TValue>
    {
        private ulong[] _occupied;
        private TValue[] _values;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }


        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values.Length; }
        }

        public BitSetDenseDictionary(int initialCapacity = 64)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _values = new TValue[initialCapacity];
            _occupied = new ulong[(initialCapacity + 63) >> 6];
        }

        public ref TValue this[int key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { Debug.Assert(ContainsKey(key)); return ref _values[key]; }
        }

        public bool ContainsKey(int key)
        {
            return (uint)key < (uint)_values.Length && (_occupied[key >> 6] & (1UL << (key & 63))) != 0UL;
        }

        public void Set(int key, TValue value)
        {
            Debug.Assert(key >= 0);
            EnsureCapacity(key + 1);
            int word = key >> 6;
            ulong mask = 1UL << (key & 63);
            if ((_occupied[word] & mask) == 0UL) { _occupied[word] |= mask; _count++; }
            _values[key] = value;
        }


        public bool TryGetValue(int key, out TValue value)
        {
            if (ContainsKey(key))
            {
                value = _values[key];
                return true;
            }
            value = default!;
            return false;
        }

        public bool Remove(int key)
        {
            if (!ContainsKey(key))
            {
                return false;
            }

            _occupied[key >> 6] &= ~(1UL << (key & 63));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                _values[key] = default!;
            }

            _count--;
            return true;
        }


        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                int emitted = 0;
                for (int key = 0; key < _values.Length && emitted < _count; key++)
                {
                    if ((_occupied[key >> 6] & (1UL << (key & 63))) != 0UL)
                    {
                        _values[key] = default!;
                        emitted++;
                    }
                }
            }

            Array.Clear(_occupied, 0, _occupied.Length);
            _count = 0;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity <= _values.Length)
            {
                return;
            }

            int next = CollectionMath.NextPowerOfTwo(capacity);
            Array.Resize(ref _values, next);
            Array.Resize(ref _occupied, (next + 63) >> 6);
        }
    }
}
