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

using NomadCore.Domain.Exceptions;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Systems.Audio.Domain.Models.Aggregates;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories.Loaders;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Services;
using NomadCore.Systems.ResourceCache.Common;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories {
	/*
	===================================================================================
	
	FMODBankRepository
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class FMODBankRepository : BaseCache<BankComposite, BankId> {
		private const string BANK_PATH = "res://Assets/Audio/Banks/";

		private readonly FMODBankLoadingStrategy _loadingStrategy;

		public FMODBankRepository( ILoggerService logger, IGameEventRegistryService eventFactory, ICVarSystemService cvarSystem, FMODSystemService fmodSystem, FMODGuidRepository guidRepository )
			: base( logger, eventFactory, new FMODBankLoader( fmodSystem, guidRepository, logger ) )
		{
			var bankLoadingStrategy = cvarSystem.GetCVar<FMODBankLoadingStrategy>( AudioConstants.CVars.FMOD.FMOD_BANK_LOADING_STRATEGY ) ?? throw new CVarMissing( AudioConstants.CVars.FMOD.FMOD_BANK_LOADING_STRATEGY );
			_loadingStrategy = bankLoadingStrategy.Value;

			if ( _loadingStrategy == FMODBankLoadingStrategy.Streaming ) {
				
			}
		}

		/*
		===============
		LoadBanks
		===============
		*/
		private void LoadBanks( ILoggerService logger ) {
			string path = FilePath.FromResourcePath( BANK_PATH ).OSPath;
			try {
				var files = System.IO.Directory.GetFiles( path, ".bank, .strings.bank" );
				for ( int i = 0; i < files.Length; i++ ) {
				}
			} catch ( Exception e ) {
				logger.PrintError( $"FMODBankRepository.LoadBanks: error loading banks from {path}\n{e}" );
				throw;
			}
		}
	};
};