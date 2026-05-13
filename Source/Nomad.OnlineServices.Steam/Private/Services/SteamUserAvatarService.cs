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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Engine.Services;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Util;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	internal sealed class SteamUserAvatarService : IUserAvatarService
	{
		public bool SupportsAvatars => true;

		private readonly SteamAvatarImageHandleDispatcher _avatarImageLoaded;
		private readonly Func<PeerId, CSteamID?> _peerIdResolver;
		private readonly ILoggerCategory _category;
		private readonly IEngineService _engineService;

		public SteamUserAvatarService( IEngineService engineService, ILoggerCategory category, Func<PeerId, CSteamID?>? peerSteamIdResolver = null )
		{
			_engineService = engineService ?? throw new ArgumentNullException( nameof( engineService ) );
			_peerIdResolver = peerSteamIdResolver ?? throw new ArgumentNullException( nameof( peerSteamIdResolver ) );
			_category = category ?? throw new ArgumentNullException( nameof( category ) );

			_avatarImageLoaded = new SteamAvatarImageHandleDispatcher();
		}

		public void Dispose()
		{
			_avatarImageLoaded?.Dispose();
		}

		public async ValueTask<UserAvatarResult> QueryAvatarAsync( PeerId userId, AvatarSize size, CancellationToken ct = default )
		{
			int imageHandle = size switch {
				AvatarSize.Small => await _avatarImageLoaded.GetSmallFriendAvatarAsync( _peerIdResolver.Invoke( userId ).Value, ct ).ConfigureAwait( false ),
				AvatarSize.Medium => await _avatarImageLoaded.GetMediumFriendAvatarAsync( _peerIdResolver.Invoke( userId ).Value, ct ).ConfigureAwait( false ),
				AvatarSize.Large => await _avatarImageLoaded.GetLargeFriendAvatarAsync( _peerIdResolver.Invoke( userId ).Value, ct ).ConfigureAwait( false ),
				_ => throw new ArgumentOutOfRangeException( nameof( size ) )
			};
			if ( imageHandle == -1 ) {
				return UserAvatarResult.Failure();
			}

			SteamUtils.GetImageSize( imageHandle, out uint width, out uint height );
			int imageSize = checked((int)( width * height * 4 ));
			byte[] buffer = new byte[imageSize];
			SteamUtils.GetImageRGBA( imageHandle, buffer, imageSize );

			var image = _engineService.CreateImageRGBA( buffer, (int)width, (int)height );
			return UserAvatarResult.Loaded( AvatarStatus.Available, image );
		}
	};
};
