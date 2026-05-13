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
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Lobby;
using Nomad.OnlineServices.Steam.Private.Network;
using Nomad.OnlineServices.Steam.Private.Registries;
 using Nomad.OnlineServices.Steam.Private.Stats;
using Nomad.OnlineServices.Steam.Private.Util;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
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
				_statsRepository = new SteamStatsRepository( _userData, _logger, _engineService );
				_statsService ??= new SteamStatsService( _statsRepository, _logger, TryResolvePeerSteamId );
				return _statsService;
			}
		}
		private SteamStatsService? _statsService = null;
		private SteamStatsRepository? _statsRepository = null;

		public IAchievementService Achievements {
			get {
				_achievementsService ??= new SteamAchievementService( _statsRepository, _logger, _eventFactory );
				return _achievementsService;
			}
		}
		private SteamAchievementService? _achievementsService = null;

		public IMatchMakingService Matchmaking {
			get {
				_matchMakingService ??= new SteamMatchMakingService( _lobbyService.Repository, _cvarSystem, _logger );
				return _matchMakingService;
			}
		}
		private SteamMatchMakingService? _matchMakingService = null;

		public ICloudStorageService CloudStorage {
			get {
				_cloudStorageService ??= new SteamCloudStorageService( _logger, _fileSystem );
				return _cloudStorageService;
			}
		}
		private SteamCloudStorageService? _cloudStorageService = null;

		public ILobbyService Lobbies {
			get {
				_lobbyService ??= new SteamLobbyService( _userData, _logger, _cvarSystem, _eventFactory );
				return _lobbyService;
			}
		}
		private SteamLobbyService? _lobbyService = null;

		public INetDriver NetDriver {
			get {
				_netDriver ??= new SteamNetDriver( _eventFactory, _category );
				return _netDriver;
			}
		}
		private SteamNetDriver? _netDriver = null;

		public IUserAvatarService AvatarService {
			get {
				_avatarService ??= new SteamUserAvatarService( _engineService, _category, TryResolvePeerSteamId );
				return _avatarService;
			}
		}
		private SteamUserAvatarService? _avatarService = null;

		private readonly ILoggerCategory _category;

		private readonly ILoggerService _logger;
		private readonly IEngineService _engineService;
		private readonly IFileSystem _fileSystem;
		private readonly ICVarSystemService _cvarSystem;
		private readonly IGameEventRegistryService _eventFactory;

		private readonly SteamUserData _userData = null;
		private readonly SteamDataCache _steamData = null;

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
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
			_cvarSystem = cvarSystem ?? throw new ArgumentNullException( nameof( cvarSystem ) );
			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_logger = logger ?? throw new ArgumentNullException( nameof( logger ) );
			_engineService = engineService ?? throw new ArgumentNullException( nameof( engineService ) );

			SteamCVarRegistry.RegisterCVars( cvarSystem );

			_category = logger.CreateCategory( "Nomad.OnlineServices.Steam", LogLevel.Info, true );

			ESteamAPIInitResult result = SteamAPI.InitEx( out string errorMessage );
			if ( result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK ) {
				_category.PrintError( $"SteamService: failed to initialize SteamAPI - {result}, {errorMessage}" );
				return;
			}

			_steamData = new SteamDataCache {
				LocalUserId = SteamUser.GetSteamID(),

				AppBuildId = SteamApps.GetAppBuildId(),
				AppId = SteamUtils.GetAppID(),
				AppOwnerId = SteamApps.GetAppOwner(),

				InitialPersonaName = SteamFriends.GetPersonaName(),
				AvailableGameLanguages = SteamApps.GetAvailableGameLanguages(),
				CurrentGameLanguage = SteamApps.GetCurrentGameLanguage(),

				Universe = SteamUtils.GetConnectedUniverse(),
				IsSteamDeck = SteamUtils.IsSteamRunningOnSteamDeck()
			};

			_userData = new SteamUserData {
				UserID = SteamUser.GetSteamID(),
				UserName = SteamFriends.GetPersonaName()
			};

			_category.PrintLine( "Initialized Steamworks SDK API Service." );
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
				_netDriver?.Dispose();
				_achievementsService?.Dispose();
				_cloudStorageService?.Dispose();

				_category?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Frame
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Frame()
		{
			if ( !SteamAPI.IsSteamRunning() ) {
				return;
			}
			SteamAPI.RunCallbacks();

			_statsService.StoreStats();
		}

		/*
		===============
		TryResolvePeerSteamId
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="peerId"></param>
		/// <returns></returns>
		private CSteamID? TryResolvePeerSteamId( PeerId peerId )
		{
			if ( _lobbyService != null && _lobbyService.TryGetSteamId( peerId, out CSteamID steamId ) ) {
				return steamId;
			}

			return null;
		}
	};
};
