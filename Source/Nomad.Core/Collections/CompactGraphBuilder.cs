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

namespace Nomad.Core.Collections
{
    public sealed class CompactGraphBuilder
    {
        private readonly PooledList<int>[] _adjacency;

        public CompactGraphBuilder(int nodeCount, int averageDegree = 4)
        {
            if (nodeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeCount));
            }

            _adjacency = new PooledList<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                _adjacency[i] = new PooledList<int>(averageDegree);
            }
        }

        public void AddEdge(int from, int to)
        {
            _adjacency[from].Add(to);
        }

        public CompactGraph Build()
        {
            int nodeCount = _adjacency.Length;
            int[] offsets = new int[nodeCount + 1];
            int total = 0;
            for (int i = 0; i < nodeCount; i++)
            {
                offsets[i] = total;
                total += _adjacency[i].Count;
            }
            offsets[nodeCount] = total;
            int[] edges = new int[total];
            for (int i = 0; i < nodeCount; i++)
            {
                _adjacency[i].Span.CopyTo(edges.AsSpan(offsets[i]));
            }
            return new CompactGraph(offsets, edges);
        }
    }
}
