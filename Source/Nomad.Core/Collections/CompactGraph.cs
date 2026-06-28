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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class CompactGraph
    {
        private readonly int[] _offsets;
        private readonly int[] _edges;

        public int NodeCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _offsets.Length - 1; } }
        public int EdgeCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _edges.Length; } }

        public CompactGraph(int[] offsets, int[] edges)
        {
            _offsets = offsets ?? throw new ArgumentNullException(nameof(offsets));
            _edges = edges ?? throw new ArgumentNullException(nameof(edges));
        }

        public ReadOnlySpan<int> GetNeighbors(int node)
        {
            Debug.Assert((uint)node < (uint)NodeCount);
            int start = _offsets[node];
            int end = _offsets[node + 1];
            return _edges.AsSpan(start, end - start);
        }
    }
}
