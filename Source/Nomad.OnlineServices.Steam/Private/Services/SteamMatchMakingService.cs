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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.CVars;
using Nomad.OnlineServices.Steam.Private.Lobby;
using Nomad.OnlineServices.Steam.Private.Util;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

#if !NET10_0_OR_GREATER
using System.Buffers;
#endif

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamMatchMakingService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamMatchMakingService : IMatchMakingService
	{
		private readonly List<LobbyInfo> _lobbies = new();

		private readonly SteamAsyncCallResultDispatcher<LobbyMatchList_t, ICollection<SteamLobbyData>> _lobbyMatchList;
		private CancellationTokenSource? _cancellationToken = null;

		private readonly ILoggerCategory _category;
		private readonly SteamLobbyRepository _repository;

		private DateTime _lastFetchTime = DateTime.UtcNow;
		private readonly int _lobbyUpdateInterval = 0;

		private ServerRange _lastRange = ServerRange.LAN;

		public bool IsSearching => _activeRequest != null;

		public MatchMakingInfo? CurrentRequest => _activeRequest;
		private MatchMakingInfo? _activeRequest = null;

		public IGameEvent<SearchResultsUpdatedEventArgs> SearchResultsUpdated => _searchResultsUpdated;
		private readonly IGameEvent<SearchResultsUpdatedEventArgs> _searchResultsUpdated = default;

		public IGameEvent<MatchFoundEventArgs> MatchFound => _matchFound;
		private readonly IGameEvent<MatchFoundEventArgs> _matchFound = default;

		public IGameEvent<MatchMakingFailedEventArgs> MatchMakingFailed => _matchMakingFailed;
		private readonly IGameEvent<MatchMakingFailedEventArgs> _matchMakingFailed = default;

		private bool _isDisposed = false;

		/*
		===============
		SteamMatchMakingService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="logger"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamMatchMakingService( SteamLobbyRepository repository, ICVarSystemService cvarSystem, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( cvarSystem, nameof( cvarSystem ) );
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_repository = repository ?? throw new ArgumentNullException( nameof( repository ) );

			_lobbyMatchList = new SteamAsyncCallResultDispatcher<LobbyMatchList_t, ICollection<SteamLobbyData>>( _category );
			_category = logger.CreateCategory( nameof( SteamMatchMakingService ), LogLevel.Info, true );

			_lastRange = ServerRange.Count;

			ICVar<int> lobbyUpdateInterval = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.LOBBY_UDDATE_INTERVAL );
			_lobbyUpdateInterval = lobbyUpdateInterval.Value;
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
				_category?.Dispose();
				_cancellationToken?.Dispose();

				_lobbyMatchList?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		SearchLobbies
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="info"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<IReadOnlyList<LobbyInfo>> SearchLobbies( MatchMakingInfo info, CancellationToken ct = default )
		{
			_cancellationToken = CancellationTokenSource.CreateLinkedTokenSource( ct );
			ct.ThrowIfCancellationRequested();

			_activeRequest = info;

			// fetch the lobby list if we haven't updated for a while, or if we just don't have anything
			bool needRefresh = (DateTime.UtcNow - _lastFetchTime).TotalMilliseconds > _lobbyUpdateInterval
							|| _lastRange != info.Range
							|| _repository.Lobbies.Count == 0;
			if ( needRefresh ) {
				await RequestLobbyListAsync( info.Range, ct );
			}

			ICollection<SteamLobbyData> steamLobbies = _repository.Lobbies;
			List<LobbyInfo> lobbies = new List<LobbyInfo>( steamLobbies.Count );
			foreach ( var lobby in steamLobbies ) {
				lobbies.Add( lobby.Info );
			}
			_activeRequest = null;

			return lobbies;
		}

		/*
		===============
		FindBestLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="info"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<LobbyInfo?> FindBestLobby( MatchMakingInfo info, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();

			var lobbies = await SearchLobbies( info, ct );
			ct.ThrowIfCancellationRequested();

#if NET10_0_OR_GREATER
			Span<int> scores = stackalloc int[lobbies.Count];
#else
			int[] arr = ArrayPool<int>.Shared.Rent( lobbies.Count );
			Span<int> scores = arr;
#endif
			scores.Clear();

			for ( int i = 0; i < lobbies.Count; i++ ) {
				ct.ThrowIfCancellationRequested();

				LobbyInfo lobby = lobbies[i];

				foreach ( var gameMode in info.GameModes ) {
					if ( lobby.GameMode.Equals( gameMode, StringComparison.InvariantCulture ) ) {
						scores[i] += 5;
						break;
					}
				}
				foreach ( var map in info.Maps ) {
					if ( lobby.Map.Equals( map, StringComparison.InvariantCulture ) ) {
						scores[i] += 5;
						break;
					}
				}
			}

#if !NET10_0_OR_GREATER
			ArrayPool<int>.Shared.Return( arr );
#endif

			return null;
		}

		/*
		===============
		StartQuickPlay
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="info"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<bool> StartQuickPlay( MatchMakingInfo info, CancellationToken ct = default )
		{
			LobbyInfo? lobby = await FindBestLobby( info, ct );
			if ( lobby == null ) {
				_matchMakingFailed.Publish( new MatchMakingFailedEventArgs() );
				return false;
			}
			return true;
		}

		/*
		===============
		Cancel
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task Cancel( CancellationToken ct = default )
		{
			_cancellationToken.Cancel();
			_activeRequest = null;
		}

		/*
		===============
		RequestLobbyList
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="range"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<ICollection<SteamLobbyData>> RequestLobbyListAsync( ServerRange range, CancellationToken ct = default )
		{
			return await _lobbyMatchList.Invoke(
				steamCall: () => {
					ELobbyDistanceFilter distanceFilter = range switch {
						ServerRange.LAN => ELobbyDistanceFilter.k_ELobbyDistanceFilterClose,
						ServerRange.Region => ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault,
						ServerRange.Continental => ELobbyDistanceFilter.k_ELobbyDistanceFilterFar,
						ServerRange.NoLimit => ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide,
						_ => throw new ArgumentOutOfRangeException( nameof( range ) )
					};
					SteamMatchmaking.AddRequestLobbyListDistanceFilter( distanceFilter );
					return SteamMatchmaking.RequestLobbyList();
				},
				resultFactory: result => {
					for ( int i = 0; i < result.m_nLobbiesMatching; i++ ) {
						CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex( i );
						_repository.AddLobby( new SteamLobbyKey( lobbyId, Guid.NewGuid() ) );
					}
					// remove lobbies that haven't been seen recently
					_repository.RemoveStaleLobbies();
					_lastRange = range;
					_lastFetchTime = DateTime.UtcNow;
					return _repository.Lobbies;
				},
				ct
			);
		}
	};
};
