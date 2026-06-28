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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class BitSetSparseDictionary<TValue>
    {
        private const int PageShift = 6;
        private const int PageSize = 64;
        private const int PageMask = 63;

        private readonly Dictionary<int, int> _slotsByPage;
        private int[] _pageKeys;
        private ulong[] _occupied;
        private TValue[][] _values;
        private int _pageCount;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public int PageCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _pageCount; } }

        public BitSetSparseDictionary(int initialPageCapacity = 16)
        {
            if (initialPageCapacity < 1)
            {
                initialPageCapacity = 1;
            }

            _slotsByPage = new Dictionary<int, int>(initialPageCapacity);
            _pageKeys = new int[initialPageCapacity];
            _occupied = new ulong[initialPageCapacity];
            _values = new TValue[initialPageCapacity][];
        }

        public bool ContainsKey(int key)
        {
            if (key < 0)
            {
                return false;
            }

            int pageKey = key >> PageShift;
            int slot;
            if (!_slotsByPage.TryGetValue(pageKey, out slot))
            {
                return false;
            }

            return (_occupied[slot] & (1UL << (key & PageMask))) != 0UL;
        }

        public void Set(int key, TValue value)
        {
            Debug.Assert(key >= 0);
            int pageKey = key >> PageShift;
            int slot = GetOrCreatePage(pageKey);
            int local = key & PageMask;
            ulong mask = 1UL << local;
            if ((_occupied[slot] & mask) == 0UL) { _occupied[slot] |= mask; _count++; }
            _values[slot][local] = value;
        }

        public bool TryGetValue(int key, out TValue value)
        {
            if (key >= 0)
            {
                int pageKey = key >> PageShift;
                int slot;
                if (_slotsByPage.TryGetValue(pageKey, out slot))
                {
                    int local = key & PageMask;
                    if ((_occupied[slot] & (1UL << local)) != 0UL)
                    {
                        value = _values[slot][local];
                        return true;
                    }
                }
            }
            value = default!;
            return false;
        }

        public bool Remove(int key)
        {
            if (key < 0)
            {
                return false;
            }

            int pageKey = key >> PageShift;
            int slot;
            if (!_slotsByPage.TryGetValue(pageKey, out slot))
            {
                return false;
            }

            int local = key & PageMask;
            ulong mask = 1UL << local;
            if ((_occupied[slot] & mask) == 0UL)
            {
                return false;
            }

            _occupied[slot] &= ~mask;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                _values[slot][local] = default!;
            }

            _count--;
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPageKeyBySlot(int slot)
        {
            Debug.Assert((uint)slot < (uint)_pageCount);
            return _pageKeys[slot];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetOccupiedMaskBySlot(int slot)
        {
            Debug.Assert((uint)slot < (uint)_pageCount);
            return _occupied[slot];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetValueBySlotLocal(int slot, int local)
        {
            Debug.Assert((uint)slot < (uint)_pageCount);
            Debug.Assert((uint)local < PageSize);
            return _values[slot][local];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueBySlotLocalRef(int slot, int local)
        {
            Debug.Assert((uint)slot < (uint)_pageCount);
            Debug.Assert((uint)local < PageSize);
            return ref _values[slot][local];
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                for (int i = 0; i < _pageCount; i++)
                {
                    TValue[]? page = _values[i];
                    if (page != null)
                    {
                        ulong mask = _occupied[i];
                        while (mask != 0UL)
                        {
                            int local = TrailingZeroCount(mask);
                            mask &= mask - 1UL;
                            page[local] = default!;
                        }
                    }
                }
            }

            _slotsByPage.Clear();
            Array.Clear(_pageKeys, 0, _pageCount);
            Array.Clear(_occupied, 0, _pageCount);
            Array.Clear(_values, 0, _pageCount);
            _pageCount = 0;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TrailingZeroCount(ulong value)
        {
#if NET8_0_OR_GREATER
            return System.Numerics.BitOperations.TrailingZeroCount(value);
#else
            if (value == 0UL) return 64;
            int count = 0;
            while ((value & 1UL) == 0UL)
            {
                count++;
                value >>= 1;
            }
            return count;
#endif
        }

        private int GetOrCreatePage(int pageKey)
        {
            int slot;
            if (_slotsByPage.TryGetValue(pageKey, out slot))
            {
                return slot;
            }

            slot = _pageCount++;
            if (slot == _pageKeys.Length)
            {
                Grow();
            }

            _pageKeys[slot] = pageKey;
            _occupied[slot] = 0UL;
            _values[slot] = new TValue[PageSize];
            _slotsByPage.Add(pageKey, slot);
            return slot;
        }

        private void Grow()
        {
            int next = _pageKeys.Length << 1;
            Array.Resize(ref _pageKeys, next);
            Array.Resize(ref _occupied, next);
            Array.Resize(ref _values, next);
        }
    }
}
