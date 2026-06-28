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

namespace Nomad.Core.Collections
{
    public static class CollectionLinqExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this PooledArray<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Length; i++)
            {
                yield return source[i];
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this PooledList<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source[i];
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this PooledQueue<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetFromOldest(i);
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this PooledStack<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetFromBottom(i);
            }
        }

        public static IEnumerable<T> AsEnumerableTopToBottom<T>(this PooledStack<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetFromTop(i);
            }
        }

        public static IEnumerable<T> AsUnorderedEnumerable<T>(this PooledBinaryHeap<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetUnorderedByIndex(i);
            }
        }

        public static IEnumerable<T> AsUnorderedEnumerable<T, TComparer>(this PooledBinaryHeap<T, TComparer> source)
            where TComparer : struct, IComparer<T>
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetUnorderedByIndex(i);
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this Arena<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.Get(i);
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this FixedRingBuffer<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetFromNewest(source.Count - 1 - i);
            }
        }

        public static IEnumerable<int> AsEnumerable(this SparseSet source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetDenseId(i);
            }
        }

        public static IEnumerable<int> AsEnumerable(this DirtySet source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetDirtyId(i);
            }
        }

        public static IEnumerable<KeyValuePair<int, TValue>> AsPairs<TValue>(this SparseSet<TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return new KeyValuePair<int, TValue>(source.GetDenseId(i), source.GetValueByDenseIndex(i));
            }
        }

        public static IEnumerable<int> Ids<TValue>(this SparseSet<TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetDenseId(i);
            }
        }

        public static IEnumerable<TValue> Values<TValue>(this SparseSet<TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetValueByDenseIndex(i);
            }
        }

        public static IEnumerable<int> AsEnumerable(this DenseIdSet source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int emitted = 0;
            for (int id = 0; id < source.Capacity && emitted < source.Count; id++)
            {
                if (source.Contains(id))
                {
                    emitted++;
                    yield return id;
                }
            }
        }

        public static IEnumerable<KeyValuePair<int, TValue>> AsPairs<TValue>(this DenseIdMap<TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int emitted = 0;
            for (int id = 0; id < source.Capacity && emitted < source.Count; id++)
            {
                if (source.Contains(id))
                {
                    emitted++;
                    yield return new KeyValuePair<int, TValue>(id, source[id]);
                }
            }
        }

        public static IEnumerable<KeyValuePair<int, TValue>> AsPairs<TValue>(this BitSetDenseDictionary<TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int emitted = 0;
            for (int key = 0; key < source.Capacity && emitted < source.Count; key++)
            {
                TValue value;
                if (source.TryGetValue(key, out value))
                {
                    emitted++;
                    yield return new KeyValuePair<int, TValue>(key, value);
                }
            }
        }

        public static IEnumerable<KeyValuePair<int, TValue>> AsPairs<TValue>(this BitSetSparseDictionary<TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int slot = 0; slot < source.PageCount; slot++)
            {
                int pageBase = source.GetPageKeyBySlot(slot) << 6;
                ulong mask = source.GetOccupiedMaskBySlot(slot);
                while (mask != 0UL)
                {
                    int local = TrailingZeroCount(mask);
                    mask &= mask - 1UL;
                    yield return new KeyValuePair<int, TValue>(pageBase + local, source.GetValueBySlotLocal(slot, local));
                }
            }
        }

        public static IEnumerable<string> AsEnumerable(this StringIdTable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int i = 0; i < source.Count; i++)
            {
                yield return source.GetString(i);
            }
        }

        public static IEnumerable<(T0 A, T1 B)> AsEnumerable<T0, T1>(this SoA2<T0, T1> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ValueTuple<T0, T1>[] result = new ValueTuple<T0, T1>[source.Count];
            Span<T0> a = source.A;
            Span<T1> b = source.B;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (a[i], b[i]);
            }

            return result;
        }

        public static IEnumerable<(T0 A, T1 B, T2 C)> AsEnumerable<T0, T1, T2>(this SoA3<T0, T1, T2> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ValueTuple<T0, T1, T2>[] result = new ValueTuple<T0, T1, T2>[source.Count];
            Span<T0> a = source.A;
            Span<T1> b = source.B;
            Span<T2> c = source.C;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (a[i], b[i], c[i]);
            }

            return result;
        }

        public static IEnumerable<(T0 A, T1 B, T2 C, T3 D)> AsEnumerable<T0, T1, T2, T3>(this SoA4<T0, T1, T2, T3> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ValueTuple<T0, T1, T2, T3>[] result = new ValueTuple<T0, T1, T2, T3>[source.Count];
            Span<T0> a = source.A;
            Span<T1> b = source.B;
            Span<T2> c = source.C;
            Span<T3> d = source.D;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (a[i], b[i], c[i], d[i]);
            }

            return result;
        }

        public static IEnumerable<T> AsEnumerable<T>(this SmallList4<T> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                yield return source[i];
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this SmallList8<T> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                yield return source[i];
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this SmallList16<T> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                yield return source[i];
            }
        }

        public static IEnumerable<(int X, int Y)> TrueCells(this BitMatrix source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    if (source.Get(x, y))
                    {
                        yield return (x, y);
                    }
                }
            }
        }

        public static IEnumerable<WeightedEdge<TWeight>> Neighbors<TWeight>(this WeightedCompactGraph<TWeight> source, int node)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetNeighbors(node).ToArray();
        }

        public static IEnumerable<int> Neighbors(this CompactGraph source, int node)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetNeighbors(node).ToArray();
        }

        public static IEnumerable<KeyValuePair<TEnum, TValue>> AsPairs<TEnum, TValue>(this PackedEnumMap<TEnum, TValue> source)
            where TEnum : struct, Enum
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Array values = Enum.GetValues(typeof(TEnum));
            for (int i = 0; i < values.Length; i++)
            {
                TEnum key = (TEnum)values.GetValue(i)!;
                if (source.Contains(key))
                {
                    yield return new KeyValuePair<TEnum, TValue>(key, source[key]);
                }
            }
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
