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
using Nomad.Core.Engine.Services;

namespace Nomad.Save.Private.Registries
{
	/*
	===================================================================================

	SaveCVarRegistry

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class SaveCVarRegistry
	{
		/*
		===============
		RegisterCVars
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="engineService"></param>
		/// <param name="cvarSystem"></param>
		public static void RegisterCVars( IEngineService engineService, ICVarSystemService cvarSystem )
		{
			cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = Constants.CVars.DATA_PATH,
					DefaultValue = $"{engineService.GetStoragePath( StorageScope.UserData )}/SaveData",
					Description = "The directory where all save data is written to.",
					Flags = CVarFlags.Init | CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = Constants.CVars.BACKUP_DIRECTORY,
					DefaultValue = $"{engineService.GetStoragePath( StorageScope.UserData )}/SaveData/Backups",
					Description = "The directory where all save data is written to.",
					Flags = CVarFlags.Init | CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.MAX_BACKUPS,
					DefaultValue = 3,
					Description = "The maximum number of automatically created backups that are allowed to exist per save file.",
					Flags = CVarFlags.Init | CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.AUTO_SAVE_ENABLED,
					DefaultValue = true,
					Description = "Enables auto-saving features.",
					Flags = CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.AUTO_SAVE_INTERVAL,
					DefaultValue = 5,
					Description = "The interval at which an auto-save is triggered. The value is represented in minutes.",
					Flags = CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.CHECKSUM_ENABLED,
					DefaultValue = true,
					Description = "Toggles save file checksums for corruption/hash validation.",
					Flags = CVarFlags.Init | CVarFlags.Developer
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.VERIFY_AFTER_WRITE,
					DefaultValue = false,
					Description = "Enables reading back the save data to validate it after writing state."
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.DEBUG_LOGGING,
#if DEBUG
					DefaultValue = true,
#else
					DefaultValue = false,
#endif
					Description = "Enables debug logging within the save system.",
					Flags = CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.LOG_SERIALIZATION_TREE,
#if DEBUG
					DefaultValue = true,
#else
					DefaultValue = false,
#endif
					Description = "Dumps the entire section/field tree to the logger.",
					Flags = CVarFlags.Archive
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.LOG_WRITE_TIMINGS,
#if DEBUG
					DefaultValue = true,
#else
					DefaultValue = false,
#endif
					Description = "Records and prints the amount of time it takes to complete write operations within the save system.",
					Flags = CVarFlags.Archive | CVarFlags.Developer
				}
			);
		}
	};
};
