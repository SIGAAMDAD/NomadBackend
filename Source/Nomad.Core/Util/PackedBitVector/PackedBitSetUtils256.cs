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
    public static class PackedBitSetUtils256
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet256 a)
        {
            return a.Raw0 == 0UL && a.Raw1 == 0UL && a.Raw2 == 0UL && a.Raw3 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            return a.Raw0 == b.Raw0 && a.Raw1 == b.Raw1 && a.Raw2 == b.Raw2 && a.Raw3 == b.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL || (a.Raw1 & b.Raw1) != 0UL || (a.Raw2 & b.Raw2) != 0UL || (a.Raw3 & b.Raw3) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0 && (a.Raw1 & b.Raw1) == b.Raw1 && (a.Raw2 & b.Raw2) == b.Raw2 && (a.Raw3 & b.Raw3) == b.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet256 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0) + PackedBitSetMath.PopCount(a.Raw1) + PackedBitSetMath.PopCount(a.Raw2) + PackedBitSetMath.PopCount(a.Raw3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet256 a)
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
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet256 Or(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            PackedBitSet256 result = default(PackedBitSet256);
            result.Raw0 = a.Raw0 | b.Raw0;
            result.Raw1 = a.Raw1 | b.Raw1;
            result.Raw2 = a.Raw2 | b.Raw2;
            result.Raw3 = a.Raw3 | b.Raw3;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet256 And(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            PackedBitSet256 result = default(PackedBitSet256);
            result.Raw0 = a.Raw0 & b.Raw0;
            result.Raw1 = a.Raw1 & b.Raw1;
            result.Raw2 = a.Raw2 & b.Raw2;
            result.Raw3 = a.Raw3 & b.Raw3;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet256 Xor(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            PackedBitSet256 result = default(PackedBitSet256);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            result.Raw1 = a.Raw1 ^ b.Raw1;
            result.Raw2 = a.Raw2 ^ b.Raw2;
            result.Raw3 = a.Raw3 ^ b.Raw3;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet256 AndNot(in PackedBitSet256 a, in PackedBitSet256 b)
        {
            PackedBitSet256 result = default(PackedBitSet256);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            result.Raw1 = a.Raw1 & ~b.Raw1;
            result.Raw2 = a.Raw2 & ~b.Raw2;
            result.Raw3 = a.Raw3 & ~b.Raw3;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet256 Not(in PackedBitSet256 a)
        {
            PackedBitSet256 result = default(PackedBitSet256);
            result.Raw0 = ~a.Raw0;
            result.Raw1 = ~a.Raw1;
            result.Raw2 = ~a.Raw2;
            result.Raw3 = ~a.Raw3;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet256 target, in PackedBitSet256 other)
        {
            target.Raw0 |= other.Raw0;
            target.Raw1 |= other.Raw1;
            target.Raw2 |= other.Raw2;
            target.Raw3 |= other.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet256 target, in PackedBitSet256 other)
        {
            target.Raw0 &= other.Raw0;
            target.Raw1 &= other.Raw1;
            target.Raw2 &= other.Raw2;
            target.Raw3 &= other.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet256 target, in PackedBitSet256 other)
        {
            target.Raw0 ^= other.Raw0;
            target.Raw1 ^= other.Raw1;
            target.Raw2 ^= other.Raw2;
            target.Raw3 ^= other.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet256 target, in PackedBitSet256 other)
        {
            target.Raw0 &= ~other.Raw0;
            target.Raw1 &= ~other.Raw1;
            target.Raw2 &= ~other.Raw2;
            target.Raw3 &= ~other.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet256 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Raw1 = ~target.Raw1;
            target.Raw2 = ~target.Raw2;
            target.Raw3 = ~target.Raw3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet256 state, in PackedBitSet256 requiredSet, in PackedBitSet256 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL && (state.Raw1 & requiredSet.Raw1) == requiredSet.Raw1 && (state.Raw1 & requiredUnset.Raw1) == 0UL && (state.Raw2 & requiredSet.Raw2) == requiredSet.Raw2 && (state.Raw2 & requiredUnset.Raw2) == 0UL && (state.Raw3 & requiredSet.Raw3) == requiredSet.Raw3 && (state.Raw3 & requiredUnset.Raw3) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet256 Apply(in PackedBitSet256 state, in PackedBitSet256 setMask, in PackedBitSet256 unsetMask)
        {
            PackedBitSet256 result = default(PackedBitSet256);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            result.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            result.Raw2 = (state.Raw2 | setMask.Raw2) & ~unsetMask.Raw2;
            result.Raw3 = (state.Raw3 | setMask.Raw3) & ~unsetMask.Raw3;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet256 state, in PackedBitSet256 setMask, in PackedBitSet256 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            state.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            state.Raw2 = (state.Raw2 | setMask.Raw2) & ~unsetMask.Raw2;
            state.Raw3 = (state.Raw3 | setMask.Raw3) & ~unsetMask.Raw3;
        }
    }
}
