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

namespace Nomad.Core.Util.PackedBitVector
{
    public sealed class PackedBitSet512DenseCache
    {
        private const int PageShift = 9;
        private const int PageMask = 511;

        private readonly PackedBitSet512[] _pages;
        private readonly bool[] _dirty;

        public int PageCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pages.Length;
        }

        public PackedBitSet512DenseCache(int maxBitId)
        {
            if (maxBitId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBitId));
            }

            int pageCount = ((maxBitId + 1) + PageMask) >> PageShift;
            if (pageCount < 1)
            {
                pageCount = 1;
            }

            _pages = new PackedBitSet512[pageCount];
            _dirty = new bool[pageCount];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int page = (int)(id >> PageShift);
            return _pages[page].Get((int)(id & PageMask));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int page = (int)(id >> PageShift);
            _pages[page].Set((int)(id & PageMask));
            _dirty[page] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int page = (int)(id >> PageShift);
            _pages[page].Unset((int)(id & PageMask));
            _dirty[page] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int bitId, bool value)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int page = (int)(id >> PageShift);
            _pages[page].Set((int)(id & PageMask), value);
            _dirty[page] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Toggle(int bitId)
        {
            Debug.Assert(bitId >= 0);
            uint id = (uint)bitId;
            int page = (int)(id >> PageShift);
            bool value = _pages[page].Toggle((int)(id & PageMask));
            _dirty[page] = true;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref PackedBitSet512 GetPageByIndex(int pageIndex)
        {
            Debug.Assert((uint)pageIndex < (uint)_pages.Length);
            return ref _pages[pageIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty(int pageIndex)
        {
            Debug.Assert((uint)pageIndex < (uint)_dirty.Length);
            return _dirty[pageIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkClean(int pageIndex)
        {
            Debug.Assert((uint)pageIndex < (uint)_dirty.Length);
            _dirty[pageIndex] = false;
        }

        public void Clear()
        {
            Array.Clear(_pages, 0, _pages.Length);
            Array.Clear(_dirty, 0, _dirty.Length);
        }
    }
}
