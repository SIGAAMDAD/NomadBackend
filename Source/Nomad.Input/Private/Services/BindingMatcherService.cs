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
using Nomad.Input.Private.Extensions;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================
	
	BindingMatcherService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class BindingMatcherService
	{
		private readonly CompiledBindingRepository _compiledBindings;
		private readonly InputStateService _stateService;

		private int[] _matchedBindingIndices = Array.Empty<int>();
		private int[] _matchedScores = Array.Empty<int>();

		public BindingMatcherService( CompiledBindingRepository compiledBindings, InputStateService stateService )
		{
			_compiledBindings = compiledBindings ?? throw new ArgumentNullException( nameof( compiledBindings ) );
			_stateService = stateService ?? throw new ArgumentNullException( nameof( stateService ) );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public BindingMatchSet MatchKeyboard( CompiledBindingGraph graph, in KeyboardEventArgs evt, uint activeContextMask, InputScheme? activeScheme )
		{
			return MatchButtons(
				graph,
				CompiledBindingRepository.GetButtonCandidateIndices( graph, InputDeviceSlot.Keyboard, evt.KeyNum.ToControlId(), evt.Pressed ),
				activeContextMask,
				activeScheme
			);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public BindingMatchSet MatchMouseButton( CompiledBindingGraph graph, in MouseButtonEventArgs evt, uint activeContextMask, InputScheme? activeScheme )
		{
			return MatchButtons(
				graph,
				CompiledBindingRepository.GetButtonCandidateIndices( graph, InputDeviceSlot.Mouse, evt.Button.ToControlId(), evt.Pressed ),
				activeContextMask,
				activeScheme
			);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public BindingMatchSet MatchGamepadButton( CompiledBindingGraph graph, in GamepadButtonEventArgs evt, uint activeContextMask, InputScheme? activeScheme )
		{
			return MatchButtons(
				graph,
				CompiledBindingRepository.GetButtonCandidateIndices( graph, GetGamepadDeviceSlot( evt.DeviceId ), evt.Button.ToControlId(), evt.Pressed ),
				activeContextMask,
				activeScheme
			);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public BindingMatchSet MatchGamepadAxis( CompiledBindingGraph graph, InputDeviceSlot device, InputControlId control, uint activeContextMask, InputScheme? activeScheme )
		{
			return MatchAxes(
				graph,
				CompiledBindingRepository.GetAxisCandidateIndices( graph, device, control ),
				activeContextMask,
				activeScheme
			);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public BindingMatchSet MatchMouseDelta( CompiledBindingGraph graph, uint activeContextMask, InputScheme? activeScheme )
		{
			if ( _stateService.MouseDelta == Vector2.Zero ) {
				return new BindingMatchSet( ReadOnlySpan<int>.Empty, ReadOnlySpan<int>.Empty );
			}

			return MatchAxes(
				graph,
				CompiledBindingRepository.GetDeltaCandidateIndices( graph, InputDeviceSlot.Mouse, InputControlId.Delta ),
				activeContextMask,
				activeScheme
			);
		}

		private BindingMatchSet MatchButtons( CompiledBindingGraph graph, ReadOnlySpan<int> candidateIndices, uint activeContextMask, InputScheme? activeScheme )
		{
			EnsureMatchCapacity( candidateIndices.Length );

			int count = 0;
			bool hasActiveScheme = activeScheme.HasValue;
			InputScheme activeSchemeValue = activeScheme.GetValueOrDefault();

			for ( int i = 0; i < candidateIndices.Length; i++ ) {
				int bindingIndex = candidateIndices[i];
				ref readonly var binding = ref graph.Bindings[bindingIndex];

				if ( !ContextMatches( binding.ContextMask, activeContextMask ) ) {
					continue;
				}
				if ( hasActiveScheme && binding.Scheme != activeSchemeValue ) {
					continue;
				}
				if ( !ModifiersSatisfied( in binding ) ) {
					continue;
				}

				_matchedBindingIndices[count] = bindingIndex;
				_matchedScores[count] = binding.ScoreBase;
				count++;
			}

			return new BindingMatchSet(
				_matchedBindingIndices.AsSpan( 0, count ),
				_matchedScores.AsSpan( 0, count )
			);
		}

		private BindingMatchSet MatchAxes( CompiledBindingGraph graph, ReadOnlySpan<int> candidateIndices, uint activeContextMask, InputScheme? activeScheme )
		{
			EnsureMatchCapacity( candidateIndices.Length );

			int count = 0;
			bool hasActiveScheme = activeScheme.HasValue;
			InputScheme activeSchemeValue = activeScheme.GetValueOrDefault();

			for ( int i = 0; i < candidateIndices.Length; i++ ) {
				int bindingIndex = candidateIndices[i];
				ref readonly var binding = ref graph.Bindings[bindingIndex];

				if ( !ContextMatches( binding.ContextMask, activeContextMask ) ) {
					continue;
				}
				if ( hasActiveScheme && binding.Scheme != activeSchemeValue ) {
					continue;
				}

				_matchedBindingIndices[count] = bindingIndex;
				_matchedScores[count] = binding.ScoreBase;
				count++;
			}

			return new BindingMatchSet(
				_matchedBindingIndices.AsSpan( 0, count ),
				_matchedScores.AsSpan( 0, count )
			);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private unsafe bool ModifiersSatisfied( in CompiledBinding binding )
		{
			if ( binding.Kind != InputBindingKind.Button ) {
				return true;
			}

			ulong* keyboardWords = _stateService.GetPressedWords( InputDeviceSlot.Keyboard );
			ref readonly var button = ref binding.Button;

			return ((keyboardWords[0] & button.ModifierMask0) == button.ModifierMask0)
				&& ((keyboardWords[1] & button.ModifierMask1) == button.ModifierMask1)
				&& ((keyboardWords[2] & button.ModifierMask2) == button.ModifierMask2)
				&& ((keyboardWords[3] & button.ModifierMask3) == button.ModifierMask3);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static bool ContextMatches( uint bindingMask, uint activeMask )
		{
			return bindingMask == 0 || (bindingMask & activeMask) != 0;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void EnsureMatchCapacity( int requiredCapacity )
		{
			if ( _matchedBindingIndices.Length >= requiredCapacity ) {
				return;
			}

			int newCapacity = Math.Max( requiredCapacity, _matchedBindingIndices.Length == 0 ? 8 : _matchedBindingIndices.Length * 2 );
			Array.Resize( ref _matchedBindingIndices, newCapacity );
			Array.Resize( ref _matchedScores, newCapacity );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static InputDeviceSlot GetGamepadDeviceSlot( int deviceId )
		{
			return deviceId switch {
				0 => InputDeviceSlot.Gamepad0,
				1 => InputDeviceSlot.Gamepad1,
				2 => InputDeviceSlot.Gamepad2,
				3 => InputDeviceSlot.Gamepad3,
				_ => throw new ArgumentOutOfRangeException( nameof( deviceId ) )
			};
		}
	};
};
