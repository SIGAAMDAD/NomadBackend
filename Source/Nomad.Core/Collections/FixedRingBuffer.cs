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
    public sealed class FixedRingBuffer<T>
    {
        private readonly T[] _items;
        private int _start;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public int Capacity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _items.Length; } }

        public FixedRingBuffer(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new T[capacity];
        }

        public void PushBack(T value)
        {
            int index = (_start + _count) % _items.Length;
            if (_count == _items.Length)
            {
                _items[index] = value;
                _start = (_start + 1) % _items.Length;
            }
            else
            {
                _items[index] = value;
                _count++;
            }
        }

        public ref T GetFromNewest(int offset)
        {
            Debug.Assert((uint)offset < (uint)_count);
            int index = (_start + _count - 1 - offset) % _items.Length;
            return ref _items[index];
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_items, 0, _items.Length);
            }

            _start = 0;
            _count = 0;
        }
    }
}
