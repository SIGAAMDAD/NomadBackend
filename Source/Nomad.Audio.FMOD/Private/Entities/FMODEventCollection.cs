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

using System.Collections.Generic;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Interfaces;
using Nomad.Core.Logger;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================

	FMODEventCollection

	===================================================================================
	*/
	/// <summary>
	/// Contains a collection of <see cref="FMODEvent"/> objects, most usually for a bank.
	/// </summary>

	internal record FMODEventCollection : IEventCollection
	{
		public int EventCount => _events.Length;

		private readonly FMOD.Studio.EventDescription[] _events;
		private readonly List<string> _eventIds;

		/*
		===============
		FMODEventCollection
		===============
		*/
		/// <summary>
		/// Creates an FMODEventCollection
		/// </summary>
		/// <param name="events"></param>
		/// <param name="guidRepository"></param>
		/// <param name="logger"></param>
		public FMODEventCollection( FMOD.Studio.EventDescription[] events, FMODGuidRepository guidRepository, ILoggerService logger )
		{
			_events = events;
			_eventIds = new List<string>( _events.Length );
			for ( int i = 0; i < _events.Length; i++ ) {
				FMODValidator.ValidateCall( _events[i].getID( out var guid ) );
				FMODValidator.ValidateCall( _events[i].getPath( out var path ) );
				guidRepository.AddEventId( path, guid );
			}
		}

		/*
		===============
		ContainsEvent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventId"></param>
		/// <returns></returns>
		public bool ContainsEvent( string eventId )
		{
			return _eventIds.Contains( eventId );
		}

		/*
		===============
		GetEventIds
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<string> GetEventIds()
		{
			return _eventIds;
		}
	};
};
