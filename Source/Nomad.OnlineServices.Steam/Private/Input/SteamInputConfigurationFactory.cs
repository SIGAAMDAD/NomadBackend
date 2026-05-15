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
using System.IO;
using Nomad.Core.Util;
using Nomad.OnlineServices.Steam.ValueObjects;

namespace Nomad.OnlineServices.Steam.Private.Input
{
	internal static class SteamInputConfigurationFactory
	{
		public static SteamInputConfiguration CreateDefault()
		{
			string manifestPath = Path.Combine( AppContext.BaseDirectory, "steam_input_manifest.vdf" );

			return new SteamInputConfiguration(
				manifestPath: manifestPath,
				defaultActionSet: "menu",
				digitalBindings: new SteamInputDigitalBinding[] {
					new SteamInputDigitalBinding( new InternString( "ui.accept" ), "menu_accept" ),
					new SteamInputDigitalBinding( new InternString( "ui.cancel" ), "menu_cancel" ),
					new SteamInputDigitalBinding( new InternString( "ui.pause" ), "game_pause" ),
				},
				floatBindings: Array.Empty<SteamInputFloatBinding>(),
				axisBindings: new SteamInputAxisBinding[] {
					new SteamInputAxisBinding( new InternString( "ui.navigate" ), "menu_navigate" ),
					new SteamInputAxisBinding( new InternString( "player.move" ), "game_move" ),
					new SteamInputAxisBinding( new InternString( "player.look" ), "game_look" )
				},
				explicitRunFrame: false
			);
		}
	};
};
