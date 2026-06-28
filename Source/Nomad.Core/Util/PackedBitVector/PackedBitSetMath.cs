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

using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Numerics;
#endif

namespace Nomad.Core.Util.PackedBitVector
{
    internal static class PackedBitSetMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WordCountForBits(int bitCount)
        {
            return (bitCount + 63) >> 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(ulong value)
        {
#if NET8_0_OR_GREATER
            return BitOperations.PopCount(value);
#else
            value = value - ((value >> 1) & 0x5555555555555555UL);
            value = (value & 0x3333333333333333UL) + ((value >> 2) & 0x3333333333333333UL);
            return (int)((((value + (value >> 4)) & 0x0F0F0F0F0F0F0F0FUL) * 0x0101010101010101UL) >> 56);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroCount(ulong value)
        {
#if NET8_0_OR_GREATER
            return BitOperations.TrailingZeroCount(value);
#else
            if (value == 0UL)
            {
                return 64;
            }

            int count = 0;
            while ((value & 1UL) == 0UL)
            {
                count++;
                value >>= 1;
            }
            return count;
#endif
        }
    }
}
