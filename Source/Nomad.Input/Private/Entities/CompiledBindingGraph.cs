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

namespace Nomad.Input.Private.ValueObjects
{
	/*
	===================================================================================
	
	CompiledBindingGraph
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class CompiledBindingGraph
	{
		public static readonly CompiledBindingGraph Empty = new(
			Array.Empty<CompiledBinding>(),
			Array.Empty<CompiledActionInfo>(),
			Array.Empty<Bucket>(),
			Array.Empty<int>(),
			Array.Empty<Bucket>(),
			Array.Empty<int>(),
			Array.Empty<Bucket>(),
			Array.Empty<int>(),
			Array.Empty<int>(),
			Array.Empty<int>()
		);

		public readonly CompiledBinding[] Bindings;
		public readonly CompiledActionInfo[] Actions;

		public readonly Bucket[] ButtonBuckets;
		public readonly int[] ButtonBindingIndices;

		public readonly Bucket[] AxisBuckets;
		public readonly int[] AxisBindingIndices;

		public readonly Bucket[] DeltaBuckets;
		public readonly int[] DeltaBindingIndices;

		public readonly int[] Composite1DBindingIndices;
		public readonly int[] Composite2DBindingIndices;

		public CompiledBindingGraph(
			CompiledBinding[] bindings,
			CompiledActionInfo[] actions,
			Bucket[] buttonBuckets,
			int[] buttonBindingIndices,
			Bucket[] axisBuckets,
			int[] axisBindingIndices,
			Bucket[] deltaBuckets,
			int[] deltaBindingIndices,
			int[] composite1DBindingIndices,
			int[] composite2DBindingIndices
		)
		{
			Bindings = bindings;
			Actions = actions;
			ButtonBuckets = buttonBuckets;
			ButtonBindingIndices = buttonBindingIndices;
			AxisBuckets = axisBuckets;
			AxisBindingIndices = axisBindingIndices;
			DeltaBuckets = deltaBuckets;
			DeltaBindingIndices = deltaBindingIndices;
			Composite1DBindingIndices = composite1DBindingIndices;
			Composite2DBindingIndices = composite2DBindingIndices;
		}
	};
};
