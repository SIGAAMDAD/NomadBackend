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

using System.Threading;
using System.Threading.Tasks;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Core.ResourceCache;
using System;

namespace Nomad.Audio.Fmod.Private.Repositories.Loaders
{
	/*
	===================================================================================

	FMODEventLoader

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODEventLoader : IResourceLoader
	{
		private readonly FMODDevice _fmodSystem;
		private readonly FMODGuidRepository _guidRepository;
		private readonly ILoggerCategory _category;

		/*
		===============
		FMODEventLoader
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fmodSystem"></param>
		/// <param name="guidRepository"></param>
		/// <param name="category"></param>
		public FMODEventLoader( FMODDevice fmodSystem, FMODGuidRepository guidRepository, ILoggerCategory category )
		{
			_fmodSystem = fmodSystem;
			_guidRepository = guidRepository;
			_category = category;
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Result<TResource> Load<TResource, TId>( TId id )
		{
			if ( id is not string path ) {
				throw new InvalidCastException();
			}
			try {
				FMODValidator.ValidateCall( _fmodSystem.StudioSystem.getEvent( path, out var eventDescription ) );
				FMODValidator.ValidateCall( eventDescription.getID( out var guid ) );
				_guidRepository.AddEventId( path, guid );
				_category.PrintLine( $"FMODEventLoader.LoadEvent: loaded event '{path}'" );

				var resource = new FMODEventResource( eventDescription );
				if ( resource is TResource result ) {
					return Result<TResource>.Success( result );
				} else {
					throw new InvalidCastException();
				}
			} catch ( FMODException e ) {
				_category.PrintError( $"FMODEventLoader.LoadEvent: failed to load event '{path}' - {e.Error}" );
				throw;
			}
		}

		/*
		===============
		LoadAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<Result<TResource>> LoadAsync<TResource, TId>( TId id, CancellationToken ct = default )
		{
			if ( id is not string path ) {
				throw new InvalidCastException();
			}
			try {
				ct.ThrowIfCancellationRequested();

				FMODValidator.ValidateCall( _fmodSystem.StudioSystem.getEvent( path, out var eventDescription ) );
				FMODValidator.ValidateCall( eventDescription.getID( out var guid ) );
				_guidRepository.AddEventId( path, guid );
				_category.PrintLine( $"FMODEventLoader.LoadEventAsync: loaded event '{path}'" );

				var resource = new FMODEventResource( eventDescription );
				if ( resource is TResource result ) {
					return Result<TResource>.Success( result );
				} else {
					throw new InvalidCastException();
				}
			} catch ( FMODException e ) {
				_category.PrintError( $"FMODEventLoader.LoadEventAsync: failed to load event '{path}' - {e.Error}" );
				throw;
			}
		}
	};
};
