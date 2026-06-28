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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Util.PackedBitVector
{
    public static class PackedBitSetLinqExtensions
    {
        public static IEnumerable<int> AsEnumerable(this PackedBitSet8 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet8 bits)
        {
            ulong word0 = (bits.Raw0 & 0x00000000000000FFUL);
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet8 bits)
        {
            int[] result = new int[PackedBitSetUtils8.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet8 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet8DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet8 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 3;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet8DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet8SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet8 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 3;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet8SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet16 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet16 bits)
        {
            ulong word0 = (bits.Raw0 & 0x000000000000FFFFUL);
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet16 bits)
        {
            int[] result = new int[PackedBitSetUtils16.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet16 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet16DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet16 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 4;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet16DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet16SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet16 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 4;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet16SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet32 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet32 bits)
        {
            ulong word0 = (bits.Raw0 & 0x00000000FFFFFFFFUL);
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet32 bits)
        {
            int[] result = new int[PackedBitSetUtils32.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet32 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet32DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet32 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 5;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet32DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet32SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet32 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 5;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet32SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet64 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet64 bits)
        {
            ulong word0 = bits.Raw0;
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet64 bits)
        {
            int[] result = new int[PackedBitSetUtils64.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet64 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet64DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet64 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 6;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet64DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet64SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet64 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 6;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet64SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet128 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet128 bits)
        {
            ulong word0 = bits.Raw0;
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
            ulong word1 = bits.Raw1;
            while (word1 != 0UL)
            {
                int bit = TrailingZeroCount(word1);
                word1 &= word1 - 1UL;
                yield return 64 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet128 bits)
        {
            int[] result = new int[PackedBitSetUtils128.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet128 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet128DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet128 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 7;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet128DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet128SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet128 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 7;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet128SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet256 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet256 bits)
        {
            ulong word0 = bits.Raw0;
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
            ulong word1 = bits.Raw1;
            while (word1 != 0UL)
            {
                int bit = TrailingZeroCount(word1);
                word1 &= word1 - 1UL;
                yield return 64 + bit;
            }
            ulong word2 = bits.Raw2;
            while (word2 != 0UL)
            {
                int bit = TrailingZeroCount(word2);
                word2 &= word2 - 1UL;
                yield return 128 + bit;
            }
            ulong word3 = bits.Raw3;
            while (word3 != 0UL)
            {
                int bit = TrailingZeroCount(word3);
                word3 &= word3 - 1UL;
                yield return 192 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet256 bits)
        {
            int[] result = new int[PackedBitSetUtils256.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet256 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet256DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet256 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 8;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet256DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet256SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet256 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 8;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet256SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet512 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet512 bits)
        {
            ulong word0 = bits.Raw0;
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
            ulong word1 = bits.Raw1;
            while (word1 != 0UL)
            {
                int bit = TrailingZeroCount(word1);
                word1 &= word1 - 1UL;
                yield return 64 + bit;
            }
            ulong word2 = bits.Raw2;
            while (word2 != 0UL)
            {
                int bit = TrailingZeroCount(word2);
                word2 &= word2 - 1UL;
                yield return 128 + bit;
            }
            ulong word3 = bits.Raw3;
            while (word3 != 0UL)
            {
                int bit = TrailingZeroCount(word3);
                word3 &= word3 - 1UL;
                yield return 192 + bit;
            }
            ulong word4 = bits.Raw4;
            while (word4 != 0UL)
            {
                int bit = TrailingZeroCount(word4);
                word4 &= word4 - 1UL;
                yield return 256 + bit;
            }
            ulong word5 = bits.Raw5;
            while (word5 != 0UL)
            {
                int bit = TrailingZeroCount(word5);
                word5 &= word5 - 1UL;
                yield return 320 + bit;
            }
            ulong word6 = bits.Raw6;
            while (word6 != 0UL)
            {
                int bit = TrailingZeroCount(word6);
                word6 &= word6 - 1UL;
                yield return 384 + bit;
            }
            ulong word7 = bits.Raw7;
            while (word7 != 0UL)
            {
                int bit = TrailingZeroCount(word7);
                word7 &= word7 - 1UL;
                yield return 448 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet512 bits)
        {
            int[] result = new int[PackedBitSetUtils512.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet512 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet512DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet512 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 9;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet512DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet512SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet512 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 9;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet512SparseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet1024 bits)
        {
            return bits.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet1024 bits)
        {
            ulong word0 = bits.Raw0;
            while (word0 != 0UL)
            {
                int bit = TrailingZeroCount(word0);
                word0 &= word0 - 1UL;
                yield return 0 + bit;
            }
            ulong word1 = bits.Raw1;
            while (word1 != 0UL)
            {
                int bit = TrailingZeroCount(word1);
                word1 &= word1 - 1UL;
                yield return 64 + bit;
            }
            ulong word2 = bits.Raw2;
            while (word2 != 0UL)
            {
                int bit = TrailingZeroCount(word2);
                word2 &= word2 - 1UL;
                yield return 128 + bit;
            }
            ulong word3 = bits.Raw3;
            while (word3 != 0UL)
            {
                int bit = TrailingZeroCount(word3);
                word3 &= word3 - 1UL;
                yield return 192 + bit;
            }
            ulong word4 = bits.Raw4;
            while (word4 != 0UL)
            {
                int bit = TrailingZeroCount(word4);
                word4 &= word4 - 1UL;
                yield return 256 + bit;
            }
            ulong word5 = bits.Raw5;
            while (word5 != 0UL)
            {
                int bit = TrailingZeroCount(word5);
                word5 &= word5 - 1UL;
                yield return 320 + bit;
            }
            ulong word6 = bits.Raw6;
            while (word6 != 0UL)
            {
                int bit = TrailingZeroCount(word6);
                word6 &= word6 - 1UL;
                yield return 384 + bit;
            }
            ulong word7 = bits.Raw7;
            while (word7 != 0UL)
            {
                int bit = TrailingZeroCount(word7);
                word7 &= word7 - 1UL;
                yield return 448 + bit;
            }
            ulong word8 = bits.Raw8;
            while (word8 != 0UL)
            {
                int bit = TrailingZeroCount(word8);
                word8 &= word8 - 1UL;
                yield return 512 + bit;
            }
            ulong word9 = bits.Raw9;
            while (word9 != 0UL)
            {
                int bit = TrailingZeroCount(word9);
                word9 &= word9 - 1UL;
                yield return 576 + bit;
            }
            ulong word10 = bits.Raw10;
            while (word10 != 0UL)
            {
                int bit = TrailingZeroCount(word10);
                word10 &= word10 - 1UL;
                yield return 640 + bit;
            }
            ulong word11 = bits.Raw11;
            while (word11 != 0UL)
            {
                int bit = TrailingZeroCount(word11);
                word11 &= word11 - 1UL;
                yield return 704 + bit;
            }
            ulong word12 = bits.Raw12;
            while (word12 != 0UL)
            {
                int bit = TrailingZeroCount(word12);
                word12 &= word12 - 1UL;
                yield return 768 + bit;
            }
            ulong word13 = bits.Raw13;
            while (word13 != 0UL)
            {
                int bit = TrailingZeroCount(word13);
                word13 &= word13 - 1UL;
                yield return 832 + bit;
            }
            ulong word14 = bits.Raw14;
            while (word14 != 0UL)
            {
                int bit = TrailingZeroCount(word14);
                word14 &= word14 - 1UL;
                yield return 896 + bit;
            }
            ulong word15 = bits.Raw15;
            while (word15 != 0UL)
            {
                int bit = TrailingZeroCount(word15);
                word15 &= word15 - 1UL;
                yield return 960 + bit;
            }
        }

        public static int[] ToSetBitArray(this PackedBitSet1024 bits)
        {
            int[] result = new int[PackedBitSetUtils1024.PopCount(in bits)];
            int index = 0;
            foreach (int bit in bits.SetBits())
            {
                result[index++] = bit;
            }

            return result;
        }

        public static void ForEachSetBit(this PackedBitSet1024 bits, Action<int> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (int bit in bits.SetBits())
            {
                action(bit);
            }
        }

        public static IEnumerable<int> SetBits(this PackedBitSet1024DenseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int pageIndex = 0; pageIndex < cache.PageCount; pageIndex++)
            {
                PackedBitSet1024 page = cache.GetPageByIndex(pageIndex);
                int pageBase = pageIndex << 10;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet1024DenseCache cache)
        {
            return cache.SetBits();
        }

        public static IEnumerable<int> SetBits(this PackedBitSet1024SparseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            for (int slot = 0; slot < cache.PageCount; slot++)
            {
                PackedBitSet1024 page = cache.GetPageBySlot(slot);
                int pageBase = cache.GetPageKeyBySlot(slot) << 10;
                foreach (int local in page.SetBits())
                {
                    yield return pageBase + local;
                }
            }
        }

        public static IEnumerable<int> AsEnumerable(this PackedBitSet1024SparseCache cache)
        {
            return cache.SetBits();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TrailingZeroCount(ulong value)
        {
#if NET8_0_OR_GREATER
            return System.Numerics.BitOperations.TrailingZeroCount(value);
#else
            if (value == 0UL) return 64;
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
