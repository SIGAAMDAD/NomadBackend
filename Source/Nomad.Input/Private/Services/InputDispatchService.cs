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
using Nomad.Core.Events;
using Nomad.Core.Util;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================

	InputDispatchService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class InputDispatchService
	{
		private IGameEvent<ButtonActionEventArgs>?[] _buttonEvents = Array.Empty<IGameEvent<ButtonActionEventArgs>?>();
		private IGameEvent<FloatActionEventArgs>?[] _floatEvents = Array.Empty<IGameEvent<FloatActionEventArgs>?>();
		private IGameEvent<AxisActionEventArgs>?[] _axisEvents = Array.Empty<IGameEvent<AxisActionEventArgs>?>();
		private readonly IGameEventRegistryService _eventFactory;

		public InputDispatchService( IGameEventRegistryService eventFactory )
		{
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
		}

		public void Dispatch( CompiledBindingGraph graph, in ResolvedAction action )
		{
			EnsureCapacity( graph.Actions.Length );

			int slot = action.ActionIndex;
			InternString actionId = graph.Actions[slot].ActionId;

			switch ( action.ValueType ) {
				case InputValueType.Button:
					GetButtonEvent( slot, actionId ).Publish(
						new ButtonActionEventArgs( actionId, action.Phase, action.ButtonValue, action.TimeStamp, action.DeviceSlot, action.LocalSlot )
					);
					break;
				case InputValueType.Float:
					GetFloatEvent( slot, actionId ).Publish(
						new FloatActionEventArgs( actionId, action.Phase, action.FloatValue, action.TimeStamp, action.DeviceSlot, action.LocalSlot )
					);
					break;
				case InputValueType.Vector2:
					GetAxisEvent( slot, actionId ).Publish(
						new AxisActionEventArgs( actionId, action.Phase, action.Vector2Value, action.TimeStamp, action.DeviceSlot, action.LocalSlot )
					);
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof( action ) );
			}
		}

		/*
		===============
		GetButtonEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="actionId"></param>
		/// <returns></returns>
		private IGameEvent<ButtonActionEventArgs> GetButtonEvent( int slot, InternString actionId )
		{
			var gameEvent = _buttonEvents[slot];
			if ( gameEvent != null ) {
				return gameEvent;
			}

			gameEvent = _eventFactory.GetEvent<ButtonActionEventArgs>(
				string.Concat( (string)actionId, ":", ButtonActionEventArgs.Name ),
				ButtonActionEventArgs.NameSpace
			);

			_buttonEvents[slot] = gameEvent;
			return gameEvent;
		}

		private IGameEvent<FloatActionEventArgs> GetFloatEvent( int slot, InternString actionId )
		{
			var gameEvent = _floatEvents[slot];
			if ( gameEvent != null ) {
				return gameEvent;
			}

			gameEvent = _eventFactory.GetEvent<FloatActionEventArgs>(
				string.Concat( (string)actionId, ":", FloatActionEventArgs.Name ),
				FloatActionEventArgs.NameSpace
			);

			_floatEvents[slot] = gameEvent;
			return gameEvent;
		}

		private IGameEvent<AxisActionEventArgs> GetAxisEvent( int slot, InternString actionId )
		{
			var gameEvent = _axisEvents[slot];
			if ( gameEvent != null ) {
				return gameEvent;
			}

			gameEvent = _eventFactory.GetEvent<AxisActionEventArgs>(
				string.Concat( (string)actionId, ":", AxisActionEventArgs.Name ),
				AxisActionEventArgs.NameSpace
			);

			_axisEvents[slot] = gameEvent;
			return gameEvent;
		}

		private void EnsureCapacity( int actionCount )
		{
			if ( _buttonEvents.Length >= actionCount ) {
				return;
			}

			Array.Resize( ref _buttonEvents, actionCount );
			Array.Resize( ref _floatEvents, actionCount );
			Array.Resize( ref _axisEvents, actionCount );
		}
	};
};
