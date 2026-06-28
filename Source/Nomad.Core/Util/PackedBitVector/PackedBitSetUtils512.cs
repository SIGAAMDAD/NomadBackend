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

namespace Nomad.Core.Util.PackedBitVector
{
    public static class PackedBitSetUtils512
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet512 a)
        {
            return a.Raw0 == 0UL && a.Raw1 == 0UL && a.Raw2 == 0UL && a.Raw3 == 0UL && a.Raw4 == 0UL && a.Raw5 == 0UL && a.Raw6 == 0UL && a.Raw7 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            return a.Raw0 == b.Raw0 && a.Raw1 == b.Raw1 && a.Raw2 == b.Raw2 && a.Raw3 == b.Raw3 && a.Raw4 == b.Raw4 && a.Raw5 == b.Raw5 && a.Raw6 == b.Raw6 && a.Raw7 == b.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL || (a.Raw1 & b.Raw1) != 0UL || (a.Raw2 & b.Raw2) != 0UL || (a.Raw3 & b.Raw3) != 0UL || (a.Raw4 & b.Raw4) != 0UL || (a.Raw5 & b.Raw5) != 0UL || (a.Raw6 & b.Raw6) != 0UL || (a.Raw7 & b.Raw7) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0 && (a.Raw1 & b.Raw1) == b.Raw1 && (a.Raw2 & b.Raw2) == b.Raw2 && (a.Raw3 & b.Raw3) == b.Raw3 && (a.Raw4 & b.Raw4) == b.Raw4 && (a.Raw5 & b.Raw5) == b.Raw5 && (a.Raw6 & b.Raw6) == b.Raw6 && (a.Raw7 & b.Raw7) == b.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet512 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0) + PackedBitSetMath.PopCount(a.Raw1) + PackedBitSetMath.PopCount(a.Raw2) + PackedBitSetMath.PopCount(a.Raw3) + PackedBitSetMath.PopCount(a.Raw4) + PackedBitSetMath.PopCount(a.Raw5) + PackedBitSetMath.PopCount(a.Raw6) + PackedBitSetMath.PopCount(a.Raw7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet512 a)
        {
            if (a.Raw0 != 0UL)
            {
                return 0 + PackedBitSetMath.TrailingZeroCount(a.Raw0);
            }
            if (a.Raw1 != 0UL)
            {
                return 64 + PackedBitSetMath.TrailingZeroCount(a.Raw1);
            }
            if (a.Raw2 != 0UL)
            {
                return 128 + PackedBitSetMath.TrailingZeroCount(a.Raw2);
            }
            if (a.Raw3 != 0UL)
            {
                return 192 + PackedBitSetMath.TrailingZeroCount(a.Raw3);
            }
            if (a.Raw4 != 0UL)
            {
                return 256 + PackedBitSetMath.TrailingZeroCount(a.Raw4);
            }
            if (a.Raw5 != 0UL)
            {
                return 320 + PackedBitSetMath.TrailingZeroCount(a.Raw5);
            }
            if (a.Raw6 != 0UL)
            {
                return 384 + PackedBitSetMath.TrailingZeroCount(a.Raw6);
            }
            if (a.Raw7 != 0UL)
            {
                return 448 + PackedBitSetMath.TrailingZeroCount(a.Raw7);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet512 Or(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            PackedBitSet512 result = default(PackedBitSet512);
            result.Raw0 = a.Raw0 | b.Raw0;
            result.Raw1 = a.Raw1 | b.Raw1;
            result.Raw2 = a.Raw2 | b.Raw2;
            result.Raw3 = a.Raw3 | b.Raw3;
            result.Raw4 = a.Raw4 | b.Raw4;
            result.Raw5 = a.Raw5 | b.Raw5;
            result.Raw6 = a.Raw6 | b.Raw6;
            result.Raw7 = a.Raw7 | b.Raw7;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet512 And(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            PackedBitSet512 result = default(PackedBitSet512);
            result.Raw0 = a.Raw0 & b.Raw0;
            result.Raw1 = a.Raw1 & b.Raw1;
            result.Raw2 = a.Raw2 & b.Raw2;
            result.Raw3 = a.Raw3 & b.Raw3;
            result.Raw4 = a.Raw4 & b.Raw4;
            result.Raw5 = a.Raw5 & b.Raw5;
            result.Raw6 = a.Raw6 & b.Raw6;
            result.Raw7 = a.Raw7 & b.Raw7;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet512 Xor(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            PackedBitSet512 result = default(PackedBitSet512);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            result.Raw1 = a.Raw1 ^ b.Raw1;
            result.Raw2 = a.Raw2 ^ b.Raw2;
            result.Raw3 = a.Raw3 ^ b.Raw3;
            result.Raw4 = a.Raw4 ^ b.Raw4;
            result.Raw5 = a.Raw5 ^ b.Raw5;
            result.Raw6 = a.Raw6 ^ b.Raw6;
            result.Raw7 = a.Raw7 ^ b.Raw7;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet512 AndNot(in PackedBitSet512 a, in PackedBitSet512 b)
        {
            PackedBitSet512 result = default(PackedBitSet512);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            result.Raw1 = a.Raw1 & ~b.Raw1;
            result.Raw2 = a.Raw2 & ~b.Raw2;
            result.Raw3 = a.Raw3 & ~b.Raw3;
            result.Raw4 = a.Raw4 & ~b.Raw4;
            result.Raw5 = a.Raw5 & ~b.Raw5;
            result.Raw6 = a.Raw6 & ~b.Raw6;
            result.Raw7 = a.Raw7 & ~b.Raw7;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet512 Not(in PackedBitSet512 a)
        {
            PackedBitSet512 result = default(PackedBitSet512);
            result.Raw0 = ~a.Raw0;
            result.Raw1 = ~a.Raw1;
            result.Raw2 = ~a.Raw2;
            result.Raw3 = ~a.Raw3;
            result.Raw4 = ~a.Raw4;
            result.Raw5 = ~a.Raw5;
            result.Raw6 = ~a.Raw6;
            result.Raw7 = ~a.Raw7;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet512 target, in PackedBitSet512 other)
        {
            target.Raw0 |= other.Raw0;
            target.Raw1 |= other.Raw1;
            target.Raw2 |= other.Raw2;
            target.Raw3 |= other.Raw3;
            target.Raw4 |= other.Raw4;
            target.Raw5 |= other.Raw5;
            target.Raw6 |= other.Raw6;
            target.Raw7 |= other.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet512 target, in PackedBitSet512 other)
        {
            target.Raw0 &= other.Raw0;
            target.Raw1 &= other.Raw1;
            target.Raw2 &= other.Raw2;
            target.Raw3 &= other.Raw3;
            target.Raw4 &= other.Raw4;
            target.Raw5 &= other.Raw5;
            target.Raw6 &= other.Raw6;
            target.Raw7 &= other.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet512 target, in PackedBitSet512 other)
        {
            target.Raw0 ^= other.Raw0;
            target.Raw1 ^= other.Raw1;
            target.Raw2 ^= other.Raw2;
            target.Raw3 ^= other.Raw3;
            target.Raw4 ^= other.Raw4;
            target.Raw5 ^= other.Raw5;
            target.Raw6 ^= other.Raw6;
            target.Raw7 ^= other.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet512 target, in PackedBitSet512 other)
        {
            target.Raw0 &= ~other.Raw0;
            target.Raw1 &= ~other.Raw1;
            target.Raw2 &= ~other.Raw2;
            target.Raw3 &= ~other.Raw3;
            target.Raw4 &= ~other.Raw4;
            target.Raw5 &= ~other.Raw5;
            target.Raw6 &= ~other.Raw6;
            target.Raw7 &= ~other.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet512 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Raw1 = ~target.Raw1;
            target.Raw2 = ~target.Raw2;
            target.Raw3 = ~target.Raw3;
            target.Raw4 = ~target.Raw4;
            target.Raw5 = ~target.Raw5;
            target.Raw6 = ~target.Raw6;
            target.Raw7 = ~target.Raw7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet512 state, in PackedBitSet512 requiredSet, in PackedBitSet512 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL && (state.Raw1 & requiredSet.Raw1) == requiredSet.Raw1 && (state.Raw1 & requiredUnset.Raw1) == 0UL && (state.Raw2 & requiredSet.Raw2) == requiredSet.Raw2 && (state.Raw2 & requiredUnset.Raw2) == 0UL && (state.Raw3 & requiredSet.Raw3) == requiredSet.Raw3 && (state.Raw3 & requiredUnset.Raw3) == 0UL && (state.Raw4 & requiredSet.Raw4) == requiredSet.Raw4 && (state.Raw4 & requiredUnset.Raw4) == 0UL && (state.Raw5 & requiredSet.Raw5) == requiredSet.Raw5 && (state.Raw5 & requiredUnset.Raw5) == 0UL && (state.Raw6 & requiredSet.Raw6) == requiredSet.Raw6 && (state.Raw6 & requiredUnset.Raw6) == 0UL && (state.Raw7 & requiredSet.Raw7) == requiredSet.Raw7 && (state.Raw7 & requiredUnset.Raw7) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet512 Apply(in PackedBitSet512 state, in PackedBitSet512 setMask, in PackedBitSet512 unsetMask)
        {
            PackedBitSet512 result = default(PackedBitSet512);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            result.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            result.Raw2 = (state.Raw2 | setMask.Raw2) & ~unsetMask.Raw2;
            result.Raw3 = (state.Raw3 | setMask.Raw3) & ~unsetMask.Raw3;
            result.Raw4 = (state.Raw4 | setMask.Raw4) & ~unsetMask.Raw4;
            result.Raw5 = (state.Raw5 | setMask.Raw5) & ~unsetMask.Raw5;
            result.Raw6 = (state.Raw6 | setMask.Raw6) & ~unsetMask.Raw6;
            result.Raw7 = (state.Raw7 | setMask.Raw7) & ~unsetMask.Raw7;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet512 state, in PackedBitSet512 setMask, in PackedBitSet512 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            state.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            state.Raw2 = (state.Raw2 | setMask.Raw2) & ~unsetMask.Raw2;
            state.Raw3 = (state.Raw3 | setMask.Raw3) & ~unsetMask.Raw3;
            state.Raw4 = (state.Raw4 | setMask.Raw4) & ~unsetMask.Raw4;
            state.Raw5 = (state.Raw5 | setMask.Raw5) & ~unsetMask.Raw5;
            state.Raw6 = (state.Raw6 | setMask.Raw6) & ~unsetMask.Raw6;
            state.Raw7 = (state.Raw7 | setMask.Raw7) & ~unsetMask.Raw7;
        }
    }
}
