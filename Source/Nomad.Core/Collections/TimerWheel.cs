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
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class TimerWheel<T>
    {
        private struct Entry
        {
            public int TargetTick;
            public T Value;
        }

        private readonly PooledList<Entry>[] _buckets;
        private readonly int _mask;
        private int _tick;

        public int CurrentTick { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _tick; } }

        public TimerWheel(int bucketCountPowerOfTwo = 256, int bucketCapacity = 4)
        {
            int bucketCount = CollectionMath.NextPowerOfTwo(bucketCountPowerOfTwo);
            _mask = bucketCount - 1;
            _buckets = new PooledList<Entry>[bucketCount];
            for (int i = 0; i < bucketCount; i++)
            {
                _buckets[i] = new PooledList<Entry>(bucketCapacity);
            }
        }

        public void Schedule(int delayTicks, T value)
        {
            if (delayTicks < 0)
            {
                delayTicks = 0;
            }

            int target = _tick + delayTicks;
            int bucket = target & _mask;
            _buckets[bucket].Add(new Entry { TargetTick = target, Value = value });
        }

        public void Advance(PooledList<T> output)
        {
            _tick++;
            PooledList<Entry> bucket = _buckets[_tick & _mask];
            int i = 0;
            while (i < bucket.Count)
            {
                ref Entry entry = ref bucket[i];
                if (entry.TargetTick <= _tick)
                {
                    output.Add(entry.Value);
                    bucket.RemoveAtSwapBack(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }
}
