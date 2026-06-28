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
using System.Linq;
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

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamMatchMakingService

	===================================================================================
	*/
	/// <summary>
	/// Steam lobby search and quickplay matchmaking service.
	/// </summary>

	internal sealed class SteamMatchMakingService : IMatchMakingService
	{
		private readonly SteamAsyncCallResultDispatcher<LobbyMatchList_t, ICollection<SteamLobbyData>> _lobbyMatchList;
		private CancellationTokenSource? _cancellationToken = null;

		private readonly ILoggerCategory _category;
		private readonly SteamLobbyRepository _repository;

		private DateTime _lastFetchTime = DateTime.MinValue;
		private readonly int _lobbyUpdateInterval;

		private ServerRange _lastRange = ServerRange.Count;

		public bool IsSearching => _activeRequest != null;

		public MatchMakingInfo? CurrentRequest => _activeRequest;
		private MatchMakingInfo? _activeRequest = null;

		public IGameEvent<SearchResultsUpdatedEventArgs> SearchResultsUpdated => _searchResultsUpdated;
		private readonly IGameEvent<SearchResultsUpdatedEventArgs> _searchResultsUpdated;

		public IGameEvent<MatchFoundEventArgs> MatchFound => _matchFound;
		private readonly IGameEvent<MatchFoundEventArgs> _matchFound;

		public IGameEvent<MatchMakingFailedEventArgs> MatchMakingFailed => _matchMakingFailed;
		private readonly IGameEvent<MatchMakingFailedEventArgs> _matchMakingFailed;

		private bool _isDisposed = false;

		/*
		===============
		SteamMatchMakingService
		===============
		*/
		/// <summary>
		/// Creates a Steam matchmaking service.
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		public SteamMatchMakingService(
			SteamLobbyRepository repository,
			ICVarSystemService cvarSystem,
			ILoggerService logger,
			IGameEventRegistryService eventFactory
		)
		{
			ArgumentGuard.ThrowIfNull( cvarSystem, nameof( cvarSystem ) );
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );
			ArgumentGuard.ThrowIfNull( eventFactory, nameof( eventFactory ) );

			_repository = repository ?? throw new ArgumentNullException( nameof( repository ) );

			_category = logger.CreateCategory( nameof( SteamMatchMakingService ), LogLevel.Info, true );
			_lobbyMatchList = new SteamAsyncCallResultDispatcher<LobbyMatchList_t, ICollection<SteamLobbyData>>( _category );

			ICVar<int> lobbyUpdateInterval = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.LOBBY_UDDATE_INTERVAL );
			_lobbyUpdateInterval = lobbyUpdateInterval.Value;

			_searchResultsUpdated = eventFactory.GetEvent<SearchResultsUpdatedEventArgs>(
				SearchResultsUpdatedEventArgs.Name,
				SearchResultsUpdatedEventArgs.NameSpace
			);
			_matchFound = eventFactory.GetEvent<MatchFoundEventArgs>(
				MatchFoundEventArgs.Name,
				MatchFoundEventArgs.NameSpace
			);
			_matchMakingFailed = eventFactory.GetEvent<MatchMakingFailedEventArgs>(
				MatchMakingFailedEventArgs.Name,
				MatchMakingFailedEventArgs.NameSpace
			);
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Disposes matchmaking resources.
		/// </summary>
		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			_cancellationToken?.Cancel();
			_cancellationToken?.Dispose();
			_lobbyMatchList.Dispose();
			_category.Dispose();

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		SearchLobbies
		===============
		*/
		/// <inheritdoc />
		public async Task<IReadOnlyList<LobbyInfo>> SearchLobbies( MatchMakingInfo info, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( info, nameof( info ) );

			_cancellationToken?.Dispose();
			_cancellationToken = CancellationTokenSource.CreateLinkedTokenSource( ct );
			CancellationToken linkedToken = _cancellationToken.Token;
			linkedToken.ThrowIfCancellationRequested();

			_activeRequest = info;

			try {
				bool needRefresh = (DateTime.UtcNow - _lastFetchTime).TotalMilliseconds > _lobbyUpdateInterval
								|| _lastRange != info.Range
								|| _repository.Lobbies.Count == 0;
				if ( needRefresh ) {
					await RequestLobbyListAsync( info, linkedToken ).ConfigureAwait( false );
				}

				IReadOnlyList<LobbyInfo> lobbies = FilterAndRankLobbies( _repository.Lobbies, info );
				_searchResultsUpdated.Publish( new SearchResultsUpdatedEventArgs() );
				return lobbies;
			} finally {
				_activeRequest = null;
			}
		}

		/*
		===============
		FindBestLobby
		===============
		*/
		/// <inheritdoc />
		public async Task<LobbyInfo?> FindBestLobby( MatchMakingInfo info, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( info, nameof( info ) );

			IReadOnlyList<LobbyInfo> lobbies = await SearchLobbies( info, ct ).ConfigureAwait( false );
			ct.ThrowIfCancellationRequested();
			return lobbies.Count > 0 ? lobbies[0] : null;
		}

		/*
		===============
		StartQuickPlay
		===============
		*/
		/// <inheritdoc />
		public async Task<bool> StartQuickPlay( MatchMakingInfo info, CancellationToken ct = default )
		{
			ThrowIfDisposed();

			LobbyInfo? lobby = await FindBestLobby( info, ct ).ConfigureAwait( false );
			if ( lobby == null ) {
				_matchMakingFailed.Publish( new MatchMakingFailedEventArgs() );
				return false;
			}

			_matchFound.Publish( new MatchFoundEventArgs( lobby.Id.Value ) );
			return true;
		}

		/*
		===============
		Cancel
		===============
		*/
		/// <inheritdoc />
		public Task Cancel( CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();

			_cancellationToken?.Cancel();
			_activeRequest = null;
			return Task.CompletedTask;
		}

		/*
		===============
		RequestLobbyListAsync
		===============
		*/
		/// <summary>
		/// Requests and caches a fresh Steam lobby list.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<ICollection<SteamLobbyData>> RequestLobbyListAsync( MatchMakingInfo info, CancellationToken ct = default )
		{
			return await _lobbyMatchList.Invoke(
				steamCall: () => {
					SteamMatchmaking.AddRequestLobbyListDistanceFilter( GetDistanceFilter( info.Range ) );
					SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable( 1 );
					SteamMatchmaking.AddRequestLobbyListResultCountFilter( 50 );

					if ( info.FriendsOnly ) {
						SteamMatchmaking.AddRequestLobbyListStringFilter(
							nameof( LobbyInfo.Visibility ),
							LobbyVisibility.FriendsOnly.ToString(),
							ELobbyComparison.k_ELobbyComparisonEqual
						);
					}

					ApplyFirstStringFilter( nameof( LobbyInfo.Map ), info.Maps );
					ApplyFirstStringFilter( nameof( LobbyInfo.GameMode ), info.GameModes );
					ApplyMetadataFilters( info.Metadata );

					return SteamMatchmaking.RequestLobbyList();
				},
				resultFactory: result => {
					for ( int i = 0; i < result.m_nLobbiesMatching; i++ ) {
						CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex( i );
						_repository.AddLobby( new SteamLobbyKey( lobbyId, Guid.NewGuid() ) );
					}

					_repository.RemoveStaleLobbies();
					_lastRange = info.Range;
					_lastFetchTime = DateTime.UtcNow;
					return _repository.Lobbies;
				},
				ct
			).ConfigureAwait( false );
		}

		private static ELobbyDistanceFilter GetDistanceFilter( ServerRange range )
		{
			return range switch {
				ServerRange.LAN => ELobbyDistanceFilter.k_ELobbyDistanceFilterClose,
				ServerRange.Region => ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault,
				ServerRange.Continental => ELobbyDistanceFilter.k_ELobbyDistanceFilterFar,
				ServerRange.NoLimit => ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide,
				_ => throw new ArgumentOutOfRangeException( nameof( range ) )
			};
		}

		private static void ApplyFirstStringFilter( string key, IReadOnlyList<string>? values )
		{
			if ( values == null || values.Count == 0 ) {
				return;
			}

			SteamMatchmaking.AddRequestLobbyListStringFilter(
				key,
				values[0],
				ELobbyComparison.k_ELobbyComparisonEqual
			);
		}

		private static void ApplyMetadataFilters( IReadOnlyDictionary<string, string>? metadata )
		{
			if ( metadata == null ) {
				return;
			}

			foreach ( var pair in metadata ) {
				SteamMatchmaking.AddRequestLobbyListStringFilter(
					pair.Key,
					pair.Value,
					ELobbyComparison.k_ELobbyComparisonEqual
				);
			}
		}

		private static IReadOnlyList<LobbyInfo> FilterAndRankLobbies( ICollection<SteamLobbyData> source, MatchMakingInfo info )
		{
			return source
				.Select( lobby => lobby.Info )
				.Where( lobby => IsMatch( lobby, info ) )
				.OrderByDescending( lobby => ScoreLobby( lobby, info ) )
				.ToArray();
		}

		private static bool IsMatch( LobbyInfo lobby, MatchMakingInfo info )
		{
			if ( lobby.MaxPlayers > 0 && lobby.PlayerCount >= lobby.MaxPlayers ) {
				return false;
			}
			if ( info.FriendsOnly && lobby.Visibility != LobbyVisibility.FriendsOnly ) {
				return false;
			}
			if ( info.Maps != null && info.Maps.Count > 0 && !ContainsOrdinalIgnoreCase( info.Maps, lobby.Map ) ) {
				return false;
			}
			if ( info.GameModes != null && info.GameModes.Count > 0 && !ContainsOrdinalIgnoreCase( info.GameModes, lobby.GameMode ) ) {
				return false;
			}
			if ( !MetadataMatches( lobby.Metadata, info.Metadata ) ) {
				return false;
			}

			return true;
		}

		private static bool MetadataMatches( IReadOnlyDictionary<string, string>? lobbyMetadata, IReadOnlyDictionary<string, string>? requestMetadata )
		{
			if ( requestMetadata == null || requestMetadata.Count == 0 ) {
				return true;
			}
			if ( lobbyMetadata == null ) {
				return false;
			}

			foreach ( var pair in requestMetadata ) {
				if ( !lobbyMetadata.TryGetValue( pair.Key, out string? value )
					|| !string.Equals( value, pair.Value, StringComparison.OrdinalIgnoreCase ) ) {
					return false;
				}
			}

			return true;
		}

		private static int ScoreLobby( LobbyInfo lobby, MatchMakingInfo info )
		{
			int score = 0;
			if ( ContainsOrdinalIgnoreCase( info.Maps, lobby.Map ) ) {
				score += 50;
			}
			if ( ContainsOrdinalIgnoreCase( info.GameModes, lobby.GameMode ) ) {
				score += 50;
			}
			if ( lobby.MaxPlayers > 0 ) {
				score += Math.Max( 0, lobby.MaxPlayers - lobby.PlayerCount );
			}
			if ( lobby.Visibility == LobbyVisibility.Public ) {
				score += 1;
			}
			return score;
		}

		private static bool ContainsOrdinalIgnoreCase( IReadOnlyList<string>? values, string? candidate )
		{
			if ( values == null || values.Count == 0 || candidate == null ) {
				return false;
			}

			for ( int i = 0; i < values.Count; i++ ) {
				if ( string.Equals( values[i], candidate, StringComparison.OrdinalIgnoreCase ) ) {
					return true;
				}
			}

			return false;
		}

		private void ThrowIfDisposed()
		{
			if ( _isDisposed ) {
				throw new ObjectDisposedException( nameof( SteamMatchMakingService ) );
			}
		}
	};
};
