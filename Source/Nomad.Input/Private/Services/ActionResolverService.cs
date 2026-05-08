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
using System.Numerics;
using System.Runtime.CompilerServices;
using Nomad.Core.Input;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================
	
	ActionResolverService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class ActionResolverService
	{
		private const int RESOLVED_ACTION_BUFFER_RING_SIZE = 8;

		private readonly CompiledBindingRepository _compiledBindings;
		private readonly InputStateService _stateService;

		private InputActionPhase[] _phaseByAction = Array.Empty<InputActionPhase>();
		private int[] _slotGeneration = Array.Empty<int>();
		private int[] _touchedSlots = Array.Empty<int>();

		private int[] _bestBindingIndex = Array.Empty<int>();
		private int[] _bestScore = Array.Empty<int>();
		private float[] _bestMagnitude = Array.Empty<float>();
		private bool[] _bestButtonValue = Array.Empty<bool>();
		private float[] _bestFloatValue = Array.Empty<float>();
		private Vector2[] _bestVector2Value = Array.Empty<Vector2>();
		private InputValueType[] _bestValueType = Array.Empty<InputValueType>();

		private int _generation;
		private int _touchedCount;

		private ResolvedAction[][] _resolvedActionBuffers = Array.Empty<ResolvedAction[]>();
		private int _resolvedActionBufferCursor;

		public ActionResolverService( CompiledBindingRepository compiledBindings, InputStateService stateService )
		{
			_compiledBindings = compiledBindings ?? throw new ArgumentNullException( nameof( compiledBindings ) );
			_stateService = stateService ?? throw new ArgumentNullException( nameof( stateService ) );
		}

		public ReadOnlySpan<ResolvedAction> ResolveMatchesNonAlloc( CompiledBindingGraph graph, BindingMatchSet matches, long timeStamp )
		{
			BeginPass( graph.Actions.Length );

			for ( int i = 0; i < matches.Length; i++ ) {
				int bindingIndex = matches.BindingIndices[i];
				ref readonly var binding = ref graph.Bindings[bindingIndex];

				if ( !TryEvaluateBinding( in binding, out var valueType, out var buttonValue, out var floatValue, out var vector2Value, out var magnitude ) ) {
					continue;
				}

				StoreBestCandidate(
					actionSlot: binding.ActionIndex,
					bindingIndex: bindingIndex,
					score: matches.Scores[i],
					valueType: valueType,
					buttonValue: buttonValue,
					floatValue: floatValue,
					vector2Value: vector2Value,
					magnitude: magnitude
				);
			}

			return MaterializeResolvedActions( graph, timeStamp );
		}

		public ReadOnlySpan<ResolvedAction> ResolveKeyboardCompositesNonAlloc( CompiledBindingGraph graph, uint activeContextMask, InputScheme? activeScheme, long timeStamp )
		{
			BeginPass( graph.Actions.Length );

			bool hasActiveScheme = activeScheme.HasValue;
			InputScheme activeSchemeValue = activeScheme.GetValueOrDefault();

			ReadOnlySpan<int> composite1D = CompiledBindingRepository.GetComposite1DBindingIndices( graph );
			for ( int i = 0; i < composite1D.Length; i++ ) {
				int bindingIndex = composite1D[i];
				ref readonly var binding = ref graph.Bindings[bindingIndex];

				if ( !ContextMatches( binding.ContextMask, activeContextMask ) ) {
					continue;
				}
				if ( hasActiveScheme && binding.Scheme != activeSchemeValue ) {
					continue;
				}

				float value = ResolveComposite1D( in binding.Axis1DComposite );
				StoreBestCandidate(
					actionSlot: binding.ActionIndex,
					bindingIndex: bindingIndex,
					score: binding.ScoreBase,
					valueType: InputValueType.Float,
					buttonValue: false,
					floatValue: value,
					vector2Value: default,
					magnitude: MathF.Abs( value )
				);
			}

			ReadOnlySpan<int> composite2D = CompiledBindingRepository.GetComposite2DBindingIndices( graph );
			for ( int i = 0; i < composite2D.Length; i++ ) {
				int bindingIndex = composite2D[i];
				ref readonly var binding = ref graph.Bindings[bindingIndex];

				if ( !ContextMatches( binding.ContextMask, activeContextMask ) ) {
					continue;
				}
				if ( hasActiveScheme && binding.Scheme != activeSchemeValue ) {
					continue;
				}

				Vector2 value = ResolveComposite2D( in binding.Axis2DComposite );
				StoreBestCandidate(
					actionSlot: binding.ActionIndex,
					bindingIndex: bindingIndex,
					score: binding.ScoreBase,
					valueType: InputValueType.Vector2,
					buttonValue: false,
					floatValue: 0.0f,
					vector2Value: value,
					magnitude: value.LengthSquared()
				);
			}

			return MaterializeResolvedActions( graph, timeStamp );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void BeginPass( int actionCount )
		{
			EnsureActionCapacity( actionCount );
			_touchedCount = 0;

			_generation++;
			if ( _generation == 0 ) {
				Array.Clear( _slotGeneration, 0, _slotGeneration.Length );
				_generation = 1;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void StoreBestCandidate(
			int actionSlot,
			int bindingIndex,
			int score,
			InputValueType valueType,
			bool buttonValue,
			float floatValue,
			Vector2 vector2Value,
			float magnitude
		)
		{
			if ( _slotGeneration[actionSlot] != _generation ) {
				_slotGeneration[actionSlot] = _generation;
				_touchedSlots[_touchedCount++] = actionSlot;

				_bestBindingIndex[actionSlot] = bindingIndex;
				_bestScore[actionSlot] = score;
				_bestMagnitude[actionSlot] = magnitude;
				_bestValueType[actionSlot] = valueType;
				_bestButtonValue[actionSlot] = buttonValue;
				_bestFloatValue[actionSlot] = floatValue;
				_bestVector2Value[actionSlot] = vector2Value;
				return;
			}

			if ( score < _bestScore[actionSlot] ) {
				return;
			}
			if ( score == _bestScore[actionSlot] && magnitude <= _bestMagnitude[actionSlot] ) {
				return;
			}

			_bestBindingIndex[actionSlot] = bindingIndex;
			_bestScore[actionSlot] = score;
			_bestMagnitude[actionSlot] = magnitude;
			_bestValueType[actionSlot] = valueType;
			_bestButtonValue[actionSlot] = buttonValue;
			_bestFloatValue[actionSlot] = floatValue;
			_bestVector2Value[actionSlot] = vector2Value;
		}

		private ReadOnlySpan<ResolvedAction> MaterializeResolvedActions( CompiledBindingGraph graph, long timeStamp )
		{
			ResolvedAction[] resolvedActionBuffer = GetResolvedActionBuffer( _touchedCount );

			int actionCount = 0;
			for ( int i = 0; i < _touchedCount; i++ ) {
				int slot = _touchedSlots[i];
				InputValueType valueType = _bestValueType[slot];

				bool isActive = valueType switch {
					InputValueType.Button => _bestButtonValue[slot],
					InputValueType.Float => MathF.Abs( _bestFloatValue[slot] ) > 0.0001f,
					InputValueType.Vector2 => _bestVector2Value[slot].LengthSquared() > 0.0001f,
					_ => false
				};

				if ( !TryResolvePhase( slot, isActive, out var phase ) ) {
					continue;
				}

				resolvedActionBuffer[actionCount++] = new ResolvedAction(
					actionId: graph.Actions[slot].ActionId,
					actionIndex: slot,
					valueType: valueType,
					phase: phase,
					timeStamp: timeStamp,
					buttonValue: _bestButtonValue[slot],
					floatValue: _bestFloatValue[slot],
					vector2Value: _bestVector2Value[slot]
				);
			}

			return resolvedActionBuffer.AsSpan( 0, actionCount );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private bool TryResolvePhase( int actionSlot, bool isActive, out InputActionPhase phase )
		{
			InputActionPhase previousPhase = _phaseByAction[actionSlot];
			bool wasActive = previousPhase is InputActionPhase.Started or InputActionPhase.Performed;

			if ( isActive ) {
				phase = wasActive ? InputActionPhase.Performed : InputActionPhase.Started;
				_phaseByAction[actionSlot] = phase;
				return true;
			}

			if ( wasActive ) {
				phase = InputActionPhase.Canceled;
				_phaseByAction[actionSlot] = phase;
				return true;
			}

			phase = default;
			return false;
		}

		private bool TryEvaluateBinding( in CompiledBinding binding, out InputValueType valueType, out bool buttonValue, out float floatValue, out Vector2 vector2Value, out float magnitude )
		{
			switch ( binding.Kind ) {
				case InputBindingKind.Button: {
						bool pressed = _stateService.IsPressed( binding.Button.DeviceId, binding.Button.ControlId );
						valueType = InputValueType.Button;
						buttonValue = pressed;
						floatValue = 0.0f;
						vector2Value = default;
						magnitude = pressed ? 1.0f : 0.0f;
						return true;
					}

				case InputBindingKind.Axis1D: {
						float value = ApplyAxis1DProcessors(
							_stateService.GetAxis1D( binding.Axis1D.DeviceId, binding.Axis1D.ControlId ),
							in binding.Axis1D
						);

						valueType = InputValueType.Float;
						buttonValue = false;
						floatValue = value;
						vector2Value = default;
						magnitude = MathF.Abs( value );
						return true;
					}

				case InputBindingKind.Axis2D: {
						Vector2 value = ApplyAxis2DProcessors(
							_stateService.GetAxis2D( binding.Axis2D.DeviceId, binding.Axis2D.ControlId ),
							in binding.Axis2D
						);

						valueType = InputValueType.Vector2;
						buttonValue = false;
						floatValue = 0.0f;
						vector2Value = value;
						magnitude = value.LengthSquared();
						return true;
					}

				case InputBindingKind.Delta2D: {
						Vector2 value = ApplyDelta2DProcessors( _stateService.MouseDelta, in binding.Delta2D );

						valueType = InputValueType.Vector2;
						buttonValue = false;
						floatValue = 0.0f;
						vector2Value = value;
						magnitude = value.LengthSquared();
						return true;
					}

				default:
					valueType = default;
					buttonValue = default;
					floatValue = default;
					vector2Value = default;
					magnitude = default;
					return false;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private float ResolveComposite1D( in Axis1DCompositeBinding binding )
		{
			float negative = _stateService.IsPressed( InputDeviceSlot.Keyboard, binding.Negative ) ? 1.0f : 0.0f;
			float positive = _stateService.IsPressed( InputDeviceSlot.Keyboard, binding.Positive ) ? 1.0f : 0.0f;
			float value = (positive - negative) * binding.Scale;

			if ( binding.Normalize ) {
				value = Math.Clamp( value, -1.0f, 1.0f );
			}

			return value;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private Vector2 ResolveComposite2D( in Axis2DCompositeBinding binding )
		{
			float up = _stateService.IsPressed( InputDeviceSlot.Keyboard, binding.Up ) ? 1.0f : 0.0f;
			float down = _stateService.IsPressed( InputDeviceSlot.Keyboard, binding.Down ) ? 1.0f : 0.0f;
			float left = _stateService.IsPressed( InputDeviceSlot.Keyboard, binding.Left ) ? 1.0f : 0.0f;
			float right = _stateService.IsPressed( InputDeviceSlot.Keyboard, binding.Right ) ? 1.0f : 0.0f;

			Vector2 value = new(
				(right - left) * binding.ScaleX,
				(up - down) * binding.ScaleY
			);

			if ( binding.Normalize && value != Vector2.Zero ) {
				value = Vector2.Normalize( value );
			}

			return value;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static float ApplyAxis1DProcessors( float value, in Axis1DBinding binding )
		{
			if ( MathF.Abs( value ) < binding.Deadzone ) {
				value = 0.0f;
			}
			if ( binding.Invert ) {
				value = -value;
			}

			return value * binding.Sensitivity * binding.Scale;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static Vector2 ApplyAxis2DProcessors( Vector2 value, in Axis2DBinding binding )
		{
			float deadzoneSq = binding.Deadzone * binding.Deadzone;
			if ( value.LengthSquared() < deadzoneSq ) {
				return Vector2.Zero;
			}
			if ( binding.InvertX ) {
				value.X = -value.X;
			}
			if ( binding.InvertY ) {
				value.Y = -value.Y;
			}

			value.X *= binding.ScaleX * binding.Sensitivity;
			value.Y *= binding.ScaleY * binding.Sensitivity;
			return value;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static Vector2 ApplyDelta2DProcessors( Vector2 value, in Delta2DBinding binding )
		{
			if ( binding.InvertX ) {
				value.X = -value.X;
			}
			if ( binding.InvertY ) {
				value.Y = -value.Y;
			}

			value.X *= binding.ScaleX * binding.Sensitivity;
			value.Y *= binding.ScaleY * binding.Sensitivity;
			return value;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static bool ContextMatches( uint bindingMask, uint activeMask )
		{
			return bindingMask == 0 || (bindingMask & activeMask) != 0;
		}

		private void EnsureActionCapacity( int actionCount )
		{
			if ( _phaseByAction.Length >= actionCount ) {
				return;
			}

			int previousLength = _phaseByAction.Length;
			Array.Resize( ref _phaseByAction, actionCount );
			Array.Fill( _phaseByAction, InputActionPhase.Count, previousLength, actionCount - previousLength );
			Array.Resize( ref _slotGeneration, actionCount );
			Array.Resize( ref _touchedSlots, actionCount );

			Array.Resize( ref _bestBindingIndex, actionCount );
			Array.Resize( ref _bestScore, actionCount );
			Array.Resize( ref _bestMagnitude, actionCount );
			Array.Resize( ref _bestButtonValue, actionCount );
			Array.Resize( ref _bestFloatValue, actionCount );
			Array.Resize( ref _bestVector2Value, actionCount );
			Array.Resize( ref _bestValueType, actionCount );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private ResolvedAction[] GetResolvedActionBuffer( int requiredCapacity )
		{
			if ( _resolvedActionBuffers.Length == 0 ) {
				_resolvedActionBuffers = new ResolvedAction[RESOLVED_ACTION_BUFFER_RING_SIZE][];
				Array.Fill( _resolvedActionBuffers, Array.Empty<ResolvedAction>() );
			}

			int bufferIndex = _resolvedActionBufferCursor;
			_resolvedActionBufferCursor = (_resolvedActionBufferCursor + 1) % _resolvedActionBuffers.Length;

			ResolvedAction[] buffer = _resolvedActionBuffers[bufferIndex];
			if ( buffer.Length >= requiredCapacity ) {
				return buffer;
			}

			int newCapacity = Math.Max( requiredCapacity, buffer.Length == 0 ? 8 : buffer.Length * 2 );
			Array.Resize( ref buffer, newCapacity );
			_resolvedActionBuffers[bufferIndex] = buffer;
			return buffer;
		}
	};
};
