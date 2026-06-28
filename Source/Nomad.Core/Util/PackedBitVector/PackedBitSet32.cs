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
    public struct PackedBitSet32
    {
        public const int BitCount = 32;
        public const int WordCount = 1;
        private const ulong ValidMask = 0x00000000FFFFFFFFUL;

        private ulong _word0;

        public ulong Raw0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word0 = value;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_word0 & ValidMask) == 0UL;
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
            Debug.Assert((uint)index < 32U);
            ulong mask = 1UL << (index & 63);
            ulong word = _word0;
            return (word & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
            Debug.Assert((uint)index < 32U);
            ulong mask = 1UL << (index & 63);
            _word0 |= mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int index)
        {
            Debug.Assert((uint)index < 32U);
            ulong mask = 1UL << (index & 63);
            _word0 &= ~mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            Debug.Assert((uint)index < 32U);
            ulong fill = value ? ulong.MaxValue : 0UL;
            ulong bit = 1UL << (index & 63);
            ulong mask = fill & bit;
            _word0 = (_word0 & ~bit) | mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Toggle(int index)
        {
            Debug.Assert((uint)index < 32U);
            ulong mask = 1UL << (index & 63);
            _word0 ^= mask;
            return (_word0 & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
_word0 = 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll()
        {
_word0 = ValidMask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sanitize()
        {
            _word0 &= ValidMask;
        }

    }
}
