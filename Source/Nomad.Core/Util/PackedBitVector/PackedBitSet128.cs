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

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Util.PackedBitVector
{
    public struct PackedBitSet128
    {
        public const int BitCount = 128;
        public const int WordCount = 2;
        private const ulong ValidMask = ulong.MaxValue;

        private ulong _word0;
        private ulong _word1;

        public ulong Raw0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word0 = value;
        }

        public ulong Raw1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word1 = value;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_word0 | _word1) == 0UL;
        }

        public bool this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => Get(index);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Set(index, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Get(int index)
        {
            Debug.Assert((uint)index < 128U);
            ulong mask = 1UL << (index & 63);
            ulong word = GetWordUnchecked((int)((uint)index >> 6));
            return (word & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
            Debug.Assert((uint)index < 128U);
            ulong mask = 1UL << (index & 63);
            switch ((uint)index >> 6)
            {
                case 0: _word0 |= mask; break;
                case 1: _word1 |= mask; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int index)
        {
            Debug.Assert((uint)index < 128U);
            ulong mask = 1UL << (index & 63);
            switch ((uint)index >> 6)
            {
                case 0: _word0 &= ~mask; break;
                case 1: _word1 &= ~mask; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            Debug.Assert((uint)index < 128U);
            ulong fill = value ? ulong.MaxValue : 0UL;
            ulong bit = 1UL << (index & 63);
            ulong mask = fill & bit;
            switch ((uint)index >> 6)
            {
                case 0: _word0 = (_word0 & ~bit) | mask; break;
                case 1: _word1 = (_word1 & ~bit) | mask; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Toggle(int index)
        {
            Debug.Assert((uint)index < 128U);
            ulong mask = 1UL << (index & 63);
            switch ((uint)index >> 6)
            {
                case 0:
                    _word0 ^= mask;
                    return (_word0 & mask) != 0UL;
                case 1:
                    _word1 ^= mask;
                    return (_word1 & mask) != 0UL;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _word0 = 0UL;
            _word1 = 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll()
        {
            _word0 = ulong.MaxValue;
            _word1 = ulong.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ulong GetWordUnchecked(int wordIndex)
        {
            switch ((uint)wordIndex)
            {
                case 0: return _word0;
                case 1: return _word1;
                default: return 0UL;
            }
        }
    }
}
