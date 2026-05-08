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

namespace Nomad.Save.Private
{
	internal static class Constants
	{
		internal static class CVars
		{
			public const string NAMESPACE = "Nomad.Save";

			public const string DATA_PATH = NAMESPACE + ".DataPath";
			public const string TEMP_DIRECTORY = NAMESPACE + ".TempDirectory";
			public const string BACKUP_DIRECTORY = NAMESPACE + ".BackupDirectory";
			public const string MAX_BACKUPS = NAMESPACE + ".MaxBackups";

			public const string VERIFY_AFTER_WRITE = NAMESPACE + ".VerifyAfterWrite";

			public const string AUTO_SAVE_ENABLED = NAMESPACE + ".AutoSaveEnabled";
			public const string AUTO_SAVE_INTERVAL = NAMESPACE + ".AutoSaveInterval";

			public const string COMPRESSION_ENABLED = NAMESPACE + ".CompressionEnabled";
			public const string COMPRESSION_LEVEL = NAMESPACE + ".CompressionLevel";

			public const string CHECKSUM_ENABLED = NAMESPACE + ".ChecksumEnabled";

			public const string DEBUG_LOGGING = NAMESPACE + ".DebugLogging";
			public const string LOG_SERIALIZATION_TREE = NAMESPACE + ".LogSerializationTree";
			public const string LOG_WRITE_TIMINGS = NAMESPACE + ".LogWriteTimings";

			public const string FORCE_FULL_VALIDATION = NAMESPACE + ".ForceFullValidation";

			public const string SIMULATE_CORRUPTION = NAMESPACE + ".SimulateCorruption";
			public const string SIMULATE_SLOW_DISK_MS = NAMESPACE + ".SimulateSlowDiskMS";
		};

		public const uint API_VERSION_MAJOR = 0;
		public const uint API_VERSION_MINOR = 1;
		public const uint API_VERSION_PATCH = 0;

		public const int SECTION_NAME_MAX_LENGTH = 128;
		public const int MAX_FIELD_NAME_LENGTH = 256;

		public const ulong HEADER_MAGIC = 0x5f3759df67217274;
	};
};
