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
    public static class CollectionStandardExtensions
    {
        public static void AddRange<T>(this PooledList<T> list, IEnumerable<T> values)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            ICollection<T>? collection = values as ICollection<T>;
            if (collection != null)
            {
                list.EnsureCapacity(list.Count + collection.Count);
            }

            foreach (T value in values)
            {
                list.Add(value);
            }
        }

        public static void AddRange<T>(this PooledQueue<T> queue, IEnumerable<T> values)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (T value in values)
            {
                queue.Enqueue(value);
            }
        }

        public static T[] ToArray<T>(this PooledList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            T[] result = new T[list.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = list[i];
            }

            return result;
        }

        public static void CopyTo<T>(this PooledList<T> list, T[] destination, int destinationIndex = 0)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if ((uint)destinationIndex > (uint)destination.Length || destination.Length - destinationIndex < list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            }
            for (int i = 0; i < list.Count; i++)
            {
                destination[destinationIndex + i] = list[i];
            }
        }

        public static int IndexOf<T>(this PooledList<T> list, T value)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool Contains<T>(this PooledList<T> list, T value)
        {
            return IndexOf(list, value) >= 0;
        }

        public static int RemoveAll<T>(this PooledList<T> list, Predicate<T> match)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            int removed = 0;
            int i = 0;
            while (i < list.Count)
            {
                if (match(list[i]))
                {
                    list.RemoveAtSwapBack(i);
                    removed++;
                }
                else
                {
                    i++;
                }
            }
            return removed;
        }

        public static void Reverse<T>(this PooledList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            int left = 0;
            int right = list.Count - 1;
            while (left < right)
            {
                Swap(list, left++, right--);
            }
        }

        public static void Sort<T>(this PooledList<T> list)
        {
            Sort(list, Comparer<T>.Default);
        }

        public static void Sort<T>(this PooledList<T> list, IComparer<T>? comparer)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            comparer = comparer ?? Comparer<T>.Default;
            if (list.Count > 1)
            {
                QuickSort(list, 0, list.Count - 1, comparer);
            }
        }

        public static int BinarySearch<T>(this PooledList<T> list, T value)
        {
            return BinarySearch(list, value, Comparer<T>.Default);
        }

        public static int BinarySearch<T>(this PooledList<T> list, T value, IComparer<T>? comparer)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            comparer = comparer ?? Comparer<T>.Default;
            int low = 0;
            int high = list.Count - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                int cmp = comparer.Compare(list[mid], value);
                if (cmp == 0)
                {
                    return mid;
                }

                if (cmp < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return ~low;
        }

        public static T[] ToArray<T>(this SmallList4<T> list)
        {
            T[] result = new T[list.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = list[i];
            }

            return result;
        }

        public static T[] ToArray<T>(this SmallList8<T> list)
        {
            T[] result = new T[list.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = list[i];
            }

            return result;
        }

        public static T[] ToArray<T>(this SmallList16<T> list)
        {
            T[] result = new T[list.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = list[i];
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(PooledList<T> list, int a, int b)
        {
            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        private static void QuickSort<T>(PooledList<T> list, int left, int right, IComparer<T> comparer)
        {
            while (left < right)
            {
                int i = left;
                int j = right;
                T pivot = list[left + ((right - left) >> 1)];

                while (i <= j)
                {
                    while (comparer.Compare(list[i], pivot) < 0)
                    {
                        i++;
                    }

                    while (comparer.Compare(list[j], pivot) > 0)
                    {
                        j--;
                    }

                    if (i <= j)
                    {
                        if (i != j)
                        {
                            Swap(list, i, j);
                        }

                        i++;
                        j--;
                    }
                }

                if (j - left < right - i)
                {
                    if (left < j)
                    {
                        QuickSort(list, left, j, comparer);
                    }

                    left = i;
                }
                else
                {
                    if (i < right)
                    {
                        QuickSort(list, i, right, comparer);
                    }

                    right = j;
                }
            }
        }
    }
}
