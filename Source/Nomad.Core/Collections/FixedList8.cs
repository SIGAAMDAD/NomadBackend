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
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FixedList8<T> : IDisposable
    {
        private const int InlineCapacity = 8;

        private int _count;
        private T[]? _heap;

#if NET8_0_OR_GREATER
        [InlineArray(InlineCapacity)]
        private struct InlineBuffer
        {
            private T _element0;
        }
        private InlineBuffer _inline;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T InlineAt(int index)
        {
            return ref _inline[index];
        }
#else
		private T _0, _1, _2, _3, _4, _5, _6, _7;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ref T InlineAt(int index)
		{
			switch (index)
			{
				case 0: return ref _0;
				case 1: return ref _1;
				case 2: return ref _2;
				case 3: return ref _3;
				case 4: return ref _4;
				case 5: return ref _5;
				case 6: return ref _6;
				case 7: return ref _7;
				default: throw new ArgumentNullException(nameof(index));
			}
		}
#endif

        /// <summary>
        ///
        /// </summary>
        public int Count => _count;

        /// <summary>
        ///
        /// </summary>
        public int Capacity => _heap?.Length ?? InlineCapacity;

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ref T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (_heap is null)
                {
                    return ref InlineAt(index);
                }

                return ref _heap[index];
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (_heap is null)
            {
                if (_count < InlineCapacity)
                {
                    InlineAt(_count++) = item;
                    return;
                }

                MoveInlineToHeap(InlineCapacity * 3);
            }
            else if (_count == _heap.Length)
            {
                GrowHeap(_count + 1);
            }

            _heap![_count++] = item;
        }

        /// <summary>
        ///
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (_heap is null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        InlineAt(i) = default!;
                    }
                }
                else
                {
                    Array.Clear(_heap, 0, _count);
                }
            }

            _count = 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public bool RemoveSwapBack(T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(this[i], item))
                {
                    RemoveAtSwapBack(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void RemoveAtSwapBack(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int last = _count - 1;

            if (index != last)
            {
                this[index] = this[last];
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                this[last] = default!;
            }

            _count--;
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
            if (_heap is not null)
            {
                ArrayPool<T>.Shared.Return(
                    _heap,
                    RuntimeHelpers.IsReferenceOrContainsReferences<T>());

                _heap = null;
            }

            _count = 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="newCapacity"></param>
        private void MoveInlineToHeap(int newCapacity)
        {
            T[] arr = ArrayPool<T>.Shared.Rent(newCapacity);

            for (int i = 0; i < _count; i++)
            {
                arr[i] = InlineAt(i);
            }

            _heap = arr;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="requiredCapacity"></param>
        private void GrowHeap(int requiredCapacity)
        {
            T[] old = _heap!;
            int newCapacity = Math.Max(requiredCapacity, old.Length * 2);
            T[] arr = ArrayPool<T>.Shared.Rent(newCapacity);

            Array.Copy(old, 0, arr, 0, _count);

            ArrayPool<T>.Shared.Return(
                old,
                RuntimeHelpers.IsReferenceOrContainsReferences<T>());

            _heap = arr;
        }
    }
}
