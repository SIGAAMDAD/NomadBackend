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
	
	internal sealed class FMODEventLoader( FMODSystemService fmodSystem, FMODGuidRepository guidRepository ) : IResourceLoader<FMODEventResource, FMODEventId> {
		public IResourceLoader<FMODEventResource, FMODEventId>.LoadCallback Load => LoadEvent;
		public IResourceLoader<FMODEventResource, FMODEventId>.LoadAsyncCallback LoadAsync => LoadEventAsync;

		private readonly FMODSystemService _fmodSystem = fmodSystem;
		private readonly FMODGuidRepository _guidRepository = guidRepository;
		
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
		private Result<FMODEventResource> LoadEvent( FMODEventId id ) {
			FMODValidator.ValidateCall( _fmodSystem.StudioSystem.getEventByID( id.Value, out var eventDescription ) );
			FMODValidator.ValidateCall( eventDescription.getPath( out var path ) );

			_guidRepository.AddEventId( path, id );

			return Result<FMODEventResource>.Success( new FMODEventResource( eventDescription ) );
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
		private async Task<Result<FMODEventResource>> LoadEventAsync( FMODEventId id, CancellationToken ct = default ) {
			ct.ThrowIfCancellationRequested();

			FMODValidator.ValidateCall( _fmodSystem.StudioSystem.getEventByID( id.Value, out var eventDescription ) );
			FMODValidator.ValidateCall( eventDescription.getPath( out var path ) );

			_guidRepository.AddEventId( path, id );

			return Result<FMODEventResource>.Success( new FMODEventResource( eventDescription ) );
		}
	};
};