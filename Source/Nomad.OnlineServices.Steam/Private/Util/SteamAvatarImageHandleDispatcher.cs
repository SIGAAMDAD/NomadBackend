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
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Util
{
	internal sealed class SteamAvatarImageHandleDispatcher : IDisposable
	{
		private readonly SteamAsyncImageHandleDispatcher<AvatarImageLoaded_t, CSteamID> _dispatcher;

		public SteamAvatarImageHandleDispatcher()
		{
			_dispatcher = new SteamAsyncImageHandleDispatcher<AvatarImageLoaded_t, CSteamID>(
				callbackKeySelector: static callback => callback.m_steamID,
				callbackImageHandleSelector: static callback => callback.m_iImage
			);
		}

		public Task<int> GetSmallFriendAvatarAsync(
			CSteamID steamId,
			CancellationToken ct = default )
		{
			return _dispatcher.Invoke(
				key: steamId,
				steamCall: () => SteamFriends.GetSmallFriendAvatar( steamId ),

				// Valid Steam image handle.
				isReadyHandle: static handle => handle > 0,

				// Official docs for small avatars specify 0 as no avatar set.
				// Treat 0 as terminal so UI can show a default silhouette.
				isTerminalHandle: static handle => handle == 0,

				ct: ct
			);
		}

		public Task<int> GetMediumFriendAvatarAsync(
			CSteamID steamId,
			CancellationToken ct = default )
		{
			return _dispatcher.Invoke(
				key: steamId,
				steamCall: () => SteamFriends.GetMediumFriendAvatar( steamId ),
				isReadyHandle: static handle => handle > 0,
				isTerminalHandle: static handle => handle == 0,
				ct: ct
			);
		}

		public Task<int> GetLargeFriendAvatarAsync(
			CSteamID steamId,
			CancellationToken ct = default
		)
		{
			return _dispatcher.Invoke(
				key: steamId,
				steamCall: () => SteamFriends.GetLargeFriendAvatar( steamId ),
				isReadyHandle: static handle => handle > 0,

				// Large avatar docs explicitly say:
				//  0  = no avatar
				// -1  = not loaded yet, wait for AvatarImageLoaded_t
				isTerminalHandle: static handle => handle == 0,

				ct: ct
			);
		}

		public void Dispose()
		{
			_dispatcher.Dispose();
		}
	};
};
