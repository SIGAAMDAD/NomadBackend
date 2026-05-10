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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Engine.Services;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.Util;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Stats
{
	/*
	===================================================================================

	SteamStatsRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamStatsRepository : IDisposable
	{
		public int NumAchievements => _achievements.Count;
		public int NumStats => _stats.Count;

		private readonly ConcurrentDictionary<string, SteamAchievementInfo> _achievements;
		private readonly ConcurrentDictionary<string, SteamStatData> _stats;

		private readonly HashSet<string> _dirtyStats;

		private readonly IEngineService _engineService;
		private readonly ILoggerCategory _category;
		private readonly SteamUserData _userData;

		private readonly Callback<UserStatsReceived_t> _userStatsReceived;
		private readonly Callback<UserStatsStored_t> _userStatsStored;
		private readonly Callback<UserStatsUnloaded_t> _userStatsUnloaded;
		private readonly Callback<UserAchievementIconFetched_t> _userAchievementIconFetched;
		private readonly Callback<UserAchievementStored_t> _userAchievementStored;

		private bool _isDisposed = false;

		private bool _storeInFlight = false;
		private bool _storeAgainAfterCurrent = false;

		public bool IsReady => _isReady;
		private volatile bool _isReady = false;

		public event Action<InternString> AchievementUnlocked;
		public event Action<InternString, float, float> AchievementProgressChanged;
		public event Action StatsUpdated;

		/*
		===============
		SteamStatsRepository
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="userData"></param>
		/// <param name="logger"></param>
		/// <param name="engineService"></param>
		public SteamStatsRepository( SteamUserData userData, ILoggerService logger, IEngineService engineService )
		{
			ArgumentGuard.ThrowIfNull( engineService, nameof( engineService ) );
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_userStatsReceived = Callback<UserStatsReceived_t>.Create( OnUserStatsReceived );
			_userStatsStored = Callback<UserStatsStored_t>.Create( OnUserStatsStored );
			_userStatsUnloaded = Callback<UserStatsUnloaded_t>.Create( OnUserStatsUnloaded );
			_userAchievementIconFetched = Callback<UserAchievementIconFetched_t>.Create( OnUserAchievementIconFetched );
			_userAchievementStored = Callback<UserAchievementStored_t>.Create( OnUserAchievementStored );

			_engineService = engineService;

			_achievements = new ConcurrentDictionary<string, SteamAchievementInfo>();
			_stats = new ConcurrentDictionary<string, SteamStatData>();
			_dirtyStats = new HashSet<string>();

			_category = logger.CreateCategory( nameof( SteamStatsRepository ), LogLevel.Info, true );
			_userData = userData ?? throw new ArgumentNullException( nameof( userData ) );

			RequestStats();
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
				_userStatsReceived?.Dispose();
				_userStatsStored?.Dispose();
				_userStatsUnloaded?.Dispose();
				_userAchievementIconFetched?.Dispose();
				_userAchievementStored?.Dispose();

				_category?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		RequestStats
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool RequestStats()
		{
			_category.PrintDebug( $"RequestStats: requesting stats for user '{_userData.UserName}'." );

			SteamAPICall_t hCallback = SteamUserStats.RequestUserStats( _userData.UserID );
			if ( hCallback == SteamAPICall_t.Invalid ) {
				_category.PrintError( "RequestStats: Steam returned inavlid API call handle." );
				return false;
			}

			_category.PrintDebug( $"RequestStats: request submitted. Call={hCallback.m_SteamAPICall}" );
			return true;
		}

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
		public IAchievementInfo? GetAchievementInfo( InternString achievementId )
		{
			CheckAchievementReady( nameof( GetAchievementInfo ), achievementId );

			if ( !_achievements.TryGetValue( achievementId, out var achievementInfo ) ) {
				return null;
			}

			return achievementInfo;
		}

		/*
		===============
		SetAchievementProgress
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="achievementId"></param>
		/// <param name="progress"></param>
		public void SetAchievementProgress( InternString achievementId, float progress )
		{
			CheckAchievementReady( nameof( GetAchievementInfo ), achievementId );

			if ( !_achievements.TryGetValue( achievementId, out var info ) ) {
				return;
			}

			if ( !info.HasProgress ) {
				_category.PrintError( $"Achievement '{achievementId}' does not utilize a progress metric!" );
				return;
			}

			info.UpdateProgress( progress );
			bool success = SteamUserStats.IndicateAchievementProgress( achievementId, (uint)progress, (uint)info.MaxProgress );
			if ( !success ) {
				_category.PrintWarning( $"SetAchievementProgress: SteamUserStats.IndicateAchievementProgress failed for '{(string)achievementId}'." );
			}
			SetStatFloat( new InternString( info.StatId ), progress );

			if ( progress >= info.MaxProgress ) {
				UnlockAchievement( achievementId );
			}

			StoreStats();
			AchievementProgressChanged?.Invoke( achievementId, progress, info.MaxProgress );
		}

		/*
		===============
		UnlockAchievement
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="achievementId"></param>
		public void UnlockAchievement( InternString achievementId )
		{
			CheckAchievementReady( nameof( GetAchievementInfo ), achievementId );

			if ( !_achievements.ContainsKey( achievementId ) ) {
				_category.PrintError( $"UnlockAchievement: Achievement '{achievementId}' does not exist!" );
				return;
			}
			if ( SteamUserStats.SetAchievement( achievementId ) ) {
				StoreStats();
			} else {
				_category.PrintWarning( $"UnlockAchievement: SteamUserStats.SetAchievement(true) failed for '{(string)achievementId}'." );
			}
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
		public void LockAchievement( InternString achievementId )
		{
			CheckAchievementReady( nameof( GetAchievementInfo ), achievementId );

			if ( !_achievements.TryGetValue( achievementId, out SteamAchievementInfo info ) ) {
				_category.PrintError( $"LockAchievement: Achievement '{achievementId}' does not exist!" );
				return;
			}

			if ( SteamUserStats.ClearAchievement( achievementId ) ) {
				info.SetAchieved( false );
				StoreStats();
			} else {
				_category.PrintWarning( $"LockAchievement: SteamUserStats.ClearAchievement failed for '{(string)achievementId}'." );
			}
		}

		/*
		===============
		GetStatFloat
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statId"></param>
		/// <returns></returns>
		public float GetStatFloat( InternString statId )
		{
			CheckStatReady( nameof( GetStatFloat ), statId );

			if ( _stats.TryGetValue( statId, out SteamStatData stat ) && stat.IsFloat ) {
				if ( !stat.IsFloat ) {
					_category.PrintWarning( $"GetStatFloat: stat '{(string)statId}' was cached as an int." );
					return 0.0f;
				}
				return stat.Value.FloatValue;
			}

			bool success = SteamUserStats.GetStat( statId, out float value );
			if ( !success ) {
				_category.PrintWarning( $"GetStatFloat: SteamUserStats.GetStat failed for '{(string)statId}'" );
			}

			_stats[statId] = new SteamStatData {
				Name = new InternString( statId ),
				Value = new SteamStatData.Data { FloatValue = value },
				IsDirty = false,
				IsFloat = true
			};

			return value;
		}

		/*
		===============
		GetStatInt
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statId"></param>
		/// <returns></returns>
		public int GetStatInt( InternString statId )
		{
			CheckStatReady( nameof( GetStatInt ), statId );

			if ( _stats.TryGetValue( statId, out SteamStatData stat ) && !stat.IsFloat ) {
				if ( !stat.IsFloat ) {
					_category.PrintWarning( $"GetStatInt: stat '{(string)statId}' was cached as a float." );
					return 0;
				}
				return stat.Value.IntValue;
			}

			bool success = SteamUserStats.GetStat( statId, out int value );
			if ( !success ) {
				_category.PrintWarning( $"GetStatInt: SteamUserStats.GetStat failed for '{(string)statId}'" );
			}

			_stats[statId] = new SteamStatData {
				Name = new InternString( statId ),
				Value = new SteamStatData.Data { IntValue = value },
				IsDirty = false,
				IsFloat = false
			};

			return value;
		}

		/*
		===============
		SetStatFloat
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statId"></param>
		/// <param name="value"></param>
		public void SetStatFloat( InternString statId, float value )
		{
			CheckStatReady( nameof( SetStatFloat ), statId );

			_stats[statId] = new SteamStatData {
				Name = new InternString( statId ),
				Value = new SteamStatData.Data { FloatValue = value },
				IsDirty = true,
				IsFloat = true
			};

			lock ( _dirtyStats ) {
				_dirtyStats.Add( statId );
			}
		}

		/*
		===============
		SetStatInt
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="statId"></param>
		/// <param name="value"></param>
		public void SetStatInt( InternString statId, int value )
		{
			CheckStatReady( nameof( SetStatInt ), statId );

			_stats[statId] = new SteamStatData {
				Name = new InternString( statId ),
				Value = new SteamStatData.Data { IntValue = value },
				IsDirty = true,
				IsFloat = false
			};

			lock ( _dirtyStats ) {
				_dirtyStats.Add( statId );
			}
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
		public bool StoreStats()
		{
			bool anyFailed = false;

			// Write dirty stats
			lock ( _dirtyStats ) {
				if ( _storeInFlight ) {
					_storeAgainAfterCurrent = true;
					_category.PrintDebug( "StoreStats: store already in flight; scheduling another store." );
					return true;
				}

				_storeInFlight = true;

				// copy to avoid modification during iteration
				foreach ( var name in _dirtyStats.ToList() ) {
					if ( !_stats.TryGetValue( name, out SteamStatData stat ) ) {
						continue;
					}

					bool success;
					if ( stat.IsFloat ) {
						success = SteamUserStats.SetStat( name, stat.Value.FloatValue );
					} else {
						success = SteamUserStats.SetStat( name, stat.Value.IntValue );
					}

					if ( success ) {
						stat.IsDirty = false;
						_stats[name] = stat;
						_dirtyStats.Remove( name );
					} else {
						anyFailed = true;
						_category.PrintError( $"Failed to set stat '{name}'" );
					}
				}
			}

			// Upload all stats (Steam expects this after SetStat calls)
			bool submitted = SteamUserStats.StoreStats();
			if ( !submitted ) {
				lock ( _dirtyStats ) {
					_storeInFlight = false;
				}
			}
			return !anyFailed && submitted;
		}

		/*
		===============
		OnUserStatsUnloaded
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnUserStatsUnloaded( UserStatsUnloaded_t pCallback )
		{
		}

		/*
		===============
		OnUserStatsStored
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnUserStatsStored( UserStatsStored_t pCallback )
		{
			lock ( _dirtyStats ) {
				_storeInFlight = false;
			}

			if ( pCallback.m_eResult != EResult.k_EResultOK ) {
				_category.PrintError( $"OnUserStatsStored: Steam failed to store stats - {pCallback.m_eResult}." );
				return;
			}

			_category.PrintLine( "OnUserStatsStored: Steam confirmed stats were stored." );

			bool shouldStoreAgain = false;
			lock ( _dirtyStats ) {
				shouldStoreAgain = _storeAgainAfterCurrent;
				_storeAgainAfterCurrent = false;
			}

			if ( shouldStoreAgain ) {
				_category.PrintDebug( "OnUserStatsStored: submitting queued store request." );
				StoreStats();
			}
		}

		/*
		===============
		OnUserStatsReceived
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnUserStatsReceived( UserStatsReceived_t pCallback )
		{
			if ( pCallback.m_eResult != EResult.k_EResultOK ) {
				return;
			}

			_isReady = true;

			int numAchievements = (int)SteamUserStats.GetNumAchievements();
			for ( uint i = 0; i < numAchievements; i++ ) {
				string name = SteamUserStats.GetAchievementName( i );
				_achievements[name] = new SteamAchievementInfo( name );
			}

			StatsUpdated?.Invoke();
		}

		/*
		===============
		OnUserAchievementStored
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnUserAchievementStored( UserAchievementStored_t pCallback )
		{
			if ( !_achievements.TryGetValue( pCallback.m_rgchAchievementName, out SteamAchievementInfo info ) ) {
				return;
			}

			if ( pCallback.m_nCurProgress == pCallback.m_nMaxProgress ) {
				info.SetAchieved( true );
				AchievementUnlocked?.Invoke( new InternString( pCallback.m_rgchAchievementName ) );
			} else {
				AchievementProgressChanged?.Invoke(
					new InternString( pCallback.m_rgchAchievementName ),
					pCallback.m_nCurProgress,
					pCallback.m_nMaxProgress
				);
			}
		}

		/*
		===============
		OnUserAchievementIconFetched
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnUserAchievementIconFetched( UserAchievementIconFetched_t pCallback )
		{
			if ( !_achievements.TryGetValue( pCallback.m_rgchAchievementName, out var info ) ) {
				return;
			}
			info.SetIcon( pCallback.m_nIconHandle, _engineService );
		}

		/*
		===============
		CheckAchievementReady
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="achievementId"></param>
		private void CheckAchievementReady( string methodName, string achievementId )
		{
			if ( !_isReady ) {
				_category.PrintWarning( $"{methodName}: stats are not ready yet. Achievement='{achievementId}'." );
			}
		}

		/*
		===============
		CheckStatReady
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="statId"></param>
		private void CheckStatReady( string methodName, string statId )
		{
			if ( !_isReady ) {
				_category.PrintWarning( $"{methodName}: stats are not ready yet. Stat='{statId}'." );
			}
		}
	};
};
