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

using Nomad.Core.CVars;

namespace Nomad.Input.Private.Registries
{
	internal static class InputCVarRegistry
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
				new CVarCreateInfo<string> {
					Name = Constants.CVars.DEFAULTS_PATH,
					DefaultValue = "Assets/Config/Bindings/DefaultConfig.json",
					Description = "The default configuration file path for the input subsystem.",
					Flags = CVarFlags.Init
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = Constants.CVars.KEYBOARD_MOUSE_MAPPING,
					DefaultValue = "KeyboardAndMouse",
					Description = "The active keyboard and mouse input binding configuration.",
					Flags = CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = Constants.CVars.GAMEPAD_MAPPING,
					DefaultValue = "Gamepad",
					Description = "The active gamepad input binding configuration.",
					Flags = CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.INPUT_DELAY_MS,
					DefaultValue = 0,
					Description = "The artifical delay time for simulating input lag.",
					Flags = CVarFlags.Archive
				}
			);
		}
	};
};
