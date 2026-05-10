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
using System.Threading.Tasks;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;

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
		public SteamStatsService( SteamStatsRepository statsRepository, ILoggerService logger )
		{
			_category = logger.CreateCategory( nameof( SteamStatsService ), LogLevel.Info, true );

			_statsRepository = statsRepository;
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
		public async ValueTask<float> GetStatFloat( string statName )
			=> _statsRepository.GetStatFloat( statName );

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
		public async ValueTask<int> GetStatInt( string statName )
			=> _statsRepository.GetStatInt( statName );

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
		public async ValueTask SetStatFloat( string statName, float value )
			=> _statsRepository.SetStatFloat( statName, value );

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
		public async ValueTask SetStatInt( string statName, int value )
			=> _statsRepository.SetStatInt( statName, value );

		public async ValueTask<bool> StoreStats()
			=> _statsRepository.StoreStats();
	};
};
