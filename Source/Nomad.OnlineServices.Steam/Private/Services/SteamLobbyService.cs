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
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.CVars;
using Nomad.OnlineServices.Steam.Private.Entities;
using Nomad.OnlineServices.Steam.Private.Repositories;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamLobbyService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamLobbyService : ILobbyService
	{
		public bool IsInLobby => _current != null;
		public bool IsLobbyLeader => _current != null && _current.Info.OwnerId == _userData.UserID.m_SteamID;

		public LobbyInfo? Current => _current != null ? _current.Info.Info : null;
		private SteamLobbyInstance? _current = null;

		private readonly SteamLobbyRepository _repository;

		private readonly object _operationsLock = new object();

		private readonly CVarBinding<int> _maxPlayers;

		private readonly SteamUserData _userData;

		private readonly Callback<LobbyInvite_t> _lobbyInvite;
		private readonly Callback<LobbyChatMsg_t> _lobbyChatMsg;
		private readonly Callback<LobbyChatUpdate_t> _lobbyMemberStatusChanged;
		private readonly Callback<LobbyKicked_t> _lobbyKicked;

		private readonly SteamAsyncCallbackDispatcher<LobbyEnter_t, bool> _lobbyEnter;
		private readonly SteamAsyncCallbackDispatcher<LobbyCreated_t, SteamLobbyData> _lobbyCreated;
		private readonly SteamAsyncCallbackDispatcher<LobbyChatUpdate_t, bool> _lobbyStatusChanged;

		private readonly ICVarSystemService _cvarSystem;
		private readonly ILoggerCategory _category;
		private readonly IGameEventRegistryService _eventFactory;

		private bool _isDisposed = false;

		public IGameEvent<LobbyJoinedResultEventArgs> LobbyJoined => _lobbyJoined;
		private readonly IGameEvent<LobbyJoinedResultEventArgs> _lobbyJoined = default;

		public IGameEvent<LobbyLeaveResultEventArgs> LobbyLeft => _lobbyLeft;
		private readonly IGameEvent<LobbyLeaveResultEventArgs> _lobbyLeft = default;

		public IGameEvent<LobbyStartResultEventArgs> LobbyStarted => _lobbyStarted;
		private readonly IGameEvent<LobbyStartResultEventArgs> _lobbyStarted = default;

		/*
		===============
		SteamLobbyService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="userData"></param>
		/// <param name="appData"></param>
		/// <param name="logger"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="eventFactory"></param>
		public SteamLobbyService( SteamUserData userData, SteamAppData appData, ILoggerService logger, ICVarSystemService cvarSystem, IGameEventRegistryService eventFactory )
		{
			_cvarSystem = cvarSystem ?? throw new ArgumentNullException( nameof( cvarSystem ) );
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
			_userData = userData;

			_category = logger.CreateCategory( nameof( SteamLobbyService ), LogLevel.Info, true );

			_lobbyInvite = Callback<LobbyInvite_t>.Create( OnLobbyInvite );
			_lobbyChatMsg = Callback<LobbyChatMsg_t>.Create( OnLobbyChatMsg );
			_lobbyMemberStatusChanged = Callback<LobbyChatUpdate_t>.Create( OnLobbyMemberStatusChanged );
			_lobbyEnter = new SteamAsyncCallbackDispatcher<LobbyEnter_t, bool>();
			_lobbyCreated = new SteamAsyncCallbackDispatcher<LobbyCreated_t, SteamLobbyData>();
			_lobbyStatusChanged = new SteamAsyncCallbackDispatcher<LobbyChatUpdate_t, bool>();

			_repository = new SteamLobbyRepository( cvarSystem );

			_maxPlayers = new CVarBinding<int>( cvarSystem.GetCVarOrThrow<int>( Constants.CVars.LOBBY_MAX_CLIENTS ) );

			_lobbyJoined = eventFactory
				.GetEvent<LobbyJoinedResultEventArgs>(
					LobbyJoinedResultEventArgs.Name,
					LobbyJoinedResultEventArgs.NameSpace
				);

			_lobbyLeft = eventFactory
				.GetEvent<LobbyLeaveResultEventArgs>(
					LobbyLeaveResultEventArgs.Name,
					LobbyLeaveResultEventArgs.NameSpace
				);
		}

		/*
		===============
		OnLobbyMemberStatusChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnLobbyMemberStatusChanged( LobbyChatUpdate_t pCallback )
		{
			CSteamID userChangedId = (CSteamID)pCallback.m_ulSteamIDUserChanged;
			switch ( (EChatMemberStateChange)pCallback.m_rgfChatMemberStateChange ) {
				case EChatMemberStateChange.k_EChatMemberStateChangeBanned:
				case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
				case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
				case EChatMemberStateChange.k_EChatMemberStateChangeKicked:
					break;
				case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
					break;
			}
		}

		/*
		===============
		OnLobbyChatMsg
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnLobbyChatMsg( LobbyChatMsg_t pCallback )
		{
		}

		/*
		===============
		OnLobbyInvite
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnLobbyInvite( LobbyInvite_t pCallback )
		{
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
				_current?.Dispose();

				_repository?.Dispose();

				_lobbyInvite?.Dispose();
				_lobbyChatMsg?.Dispose();
				_lobbyEnter?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		CreateLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobbyInfo"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<Guid> CreateLobby( LobbyInfo lobbyInfo, CancellationToken ct = default )
		{
			SteamLobbyData? lobby = await CreateLobbyInternal( lobbyInfo, ct );
			if ( lobby == null ) {
				_lobbyStarted.Publish( new LobbyStartResultEventArgs( false, Guid.Empty ) );
				return Guid.Empty;
			}

			lock ( _operationsLock ) {
				_repository.AddLobby( lobby );
				_current = new SteamLobbyInstance( lobby, _cvarSystem, _eventFactory );
				_lobbyStarted.Publish( new LobbyStartResultEventArgs( true, lobby.Guid ) );
			}
			return lobby.Guid;
		}

		/*
		===============
		JoinLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobbyId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<bool> JoinLobby( Guid lobbyId, CancellationToken ct = default )
		{
			if ( !_repository.TryGetLobby( lobbyId, out SteamLobbyData? lobby ) ) {
				return false;
			}
			return await _lobbyEnter.Invoke(
				result => {
					switch ( (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse ) {
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseBanned:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseFull:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseClanDisabled:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseError:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseLimited:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseMemberBlockedYou:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseRatelimitExceeded:
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseYouBlockedMember:
							return false;
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess:
							break;
						default:
							throw new ArgumentOutOfRangeException( nameof( result ) );
					}
					lock ( _operationsLock ) {
						_current = new SteamLobbyInstance( lobby, _cvarSystem, _eventFactory );
						_lobbyJoined.Publish( new LobbyJoinedResultEventArgs( lobby.Guid ) );
					}
					return true;
				},
				() => SteamMatchmaking.JoinLobby( lobby.Id ),
				ct
			);
		}

		/*
		===============
		LeaveLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<bool> LeaveLobby( CancellationToken ct = default )
		{
			if ( _current == null ) {
				return false;
			}
			SteamMatchmaking.LeaveLobby( _current.Info.Id );
			return true;
		}

		public async Task<bool> PromoteMember( Guid player, CancellationToken ct = default )
		{
			return false;
		}

		/*
		===============
		CreateLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="info"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<SteamLobbyData?> CreateLobbyInternal( LobbyInfo info, CancellationToken ct = default )
		{
			if ( info.MaxPlayers < 1 || info.MaxPlayers > _maxPlayers.Value ) {
				throw new ArgumentOutOfRangeException( nameof( info ), "LobbyInfo.MaxPlayers is less than 1 or greater than MaxPlayers!" );
			}

			ELobbyType type = info.Visibility switch {
				LobbyVisibility.Private => ELobbyType.k_ELobbyTypePrivate,
				LobbyVisibility.Public => ELobbyType.k_ELobbyTypePublic,
				LobbyVisibility.FriendsOnly => ELobbyType.k_ELobbyTypeFriendsOnly,
				_ => throw new ArgumentOutOfRangeException( nameof( info ) )
			};

			return await _lobbyCreated.Invoke(
				result => {
					if ( result.m_eResult != EResult.k_EResultOK ) {
						_category.PrintError( $"SteamLobbyFactory.OnLobbyCreated: error creating lobby - {result.m_eResult}" );
						return null;
					}
					_category.PrintLine( $"SteamLobbyFactory.OnLobbyFactory: created new lobby with CSteamID '{result.m_ulSteamIDLobby}'" );

					CSteamID id = (CSteamID)result.m_ulSteamIDLobby;

					// setup default metadata
					SteamMatchmaking.SetLobbyOwner( id, _userData.UserID );
					SteamMatchmaking.SetLobbyMemberLimit( id, info.MaxPlayers );
					SteamMatchmaking.SetLobbyJoinable( id, true );

					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.Name ), info.Name );
					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.Map ), info.Map );
					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.GameMode ), info.GameMode );
					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.Visibility ), info.Visibility.ToString() );

					foreach ( var metadata in info.Metadata ) {
						SteamMatchmaking.SetLobbyData( id, metadata.Key, metadata.Value );
					}
					return new SteamLobbyData( id, info, Guid.NewGuid() );
				},
				() => SteamMatchmaking.CreateLobby( type, info.MaxPlayers ),
				ct
			);
		}
	};
};
