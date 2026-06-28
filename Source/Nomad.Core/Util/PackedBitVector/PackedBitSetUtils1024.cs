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
    public static class PackedBitSetUtils1024
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(in PackedBitSet1024 a)
        {
            return a.Raw0 == 0UL && a.Raw1 == 0UL && a.Raw2 == 0UL && a.Raw3 == 0UL && a.Raw4 == 0UL && a.Raw5 == 0UL && a.Raw6 == 0UL && a.Raw7 == 0UL && a.Raw8 == 0UL && a.Raw9 == 0UL && a.Raw10 == 0UL && a.Raw11 == 0UL && a.Raw12 == 0UL && a.Raw13 == 0UL && a.Raw14 == 0UL && a.Raw15 == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            return a.Raw0 == b.Raw0 && a.Raw1 == b.Raw1 && a.Raw2 == b.Raw2 && a.Raw3 == b.Raw3 && a.Raw4 == b.Raw4 && a.Raw5 == b.Raw5 && a.Raw6 == b.Raw6 && a.Raw7 == b.Raw7 && a.Raw8 == b.Raw8 && a.Raw9 == b.Raw9 && a.Raw10 == b.Raw10 && a.Raw11 == b.Raw11 && a.Raw12 == b.Raw12 && a.Raw13 == b.Raw13 && a.Raw14 == b.Raw14 && a.Raw15 == b.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            return (a.Raw0 & b.Raw0) != 0UL || (a.Raw1 & b.Raw1) != 0UL || (a.Raw2 & b.Raw2) != 0UL || (a.Raw3 & b.Raw3) != 0UL || (a.Raw4 & b.Raw4) != 0UL || (a.Raw5 & b.Raw5) != 0UL || (a.Raw6 & b.Raw6) != 0UL || (a.Raw7 & b.Raw7) != 0UL || (a.Raw8 & b.Raw8) != 0UL || (a.Raw9 & b.Raw9) != 0UL || (a.Raw10 & b.Raw10) != 0UL || (a.Raw11 & b.Raw11) != 0UL || (a.Raw12 & b.Raw12) != 0UL || (a.Raw13 & b.Raw13) != 0UL || (a.Raw14 & b.Raw14) != 0UL || (a.Raw15 & b.Raw15) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAll(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            return (a.Raw0 & b.Raw0) == b.Raw0 && (a.Raw1 & b.Raw1) == b.Raw1 && (a.Raw2 & b.Raw2) == b.Raw2 && (a.Raw3 & b.Raw3) == b.Raw3 && (a.Raw4 & b.Raw4) == b.Raw4 && (a.Raw5 & b.Raw5) == b.Raw5 && (a.Raw6 & b.Raw6) == b.Raw6 && (a.Raw7 & b.Raw7) == b.Raw7 && (a.Raw8 & b.Raw8) == b.Raw8 && (a.Raw9 & b.Raw9) == b.Raw9 && (a.Raw10 & b.Raw10) == b.Raw10 && (a.Raw11 & b.Raw11) == b.Raw11 && (a.Raw12 & b.Raw12) == b.Raw12 && (a.Raw13 & b.Raw13) == b.Raw13 && (a.Raw14 & b.Raw14) == b.Raw14 && (a.Raw15 & b.Raw15) == b.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(in PackedBitSet1024 a)
        {
            return PackedBitSetMath.PopCount(a.Raw0) + PackedBitSetMath.PopCount(a.Raw1) + PackedBitSetMath.PopCount(a.Raw2) + PackedBitSetMath.PopCount(a.Raw3) + PackedBitSetMath.PopCount(a.Raw4) + PackedBitSetMath.PopCount(a.Raw5) + PackedBitSetMath.PopCount(a.Raw6) + PackedBitSetMath.PopCount(a.Raw7) + PackedBitSetMath.PopCount(a.Raw8) + PackedBitSetMath.PopCount(a.Raw9) + PackedBitSetMath.PopCount(a.Raw10) + PackedBitSetMath.PopCount(a.Raw11) + PackedBitSetMath.PopCount(a.Raw12) + PackedBitSetMath.PopCount(a.Raw13) + PackedBitSetMath.PopCount(a.Raw14) + PackedBitSetMath.PopCount(a.Raw15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstSetBit(in PackedBitSet1024 a)
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
            if (a.Raw8 != 0UL)
            {
                return 512 + PackedBitSetMath.TrailingZeroCount(a.Raw8);
            }
            if (a.Raw9 != 0UL)
            {
                return 576 + PackedBitSetMath.TrailingZeroCount(a.Raw9);
            }
            if (a.Raw10 != 0UL)
            {
                return 640 + PackedBitSetMath.TrailingZeroCount(a.Raw10);
            }
            if (a.Raw11 != 0UL)
            {
                return 704 + PackedBitSetMath.TrailingZeroCount(a.Raw11);
            }
            if (a.Raw12 != 0UL)
            {
                return 768 + PackedBitSetMath.TrailingZeroCount(a.Raw12);
            }
            if (a.Raw13 != 0UL)
            {
                return 832 + PackedBitSetMath.TrailingZeroCount(a.Raw13);
            }
            if (a.Raw14 != 0UL)
            {
                return 896 + PackedBitSetMath.TrailingZeroCount(a.Raw14);
            }
            if (a.Raw15 != 0UL)
            {
                return 960 + PackedBitSetMath.TrailingZeroCount(a.Raw15);
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet1024 Or(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            PackedBitSet1024 result = default(PackedBitSet1024);
            result.Raw0 = a.Raw0 | b.Raw0;
            result.Raw1 = a.Raw1 | b.Raw1;
            result.Raw2 = a.Raw2 | b.Raw2;
            result.Raw3 = a.Raw3 | b.Raw3;
            result.Raw4 = a.Raw4 | b.Raw4;
            result.Raw5 = a.Raw5 | b.Raw5;
            result.Raw6 = a.Raw6 | b.Raw6;
            result.Raw7 = a.Raw7 | b.Raw7;
            result.Raw8 = a.Raw8 | b.Raw8;
            result.Raw9 = a.Raw9 | b.Raw9;
            result.Raw10 = a.Raw10 | b.Raw10;
            result.Raw11 = a.Raw11 | b.Raw11;
            result.Raw12 = a.Raw12 | b.Raw12;
            result.Raw13 = a.Raw13 | b.Raw13;
            result.Raw14 = a.Raw14 | b.Raw14;
            result.Raw15 = a.Raw15 | b.Raw15;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet1024 And(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            PackedBitSet1024 result = default(PackedBitSet1024);
            result.Raw0 = a.Raw0 & b.Raw0;
            result.Raw1 = a.Raw1 & b.Raw1;
            result.Raw2 = a.Raw2 & b.Raw2;
            result.Raw3 = a.Raw3 & b.Raw3;
            result.Raw4 = a.Raw4 & b.Raw4;
            result.Raw5 = a.Raw5 & b.Raw5;
            result.Raw6 = a.Raw6 & b.Raw6;
            result.Raw7 = a.Raw7 & b.Raw7;
            result.Raw8 = a.Raw8 & b.Raw8;
            result.Raw9 = a.Raw9 & b.Raw9;
            result.Raw10 = a.Raw10 & b.Raw10;
            result.Raw11 = a.Raw11 & b.Raw11;
            result.Raw12 = a.Raw12 & b.Raw12;
            result.Raw13 = a.Raw13 & b.Raw13;
            result.Raw14 = a.Raw14 & b.Raw14;
            result.Raw15 = a.Raw15 & b.Raw15;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet1024 Xor(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            PackedBitSet1024 result = default(PackedBitSet1024);
            result.Raw0 = a.Raw0 ^ b.Raw0;
            result.Raw1 = a.Raw1 ^ b.Raw1;
            result.Raw2 = a.Raw2 ^ b.Raw2;
            result.Raw3 = a.Raw3 ^ b.Raw3;
            result.Raw4 = a.Raw4 ^ b.Raw4;
            result.Raw5 = a.Raw5 ^ b.Raw5;
            result.Raw6 = a.Raw6 ^ b.Raw6;
            result.Raw7 = a.Raw7 ^ b.Raw7;
            result.Raw8 = a.Raw8 ^ b.Raw8;
            result.Raw9 = a.Raw9 ^ b.Raw9;
            result.Raw10 = a.Raw10 ^ b.Raw10;
            result.Raw11 = a.Raw11 ^ b.Raw11;
            result.Raw12 = a.Raw12 ^ b.Raw12;
            result.Raw13 = a.Raw13 ^ b.Raw13;
            result.Raw14 = a.Raw14 ^ b.Raw14;
            result.Raw15 = a.Raw15 ^ b.Raw15;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet1024 AndNot(in PackedBitSet1024 a, in PackedBitSet1024 b)
        {
            PackedBitSet1024 result = default(PackedBitSet1024);
            result.Raw0 = a.Raw0 & ~b.Raw0;
            result.Raw1 = a.Raw1 & ~b.Raw1;
            result.Raw2 = a.Raw2 & ~b.Raw2;
            result.Raw3 = a.Raw3 & ~b.Raw3;
            result.Raw4 = a.Raw4 & ~b.Raw4;
            result.Raw5 = a.Raw5 & ~b.Raw5;
            result.Raw6 = a.Raw6 & ~b.Raw6;
            result.Raw7 = a.Raw7 & ~b.Raw7;
            result.Raw8 = a.Raw8 & ~b.Raw8;
            result.Raw9 = a.Raw9 & ~b.Raw9;
            result.Raw10 = a.Raw10 & ~b.Raw10;
            result.Raw11 = a.Raw11 & ~b.Raw11;
            result.Raw12 = a.Raw12 & ~b.Raw12;
            result.Raw13 = a.Raw13 & ~b.Raw13;
            result.Raw14 = a.Raw14 & ~b.Raw14;
            result.Raw15 = a.Raw15 & ~b.Raw15;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet1024 Not(in PackedBitSet1024 a)
        {
            PackedBitSet1024 result = default(PackedBitSet1024);
            result.Raw0 = ~a.Raw0;
            result.Raw1 = ~a.Raw1;
            result.Raw2 = ~a.Raw2;
            result.Raw3 = ~a.Raw3;
            result.Raw4 = ~a.Raw4;
            result.Raw5 = ~a.Raw5;
            result.Raw6 = ~a.Raw6;
            result.Raw7 = ~a.Raw7;
            result.Raw8 = ~a.Raw8;
            result.Raw9 = ~a.Raw9;
            result.Raw10 = ~a.Raw10;
            result.Raw11 = ~a.Raw11;
            result.Raw12 = ~a.Raw12;
            result.Raw13 = ~a.Raw13;
            result.Raw14 = ~a.Raw14;
            result.Raw15 = ~a.Raw15;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrInPlace(ref PackedBitSet1024 target, in PackedBitSet1024 other)
        {
            target.Raw0 |= other.Raw0;
            target.Raw1 |= other.Raw1;
            target.Raw2 |= other.Raw2;
            target.Raw3 |= other.Raw3;
            target.Raw4 |= other.Raw4;
            target.Raw5 |= other.Raw5;
            target.Raw6 |= other.Raw6;
            target.Raw7 |= other.Raw7;
            target.Raw8 |= other.Raw8;
            target.Raw9 |= other.Raw9;
            target.Raw10 |= other.Raw10;
            target.Raw11 |= other.Raw11;
            target.Raw12 |= other.Raw12;
            target.Raw13 |= other.Raw13;
            target.Raw14 |= other.Raw14;
            target.Raw15 |= other.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInPlace(ref PackedBitSet1024 target, in PackedBitSet1024 other)
        {
            target.Raw0 &= other.Raw0;
            target.Raw1 &= other.Raw1;
            target.Raw2 &= other.Raw2;
            target.Raw3 &= other.Raw3;
            target.Raw4 &= other.Raw4;
            target.Raw5 &= other.Raw5;
            target.Raw6 &= other.Raw6;
            target.Raw7 &= other.Raw7;
            target.Raw8 &= other.Raw8;
            target.Raw9 &= other.Raw9;
            target.Raw10 &= other.Raw10;
            target.Raw11 &= other.Raw11;
            target.Raw12 &= other.Raw12;
            target.Raw13 &= other.Raw13;
            target.Raw14 &= other.Raw14;
            target.Raw15 &= other.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorInPlace(ref PackedBitSet1024 target, in PackedBitSet1024 other)
        {
            target.Raw0 ^= other.Raw0;
            target.Raw1 ^= other.Raw1;
            target.Raw2 ^= other.Raw2;
            target.Raw3 ^= other.Raw3;
            target.Raw4 ^= other.Raw4;
            target.Raw5 ^= other.Raw5;
            target.Raw6 ^= other.Raw6;
            target.Raw7 ^= other.Raw7;
            target.Raw8 ^= other.Raw8;
            target.Raw9 ^= other.Raw9;
            target.Raw10 ^= other.Raw10;
            target.Raw11 ^= other.Raw11;
            target.Raw12 ^= other.Raw12;
            target.Raw13 ^= other.Raw13;
            target.Raw14 ^= other.Raw14;
            target.Raw15 ^= other.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndNotInPlace(ref PackedBitSet1024 target, in PackedBitSet1024 other)
        {
            target.Raw0 &= ~other.Raw0;
            target.Raw1 &= ~other.Raw1;
            target.Raw2 &= ~other.Raw2;
            target.Raw3 &= ~other.Raw3;
            target.Raw4 &= ~other.Raw4;
            target.Raw5 &= ~other.Raw5;
            target.Raw6 &= ~other.Raw6;
            target.Raw7 &= ~other.Raw7;
            target.Raw8 &= ~other.Raw8;
            target.Raw9 &= ~other.Raw9;
            target.Raw10 &= ~other.Raw10;
            target.Raw11 &= ~other.Raw11;
            target.Raw12 &= ~other.Raw12;
            target.Raw13 &= ~other.Raw13;
            target.Raw14 &= ~other.Raw14;
            target.Raw15 &= ~other.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotInPlace(ref PackedBitSet1024 target)
        {
            target.Raw0 = ~target.Raw0;
            target.Raw1 = ~target.Raw1;
            target.Raw2 = ~target.Raw2;
            target.Raw3 = ~target.Raw3;
            target.Raw4 = ~target.Raw4;
            target.Raw5 = ~target.Raw5;
            target.Raw6 = ~target.Raw6;
            target.Raw7 = ~target.Raw7;
            target.Raw8 = ~target.Raw8;
            target.Raw9 = ~target.Raw9;
            target.Raw10 = ~target.Raw10;
            target.Raw11 = ~target.Raw11;
            target.Raw12 = ~target.Raw12;
            target.Raw13 = ~target.Raw13;
            target.Raw14 = ~target.Raw14;
            target.Raw15 = ~target.Raw15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Satisfies(in PackedBitSet1024 state, in PackedBitSet1024 requiredSet, in PackedBitSet1024 requiredUnset)
        {
            return (state.Raw0 & requiredSet.Raw0) == requiredSet.Raw0 && (state.Raw0 & requiredUnset.Raw0) == 0UL && (state.Raw1 & requiredSet.Raw1) == requiredSet.Raw1 && (state.Raw1 & requiredUnset.Raw1) == 0UL && (state.Raw2 & requiredSet.Raw2) == requiredSet.Raw2 && (state.Raw2 & requiredUnset.Raw2) == 0UL && (state.Raw3 & requiredSet.Raw3) == requiredSet.Raw3 && (state.Raw3 & requiredUnset.Raw3) == 0UL && (state.Raw4 & requiredSet.Raw4) == requiredSet.Raw4 && (state.Raw4 & requiredUnset.Raw4) == 0UL && (state.Raw5 & requiredSet.Raw5) == requiredSet.Raw5 && (state.Raw5 & requiredUnset.Raw5) == 0UL && (state.Raw6 & requiredSet.Raw6) == requiredSet.Raw6 && (state.Raw6 & requiredUnset.Raw6) == 0UL && (state.Raw7 & requiredSet.Raw7) == requiredSet.Raw7 && (state.Raw7 & requiredUnset.Raw7) == 0UL && (state.Raw8 & requiredSet.Raw8) == requiredSet.Raw8 && (state.Raw8 & requiredUnset.Raw8) == 0UL && (state.Raw9 & requiredSet.Raw9) == requiredSet.Raw9 && (state.Raw9 & requiredUnset.Raw9) == 0UL && (state.Raw10 & requiredSet.Raw10) == requiredSet.Raw10 && (state.Raw10 & requiredUnset.Raw10) == 0UL && (state.Raw11 & requiredSet.Raw11) == requiredSet.Raw11 && (state.Raw11 & requiredUnset.Raw11) == 0UL && (state.Raw12 & requiredSet.Raw12) == requiredSet.Raw12 && (state.Raw12 & requiredUnset.Raw12) == 0UL && (state.Raw13 & requiredSet.Raw13) == requiredSet.Raw13 && (state.Raw13 & requiredUnset.Raw13) == 0UL && (state.Raw14 & requiredSet.Raw14) == requiredSet.Raw14 && (state.Raw14 & requiredUnset.Raw14) == 0UL && (state.Raw15 & requiredSet.Raw15) == requiredSet.Raw15 && (state.Raw15 & requiredUnset.Raw15) == 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedBitSet1024 Apply(in PackedBitSet1024 state, in PackedBitSet1024 setMask, in PackedBitSet1024 unsetMask)
        {
            PackedBitSet1024 result = default(PackedBitSet1024);
            result.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            result.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            result.Raw2 = (state.Raw2 | setMask.Raw2) & ~unsetMask.Raw2;
            result.Raw3 = (state.Raw3 | setMask.Raw3) & ~unsetMask.Raw3;
            result.Raw4 = (state.Raw4 | setMask.Raw4) & ~unsetMask.Raw4;
            result.Raw5 = (state.Raw5 | setMask.Raw5) & ~unsetMask.Raw5;
            result.Raw6 = (state.Raw6 | setMask.Raw6) & ~unsetMask.Raw6;
            result.Raw7 = (state.Raw7 | setMask.Raw7) & ~unsetMask.Raw7;
            result.Raw8 = (state.Raw8 | setMask.Raw8) & ~unsetMask.Raw8;
            result.Raw9 = (state.Raw9 | setMask.Raw9) & ~unsetMask.Raw9;
            result.Raw10 = (state.Raw10 | setMask.Raw10) & ~unsetMask.Raw10;
            result.Raw11 = (state.Raw11 | setMask.Raw11) & ~unsetMask.Raw11;
            result.Raw12 = (state.Raw12 | setMask.Raw12) & ~unsetMask.Raw12;
            result.Raw13 = (state.Raw13 | setMask.Raw13) & ~unsetMask.Raw13;
            result.Raw14 = (state.Raw14 | setMask.Raw14) & ~unsetMask.Raw14;
            result.Raw15 = (state.Raw15 | setMask.Raw15) & ~unsetMask.Raw15;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyInPlace(ref PackedBitSet1024 state, in PackedBitSet1024 setMask, in PackedBitSet1024 unsetMask)
        {
            state.Raw0 = (state.Raw0 | setMask.Raw0) & ~unsetMask.Raw0;
            state.Raw1 = (state.Raw1 | setMask.Raw1) & ~unsetMask.Raw1;
            state.Raw2 = (state.Raw2 | setMask.Raw2) & ~unsetMask.Raw2;
            state.Raw3 = (state.Raw3 | setMask.Raw3) & ~unsetMask.Raw3;
            state.Raw4 = (state.Raw4 | setMask.Raw4) & ~unsetMask.Raw4;
            state.Raw5 = (state.Raw5 | setMask.Raw5) & ~unsetMask.Raw5;
            state.Raw6 = (state.Raw6 | setMask.Raw6) & ~unsetMask.Raw6;
            state.Raw7 = (state.Raw7 | setMask.Raw7) & ~unsetMask.Raw7;
            state.Raw8 = (state.Raw8 | setMask.Raw8) & ~unsetMask.Raw8;
            state.Raw9 = (state.Raw9 | setMask.Raw9) & ~unsetMask.Raw9;
            state.Raw10 = (state.Raw10 | setMask.Raw10) & ~unsetMask.Raw10;
            state.Raw11 = (state.Raw11 | setMask.Raw11) & ~unsetMask.Raw11;
            state.Raw12 = (state.Raw12 | setMask.Raw12) & ~unsetMask.Raw12;
            state.Raw13 = (state.Raw13 | setMask.Raw13) & ~unsetMask.Raw13;
            state.Raw14 = (state.Raw14 | setMask.Raw14) & ~unsetMask.Raw14;
            state.Raw15 = (state.Raw15 | setMask.Raw15) & ~unsetMask.Raw15;
        }
    }
}
