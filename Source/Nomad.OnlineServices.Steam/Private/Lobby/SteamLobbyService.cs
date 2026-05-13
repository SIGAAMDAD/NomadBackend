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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.Util;
using Nomad.CVars;
using Nomad.OnlineServices.Steam.Private.Network;
using Nomad.OnlineServices.Steam.Private.Util;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Lobby
{
	/*
	===================================================================================

	SteamLobbyService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamLobbyService : ILobbyService, IDisposable
	{
		public bool IsInLobby => _current != null;
		public bool IsLobbyLeader => _current != null && _current.Info.OwnerId == _userData.UserID.m_SteamID;

		public LobbyInfo? Current => _current != null ? _current.Info.Info : null;
		internal SteamLobbyInstance? ActiveLobby => _current;
		private SteamLobbyInstance? _current = null;

		public SteamLobbyRepository Repository => _repository;
		private readonly SteamLobbyRepository _repository;

		private readonly object _operationsLock = new object();

		private readonly CVarBinding<int> _maxPlayers;

		private readonly SteamUserData _userData;

		private readonly Callback<LobbyInvite_t> _lobbyInvite;
		private readonly Callback<LobbyChatMsg_t> _lobbyChatMsg;

		private readonly SteamAsyncCallResultDispatcher<LobbyEnter_t, LobbyJoinResult> _lobbyEnter;
		private readonly SteamAsyncCallResultDispatcher<LobbyCreated_t, SteamLobbyData> _lobbyCreated;

		private readonly ICVarSystemService _cvarSystem = null;
		private readonly ILoggerCategory _category = null;
		private readonly IGameEventRegistryService _eventFactory = null;

		private readonly SteamNetDriver _netDriver = null;

		private volatile LobbyCreateInfo? _requestInfo = null;

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
		/// <param name="logger"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="eventFactory"></param>
		public SteamLobbyService(
			SteamUserData userData,
			ILoggerService logger,
			ICVarSystemService cvarSystem,
			IGameEventRegistryService eventFactory,
			ISteamApiThreadDispatcher steamThread
		)
		{
			_cvarSystem = cvarSystem ?? throw new ArgumentNullException( nameof( cvarSystem ) );
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
			_userData = userData ?? throw new ArgumentNullException( nameof( userData ) );

			_category = logger.CreateCategory( nameof( SteamLobbyService ), LogLevel.Info, true );
			_netDriver = new SteamNetDriver( eventFactory, _category );

			_lobbyInvite = Callback<LobbyInvite_t>.Create( OnLobbyInvite );
			_lobbyChatMsg = Callback<LobbyChatMsg_t>.Create( OnLobbyChatMsg );
			_lobbyEnter = new SteamAsyncCallResultDispatcher<LobbyEnter_t, LobbyJoinResult>(
				operationName: "SteamMatchmaking.JoinLobby",
				steamThread: steamThread,
				resultFactory: result => {
					var lobbyId = new CSteamID( result.m_ulSteamIDLobby );
					if ( !_repository.TryGetLobby( lobbyId, out var lobby ) ) {
						return null;
					}
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
							return LobbyJoinResult.Failure( LobbyFailureReason.SessionFull );
						case EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess:
							break;
						default:
							throw new ArgumentOutOfRangeException( nameof( result ) );
					}
					lock ( _operationsLock ) {
						_current = new SteamLobbyInstance( lobby, _netDriver, _cvarSystem, _eventFactory );
						_lobbyJoined.Publish( new LobbyJoinedResultEventArgs( lobby.Guid ) );
					}
					return LobbyJoinResult.Joined( _current.Info.Info );
				}
			);
			_lobbyCreated = new SteamAsyncCallResultDispatcher<LobbyCreated_t, SteamLobbyData>(
				operationName: "SteamMatchmaking.CreateLobby",
				steamThread: steamThread,
				resultFactory: result => {
					if ( result.m_eResult != EResult.k_EResultOK ) {
						_category.PrintError( $"SteamLobbyFactory.OnLobbyCreated: error creating lobby - {result.m_eResult}" );
						_lobbyStarted.Publish(
							new LobbyStartResultEventArgs( success: false, id: Guid.Empty )
						);
						return null;
					}
					_category.PrintLine( $"SteamLobbyFactory.OnLobbyFactory: created new lobby with CSteamID '{result.m_ulSteamIDLobby}'" );

					CSteamID id = (CSteamID)result.m_ulSteamIDLobby;

					// setup default metadata
					SteamMatchmaking.SetLobbyOwner( id, _userData.UserID );
					SteamMatchmaking.SetLobbyMemberLimit( id, _requestInfo.MaxPlayers );
					SteamMatchmaking.SetLobbyJoinable( id, true );

					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.Name ), _requestInfo.Name );
					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.Map ), _requestInfo.Map );
					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.GameMode ), _requestInfo.GameMode );
					SteamMatchmaking.SetLobbyData( id, nameof( LobbyInfo.Visibility ), _requestInfo.Visibility.ToString() );

					if ( _requestInfo.Metadata != null ) {
						foreach ( var metadata in _requestInfo.Metadata ) {
							SteamMatchmaking.SetLobbyData( id, metadata.Key, metadata.Value );
						}
					}

					var lobbyData = new SteamLobbyData( id, _requestInfo, Guid.NewGuid() );
					_repository.AddLobby( lobbyData );

					_lobbyStarted.Publish(
						new LobbyStartResultEventArgs( success: true, id: lobbyData.Guid )
					);
					return lobbyData;
				}
			);

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

			_lobbyStarted = eventFactory
				.GetEvent<LobbyStartResultEventArgs>(
					LobbyStartResultEventArgs.Name,
					LobbyStartResultEventArgs.NameSpace
				);
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
		CreateLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobbyInfo"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<LobbyCreateResult> CreateLobby( LobbyCreateInfo lobbyInfo, CancellationToken ct = default )
		{
			SteamLobbyData? lobby = await CreateLobbyInternal( lobbyInfo, ct );
			if ( lobby == null ) {
				_lobbyStarted.Publish( new LobbyStartResultEventArgs( false, Guid.Empty ) );
				return LobbyCreateResult.Failure( LobbyFailureReason.Unknown );
			}

			lock ( _operationsLock ) {
				_repository.AddLobby( lobby );
				_current = new SteamLobbyInstance( lobby, _netDriver, _cvarSystem, _eventFactory );
				_lobbyStarted.Publish( new LobbyStartResultEventArgs( true, lobby.Guid ) );
			}
			return LobbyCreateResult.Created( new LobbyId( lobby.Guid ) );
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
		public async Task<LobbyJoinResult> JoinLobby( LobbyId lobbyId, CancellationToken ct = default )
		{
			if ( !_repository.TryGetLobby( lobbyId.Value, out SteamLobbyData? lobby ) ) {
				return LobbyJoinResult.Failure( LobbyFailureReason.SessionNotFound );
			}
			return await _lobbyEnter.ExecuteAsync(
				beginSteamCall: () => SteamMatchmaking.JoinLobby( lobby.Id ),
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
		private async Task<SteamLobbyData?> CreateLobbyInternal( LobbyCreateInfo info, CancellationToken ct = default )
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

			_requestInfo = info;

			return await _lobbyCreated.ExecuteAsync(
				beginSteamCall: () => SteamMatchmaking.CreateLobby( type, info.MaxPlayers ),
				ct: ct
			);
		}

		/*
		===============
		TryGetMember
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="peerId"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool TryGetMember( PeerId peerId, out LobbyMemberInfo member )
		{
			member = null;
			if ( _current == null ) {
				return false;
			}
			if ( _current.Members.TryGetValue( peerId, out var sessionPeer ) ) {
				member = sessionPeer.Info;
				return true;
			}
			return false;
		}

		public bool TryGetSteamId( PeerId peerId, out CSteamID steamId )
		{
			if ( _current != null ) {
				return _current.TryGetSteamId( peerId, out steamId );
			}

			steamId = CSteamID.Nil;
			return false;
		}

		/*
		===============
		GetMembers
		===============
		*/
		/// <summary>
		/// Returns a snapshot of the current members in the steam lobby.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<LobbyMemberInfo> GetMembers()
		{
			var members = new List<LobbyMemberInfo>( _current.Members.Count );
			foreach ( var member in _current.Members.Values ) {
				members.Add( member.Info );
			}
			return members;
		}

		/*
		===============
		SetLobbyRefresh
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="active"></param>
		public void SetLobbyRefresh( bool active )
		{
			_repository.SetPollingEnabled( active );
		}
	};
};
