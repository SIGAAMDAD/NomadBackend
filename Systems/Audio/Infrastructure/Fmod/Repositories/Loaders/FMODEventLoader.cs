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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Services;
using NomadCore.Systems.ResourceCache.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod {
	/*
	===================================================================================
	
	FMODEventLoader
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODEventLoader( FMODSystemService fmodSystem, FMODGuidRepository guidRepository, ILoggerService logger ) : IResourceLoader<IEventResource, EventId> {
		public IResourceLoader<IEventResource, EventId>.LoadCallback Load => LoadEvent;
		public IResourceLoader<IEventResource, EventId>.LoadAsyncCallback LoadAsync => LoadEventAsync;

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
		private Result<IEventResource> LoadEvent( EventId id ) {
			try {
				var guid = guidRepository.GetEventGuid( id );
				FMODValidator.ValidateCall( fmodSystem.StudioSystem.getEventByID( guid.Value, out var eventDescription ) );
				logger.PrintLine( $"FMODEventLoader.LoadEvent: loaded event '{id.Name}'" );

				return Result<IEventResource>.Success( new FMODEventResource( eventDescription ) );
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
		private async Task<Result<IEventResource>> LoadEventAsync( EventId id, CancellationToken ct = default ) {
			try {
				ct.ThrowIfCancellationRequested();

				var guid = guidRepository.GetEventGuid( id );

				FMODValidator.ValidateCall( fmodSystem.StudioSystem.getEventByID( guid.Value, out var eventDescription ) );
				
				logger.PrintLine( $"FMODEventLoader.LoadEventAsync: loaded event '{id.Name}'" );

				return Result<IEventResource>.Success( new FMODEventResource( eventDescription ) );
			} catch ( FMODException e ) {
				logger.PrintError( $"FMODEventLoader.LoadEventAsync: failed to load event '{id.Name}' - {e.Error}" );
				throw;
			}
		}
	};
};