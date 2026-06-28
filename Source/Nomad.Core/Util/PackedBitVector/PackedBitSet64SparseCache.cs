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

namespace Nomad.Core.Util.PackedBitVector
{
    public sealed class PackedBitSet64SparseCache
    {
        private const int PageShift = 6;
        private const int PageMask = 63;

        private readonly Dictionary<int, int> _slotsByPage;
        private int[] _pageKeys;
        private PackedBitSet64[] _pages;
        private bool[] _dirty;
        private int _count;
        private int _lastPageKey;
        private int _lastSlot;

        public int PageCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public PackedBitSet64SparseCache(int initialPageCapacity = 16)
        {
            if (initialPageCapacity < 1)
            {
                initialPageCapacity = 1;
            }

            _slotsByPage = new Dictionary<int, int>(initialPageCapacity);
            _pageKeys = new int[initialPageCapacity];
            _pages = new PackedBitSet64[initialPageCapacity];
            _dirty = new bool[initialPageCapacity];
            _lastPageKey = int.MinValue;
            _lastSlot = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int pageKey = (int)(id >> PageShift);
            int slot = FindSlot(pageKey);
            return slot >= 0 && _pages[slot].Get((int)(id & PageMask));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int pageKey = (int)(id >> PageShift);
            int slot = GetOrCreateSlot(pageKey);
            _pages[slot].Set((int)(id & PageMask));
            _dirty[slot] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int pageKey = (int)(id >> PageShift);
            int slot = FindSlot(pageKey);
            if (slot < 0)
            {
                return;
            }
            _pages[slot].Unset((int)(id & PageMask));
            _dirty[slot] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Toggle(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int pageKey = (int)(id >> PageShift);
            int slot = GetOrCreateSlot(pageKey);
            bool value = _pages[slot].Toggle((int)(id & PageMask));
            _dirty[slot] = true;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPageKeyBySlot(int slot)
        {
            Debug.Assert((uint)slot < (uint)_count);
            return _pageKeys[slot];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref PackedBitSet64 GetPageBySlot(int slot)
        {
            Debug.Assert((uint)slot < (uint)_count);
            return ref _pages[slot];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirtyBySlot(int slot)
        {
            Debug.Assert((uint)slot < (uint)_count);
            return _dirty[slot];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkCleanBySlot(int slot)
        {
            Debug.Assert((uint)slot < (uint)_count);
            _dirty[slot] = false;
        }

        public void Clear()
        {
            _slotsByPage.Clear();
            Array.Clear(_pageKeys, 0, _count);
            Array.Clear(_pages, 0, _count);
            Array.Clear(_dirty, 0, _count);
            _count = 0;
            _lastPageKey = int.MinValue;
            _lastSlot = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindSlot(int pageKey)
        {
            if (_lastPageKey == pageKey && (uint)_lastSlot < (uint)_count)
            {
                return _lastSlot;
            }

            int slot;
            if (_slotsByPage.TryGetValue(pageKey, out slot))
            {
                _lastPageKey = pageKey;
                _lastSlot = slot;
                return slot;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetOrCreateSlot(int pageKey)
        {
            int slot = FindSlot(pageKey);
            if (slot >= 0)
            {
                return slot;
            }

            slot = _count;
            if (slot == _pages.Length)
            {
                Grow();
            }

            _count = slot + 1;
            _pageKeys[slot] = pageKey;
            _pages[slot].Clear();
            _dirty[slot] = true;
            _slotsByPage.Add(pageKey, slot);
            _lastPageKey = pageKey;
            _lastSlot = slot;
            return slot;
        }

        private void Grow()
        {
            int newCapacity = _pages.Length << 1;
            Array.Resize(ref _pageKeys, newCapacity);
            Array.Resize(ref _pages, newCapacity);
            Array.Resize(ref _dirty, newCapacity);
        }
    }
}
