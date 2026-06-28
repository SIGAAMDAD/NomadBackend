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
    public static class PackedBitSetUtils16
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet16 a)
        {
            return a.Raw0 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            return a.Raw0 == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet16 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet16 a)
        {
            if (a.Raw0 != 0UL)
            {
                return 0 + PackedBitSetMath.TrailingZeroCount(a.Raw0);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet16 Or(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            PackedBitSet16 result = default(PackedBitSet16);
            result.Raw0 = a.Raw0 | b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet16 And(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            PackedBitSet16 result = default(PackedBitSet16);
            result.Raw0 = a.Raw0 & b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet16 Xor(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            PackedBitSet16 result = default(PackedBitSet16);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet16 AndNot(in PackedBitSet16 a, in PackedBitSet16 b)
        {
            PackedBitSet16 result = default(PackedBitSet16);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet16 Not(in PackedBitSet16 a)
        {
            PackedBitSet16 result = default(PackedBitSet16);
            result.Raw0 = ~a.Raw0;
            result.Sanitize();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet16 target, in PackedBitSet16 other)
        {
            target.Raw0 |= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet16 target, in PackedBitSet16 other)
        {
            target.Raw0 &= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet16 target, in PackedBitSet16 other)
        {
            target.Raw0 ^= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet16 target, in PackedBitSet16 other)
        {
            target.Raw0 &= ~other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet16 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Sanitize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet16 state, in PackedBitSet16 requiredSet, in PackedBitSet16 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet16 Apply(in PackedBitSet16 state, in PackedBitSet16 setMask, in PackedBitSet16 unsetMask)
        {
            PackedBitSet16 result = default(PackedBitSet16);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet16 state, in PackedBitSet16 setMask, in PackedBitSet16 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
        }
    }
}
