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
    public static class PackedBitSetUtils8
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet8 a)
        {
            return a.Raw0 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            return a.Raw0 == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet8 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet8 a)
        {
            if (a.Raw0 != 0UL)
            {
                return 0 + PackedBitSetMath.TrailingZeroCount(a.Raw0);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet8 Or(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            PackedBitSet8 result = default(PackedBitSet8);
            result.Raw0 = a.Raw0 | b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet8 And(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            PackedBitSet8 result = default(PackedBitSet8);
            result.Raw0 = a.Raw0 & b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet8 Xor(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            PackedBitSet8 result = default(PackedBitSet8);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet8 AndNot(in PackedBitSet8 a, in PackedBitSet8 b)
        {
            PackedBitSet8 result = default(PackedBitSet8);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet8 Not(in PackedBitSet8 a)
        {
            PackedBitSet8 result = default(PackedBitSet8);
            result.Raw0 = ~a.Raw0;
            result.Sanitize();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet8 target, in PackedBitSet8 other)
        {
            target.Raw0 |= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet8 target, in PackedBitSet8 other)
        {
            target.Raw0 &= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet8 target, in PackedBitSet8 other)
        {
            target.Raw0 ^= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet8 target, in PackedBitSet8 other)
        {
            target.Raw0 &= ~other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet8 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Sanitize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet8 state, in PackedBitSet8 requiredSet, in PackedBitSet8 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet8 Apply(in PackedBitSet8 state, in PackedBitSet8 setMask, in PackedBitSet8 unsetMask)
        {
            PackedBitSet8 result = default(PackedBitSet8);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet8 state, in PackedBitSet8 setMask, in PackedBitSet8 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
        }
    }
}
