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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Lobby;
using Nomad.OnlineServices.Steam.Private.Registries;
using Nomad.OnlineServices.Steam.Private.Services;
using Nomad.OnlineServices.Steam.Private.Stats;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private
{
	/*
	===================================================================================

	SteamService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamService : IOnlinePlatformService
	{
		public OnlinePlatform Platform => OnlinePlatform.Steam;
		public string PlatformName => nameof( OnlinePlatform.Steam );
		public bool IsAvailable => true;

		public IStatsService Stats {
			get {
				_statsService ??= new SteamStatsService( _statsRepository, _logger );
				return _statsService;
			}
		}
		private SteamStatsService _statsService;

		public IAchievementService Achievements {
			get {
				_achievementsService ??= new SteamAchievementService( _statsRepository, _logger, _eventFactory );
				return _achievementsService;
			}
		}
		private SteamAchievementService _achievementsService;

		public ICloudStorageService CloudStorage {
			get {
				_cloudStorageService ??= new SteamCloudStorageService( _logger, _fileSystem );
				return _cloudStorageService;
			}
		}
		private SteamCloudStorageService _cloudStorageService;

		public ILobbyService LobbyService {
			get {
				_lobbyService ??= new SteamLobbyService( _userData, _appData, _logger, _cvarSystem, _eventFactory );
				return _lobbyService;
			}
		}
		private SteamLobbyService _lobbyService;

		private readonly ILoggerService _logger;
		private readonly ILoggerCategory _category;

		private readonly IFileSystem _fileSystem;
		private readonly ICVarSystemService _cvarSystem;
		private readonly IGameEventRegistryService _eventFactory;

		private readonly SteamUserData _userData;
		private readonly SteamAppData _appData;

		private readonly SteamStatsRepository _statsRepository;

		private bool _isDisposed = false;

		/*
		===============
		SteamService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="fileSystem"></param>
		/// <param name="engineService"></param>
		/// <param name="eventFactory"></param>
		/// <param name="cvarSystem"></param>
		public SteamService( ILoggerService logger, IFileSystem fileSystem, IEngineService engineService, IGameEventRegistryService eventFactory, ICVarSystemService cvarSystem )
		{
			ArgumentGuard.ThrowIfNull( logger );
			ArgumentGuard.ThrowIfNull( fileSystem );
			ArgumentGuard.ThrowIfNull( engineService );
			ArgumentGuard.ThrowIfNull( eventFactory );
			ArgumentGuard.ThrowIfNull( cvarSystem );

			ESteamAPIInitResult result = SteamAPI.InitEx( out string errorMessage );
			if ( result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK ) {
				logger.PrintError( $"SteamService: failed to initialize SteamAPI - {result}, {errorMessage}" );
				return;
			}

			_userData = new SteamUserData {
				UserID = SteamUser.GetSteamID(),
				UserName = SteamFriends.GetPersonaName()
			};

			_appData = new SteamAppData {
				AppId = SteamUtils.GetAppID()
			};

			SteamCVarRegistry.RegisterCVars( cvarSystem );

			_logger = logger;
			_category = logger.CreateCategory( "Nomad.OnlineServices.Steam", LogLevel.Info, true );

			_eventFactory = eventFactory;
			_cvarSystem = cvarSystem;
			_fileSystem = fileSystem;

			_statsRepository = new SteamStatsRepository( _userData, logger, engineService );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_lobbyService?.Dispose();
				_statsService?.Dispose();
				_achievementsService?.Dispose();
				_cloudStorageService?.Dispose();
				_statsRepository?.Dispose();

				_category?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		RunCallbacks
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void RunCallbacks()
		{
			SteamAPI.RunCallbacks();
		}
	};
};
