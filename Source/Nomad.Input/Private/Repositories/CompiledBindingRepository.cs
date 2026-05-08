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
using System.Threading;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Repositories
{
	internal sealed class CompiledBindingRepository
	{
		private const int CONTROL_COUNT = (int)InputControlId.Count;
		private CompiledBindingGraph _current = CompiledBindingGraph.Empty;

		public CompiledBindingGraph Current {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => Volatile.Read( ref _current );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Replace( CompiledBindingGraph graph )
		{
			Volatile.Write( ref _current, graph );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ReadOnlySpan<int> GetButtonCandidateIndices( CompiledBindingGraph graph, InputDeviceSlot device, InputControlId control, bool pressed )
		{
			int bucketIndex = ((((int)device * CONTROL_COUNT) + (int)control) << 1) | (pressed ? 1 : 0);
			ref readonly Bucket bucket = ref graph.ButtonBuckets[bucketIndex];
			return graph.ButtonBindingIndices.AsSpan( bucket.Start, bucket.Length );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ReadOnlySpan<int> GetAxisCandidateIndices( CompiledBindingGraph graph, InputDeviceSlot device, InputControlId control )
		{
			int bucketIndex = ((int)device * CONTROL_COUNT) + (int)control;
			ref readonly Bucket bucket = ref graph.AxisBuckets[bucketIndex];
			return graph.AxisBindingIndices.AsSpan( bucket.Start, bucket.Length );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ReadOnlySpan<int> GetDeltaCandidateIndices( CompiledBindingGraph graph, InputDeviceSlot device, InputControlId control )
		{
			int bucketIndex = ((int)device * CONTROL_COUNT) + (int)control;
			ref readonly Bucket bucket = ref graph.DeltaBuckets[bucketIndex];
			return graph.DeltaBindingIndices.AsSpan( bucket.Start, bucket.Length );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ReadOnlySpan<int> GetComposite1DBindingIndices( CompiledBindingGraph graph ) => graph.Composite1DBindingIndices;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ReadOnlySpan<int> GetComposite2DBindingIndices( CompiledBindingGraph graph ) => graph.Composite2DBindingIndices;
	};
};
