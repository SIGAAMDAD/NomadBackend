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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Core.Logger;
using Nomad.Core.ResourceCache;
using Nomad.Core.Util;

namespace Nomad.Audio.Fmod.Private.Repositories.Loaders
{
	/*
	===================================================================================

	FMODBankLoader

	===================================================================================
	*/
	/// <summary>
	/// Loads FMOD banks from disk.
	/// </summary>

	internal sealed class FMODBankLoader : IResourceLoader
	{
		private readonly FMODDevice _fmodSystem;
		private readonly FMODGuidRepository _guidRepository;
		private readonly ILoggerCategory _category;

		/*
		===============
		FMODBankLoader
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fmodSystem"></param>
		/// <param name="guidRepository"></param>
		/// <param name="category"></param>
		public FMODBankLoader( FMODDevice fmodSystem, FMODGuidRepository guidRepository, ILoggerCategory category )
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
				FMODValidator.ValidateCall( _fmodSystem.StudioSystem.loadBankFile( path, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank ) );
				FMODValidator.ValidateCall( bank.getID( out var guid ) );
				_category.PrintLine( $"FMODBankLoader.LoadBank: loaded bank '{path}'" );
				_guidRepository.AddBankId( path, guid );

				var resource = new FMODBankResource( bank );
				if ( resource is TResource result ) {
					return Result<TResource>.Success( result );
				} else {
					throw new InvalidCastException();
				}
			} catch ( FMODException e ) {
				_category.PrintError( $"FMODBankLoader.LoadBank: failed to load bank '{path}' - {e.Error}" );
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

				FMODValidator.ValidateCall( _fmodSystem.StudioSystem.loadBankFile( path, FMOD.Studio.LOAD_BANK_FLAGS.NONBLOCKING, out var bank ) );
				FMODValidator.ValidateCall( bank.getID( out var guid ) );
				_category.PrintLine( $"FMODBankLoader.LoadBankAsync: loaded bank '{path}'" );
				_guidRepository.AddBankId( path, guid );

				var resource = new FMODBankResource( bank );
				if ( resource is TResource result ) {
					return Result<TResource>.Success( result );
				} else {
					throw new InvalidCastException();
				}
			} catch ( FMODException e ) {
				_category.PrintError( $"FMODBankLoader.LoadBank: failed to load bank '{path}' - {e.Error}" );
				throw;
			}
		}
	};
};
