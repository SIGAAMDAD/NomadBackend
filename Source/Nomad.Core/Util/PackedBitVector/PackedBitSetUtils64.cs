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
    public static class PackedBitSetUtils64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet64 a)
        {
            return a.Raw0 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            return a.Raw0 == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet64 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet64 a)
        {
            if (a.Raw0 != 0UL)
            {
                return 0 + PackedBitSetMath.TrailingZeroCount(a.Raw0);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet64 Or(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            PackedBitSet64 result = default(PackedBitSet64);
            result.Raw0 = a.Raw0 | b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet64 And(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            PackedBitSet64 result = default(PackedBitSet64);
            result.Raw0 = a.Raw0 & b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet64 Xor(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            PackedBitSet64 result = default(PackedBitSet64);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet64 AndNot(in PackedBitSet64 a, in PackedBitSet64 b)
        {
            PackedBitSet64 result = default(PackedBitSet64);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet64 Not(in PackedBitSet64 a)
        {
            PackedBitSet64 result = default(PackedBitSet64);
            result.Raw0 = ~a.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet64 target, in PackedBitSet64 other)
        {
            target.Raw0 |= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet64 target, in PackedBitSet64 other)
        {
            target.Raw0 &= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet64 target, in PackedBitSet64 other)
        {
            target.Raw0 ^= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet64 target, in PackedBitSet64 other)
        {
            target.Raw0 &= ~other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet64 target)
        {
            target.Raw0 = ~target.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet64 state, in PackedBitSet64 requiredSet, in PackedBitSet64 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet64 Apply(in PackedBitSet64 state, in PackedBitSet64 setMask, in PackedBitSet64 unsetMask)
        {
            PackedBitSet64 result = default(PackedBitSet64);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet64 state, in PackedBitSet64 setMask, in PackedBitSet64 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
        }
    }
}
