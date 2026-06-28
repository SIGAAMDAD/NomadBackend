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
    public static class PackedBitSetUtils128
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet128 a)
        {
            return a.Raw0 == 0UL && a.Raw1 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            return a.Raw0 == b.Raw0 && a.Raw1 == b.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL || (a.Raw1 & b.Raw1) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0 && (a.Raw1 & b.Raw1) == b.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet128 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0) + PackedBitSetMath.PopCount(a.Raw1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet128 a)
        {
            if (a.Raw0 != 0UL)
            {
                return 0 + PackedBitSetMath.TrailingZeroCount(a.Raw0);
            }
            if (a.Raw1 != 0UL)
            {
                return 64 + PackedBitSetMath.TrailingZeroCount(a.Raw1);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet128 Or(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            PackedBitSet128 result = default(PackedBitSet128);
            result.Raw0 = a.Raw0 | b.Raw0;
            result.Raw1 = a.Raw1 | b.Raw1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet128 And(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            PackedBitSet128 result = default(PackedBitSet128);
            result.Raw0 = a.Raw0 & b.Raw0;
            result.Raw1 = a.Raw1 & b.Raw1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet128 Xor(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            PackedBitSet128 result = default(PackedBitSet128);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            result.Raw1 = a.Raw1 ^ b.Raw1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet128 AndNot(in PackedBitSet128 a, in PackedBitSet128 b)
        {
            PackedBitSet128 result = default(PackedBitSet128);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            result.Raw1 = a.Raw1 & ~b.Raw1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet128 Not(in PackedBitSet128 a)
        {
            PackedBitSet128 result = default(PackedBitSet128);
            result.Raw0 = ~a.Raw0;
            result.Raw1 = ~a.Raw1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet128 target, in PackedBitSet128 other)
        {
            target.Raw0 |= other.Raw0;
            target.Raw1 |= other.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet128 target, in PackedBitSet128 other)
        {
            target.Raw0 &= other.Raw0;
            target.Raw1 &= other.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet128 target, in PackedBitSet128 other)
        {
            target.Raw0 ^= other.Raw0;
            target.Raw1 ^= other.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet128 target, in PackedBitSet128 other)
        {
            target.Raw0 &= ~other.Raw0;
            target.Raw1 &= ~other.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet128 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Raw1 = ~target.Raw1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet128 state, in PackedBitSet128 requiredSet, in PackedBitSet128 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL && (state.Raw1 & requiredSet.Raw1) == requiredSet.Raw1 && (state.Raw1 & requiredUnset.Raw1) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet128 Apply(in PackedBitSet128 state, in PackedBitSet128 setMask, in PackedBitSet128 unsetMask)
        {
            PackedBitSet128 result = default(PackedBitSet128);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            result.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet128 state, in PackedBitSet128 setMask, in PackedBitSet128 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            state.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
        }
    }
}
