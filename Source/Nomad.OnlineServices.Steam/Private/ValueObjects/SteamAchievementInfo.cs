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
using System.Buffers;
using Nomad.Core.Engine.Services;
using Nomad.Core.OnlineServices;
using Nomad.Core.Util;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	/*
	===================================================================================

	SteamAchievementInfo

	===================================================================================
	*/
	/// <summary>
	/// The Steam achievement information value object.
	/// </summary>

	internal sealed class SteamAchievementInfo : IAchievementInfo
	{
		private sealed record AchievementProgress
		{
			public InternString StatId { get; init; }
			public float Progress { get; set; }
			public float MinProgress { get; set; }
			public float MaxProgress { get; set; }
		};

		public bool Achieved => _achieved;
		private bool _achieved = false;

		public string Id => _name;
		private readonly InternString _name;

		public DateTime UnlockedTime => _unlockedTime;
		private readonly DateTime _unlockedTime;

		public bool HasProgress => _progress != null;
		public string StatId => _progress != null ? _progress.StatId : InternString.Empty;
		public float Progress => _progress != null ? _progress.Progress : 0.0f;
		public float MinProgress => _progress != null ? _progress.MinProgress : 0.0f;
		public float MaxProgress => _progress != null ? _progress.MaxProgress : 0.0f;
		private AchievementProgress? _progress = null;

		private IDisposable? _icon = null;

		/*
		===============
		SteamAchievementInfo
		===============
		*/
		/// <summary>
		/// Creates a new SteamAchievementInfo instance.
		/// </summary>
		/// <param name="name">The name of the achievement.</param>
		public SteamAchievementInfo( string name )
		{
			_name = new InternString( name );
			if ( !SteamUserStats.GetAchievementAndUnlockTime( _name, out _achieved, out uint unlockedTime ) ) {
				return;
			} else {
				_unlockedTime = DateTimeOffset.FromUnixTimeSeconds( unlockedTime ).UtcDateTime;
			}

			if ( SteamUserStats.GetAchievementProgressLimits( _name, out float minProgress, out float maxProgress ) ) {
				SteamUserStats.GetAchievementAchievedPercent( _name, out float progress );
				string statId = SteamUserStats.GetAchievementDisplayAttribute( _name, "progress_stat" );
				_progress = new AchievementProgress {
					StatId = new InternString( statId ),
					MinProgress = minProgress,
					MaxProgress = maxProgress,
					Progress = progress
				};
			}

			// query the api for the achievement's icon
//			SteamUserStats.GetAchievementIcon( _name );
		}

		/*
		===============
		SetIcon
		===============
		*/
		/// <summary>
		/// Sets the achievement icon from the fetched data.
		/// </summary>
		/// <param name="hIconHandle">The achievement icon.</param>
		/// <param name="service"></param>
		public void SetIcon( int hIconHandle, IEngineService service )
		{
			SteamUtils.GetImageSize( hIconHandle, out uint width, out uint height );

			byte[] imageBuffer = ArrayPool<byte>.Shared.Rent( (int)( width * height * 4 ) );
			SteamUtils.GetImageRGBA( hIconHandle, imageBuffer, imageBuffer.Length );

			_icon = service.CreateImageRGBA( imageBuffer, (int)width, (int)height );

			ArrayPool<byte>.Shared.Return( imageBuffer );
		}

		/*
		===============
		SetAchieved
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="achieved"></param>
		/// <returns></returns>
		public void SetAchieved( bool achieved )
		{
			_achieved = achieved;
		}

		/*
		===============
		UpdateProgress
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="progress"></param>
		public void UpdateProgress( float progress )
		{
			_progress ??= new();
			_progress.Progress = progress;
		}
	};
};
