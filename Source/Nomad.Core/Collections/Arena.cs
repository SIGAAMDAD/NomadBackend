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
    public sealed class Arena<T>
    {
        private T[] _items;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public Span<T> Span { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _items.AsSpan(0, _count); } }

        public Arena(int initialCapacity = 64)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _items = new T[initialCapacity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(T value)
        {
            if (_count == _items.Length)
            {
                Grow(_count + 1);
            }

            int index = _count++;
            _items[index] = value;
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddUninitialized(out int index)
        {
            if (_count == _items.Length)
            {
                Grow(_count + 1);
            }

            index = _count++;
            return ref _items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return ref _items[index];
        }

        public void Reset()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_items, 0, _count);
            }

            _count = 0;
        }

        private void Grow(int required)
        {
            int next = _items.Length << 1;
            if (next < required)
            {
                next = required;
            }

            Array.Resize(ref _items, next);
        }
    }
}
