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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Audio.ValueObjects;
using Nomad.Core;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.Repositories {
	/*
	===================================================================================

	FMODBankRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODBankRepository {
		private readonly FMODBankLoadingStrategy _loadingStrategy;
		private readonly FMOD.Studio.System _system;
		private readonly ConcurrentDictionary<BankHandle, FMODBankResource> _cache = new();
		private readonly ILoggerService _logger;

		public FMODBankRepository( ILoggerService logger, ICVarSystemService cvarSystem, FMOD.Studio.System system, FMODGuidRepository guidRepository ) {
			var bankLoadingStrategy = cvarSystem.GetCVar<FMODBankLoadingStrategy>( Constants.CVars.Audio.FMOD.BANK_LOADING_STRATEGY ) ?? throw new CVarMissing( AudioConstants.CVars.FMOD.FMOD_BANK_LOADING_STRATEGY );
			_loadingStrategy = bankLoadingStrategy.Value;

			_logger = logger;
			_system = system;

			if ( _loadingStrategy == FMODBankLoadingStrategy.Streaming ) {
			}
		}

		/*
		===============
		LoadBank
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="bankPath"></param>
		/// <param name="bank"></param>
		/// <returns></returns>
		public AudioResult LoadBank( string bankPath, out BankHandle bank ) {
			bank = new( bankPath.GetHashCode() );

			if ( _cache.TryGetValue( bank, out var resource ) ) {
				return AudioResult.Success;
			}

			try {
				FMODValidator.ValidateCall( _system.loadBankFile( FilePath.FromResourcePath( bankPath ).OSPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bankResource ) );
				_cache[ bank ] = new FMODBankResource( bankResource );
				_logger.PrintLine( $"FMODBankRepository.LoadBank: loaded bank '{bankPath}'." );
			} catch ( FMODException e ) {
				_logger.PrintError( $"FMODBankRepository.LoadBank: failed to load bank '{bankPath}' - {e.Error}\n{e}" );
				throw;
			}

			return AudioResult.Success;
		}

		/*
		===============
		UnloadBank
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="bank"></param>
		/// <returns></returns>
		public AudioResult UnloadBank( BankHandle bank ) {
			if ( !_cache.TryGetValue( bank, out var resource ) ) {
				return AudioResult.Error_ResourceNotFound;
			}

			return AudioResult.Success;
		}
	};
};
