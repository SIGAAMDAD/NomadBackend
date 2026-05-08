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
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.Util;
using Nomad.OnlineServices.Steam.Private.Repositories;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamAchievementService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamAchievementService : IAchievementService
	{
		public bool SupportsAchievements => true;

		public int NumAchievements => _statsRepository.NumAchievements;

		private readonly SteamStatsRepository _statsRepository;
		private readonly ILoggerService _logger;
		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;

		public IGameEvent<AchievementUnlockedEventArgs> Unlocked => _unlocked;
		private readonly IGameEvent<AchievementUnlockedEventArgs> _unlocked;

		public IGameEvent<AchievementProgressChangedEventArgs> ProgressChanged => _progressChanged;
		private readonly IGameEvent<AchievementProgressChangedEventArgs> _progressChanged;

		/*
		===============
		SteamAchievementService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="statsRepository"></param>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		public SteamAchievementService( SteamStatsRepository statsRepository, ILoggerService logger, IGameEventRegistryService eventFactory )
		{
			_statsRepository = statsRepository;
			_logger = logger;
			_category = _logger.CreateCategory( nameof( SteamAchievementService ), LogLevel.Info, true );

			_unlocked = eventFactory.GetEvent<AchievementUnlockedEventArgs>( AchievementUnlockedEventArgs.Name, AchievementUnlockedEventArgs.NameSpace );
			_progressChanged = eventFactory.GetEvent<AchievementProgressChangedEventArgs>( AchievementProgressChangedEventArgs.Name, AchievementProgressChangedEventArgs.NameSpace );

			_statsRepository.AchievementProgressChanged += OnAchievementProgressChanged;
			_statsRepository.AchievementUnlocked += OnAchievementUnlocked;
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
				_unlocked?.Dispose();
				_progressChanged?.Dispose();

				_category?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		LockAchievement
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="achievementId"></param>
		public async Task LockAchievement( string achievementId )
			=> _statsRepository.LockAchievement( achievementId );

		/*
		===============
		SetAchievementProgress
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="achievementId"></param>
		/// <param name="current"></param>
		public async Task SetAchievementProgress( string achievementId, float current )
			=> _statsRepository.SetAchievementProgress( achievementId, current );

		/*
		===============
		UnlockAchievement
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="achievementId"></param>
		public async Task UnlockAchievement( string achievementId )
			=> _statsRepository.UnlockAchievement( achievementId );

		/*
		===============
		GetAchievementInfo
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="achievementId"></param>
		/// <returns></returns>
		public IAchievementInfo? GetAchievementInfo( string achievementId )
			=> _statsRepository.GetAchievementInfo( achievementId );

		/*
		===============
		OnAchievementProgressChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="achievementId"></param>
		/// <param name="current"></param>
		/// <param name="max"></param>
		private void OnAchievementProgressChanged( string achievementId, float current, float max )
		{
			_progressChanged.Publish( new AchievementProgressChangedEventArgs( new InternString( achievementId ), current ) );
		}

		/*
		===============
		OnAchievementUnlocked
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="achievementId"></param>
		private void OnAchievementUnlocked( string achievementId )
		{
			_unlocked.Publish( new AchievementUnlockedEventArgs( new InternString( achievementId ) ) );
		}
	};
};
