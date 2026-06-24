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
using System.Diagnostics.CodeAnalysis;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Input;
using Nomad.OnlineServices.Steam.ValueObjects;
using Steamworks;
using Nomad.Input.ValueObjects;
using System.Numerics;
using Nomad.Core.Util;
using System.IO;

namespace Nomad.OnlineServices.Steam.Private.Input
{
	/*
	===================================================================================

	SteamInputService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamInputService : ISteamInputService
	{
		private readonly struct DigitalStateKey : IEquatable<DigitalStateKey>
		{
			public InputHandle_t InputHandle { get; }
			public InputDigitalActionHandle_t ActionHandle { get; }

			public DigitalStateKey( InputHandle_t inputHandle, InputDigitalActionHandle_t actionHandle )
			{
				InputHandle = inputHandle;
				ActionHandle = actionHandle;
			}

			public bool Equals( DigitalStateKey other )
			{
				return InputHandle.Equals( other.InputHandle ) && ActionHandle.Equals( other.ActionHandle );
			}

			public override bool Equals( [NotNullWhen( true )] object? obj )
			{
				return obj is DigitalStateKey other && Equals( other );
			}

			public override int GetHashCode()
			{
				return HashCode.Combine( InputHandle, ActionHandle );
			}
		};

		private readonly struct AnalogStateKey : IEquatable<AnalogStateKey>
		{
			public InputHandle_t InputHandle { get; }
			public InputAnalogActionHandle_t ActionHandle { get; }

			public AnalogStateKey( InputHandle_t inputHandle, InputAnalogActionHandle_t actionHandle )
			{
				InputHandle = inputHandle;
				ActionHandle = actionHandle;
			}

			public bool Equals( AnalogStateKey other )
			{
				return InputHandle.Equals( other.InputHandle ) && ActionHandle.Equals( other.ActionHandle );
			}

			public override bool Equals( object? obj )
			{
				return obj is AnalogStateKey other && Equals( other );
			}

			public override int GetHashCode()
			{
				return HashCode.Combine( InputHandle, ActionHandle );
			}
		};

		public bool IsInitialized => _isInitialized;
		public string CurrentActionSet => _currentActionSet;

		private readonly SteamInputConfiguration _configuration;
		private readonly IGameEventRegistryService _eventFactory;
		private readonly ILoggerCategory _category;

		private readonly Dictionary<string, InputActionSetHandle_t> _actionSetHandles = new( StringComparer.Ordinal );
		private readonly Dictionary<string, InputDigitalActionHandle_t> _digitalActionHandles = new( StringComparer.Ordinal );
		private readonly Dictionary<string, InputAnalogActionHandle_t> _analogActionHandles = new( StringComparer.Ordinal );

		private readonly Dictionary<string, IGameEvent<ButtonActionEventArgs>> _buttonEvents = new( StringComparer.Ordinal );
		private readonly Dictionary<string, IGameEvent<FloatActionEventArgs>> _floatEvents = new( StringComparer.Ordinal );
		private readonly Dictionary<string, IGameEvent<AxisActionEventArgs>> _axisEvents = new( StringComparer.Ordinal );

		private readonly Dictionary<DigitalStateKey, bool> _digitalStates = new();
		private readonly Dictionary<AnalogStateKey, float> _floatStates = new();
		private readonly Dictionary<AnalogStateKey, Vector2> _axisStates = new();

		private readonly Dictionary<InputHandle_t, InputDeviceSlot> _slotByHandle = new();
		private readonly Dictionary<InputDeviceSlot, InputHandle_t> _handleBySlot = new();

		private readonly InputHandle_t[] _connectedHandles = new InputHandle_t[Steamworks.Constants.STEAM_INPUT_MAX_COUNT];
		private readonly EInputActionOrigin[] _origins = new EInputActionOrigin[Steamworks.Constants.STEAM_INPUT_MAX_COUNT];

		private string _currentActionSet = string.Empty;
		private string[] _currentLayers = Array.Empty<string>();

		private bool _isInitialized = false;
		private bool _isDisposed = false;

		/*
		===============
		SteamInputService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		/// <param name="configuration"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamInputService( ILoggerService logger, IGameEventRegistryService eventFactory, SteamInputConfiguration configuration )
		{
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
			_configuration = configuration ?? throw new ArgumentNullException( nameof( configuration ) );
			_category = (logger ?? throw new ArgumentNullException( nameof( logger ) ))
				.CreateCategory( nameof( SteamInputService ), LogLevel.Info, true );

			_currentActionSet = _configuration.DefaultActionSet;

			Initialize();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			if ( _isInitialized ) {
				SteamInput.Shutdown();
			}

			_category.Dispose();
			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		SetActionSet
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="actionSetName"></param>
		/// <exception cref="ArgumentException"></exception>
		public void SetActionSet( string actionSetName )
		{
			if ( string.IsNullOrWhiteSpace( actionSetName ) ) {
				throw new ArgumentException( "Action set name must not be null or whitespace.", nameof( actionSetName ) );
			}

			_currentActionSet = actionSetName;
			ResolveActionSetHandle( actionSetName );
		}

		/*
		===============
		SetActionLayers
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="layerNames"></param>
		public void SetActionLayers( IReadOnlyList<string> layerNames )
		{
			if ( layerNames == null || layerNames.Count == 0 ) {
				_currentLayers = Array.Empty<string>();
				return;
			}

			var copy = new string[layerNames.Count];
			for ( int i = 0; i < layerNames.Count; i++ ) {
				copy[i] = layerNames[i] ?? string.Empty;
				if ( !string.IsNullOrWhiteSpace( copy[i] ) ) {
					ResolveActionSetHandle( copy[i] );
				}
			}

			_currentLayers = copy;
		}

		/*
		===============
		Frame
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="timeStamp"></param>
		public void Frame( long timeStamp )
		{
			if ( !_isInitialized || _isDisposed ) {
				return;
			}

			if ( _configuration.ExplicitRunFrame ) {
				SteamInput.RunFrame();
			}

			int connectedCount = SteamInput.GetConnectedControllers( _connectedHandles );
			RemapConnectedHandles( connectedCount );

			for ( int i = 0; i < connectedCount && i < 4; i++ ) {
				InputHandle_t inputHandle = _connectedHandles[i];

				ApplyCurrentActionContext( inputHandle );
				PollDigitalBindings( inputHandle, timeStamp );
				PollFloatBindings( inputHandle, timeStamp );
				PollAxisBindings( inputHandle, timeStamp );
			}
		}

		/*
		===============
		ShowBindingPanel
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public bool ShowBindingPanel( InputDeviceSlot slot )
		{
			return TryGetHandle( slot, out InputHandle_t inputHandle ) && SteamInput.ShowBindingPanel( inputHandle );
		}

		/*
		===============
		TryGetGlyphForAction
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="actionId"></param>
		/// <param name="slot"></param>
		/// <param name="glyphPath"></param>
		/// <param name="displayName"></param>
		/// <returns></returns>
		public bool TryGetGlyphForAction( InternString actionId, InputDeviceSlot slot, out string glyphPath, out string displayName )
		{
			glyphPath = string.Empty;
			displayName = string.Empty;

			if ( !TryGetHandle( slot, out InputHandle_t inputHandle ) ) {
				return false;
			}

			InputActionSetHandle_t actionSetHandle = ResolveActionSetHandle( _currentActionSet );

			for ( int i = 0; i < _configuration.DigitalBindings.Count; i++ ) {
				SteamInputDigitalBinding binding = _configuration.DigitalBindings[i];
				if ( (string)binding.ActionId != (string)actionId ) {
					continue;
				}

				InputDigitalActionHandle_t actionHandle = ResolveDigitalActionHandle( binding.SteamActionName );
				int count = SteamInput.GetDigitalActionOrigins( inputHandle, actionSetHandle, actionHandle, _origins );
				if ( count <= 0 ) {
					return false;
				}

				EInputActionOrigin origin = _origins[0];
				glyphPath = SteamInput.GetGlyphForActionOrigin_Legacy( origin );
				displayName = SteamInput.GetStringForActionOrigin( origin );
				return !string.IsNullOrWhiteSpace( glyphPath );
			}

			for ( int i = 0; i < _configuration.AxisBindings.Count; i++ ) {
				SteamInputAxisBinding binding = _configuration.AxisBindings[i];
				if ( (string)binding.ActionId != (string)actionId ) {
					continue;
				}

				InputAnalogActionHandle_t actionHandle = ResolveAnalogActionHandle( binding.SteamActionName );
				int count = SteamInput.GetAnalogActionOrigins( inputHandle, actionSetHandle, actionHandle, _origins );
				if ( count <= 0 ) {
					return false;
				}

				EInputActionOrigin origin = _origins[0];
				glyphPath = SteamInput.GetGlyphForActionOrigin_Legacy( origin );
				displayName = SteamInput.GetStringForActionOrigin( origin );
				return !string.IsNullOrWhiteSpace( glyphPath );
			}

			for ( int i = 0; i < _configuration.FloatBindings.Count; i++ ) {
				SteamInputFloatBinding binding = _configuration.FloatBindings[i];
				if ( (string)binding.ActionId != (string)actionId ) {
					continue;
				}

				InputAnalogActionHandle_t actionHandle = ResolveAnalogActionHandle( binding.SteamActionName );
				int count = SteamInput.GetAnalogActionOrigins( inputHandle, actionSetHandle, actionHandle, _origins );
				if ( count <= 0 ) {
					return false;
				}

				EInputActionOrigin origin = _origins[0];
				glyphPath = SteamInput.GetGlyphForActionOrigin_Legacy( origin );
				displayName = SteamInput.GetStringForActionOrigin( origin );
				return !string.IsNullOrWhiteSpace( glyphPath );
			}

			return false;
		}

		/*
		===============
		Initialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void Initialize()
		{
			string manifestPath = _configuration.ManifestPath;
			if ( !string.IsNullOrWhiteSpace( manifestPath ) ) {
				string fullPath = Path.GetFullPath( manifestPath );
				if ( File.Exists( fullPath ) ) {
					if ( !SteamInput.SetInputActionManifestFilePath( fullPath ) ) {
						_category.PrintWarning( $"SteamInput: failed to set input manifest path '{fullPath}'." );
					}
				} else {
					_category.PrintWarning( $"SteamInput: manifest not found at '{fullPath}'." );
				}
			}

			_isInitialized = SteamInput.Init( _configuration.ExplicitRunFrame );
			if ( !_isInitialized ) {
				_category.PrintError( "SteamInput: SteamInput.Init failed." );
				return;
			}

			ResolveActionSetHandle( _currentActionSet );

			for ( int i = 0; i < _configuration.DigitalBindings.Count; i++ ) {
				ResolveDigitalActionHandle( _configuration.DigitalBindings[i].SteamActionName );
			}

			for ( int i = 0; i < _configuration.FloatBindings.Count; i++ ) {
				ResolveAnalogActionHandle( _configuration.FloatBindings[i].SteamActionName );
			}

			for ( int i = 0; i < _configuration.AxisBindings.Count; i++ ) {
				ResolveAnalogActionHandle( _configuration.AxisBindings[i].SteamActionName );
			}

			_category.PrintLine( "SteamInput initialized." );
		}

		/*
		===============
		ApplyCurrentActionContext
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="inputHandle"></param>
		private void ApplyCurrentActionContext( InputHandle_t inputHandle )
		{
			InputActionSetHandle_t actionSetHandle = ResolveActionSetHandle( _currentActionSet );
			SteamInput.ActivateActionSet( inputHandle, actionSetHandle );

			SteamInput.DeactivateAllActionSetLayers( inputHandle );
			for ( int i = 0; i < _currentLayers.Length; i++ ) {
				string layer = _currentLayers[i];
				if ( string.IsNullOrWhiteSpace( layer ) ) {
					continue;
				}

				SteamInput.ActivateActionSetLayer( inputHandle, ResolveActionSetHandle( layer ) );
			}
		}

		private void PollDigitalBindings( InputHandle_t inputHandle, long timeStamp )
		{
			for ( int i = 0; i < _configuration.DigitalBindings.Count; i++ ) {
				SteamInputDigitalBinding binding = _configuration.DigitalBindings[i];
				InputDigitalActionHandle_t actionHandle = ResolveDigitalActionHandle( binding.SteamActionName );
				InputDigitalActionData_t data = SteamInput.GetDigitalActionData( inputHandle, actionHandle );

				bool current = data.bActive == 1 && data.bState == 1;
				var stateKey = new DigitalStateKey( inputHandle, actionHandle );
				bool previous = _digitalStates.TryGetValue( stateKey, out bool wasPressed ) && wasPressed;

				if ( !current && !previous ) {
					continue;
				}

				InputActionPhase phase = current
					? previous ? InputActionPhase.Performed : InputActionPhase.Started
					: InputActionPhase.Canceled;

				PublishButton( inputHandle, binding.ActionId, phase, current, timeStamp );

				if ( current ) {
					_digitalStates[stateKey] = true;
				} else {
					_digitalStates.Remove( stateKey );
				}
			}
		}

		private void PollFloatBindings( InputHandle_t inputHandle, long timeStamp )
		{
			for ( int i = 0; i < _configuration.FloatBindings.Count; i++ ) {
				SteamInputFloatBinding binding = _configuration.FloatBindings[i];
				InputAnalogActionHandle_t actionHandle = ResolveAnalogActionHandle( binding.SteamActionName );
				InputAnalogActionData_t data = SteamInput.GetAnalogActionData( inputHandle, actionHandle );

				float currentValue = 0.0f;
				if ( data.bActive == 1 ) {
					currentValue = binding.Channel == SteamFloatSourceChannel.X ? data.x : data.y;
				}

				var stateKey = new AnalogStateKey( inputHandle, actionHandle );
				bool previousActive = _floatStates.TryGetValue( stateKey, out float previousValue ) && MathF.Abs( previousValue ) >= binding.DeadZone;
				bool currentActive = MathF.Abs( currentValue ) >= binding.DeadZone;

				if ( !currentActive && !previousActive ) {
					continue;
				}

				InputActionPhase phase = currentActive
					? previousActive ? InputActionPhase.Performed : InputActionPhase.Started
					: InputActionPhase.Canceled;

				PublishFloat( inputHandle, binding.ActionId, phase, currentActive ? currentValue : 0.0f, timeStamp );

				if ( currentActive ) {
					_floatStates[stateKey] = currentValue;
				} else {
					_floatStates.Remove( stateKey );
				}
			}
		}

		private void PollAxisBindings( InputHandle_t inputHandle, long timeStamp )
		{
			for ( int i = 0; i < _configuration.AxisBindings.Count; i++ ) {
				SteamInputAxisBinding binding = _configuration.AxisBindings[i];
				InputAnalogActionHandle_t actionHandle = ResolveAnalogActionHandle( binding.SteamActionName );
				InputAnalogActionData_t data = SteamInput.GetAnalogActionData( inputHandle, actionHandle );

				Vector2 currentValue = data.bActive == 1 ? new Vector2( data.x, data.y ) : Vector2.Zero;

				var stateKey = new AnalogStateKey( inputHandle, actionHandle );
				bool previousActive = _axisStates.TryGetValue( stateKey, out Vector2 previousValue ) && previousValue.LengthSquared() >= binding.DeadZone * binding.DeadZone;
				bool currentActive = currentValue.LengthSquared() >= binding.DeadZone * binding.DeadZone;

				if ( !currentActive && !previousActive ) {
					continue;
				}

				InputActionPhase phase = currentActive
					? previousActive ? InputActionPhase.Performed : InputActionPhase.Started
					: InputActionPhase.Canceled;

				PublishAxis( inputHandle, binding.ActionId, phase, currentActive ? currentValue : Vector2.Zero, timeStamp );

				if ( currentActive ) {
					_axisStates[stateKey] = currentValue;
				} else {
					_axisStates.Remove( stateKey );
				}
			}
		}

		private void RemapConnectedHandles( int connectedCount )
		{
			_slotByHandle.Clear();
			_handleBySlot.Clear();

			int maxSlots = Math.Min( connectedCount, 4 );
			for ( int i = 0; i < maxSlots; i++ ) {
				InputHandle_t handle = _connectedHandles[i];
				InputDeviceSlot slot = IndexToSlot( i );

				_slotByHandle[handle] = slot;
				_handleBySlot[slot] = handle;
			}
		}

		private static InputDeviceSlot IndexToSlot( int index )
		{
			return index switch {
				0 => InputDeviceSlot.Gamepad0,
				1 => InputDeviceSlot.Gamepad1,
				2 => InputDeviceSlot.Gamepad2,
				3 => InputDeviceSlot.Gamepad3,
				_ => throw new ArgumentOutOfRangeException( nameof( index ) )
			};
		}

		private bool TryGetHandle( InputDeviceSlot slot, out InputHandle_t inputHandle )
		{
			return _handleBySlot.TryGetValue( slot, out inputHandle );
		}

		private InputActionSetHandle_t ResolveActionSetHandle( string actionSetName )
		{
			if ( _actionSetHandles.TryGetValue( actionSetName, out InputActionSetHandle_t handle ) ) {
				return handle;
			}

			handle = SteamInput.GetActionSetHandle( actionSetName );
			_actionSetHandles[actionSetName] = handle;
			return handle;
		}

		private InputDigitalActionHandle_t ResolveDigitalActionHandle( string actionName )
		{
			if ( _digitalActionHandles.TryGetValue( actionName, out InputDigitalActionHandle_t handle ) ) {
				return handle;
			}

			handle = SteamInput.GetDigitalActionHandle( actionName );
			_digitalActionHandles[actionName] = handle;
			return handle;
		}

		private InputAnalogActionHandle_t ResolveAnalogActionHandle( string actionName )
		{
			if ( _analogActionHandles.TryGetValue( actionName, out InputAnalogActionHandle_t handle ) ) {
				return handle;
			}

			handle = SteamInput.GetAnalogActionHandle( actionName );
			_analogActionHandles[actionName] = handle;
			return handle;
		}

		private void PublishButton( InputHandle_t inputHandle, InternString actionId, InputActionPhase phase, bool value, long timeStamp )
		{
			InputDeviceSlot deviceSlot = ResolveDeviceSlot( inputHandle );
			GetButtonEvent( actionId ).Publish( new ButtonActionEventArgs( actionId, phase, value, timeStamp, deviceSlot, ResolveDefaultLocalSlot( deviceSlot ) ) );
		}

		private void PublishFloat( InputHandle_t inputHandle, InternString actionId, InputActionPhase phase, float value, long timeStamp )
		{
			InputDeviceSlot deviceSlot = ResolveDeviceSlot( inputHandle );
			GetFloatEvent( actionId ).Publish( new FloatActionEventArgs( actionId, phase, value, timeStamp, deviceSlot, ResolveDefaultLocalSlot( deviceSlot ) ) );
		}

		private void PublishAxis( InputHandle_t inputHandle, InternString actionId, InputActionPhase phase, Vector2 value, long timeStamp )
		{
			InputDeviceSlot deviceSlot = ResolveDeviceSlot( inputHandle );
			GetAxisEvent( actionId ).Publish( new AxisActionEventArgs( actionId, phase, value, timeStamp, deviceSlot, ResolveDefaultLocalSlot( deviceSlot ) ) );
		}

		private InputDeviceSlot ResolveDeviceSlot( InputHandle_t inputHandle )
		{
			return _slotByHandle.TryGetValue( inputHandle, out InputDeviceSlot deviceSlot )
				? deviceSlot
				: InputDeviceSlot.Gamepad0;
		}

		private static int ResolveDefaultLocalSlot( InputDeviceSlot deviceSlot )
		{
			return deviceSlot switch {
				InputDeviceSlot.Gamepad0 => 0,
				InputDeviceSlot.Gamepad1 => 1,
				InputDeviceSlot.Gamepad2 => 2,
				InputDeviceSlot.Gamepad3 => 3,
				_ => 0
			};
		}

		private IGameEvent<ButtonActionEventArgs> GetButtonEvent( InternString actionId )
		{
			string key = (string)actionId;
			if ( _buttonEvents.TryGetValue( key, out IGameEvent<ButtonActionEventArgs>? gameEvent ) ) {
				return gameEvent;
			}

			gameEvent = _eventFactory.GetEvent<ButtonActionEventArgs>(
				string.Concat( key, ":", ButtonActionEventArgs.Name ),
				ButtonActionEventArgs.NameSpace
			);

			_buttonEvents.Add( key, gameEvent );
			return gameEvent;
		}

		private IGameEvent<FloatActionEventArgs> GetFloatEvent( InternString actionId )
		{
			string key = (string)actionId;
			if ( _floatEvents.TryGetValue( key, out IGameEvent<FloatActionEventArgs>? gameEvent ) ) {
				return gameEvent;
			}

			gameEvent = _eventFactory.GetEvent<FloatActionEventArgs>(
				string.Concat( key, ":", FloatActionEventArgs.Name ),
				FloatActionEventArgs.NameSpace
			);

			_floatEvents.Add( key, gameEvent );
			return gameEvent;
		}

		private IGameEvent<AxisActionEventArgs> GetAxisEvent( InternString actionId )
		{
			string key = (string)actionId;
			if ( _axisEvents.TryGetValue( key, out IGameEvent<AxisActionEventArgs>? gameEvent ) ) {
				return gameEvent;
			}

			gameEvent = _eventFactory.GetEvent<AxisActionEventArgs>(
				string.Concat( key, ":", AxisActionEventArgs.Name ),
				AxisActionEventArgs.NameSpace
			);

			_axisEvents.Add( key, gameEvent );
			return gameEvent;
		}
	};
};
