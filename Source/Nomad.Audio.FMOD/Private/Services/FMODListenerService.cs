/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Interfaces;
using Nomad.Core.Logger;
using Godot;
using System;
using System.Collections.Generic;

namespace Nomad.Audio.Fmod.Private.Services {
	/*
	===================================================================================

	FMODListenerService

	===================================================================================
	*/
	/// <summary>
	/// Manages FMOD listener instances.
	/// </summary>

	internal sealed class FMODListenerService : IListenerService {
		private const int MAX_LISTENERS = 4;

		public int ListenerCount => _listenerCount;
		private int _listenerCount = 0;

		public Vector2 ActiveListener => _currentListener.Position;
		private FMODListener _currentListener;

		private readonly FMODListener?[] _listeners = new FMODListener[ MAX_LISTENERS ];
		private readonly FMODDevice _system;
		private readonly ILoggerService _logger;

		/*
		===============
		FMODListenerService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="system"></param>
		public FMODListenerService( ILoggerService logger, FMODDevice system ) {
			_system = system;
			_logger = logger;

			CreateDefaultListener();
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			ClearListeners();
		}

		/*
		===============
		SetListenerPosition
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="listenerIndex"></param>
		/// <param name="position"></param>
		public void SetListenerPosition( int listenerIndex, Vector2 position ) {
			ArgumentOutOfRangeException.ThrowIfLessThan( listenerIndex, 0, nameof( listenerIndex ) );
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( listenerIndex, MAX_LISTENERS, nameof( listenerIndex ) );

			if ( listenerIndex > _listenerCount ) {
				FMODValidator.ValidateCall( _system.StudioSystem.setNumListeners( listenerIndex ) );
				_listeners[ listenerIndex ] = new FMODListener( _system.StudioSystem, listenerIndex ) { Position = position };
			}
			_listeners[ listenerIndex ].Position = position;
		}

		/*
		===============
		ClearListeners
		===============
		*/
		/// <summary>
		/// Removes all active listeners from FMOD.
		/// </summary>
		public void ClearListeners() {
			_logger.PrintLine( $"FMODListenerService.ClearListeners: cleaning up listener data..." );

			for ( int i = 0; i < _listenerCount; i++ ) {
				_listeners[ i ] = null;
			}
			if ( _listenerCount > 0 ) {
				_system.StudioSystem.setNumListeners( 0 );
				_listenerCount = 0;
			}
			CreateDefaultListener();
		}

		/*
		===============
		CreateDefaultListener
		===============
		*/
		/// <summary>
		/// Creates the default listener.
		/// </summary>
		private void CreateDefaultListener() {
			_currentListener = new FMODListener( _system.StudioSystem, 0 ) { Position = Vector2.Zero };
			_listeners[ 0 ] = _currentListener;
			_listenerCount++;

			_system.StudioSystem.setNumListeners( 1 );
		}
	};
};
