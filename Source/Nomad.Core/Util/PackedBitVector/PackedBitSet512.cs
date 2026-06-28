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
    public struct PackedBitSet512
    {
        public const int BitCount = 512;
        public const int WordCount = 8;
        private const ulong ValidMask = ulong.MaxValue;

        private ulong _word0;
        private ulong _word1;
        private ulong _word2;
        private ulong _word3;
        private ulong _word4;
        private ulong _word5;
        private ulong _word6;
        private ulong _word7;

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

        public ulong Raw2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word2 = value;
        }

        public ulong Raw3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word3 = value;
        }

        public ulong Raw4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word4 = value;
        }

        public ulong Raw5
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word5;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word5 = value;
        }

        public ulong Raw6
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word6;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word6 = value;
        }

        public ulong Raw7
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _word7;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _word7 = value;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_word0 | _word1 | _word2 | _word3 | _word4 | _word5 | _word6 | _word7) == 0UL;
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
            Debug.Assert((uint)index < 512U);
            ulong mask = 1UL << (index & 63);
            ulong word = GetWordUnchecked((int)((uint)index >> 6));
            return (word & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
            Debug.Assert((uint)index < 512U);
            ulong mask = 1UL << (index & 63);
            switch ((uint)index >> 6)
            {
                case 0: _word0 |= mask; break;
                case 1: _word1 |= mask; break;
                case 2: _word2 |= mask; break;
                case 3: _word3 |= mask; break;
                case 4: _word4 |= mask; break;
                case 5: _word5 |= mask; break;
                case 6: _word6 |= mask; break;
                case 7: _word7 |= mask; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int index)
        {
            Debug.Assert((uint)index < 512U);
            ulong mask = 1UL << (index & 63);
            switch ((uint)index >> 6)
            {
                case 0: _word0 &= ~mask; break;
                case 1: _word1 &= ~mask; break;
                case 2: _word2 &= ~mask; break;
                case 3: _word3 &= ~mask; break;
                case 4: _word4 &= ~mask; break;
                case 5: _word5 &= ~mask; break;
                case 6: _word6 &= ~mask; break;
                case 7: _word7 &= ~mask; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            Debug.Assert((uint)index < 512U);
            ulong fill = value ? ulong.MaxValue : 0UL;
            ulong bit = 1UL << (index & 63);
            ulong mask = fill & bit;
            switch ((uint)index >> 6)
            {
                case 0: _word0 = (_word0 & ~bit) | mask; break;
                case 1: _word1 = (_word1 & ~bit) | mask; break;
                case 2: _word2 = (_word2 & ~bit) | mask; break;
                case 3: _word3 = (_word3 & ~bit) | mask; break;
                case 4: _word4 = (_word4 & ~bit) | mask; break;
                case 5: _word5 = (_word5 & ~bit) | mask; break;
                case 6: _word6 = (_word6 & ~bit) | mask; break;
                case 7: _word7 = (_word7 & ~bit) | mask; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Toggle(int index)
        {
            Debug.Assert((uint)index < 512U);
            ulong mask = 1UL << (index & 63);
            switch ((uint)index >> 6)
            {
                case 0:
                    _word0 ^= mask;
                    return (_word0 & mask) != 0UL;
                case 1:
                    _word1 ^= mask;
                    return (_word1 & mask) != 0UL;
                case 2:
                    _word2 ^= mask;
                    return (_word2 & mask) != 0UL;
                case 3:
                    _word3 ^= mask;
                    return (_word3 & mask) != 0UL;
                case 4:
                    _word4 ^= mask;
                    return (_word4 & mask) != 0UL;
                case 5:
                    _word5 ^= mask;
                    return (_word5 & mask) != 0UL;
                case 6:
                    _word6 ^= mask;
                    return (_word6 & mask) != 0UL;
                case 7:
                    _word7 ^= mask;
                    return (_word7 & mask) != 0UL;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _word0 = 0UL;
            _word1 = 0UL;
            _word2 = 0UL;
            _word3 = 0UL;
            _word4 = 0UL;
            _word5 = 0UL;
            _word6 = 0UL;
            _word7 = 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll()
        {
            _word0 = ulong.MaxValue;
            _word1 = ulong.MaxValue;
            _word2 = ulong.MaxValue;
            _word3 = ulong.MaxValue;
            _word4 = ulong.MaxValue;
            _word5 = ulong.MaxValue;
            _word6 = ulong.MaxValue;
            _word7 = ulong.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ulong GetWordUnchecked(int wordIndex)
        {
            switch ((uint)wordIndex)
            {
                case 0: return _word0;
                case 1: return _word1;
                case 2: return _word2;
                case 3: return _word3;
                case 4: return _word4;
                case 5: return _word5;
                case 6: return _word6;
                case 7: return _word7;
                default: return 0UL;
            }
        }
    }
}
