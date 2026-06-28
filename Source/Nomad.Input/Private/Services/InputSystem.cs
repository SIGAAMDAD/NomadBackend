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
using System.Collections.Immutable;
using System.Numerics;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Input;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.Util;
using Nomad.Input.Interfaces;
using Nomad.Input.Private.Extensions;
using Nomad.Input.Private.Registries;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================

	InputSystem

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class InputSystem : IInputSystem, IInputContextControlService
	{
		public InputScheme Mode => _mode;
		private InputScheme _mode = InputScheme.KeyboardAndMouse;

		public uint ContextMask => _contextMask;
		private uint _contextMask = uint.MaxValue;

		public InputScheme? ActiveScheme => _filterByScheme ? _mode : null;
		private bool _filterByScheme = true;

		private readonly BindRepository _bindRepository;
		private readonly BindingMatcherService _matcherService;
		private readonly ActionResolverService _actionResolverService;
		private readonly InputStateService _stateService;
		private readonly InputDispatchService _dispatchService;
		private readonly InputDeviceSlotService _deviceSlotService;
		private readonly BindingCompilerService _compilerService;
		private readonly CompiledBindingRepository _compiledBindings;
		private readonly IBindResolver _bindResolver;
		private readonly IInputRebindService _rebindService;

		private readonly IDisposable _pauseStateChanged;
		private readonly IDisposable _keyboardEvent;
		private readonly IDisposable _mouseButtonEvent;
		private readonly IDisposable _mouseMotionEvent;
		private readonly IDisposable _mousePositionChangedEvent;
		private readonly IDisposable _gamepadButtonEvent;
		private readonly IDisposable _gamepadAxisEvent;

		private bool _processEvents = true;
		private bool _isDisposed;

		/*
		===============
		InputSystem
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fileSystem"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		/// <param name="registry"></param>
		public InputSystem( IFileSystem fileSystem, ICVarSystemService cvarSystem, ILoggerService logger, IGameEventRegistryService eventFactory, IServiceRegistry registry )
		{
			ArgumentGuard.ThrowIfNull( eventFactory, nameof( eventFactory ) );
			ArgumentGuard.ThrowIfNull( cvarSystem, nameof( cvarSystem ) );
			ArgumentGuard.ThrowIfNull( registry, nameof( registry ) );

			InputCVarRegistry.RegisterCVars( cvarSystem );

			_bindRepository = new BindRepository( fileSystem, cvarSystem, logger );
			_compiledBindings = new CompiledBindingRepository();
			_compilerService = new BindingCompilerService( _compiledBindings );

			_stateService = new InputStateService();
			_deviceSlotService = new InputDeviceSlotService();
			_dispatchService = new InputDispatchService( eventFactory );
			_matcherService = new BindingMatcherService( _compiledBindings, _stateService );
			_actionResolverService = new ActionResolverService( _compiledBindings, _stateService );

			_compilerService.CompileIntoRepository( _bindRepository.GetAllBindings() );

			_bindResolver = new BindResolver( _bindRepository, RecompileBindings );
			_rebindService = new InputRebindService( _bindRepository, _compilerService, eventFactory );

			registry.AddSingleton( _bindResolver );
			registry.AddSingleton( _rebindService );
			registry.AddSingleton<IInputContextControlService>( this );
			registry.AddSingleton<IInputDeviceSlotService>( _deviceSlotService );
			registry.AddSingleton<IInputSnapshotService>( _stateService );

			_pauseStateChanged = eventFactory
				.GetEvent<PauseStateChangedEventArgs>( PauseStateChangedEventArgs.Name, PauseStateChangedEventArgs.NameSpace )
				.Subscribe( OnPauseStateChanged );

			_keyboardEvent = eventFactory
				.GetEvent<KeyboardEventArgs>( KeyboardEventArgs.Name, KeyboardEventArgs.NameSpace )
				.Subscribe( OnKeyboardEventTriggered );

			_mouseButtonEvent = eventFactory
				.GetEvent<MouseButtonEventArgs>( MouseButtonEventArgs.Name, MouseButtonEventArgs.NameSpace )
				.Subscribe( OnMouseButtonEventTriggered );

			_mouseMotionEvent = eventFactory
				.GetEvent<MouseMotionEventArgs>( MouseMotionEventArgs.Name, MouseMotionEventArgs.NameSpace )
				.Subscribe( OnMouseMotionEventTriggered );

			_mousePositionChangedEvent = eventFactory
				.GetEvent<MousePositionChangedEventArgs>( MousePositionChangedEventArgs.Name, MousePositionChangedEventArgs.NameSpace )
				.Subscribe( OnMousePositionChangedEventTriggered );

			_gamepadAxisEvent = eventFactory
				.GetEvent<GamepadAxisEventArgs>( GamepadAxisEventArgs.Name, GamepadAxisEventArgs.NameSpace )
				.Subscribe( OnGamepadAxisEventTriggered );

			_gamepadButtonEvent = eventFactory
				.GetEvent<GamepadButtonEventArgs>( GamepadButtonEventArgs.Name, GamepadButtonEventArgs.NameSpace )
				.Subscribe( OnGamepadButtonEventTriggered );

			_deviceSlotService.DeviceSlotChanged += OnDeviceSlotChanged;
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			_pauseStateChanged.Dispose();
			_mousePositionChangedEvent.Dispose();
			_mouseMotionEvent.Dispose();
			_mouseButtonEvent.Dispose();
			_gamepadAxisEvent.Dispose();
			_gamepadButtonEvent.Dispose();
			_keyboardEvent.Dispose();
			_deviceSlotService.DeviceSlotChanged -= OnDeviceSlotChanged;

			(_rebindService as IDisposable).Dispose();
			_bindRepository.Dispose();
			_stateService.Dispose();

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		PushKeyboardEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="keyEvent"></param>
		public void PushKeyboardEvent( in KeyboardEventArgs keyEvent )
		{
			if ( !TryGetLocalSlot( InputDeviceSlot.Keyboard, out int localSlot ) ) {
				return;
			}

			_stateService.SetPressed( InputDeviceSlot.Keyboard, keyEvent.KeyNum.ToControlId(), keyEvent.Pressed );

			CompiledBindingGraph graph = _compiledBindings.Current;
			BindingMatchSet matches = _matcherService.MatchKeyboard( graph, in keyEvent, _contextMask, ActiveScheme );

			DispatchResolved( graph, _actionResolverService.ResolveMatchesNonAlloc( graph, matches, keyEvent.TimeStamp, InputDeviceSlot.Keyboard, localSlot ) );
			DispatchResolved( graph, _actionResolverService.ResolveKeyboardCompositesNonAlloc( graph, _contextMask, ActiveScheme, keyEvent.TimeStamp, InputDeviceSlot.Keyboard, localSlot ) );
		}

		/*
		===============
		PushMouseButtonEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="mouseButtonEvent"></param>
		public void PushMouseButtonEvent( in MouseButtonEventArgs mouseButtonEvent )
		{
			if ( !TryGetLocalSlot( InputDeviceSlot.Mouse, out int localSlot ) ) {
				return;
			}

			_stateService.SetPressed( InputDeviceSlot.Mouse, mouseButtonEvent.Button.ToControlId(), mouseButtonEvent.Pressed );

			CompiledBindingGraph graph = _compiledBindings.Current;
			BindingMatchSet matches = _matcherService.MatchMouseButton( graph, in mouseButtonEvent, _contextMask, ActiveScheme );

			DispatchResolved( graph, _actionResolverService.ResolveMatchesNonAlloc( graph, matches, mouseButtonEvent.TimeStamp, InputDeviceSlot.Mouse, localSlot ) );
		}

		/*
		===============
		PushMouseMotionEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="mouseMotionEvent"></param>
		public void PushMouseMotionEvent( in MouseMotionEventArgs mouseMotionEvent )
		{
			if ( !TryGetLocalSlot( InputDeviceSlot.Mouse, out int localSlot ) ) {
				return;
			}

			_stateService.AddMouseDelta( new Vector2( mouseMotionEvent.RelativeX, mouseMotionEvent.RelativeY ) );

			CompiledBindingGraph graph = _compiledBindings.Current;
			BindingMatchSet matches = _matcherService.MatchMouseDelta( graph, _contextMask, ActiveScheme );

			DispatchResolved( graph, _actionResolverService.ResolveMatchesNonAlloc( graph, matches, mouseMotionEvent.TimeStamp, InputDeviceSlot.Mouse, localSlot ) );
		}

		/*
		===============
		PushMousePositionChangedEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="mousePositionChangedEvent"></param>
		public void PushMousePositionChangedEvent( in MousePositionChangedEventArgs mousePositionChangedEvent )
		{
			if ( !TryGetLocalSlot( InputDeviceSlot.Mouse, out int localSlot ) ) {
				return;
			}

			_stateService.SetMousePosition( new Vector2( mousePositionChangedEvent.PositionX, mousePositionChangedEvent.PositionY ) );

			CompiledBindingGraph graph = _compiledBindings.Current;
			BindingMatchSet matches = _matcherService.MatchMouseDelta( graph, _contextMask, ActiveScheme );

			DispatchResolved( graph, _actionResolverService.ResolveMatchesNonAlloc( graph, matches, mousePositionChangedEvent.TimeStamp, InputDeviceSlot.Mouse, localSlot ) );
		}

		/*
		===============
		PushGamepadButtonEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="gamepadButtonEvent"></param>
		public void PushGamepadButtonEvent( in GamepadButtonEventArgs gamepadButtonEvent )
		{
			InputDeviceSlot deviceSlot = GetGamepadDeviceSlot( gamepadButtonEvent.DeviceId );
			if ( !TryGetLocalSlot( deviceSlot, out int localSlot ) ) {
				return;
			}

			_stateService.SetPressed( deviceSlot, gamepadButtonEvent.Button.ToControlId(), gamepadButtonEvent.Pressed );

			CompiledBindingGraph graph = _compiledBindings.Current;
			BindingMatchSet matches = _matcherService.MatchGamepadButton( graph, in gamepadButtonEvent, _contextMask, ActiveScheme );

			DispatchResolved( graph, _actionResolverService.ResolveMatchesNonAlloc( graph, matches, gamepadButtonEvent.TimeStamp, deviceSlot, localSlot ) );
		}

		/*
		===============
		PushGamepadAxisEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="gamepadAxisEvent"></param>
		public void PushGamepadAxisEvent( in GamepadAxisEventArgs gamepadAxisEvent )
		{
			InputDeviceSlot deviceSlot = GetGamepadDeviceSlot( gamepadAxisEvent.DeviceId );
			if ( !TryGetLocalSlot( deviceSlot, out int localSlot ) ) {
				return;
			}

			InputControlId controlId = gamepadAxisEvent.Stick.ToControlId();

			_stateService.SetAxis2D( deviceSlot, controlId, gamepadAxisEvent.Value );

			CompiledBindingGraph graph = _compiledBindings.Current;
			BindingMatchSet matches = _matcherService.MatchGamepadAxis( graph, deviceSlot, controlId, _contextMask, ActiveScheme );

			DispatchResolved( graph, _actionResolverService.ResolveMatchesNonAlloc( graph, matches, gamepadAxisEvent.TimeStamp, deviceSlot, localSlot ) );
		}

		/*
		===============
		DispatchResolved
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="actions"></param>
		private void DispatchResolved( CompiledBindingGraph graph, ReadOnlySpan<ResolvedAction> actions )
		{
			for ( int i = 0; i < actions.Length; i++ ) {
				_dispatchService.Dispatch( graph, in actions[i] );
			}
		}

		/*
		===============
		RecompileBindings
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void RecompileBindings()
		{
			_compilerService.CompileIntoRepository( _bindRepository.GetAllBindings() );
		}

		public void SetActiveScheme( InputScheme? scheme )
		{
			if ( scheme.HasValue ) {
				_mode = scheme.Value;
				_filterByScheme = true;
			} else {
				_filterByScheme = false;
			}

			_actionResolverService.ResetPhases();
		}

		public void SetContextMask( uint contextMask )
		{
			_contextMask = contextMask;
			_actionResolverService.ResetPhases();
		}

		public void EnableContext( uint contextMask )
		{
			_contextMask |= contextMask;
			_actionResolverService.ResetPhases();
		}

		public void DisableContext( uint contextMask )
		{
			_contextMask &= ~contextMask;
			_actionResolverService.ResetPhases();
		}

		public void ResetContexts()
		{
			SetContextMask( uint.MaxValue );
		}

		private bool TryGetLocalSlot( InputDeviceSlot deviceSlot, out int localSlot )
		{
			return _deviceSlotService.TryGetLocalSlot( deviceSlot, out localSlot );
		}

		private void OnDeviceSlotChanged( InputDeviceSlot deviceSlot, int localSlot )
		{
			_stateService.Clear();
			_actionResolverService.ResetPhases();
		}

		/*
		===============
		GetGamepadDeviceSlot
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="deviceId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
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

		/*
		===============
		OnKeyboardEventTriggered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnKeyboardEventTriggered( in KeyboardEventArgs args )
		{
			if ( _processEvents ) {
				PushKeyboardEvent( in args );
			}
		}

		/*
		===============
		OnMouseButtonEventTriggered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMouseButtonEventTriggered( in MouseButtonEventArgs args )
		{
			if ( _processEvents ) {
				PushMouseButtonEvent( in args );
			}
		}

		/*
		===============
		OnMouseMotionEventTriggered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMouseMotionEventTriggered( in MouseMotionEventArgs args )
		{
			if ( _processEvents ) {
				PushMouseMotionEvent( in args );
			}
		}

		/*
		===============
		OnMousePositionChangedEventTriggered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMousePositionChangedEventTriggered( in MousePositionChangedEventArgs args )
		{
			if ( _processEvents ) {
				PushMousePositionChangedEvent( in args );
			}
		}

		/*
		===============
		OnGamepadAxisEventTriggered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnGamepadAxisEventTriggered( in GamepadAxisEventArgs args )
		{
			if ( _processEvents ) {
				PushGamepadAxisEvent( in args );
			}
		}

		/*
		===============
		OnGamepadButtonEventTriggered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnGamepadButtonEventTriggered( in GamepadButtonEventArgs args )
		{
			if ( _processEvents ) {
				PushGamepadButtonEvent( in args );
			}
		}

		/*
		===============
		OnPauseStateChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnPauseStateChanged( in PauseStateChangedEventArgs args )
		{
			_processEvents = !args.IsPaused;
		}
	}
}
