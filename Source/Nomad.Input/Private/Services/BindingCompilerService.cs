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
using System.Collections.Immutable;
using Nomad.Core.Util;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================
	
	BindingCompilerService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class BindingCompilerService
	{
		private const int DEVICE_COUNT = (int)InputDeviceSlot.Count;
		private const int CONTROL_COUNT = (int)InputControlId.Count;
		private const int BUTTON_BUCKET_COUNT = DEVICE_COUNT * CONTROL_COUNT * 2;
		private const int AXIS_BUCKET_COUNT = DEVICE_COUNT * CONTROL_COUNT;

		private readonly CompiledBindingRepository _compiledBindings;

		/*
		===============
		BindingCompilerService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="compiledBindings"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public BindingCompilerService( CompiledBindingRepository compiledBindings )
		{
			_compiledBindings = compiledBindings ?? throw new ArgumentNullException( nameof( compiledBindings ) );
		}

		/*
		===============
		CompileIntoRepository
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="actions"></param>
		public void CompileIntoRepository( ImmutableArray<InputActionDefinition> actions )
		{
			_compiledBindings.Replace( Compile( actions ) );
		}

		/*
		===============
		Compile
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="actions"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static CompiledBindingGraph Compile( ImmutableArray<InputActionDefinition> actions )
		{
			if ( actions.IsDefaultOrEmpty ) {
				return CompiledBindingGraph.Empty;
			}

			var actionInfos = new CompiledActionInfo[actions.Length];
			var allBindings = new List<CompiledBinding>( EstimateBindingCapacity( actions ) );

			var buttonCounts = new int[BUTTON_BUCKET_COUNT];
			var axisCounts = new int[AXIS_BUCKET_COUNT];
			var deltaCounts = new int[AXIS_BUCKET_COUNT];

			var composite1DIndices = new List<int>( 16 );
			var composite2DIndices = new List<int>( 16 );

			for ( int actionIndex = 0; actionIndex < actions.Length; actionIndex++ ) {
				var action = actions[actionIndex];
				actionInfos[actionIndex] = new CompiledActionInfo( new InternString( action.Id ) );

				for ( int i = 0; i < action.Bindings.Length; i++ ) {
					ref readonly var bindingDef = ref action.Bindings.ItemRef( i );

					BuildModifierMask(
						in bindingDef,
						out ulong modifierMask0,
						out ulong modifierMask1,
						out ulong modifierMask2,
						out ulong modifierMask3,
						out int modifierCount
					);

					int scoreBase = ComputeScoreBase(
						priority: 0,
						modifierCount: modifierCount,
						exactScheme: true,
						consumesInput: false
					);

					var binding = new CompiledBinding(
						actionName: new InternString( action.Name ),
						actionId: new InternString( action.Id ),
						actionIndex: actionIndex,
						valueType: action.ValueType,
						kind: bindingDef.Kind,
						scheme: bindingDef.Scheme,
						priority: 0,
						scoreBase: scoreBase,
						consumesInput: false,
						contextMask: 0xFFFFFFFF,
						modifierMask0: modifierMask0,
						modifierMask1: modifierMask1,
						modifierMask2: modifierMask2,
						modifierMask3: modifierMask3,
						button: bindingDef.Button,
						axis1D: bindingDef.Axis1D,
						axis1DComposite: bindingDef.Axis1DComposite,
						axis2D: bindingDef.Axis2D,
						axis2DComposite: bindingDef.Axis2DComposite,
						delta2D: bindingDef.Delta2D
					);

					int bindingIndex = allBindings.Count;
					allBindings.Add( binding );

					switch ( binding.Kind ) {
						case InputBindingKind.Button: {
								int pressedBucket = GetButtonBucketIndex( binding.Button.DeviceId, binding.Button.ControlId, true );
								int releasedBucket = GetButtonBucketIndex( binding.Button.DeviceId, binding.Button.ControlId, false );

								buttonCounts[pressedBucket]++;
								buttonCounts[releasedBucket]++;
								break;
							}

						case InputBindingKind.Axis1D:
							axisCounts[GetAxisBucketIndex( binding.Axis1D.DeviceId, binding.Axis1D.ControlId )]++;
							break;

						case InputBindingKind.Axis2D:
							axisCounts[GetAxisBucketIndex( binding.Axis2D.DeviceId, binding.Axis2D.ControlId )]++;
							break;

						case InputBindingKind.Delta2D:
							deltaCounts[GetAxisBucketIndex( binding.Delta2D.DeviceId, binding.Delta2D.ControlId )]++;
							break;

						case InputBindingKind.Axis1DComposite:
							composite1DIndices.Add( bindingIndex );
							break;

						case InputBindingKind.Axis2DComposite:
							composite2DIndices.Add( bindingIndex );
							break;

						default:
							throw new ArgumentOutOfRangeException( nameof( binding.Kind ) );
					}
				}
			}

			var buttonBuckets = BuildBuckets( buttonCounts, out int[] buttonWriteOffsets, out int totalButtonRefs );
			var axisBuckets = BuildBuckets( axisCounts, out int[] axisWriteOffsets, out int totalAxisRefs );
			var deltaBuckets = BuildBuckets( deltaCounts, out int[] deltaWriteOffsets, out int totalDeltaRefs );

			var buttonBindingIndices = new int[totalButtonRefs];
			var axisBindingIndices = new int[totalAxisRefs];
			var deltaBindingIndices = new int[totalDeltaRefs];

			for ( int bindingIndex = 0; bindingIndex < allBindings.Count; bindingIndex++ ) {
				ref readonly var binding = ref allBindings.ItemRef( bindingIndex );

				switch ( binding.Kind ) {
					case InputBindingKind.Button: {
							int pressedBucket = GetButtonBucketIndex( binding.Button.DeviceId, binding.Button.ControlId, true );
							int releasedBucket = GetButtonBucketIndex( binding.Button.DeviceId, binding.Button.ControlId, false );

							buttonBindingIndices[buttonWriteOffsets[pressedBucket]++] = bindingIndex;
							buttonBindingIndices[buttonWriteOffsets[releasedBucket]++] = bindingIndex;
							break;
						}
					case InputBindingKind.Axis1D:
						axisBindingIndices[axisWriteOffsets[GetAxisBucketIndex( binding.Axis1D.DeviceId, binding.Axis1D.ControlId )]++] = bindingIndex;
						break;
					case InputBindingKind.Axis2D:
						axisBindingIndices[axisWriteOffsets[GetAxisBucketIndex( binding.Axis2D.DeviceId, binding.Axis2D.ControlId )]++] = bindingIndex;
						break;
					case InputBindingKind.Delta2D:
						deltaBindingIndices[deltaWriteOffsets[GetAxisBucketIndex( binding.Delta2D.DeviceId, binding.Delta2D.ControlId )]++] = bindingIndex;
						break;
				}
			}

			return new CompiledBindingGraph(
				bindings: allBindings.ToArray(),
				actions: actionInfos,
				buttonBuckets: buttonBuckets,
				buttonBindingIndices: buttonBindingIndices,
				axisBuckets: axisBuckets,
				axisBindingIndices: axisBindingIndices,
				deltaBuckets: deltaBuckets,
				deltaBindingIndices: deltaBindingIndices,
				composite1DBindingIndices: composite1DIndices.ToArray(),
				composite2DBindingIndices: composite2DIndices.ToArray()
			);
		}

		/*
		===============
		EstimateBindingCapacity
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="actions"></param>
		/// <returns></returns>
		private static int EstimateBindingCapacity( ImmutableArray<InputActionDefinition> actions )
		{
			int total = 0;
			for ( int i = 0; i < actions.Length; i++ ) {
				total += actions[i].Bindings.Length;
			}
			return total;
		}

		/*
		===============
		BuildBuckets
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="counts"></param>
		/// <param name="writeOffsets"></param>
		/// <param name="totalEntries"></param>
		/// <returns></returns>
		private static Bucket[] BuildBuckets( int[] counts, out int[] writeOffsets, out int totalEntries )
		{
			var buckets = new Bucket[counts.Length];
			writeOffsets = new int[counts.Length];

			int cursor = 0;
			for ( int i = 0; i < counts.Length; i++ ) {
				int count = counts[i];
				buckets[i] = new Bucket( cursor, count );
				writeOffsets[i] = cursor;
				cursor += count;
			}

			totalEntries = cursor;
			return buckets;
		}

		/*
		===============
		BuildModifierMask
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingDef"></param>
		/// <param name="mask0"></param>
		/// <param name="mask1"></param>
		/// <param name="mask2"></param>
		/// <param name="mask3"></param>
		/// <param name="modifierCount"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		private static void BuildModifierMask(
			in InputBindingDefinition bindingDef,
			out ulong mask0,
			out ulong mask1,
			out ulong mask2,
			out ulong mask3,
			out int modifierCount
		)
		{
			mask0 = 0UL;
			mask1 = 0UL;
			mask2 = 0UL;
			mask3 = 0UL;
			modifierCount = 0;

			if ( bindingDef.Kind != InputBindingKind.Button ) {
				return;
			}

			var modifiers = bindingDef.Button.Modifiers;
			for ( int i = 0; i < modifiers.Length; i++ ) {
				int control = (int)modifiers[i];
				int word = control >> 6;
				ulong bit = 1UL << (control & 63);

				switch ( word ) {
					case 0: mask0 |= bit; break;
					case 1: mask1 |= bit; break;
					case 2: mask2 |= bit; break;
					case 3: mask3 |= bit; break;
					default: throw new ArgumentOutOfRangeException( nameof( bindingDef ) );
				}

				modifierCount++;
			}
		}

		/*
		===============
		ComputeScoreBase
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="priority"></param>
		/// <param name="modifierCount"></param>
		/// <param name="exactScheme"></param>
		/// <param name="consumesInput"></param>
		/// <returns></returns>
		private static int ComputeScoreBase( int priority, int modifierCount, bool exactScheme, bool consumesInput )
		{
			int score = 0;
			score += priority * 100;
			score += modifierCount * 25;
			score += exactScheme ? 10 : 0;
			score += consumesInput ? 1 : 0;
			return score;
		}

		/*
		===============
		GetButtonBucketIndex
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="control"></param>
		/// <param name="pressed"></param>
		/// <returns></returns>
		private static int GetButtonBucketIndex( InputDeviceSlot device, InputControlId control, bool pressed )
		{
			return ((((int)device * CONTROL_COUNT) + (int)control) << 1) | (pressed ? 1 : 0);
		}

		/*
		===============
		GetAxisBucketIndex
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		private static int GetAxisBucketIndex( InputDeviceSlot device, InputControlId control )
		{
			return ((int)device * CONTROL_COUNT) + (int)control;
		}
	}

	internal static class ListRefExtensions
	{
		public static ref readonly T ItemRef<T>( this List<T> list, int index )
		{
#if NET6_0_OR_GREATER
			return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan( list )[index];
#else
			if ( (uint)index >= (uint)list.Count ) {
				throw new ArgumentOutOfRangeException( nameof( index ) );
			}

			return ref ListLayoutAccessor<T>.GetItems( list )[index];
#endif
		}

#if !NET6_0_OR_GREATER
		private static class ListLayoutAccessor<T> {
			[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
			private sealed class ListLayout {
				public T[] Items = null!;
			}

			public static T[] GetItems( List<T> list ) {
				return System.Runtime.CompilerServices.Unsafe.As<ListLayout>( list ).Items;
			}
		}
#endif
	};
};
