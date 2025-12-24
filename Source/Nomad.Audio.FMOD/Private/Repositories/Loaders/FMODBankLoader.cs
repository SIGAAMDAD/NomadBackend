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

using System;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Audio.Entities;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.ValueObjects;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Repositories.Loaders {
	/*
	===================================================================================

	FMODBankLoader

	===================================================================================
	*/
	/// <summary>
	/// Loads FMOD banks from disk.
	/// </summary>

	internal sealed class FMODBankLoader( FMODDevice fmodSystem, FMODGuidRepository guidRepository, ILoggerService logger ) : IResourceLoader<FMODBankResource, string> {
		public LoadCallback<FMODBankResource, string> Load => LoadBank;
		public LoadAsyncCallback<FMODBankResource, string> LoadAsync => LoadBankAsync;

		/*
		===============
		LoadBank
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private Result<FMODBankResource> LoadBank( string path ) {
			try {
				FMODValidator.ValidateCall( fmodSystem.StudioSystem.loadBankFile( FilePath.FromResourcePath( path ).OSPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank ) );
				FMODValidator.ValidateCall( bank.getID( out var guid ) );
				logger.PrintLine( $"FMODBankLoader.LoadBank: loaded bank '{path}'" );
				guidRepository.AddBankId( path, new( guid ) );

				return Result<FMODBankResource>.Success( new( bank ) );
			} catch ( FMODException e ) {
				logger.PrintError( $"FMODBankLoader.LoadBank: failed to load bank '{path}' - {e.Error}" );
				throw;
			}
		}

		/*
		===============
		LoadBankAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<Result<FMODBankResource>> LoadBankAsync( string path, CancellationToken ct = default ) {
			try {
				ct.ThrowIfCancellationRequested();

				FMODValidator.ValidateCall( fmodSystem.StudioSystem.loadBankFile( FilePath.FromResourcePath( path ).OSPath, FMOD.Studio.LOAD_BANK_FLAGS.NONBLOCKING, out var bank ) );
				FMODValidator.ValidateCall( bank.getID( out var guid ) );
				logger.PrintLine( $"FMODBankLoader.LoadBankAsync: loaded bank '{path}'" );
				guidRepository.AddBankId( path, new( guid ) );

				return Result<FMODBankResource>.Success( new( bank ) );
			} catch ( FMODException e ) {
				logger.PrintError( $"FMODBankLoader.LoadBank: failed to load bank '{path}' - {e.Error}" );
				throw;
			}
		}
	};
};
