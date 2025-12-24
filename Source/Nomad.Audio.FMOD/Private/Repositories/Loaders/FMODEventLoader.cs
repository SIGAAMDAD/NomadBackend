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

using System.Threading;
using System.Threading.Tasks;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Repositories.Loaders {
	/*
	===================================================================================

	FMODEventLoader

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODEventLoader( FMODDevice fmodSystem, FMODGuidRepository guidRepository, ILoggerService logger ) : IResourceLoader<FMODEventResource, EventId> {
		public LoadCallback<FMODEventResource, EventId> Load => LoadEvent;
		public LoadAsyncCallback<FMODEventResource, EventId> LoadAsync => LoadEventAsync;

		/*
		===============
		LoadEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private Result<FMODEventResource> LoadEvent( EventId id ) {
			try {
				var guid = guidRepository.GetEventGuid( id );
				FMODValidator.ValidateCall( fmodSystem.StudioSystem.getEventByID( guid.Value, out var eventDescription ) );
				logger.PrintLine( $"FMODEventLoader.LoadEvent: loaded event '{id.Name}'" );

				return Result<FMODEventResource>.Success( new FMODEventResource( eventDescription ) );
			} catch ( FMODException e ) {
				logger.PrintError( $"FMODEventLoader.LoadEvent: failed to load event '{id.Name}' - {e.Error}" );
				throw;
			}
		}

		/*
		===============
		LoadEventAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<Result<FMODEventResource>> LoadEventAsync( EventId id, CancellationToken ct = default ) {
			try {
				ct.ThrowIfCancellationRequested();

				var guid = guidRepository.GetEventGuid( id );

				FMODValidator.ValidateCall( fmodSystem.StudioSystem.getEventByID( guid.Value, out var eventDescription ) );

				logger.PrintLine( $"FMODEventLoader.LoadEventAsync: loaded event '{id.Name}'" );

				return Result<FMODEventResource>.Success( new FMODEventResource( eventDescription ) );
			} catch ( FMODException e ) {
				logger.PrintError( $"FMODEventLoader.LoadEventAsync: failed to load event '{id.Name}' - {e.Error}" );
				throw;
			}
		}
	};
};
