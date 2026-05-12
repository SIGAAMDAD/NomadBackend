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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.Util;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Stats
{
	/*
	===================================================================================

	SteamStatsService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamStatsService : IStatsService
	{
		public bool SupportsLeaderboards => true;

		private readonly SteamStatsRepository _statsRepository;
		private readonly ILoggerCategory _category;
		private readonly Func<PeerId, CSteamID?>? _peerSteamIdResolver;

		private bool _isDisposed = false;

		/*
		===============
		SteamStatsService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statsRepository"></param>
		/// <param name="logger"></param>
		public SteamStatsService( SteamStatsRepository statsRepository, ILoggerService logger, Func<PeerId, CSteamID?>? peerSteamIdResolver = null )
		{
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_category = logger.CreateCategory( nameof( SteamStatsService ), LogLevel.Info, true );
			_statsRepository = statsRepository ?? throw new ArgumentNullException( nameof( statsRepository ) );
			_peerSteamIdResolver = peerSteamIdResolver;
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
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		GetStatFloat
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statName"></param>
		/// <returns></returns>
		public async ValueTask<float> GetStatFloat( InternString statName )
		{
			return _statsRepository.GetStatFloat( statName );
		}

		/*
		===============
		GetStatInt
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statName"></param>
		/// <returns></returns>
		public async ValueTask<int> GetStatInt( InternString statName )
		{
			return _statsRepository.GetStatInt( statName );
		}

		public async ValueTask<int> GetUserStatInt( PeerId peerId, InternString statName, CancellationToken ct = default )
		{
			if ( !TryResolveSteamId( peerId, nameof( GetUserStatInt ), out CSteamID steamId ) ) {
				return 0;
			}

			return await _statsRepository.GetUserStatInt( steamId, statName, ct );
		}

		public async ValueTask<float> GetUserStatFloat( PeerId peerId, InternString statName, CancellationToken ct = default )
		{
			if ( !TryResolveSteamId( peerId, nameof( GetUserStatFloat ), out CSteamID steamId ) ) {
				return 0.0f;
			}

			return await _statsRepository.GetUserStatFloat( steamId, statName, ct );
		}

		/*
		===============
		SetStatFloat
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public async ValueTask SetStatFloat( InternString statName, float value )
		{
			_statsRepository.SetStatFloat( statName, value );
		}

		/*
		===============
		SetStatInt
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public async ValueTask SetStatInt( InternString statName, int value )
		{
			_statsRepository.SetStatInt( statName, value );
		}

		public async ValueTask<bool> SetUserStatInt( PeerId peerId, InternString statName, int value, CancellationToken ct = default )
		{
			if ( !TryResolveSteamId( peerId, nameof( SetUserStatInt ), out CSteamID steamId ) ) {
				return false;
			}

			return _statsRepository.SetUserStatInt( steamId, statName, value );
		}

		public async ValueTask<bool> SetUserStatFloat( PeerId peerId, InternString statName, float value, CancellationToken ct = default )
		{
			if ( !TryResolveSteamId( peerId, nameof( SetUserStatFloat ), out CSteamID steamId ) ) {
				return false;
			}

			return _statsRepository.SetUserStatFloat( steamId, statName, value );
		}

		/*
		===============
		StoreStats
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public async ValueTask<bool> StoreStats()
		{
			return _statsRepository.StoreStats();
		}

		private bool TryResolveSteamId( PeerId peerId, string methodName, out CSteamID steamId )
		{
			if ( _peerSteamIdResolver != null ) {
				CSteamID? resolved = _peerSteamIdResolver( peerId );
				if ( resolved.HasValue && resolved.Value.IsValid() ) {
					steamId = resolved.Value;
					return true;
				}
			}

			_category.PrintWarning( $"{methodName}: unable to resolve PeerId '{peerId}' to a Steam user." );
			steamId = CSteamID.Nil;
			return false;
		}
	};
};
