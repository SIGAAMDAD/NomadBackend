/*
===========================================================================
The Nomad MPLv2 Source Code
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
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Util
{
    /// <summary>
    /// Dynamic pooled packed bitset. Best for temporary hotpath usage.
    /// Must be disposed to return the rented buffer.
    /// </summary>
    public struct PooledPackedBitSet : IDisposable
    {
        private ulong[] _words;
        private int _bitCount;
        private int _wordCount;

        public readonly int BitCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _bitCount; }
        }

        public readonly int WordCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _wordCount; }
        }

        public PooledPackedBitSet(int bitCount, bool clear = true)
        {
            if (bitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), bitCount, "Bit count must be positive.");
            }

            _bitCount = bitCount;
            _wordCount = (int)(((uint)bitCount + 63U) >> 6);
            _words = ArrayPool<ulong>.Shared.Rent(_wordCount);

            if (clear)
            {
                Array.Clear(_words, 0, _wordCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Get(int index)
        {
            PackedBitSetDebug.CheckIndex(index, _bitCount);
            return (_words[index >> 6] & (1UL << (index & 63))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            PackedBitSetDebug.CheckIndex(index, _bitCount);

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
            PackedBitSetDebug.CheckIndex(index, _bitCount);
            _words[index >> 6] |= 1UL << (index & 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBit(int index)
        {
            PackedBitSetDebug.CheckIndex(index, _bitCount);
            _words[index >> 6] &= ~(1UL << (index & 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Toggle(int index)
        {
            PackedBitSetDebug.CheckIndex(index, _bitCount);
            _words[index >> 6] ^= 1UL << (index & 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsEmpty()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (_words[i] != 0UL)
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(_words, 0, _wordCount);
        }

        public void Fill()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                _words[i] = ulong.MaxValue;
            }

            int usedBitsInLastWord = _bitCount & 63;
            if (usedBitsInLastWord != 0)
            {
                _words[_wordCount - 1] &= (1UL << usedBitsInLastWord) - 1UL;
            }
        }

        public void Dispose()
        {
            ulong[] words = _words;
            if (words == null)
            {
                return;
            }

            Array.Clear(words, 0, _wordCount);
            ArrayPool<ulong>.Shared.Return(words, clearArray: false);

            _words = null;
            _bitCount = 0;
            _wordCount = 0;
        }
    }
}
