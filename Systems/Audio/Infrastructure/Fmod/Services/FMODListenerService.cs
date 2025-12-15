/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Exceptions;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Services {
	/*
	===================================================================================
	
	FMODListenerService
	
	===================================================================================
	*/
	/// <summary>
	/// Manages FMOD listener instances.
	/// </summary>
	
	internal sealed class FMODListenerService( ILoggerService logger, FMODSystemService system ) : IListenerService {
		private const int MAX_LISTENERS = 4;

		public int ListenerCount => _listenerCount;
		private int _listenerCount = 0;

		public IListener? ActiveListener => _currentListener;
		private IListener? _currentListener;

		private readonly FMODListener?[] _listeners = new FMODListener[ MAX_LISTENERS ];
		private readonly FMODSystemService _system = system;
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
			_system.StudioSystem.setNumListeners( 0 );
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