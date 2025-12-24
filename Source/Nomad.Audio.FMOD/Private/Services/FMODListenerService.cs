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
using Nomad.Audio.Fmod.Private.Exceptions;
using Nomad.Audio.Interfaces;
using Nomad.Core.Logger;

namespace Nomad.Audio.Fmod.Private.Services {
	/*
	===================================================================================

	FMODListenerService

	===================================================================================
	*/
	/// <summary>
	/// Manages FMOD listener instances.
	/// </summary>

	internal sealed class FMODListenerService( ILoggerService logger, FMODDevice system ) : IListenerService {
		private const int MAX_LISTENERS = 4;

		public int ListenerCount => _listenerCount;
		private int _listenerCount = 0;

		public IListener? ActiveListener => _currentListener;
		private IListener? _currentListener;

		private readonly FMODListener?[] _listeners = new FMODListener[ MAX_LISTENERS ];
		private readonly FMODDevice _system = system;
		private readonly ILoggerService _logger = logger;

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
		AddListener
		===============
		*/
		/// <summary>
		/// Allocates a new FMOD listener instance.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		/// <exception cref="TooManyListenersException"></exception>
		public IListener AddListener() {
			if ( _listenerCount >= _listeners.Length ) {
				throw new TooManyListenersException();
			}

			_logger.PrintLine( $"FMODListenerService.AddListener: allocating listener..." );

			lock ( _listeners ) {
				var listener = new FMODListener( _system.StudioSystem, _listenerCount );
				_listeners[ _listenerCount++ ] = listener;

				// assign the active listener if we haven't already
				_currentListener ??= listener;

				_system.StudioSystem.setNumListeners( _listenerCount );

				return listener;
			}
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
		}

		/*
		===============
		SetActiveListener
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="listener"></param>
		/// <returns></returns>
		public IListener SetActiveListener( IListener listener ) {
			_logger.PrintLine( $"FMODListenerService.SetActiveListener: setting new listener..." );

			_currentListener = listener;
			return listener;
		}
	};
};
