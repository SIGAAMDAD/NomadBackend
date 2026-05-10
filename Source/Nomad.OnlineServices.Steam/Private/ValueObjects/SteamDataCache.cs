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
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	internal sealed record SteamDataCache
	{
		public CSteamID LocalUserId { get; init; } = CSteamID.Nil;

		public AppId_t AppId { get; init; } = AppId_t.Invalid;
		public int AppBuildId { get; init; } = 0;

		public CSteamID AppOwnerId { get; init; } = CSteamID.Nil;

		public string InitialPersonaName { get; init; } = string.Empty;
		public string CurrentGameLanguage { get; init; } = string.Empty;
		public string AvailableGameLanguages { get; init; } = string.Empty;

		public bool IsSteamDeck { get; init; } = false;
		public EUniverse Universe { get; init; } = EUniverse.k_EUniverseInvalid;

		public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
	};
};
