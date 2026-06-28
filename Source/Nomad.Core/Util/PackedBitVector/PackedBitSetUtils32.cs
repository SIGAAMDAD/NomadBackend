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
    public static class PackedBitSetUtils32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet32 a)
        {
            return a.Raw0 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            return a.Raw0 == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet32 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet32 a)
        {
            if (a.Raw0 != 0UL)
            {
                return 0 + PackedBitSetMath.TrailingZeroCount(a.Raw0);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet32 Or(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            PackedBitSet32 result = default(PackedBitSet32);
            result.Raw0 = a.Raw0 | b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet32 And(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            PackedBitSet32 result = default(PackedBitSet32);
            result.Raw0 = a.Raw0 & b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet32 Xor(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            PackedBitSet32 result = default(PackedBitSet32);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet32 AndNot(in PackedBitSet32 a, in PackedBitSet32 b)
        {
            PackedBitSet32 result = default(PackedBitSet32);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet32 Not(in PackedBitSet32 a)
        {
            PackedBitSet32 result = default(PackedBitSet32);
            result.Raw0 = ~a.Raw0;
            result.Sanitize();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet32 target, in PackedBitSet32 other)
        {
            target.Raw0 |= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet32 target, in PackedBitSet32 other)
        {
            target.Raw0 &= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet32 target, in PackedBitSet32 other)
        {
            target.Raw0 ^= other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet32 target, in PackedBitSet32 other)
        {
            target.Raw0 &= ~other.Raw0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet32 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Sanitize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet32 state, in PackedBitSet32 requiredSet, in PackedBitSet32 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet32 Apply(in PackedBitSet32 state, in PackedBitSet32 setMask, in PackedBitSet32 unsetMask)
        {
            PackedBitSet32 result = default(PackedBitSet32);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet32 state, in PackedBitSet32 setMask, in PackedBitSet32 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
        }
    }
}
