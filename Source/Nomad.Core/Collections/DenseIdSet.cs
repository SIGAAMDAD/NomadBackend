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
    public sealed class DenseIdSet
    {
        private ulong[] _words;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public int Capacity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _words.Length << 6; } }

        public DenseIdSet(int initialCapacity = 64)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _words = new ulong[(initialCapacity + 63) >> 6];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int id)
        {
            Debug.Assert(id >= 0);
            int word = id >> 6;
            return (uint)word < (uint)_words.Length && (_words[word] & (1UL << (id & 63))) != 0UL;
        }

        public bool Add(int id)
        {
            Debug.Assert(id >= 0);
            EnsureCapacity(id + 1);
            int word = id >> 6;
            ulong mask = 1UL << (id & 63);
            ulong old = _words[word];
            if ((old & mask) != 0UL)
            {
                return false;
            }

            _words[word] = old | mask;
            _count++;
            return true;
        }

        public bool Remove(int id)
        {
            Debug.Assert(id >= 0);
            int word = id >> 6;
            if ((uint)word >= (uint)_words.Length)
            {
                return false;
            }

            ulong mask = 1UL << (id & 63);
            ulong old = _words[word];
            if ((old & mask) == 0UL)
            {
                return false;
            }

            _words[word] = old & ~mask;
            _count--;
            return true;
        }

        public void EnsureCapacity(int capacity)
        {
            int words = (capacity + 63) >> 6;
            if (words > _words.Length)
            {
                Array.Resize(ref _words, CollectionMath.NextPowerOfTwo(words));
            }
        }

        public void Clear()
        {
            Array.Clear(_words, 0, _words.Length);
            _count = 0;
        }
    }
}
