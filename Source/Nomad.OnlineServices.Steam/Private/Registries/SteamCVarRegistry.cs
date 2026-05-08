/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.CVars;

namespace Nomad.OnlineServices.Steam.Private.Registries
{
	/*
	===================================================================================

	SteamCVarRegistry

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class SteamCVarRegistry
	{
		/*
		===============
		RegisterCVars
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="cvarSystem"></param>
		public static void RegisterCVars( ICVarSystemService cvarSystem )
		{
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.LOBBY_MAX_CLIENTS,
					DefaultValue = Constants.MAX_LOBBY_PLAYERS,
					Description = "The maximum number of clients allowed in a lobby at a time.",
					Flags = CVarFlags.Init | CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.LOBBY_PURGE_INTERVAL,
					DefaultValue = Constants.LOBBY_PURGE_TIME_SEC,
					Description = "The interval in seconds in which the lobby system waits for a lobby to ping back. If a lobby does not ping back within the given time, it is automatically removed from the cache.",
					Flags = CVarFlags.Init | CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.LOBBY_METADATA_FETCH_INTERVAL,
					DefaultValue = Constants.LOBBY_UPDATE_TIME_SEC,
					Description = "The interval in seconds in which the current lobby's local metadata will be updated from steam services.",
					Flags = CVarFlags.Init | CVarFlags.Archive
				}
			);
		}
	};
};
