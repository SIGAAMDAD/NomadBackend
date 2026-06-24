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
using System.Runtime.CompilerServices;

namespace Nomad.Core.Util
{
    /// <summary>
    /// Dynamic heap-backed packed bitset. Best for long-lived reusable instances.
    /// </summary>
    public sealed class PackedBitSet
    {
        private readonly ulong[] _words;

        public readonly int BitCount;
        public readonly int WordCount;

        public PackedBitSet(int bitCount)
        {
            if (bitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), bitCount, "Bit count must be positive.");
            }

            BitCount = bitCount;
            WordCount = (int)(((uint)bitCount + 63U) >> 6);
            _words = new ulong[WordCount];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            PackedBitSetDebug.CheckIndex(index, BitCount);
            return (_words[index >> 6] & (1UL << (index & 63))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            PackedBitSetDebug.CheckIndex(index, BitCount);

            ref ulong word = ref _words[index >> 6];
            ulong mask = 1UL << (index & 63);

            if (value)
            {
                word |= mask;
            }
            else
            {
                word &= ~mask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int index)
        {
            PackedBitSetDebug.CheckIndex(index, BitCount);
            _words[index >> 6] |= 1UL << (index & 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBit(int index)
        {
            PackedBitSetDebug.CheckIndex(index, BitCount);
            _words[index >> 6] &= ~(1UL << (index & 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Toggle(int index)
        {
            PackedBitSetDebug.CheckIndex(index, BitCount);
            _words[index >> 6] ^= 1UL << (index & 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(_words, 0, WordCount);
        }

        public void Fill()
        {
            for (int i = 0; i < WordCount; i++)
            {
                _words[i] = ulong.MaxValue;
            }

            int usedBitsInLastWord = BitCount & 63;
            if (usedBitsInLastWord != 0)
            {
                _words[WordCount - 1] &= (1UL << usedBitsInLastWord) - 1UL;
            }
        }
    }
}
