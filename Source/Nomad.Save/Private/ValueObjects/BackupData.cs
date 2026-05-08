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

namespace Nomad.Save.Private.ValueObjects
{
	/*
	===================================================================================

	BackupData

	===================================================================================
	*/
	/// <summary>
	/// Describes a save backup file known to the save system.
	/// </summary>

	internal readonly struct BackupData : IEquatable<BackupData>
	{
		public static readonly BackupData Empty = new BackupData(
			sourcePath: string.Empty,
			backupPath: string.Empty,
			saveName: string.Empty,
			createdUtc: default,
			sizeBytes: 0,
			order: 0
		);

		public string SourcePath { get; }
		public string BackupPath { get; }
		public string SaveName { get; }
		public DateTime CreatedUtc { get; }
		public long SizeBytes { get; }
		public int Order { get; }

		public bool IsValid => !string.IsNullOrWhiteSpace( BackupPath ) && !string.IsNullOrWhiteSpace( SaveName );

		public BackupData(
			string sourcePath,
			string backupPath,
			string saveName,
			DateTime createdUtc,
			long sizeBytes,
			int order
		)
		{
			SourcePath = sourcePath ?? string.Empty;
			BackupPath = backupPath ?? string.Empty;
			SaveName = saveName ?? string.Empty;
			CreatedUtc = createdUtc;
			SizeBytes = sizeBytes;
			Order = order;
		}

		public bool Equals( BackupData other )
		{
			return BackupPath.Equals( other.BackupPath, StringComparison.OrdinalIgnoreCase )
				&& SaveName.Equals( other.SaveName, StringComparison.Ordinal )
				&& CreatedUtc.Equals( other.CreatedUtc )
				&& Order == other.Order;
		}

		public override bool Equals( object? obj )
		{
			return obj is BackupData other && Equals( other );
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				BackupPath.ToUpperInvariant(),
				SaveName,
				CreatedUtc,
				Order
			);
		}

		public static bool operator ==( BackupData left, BackupData right )
			=> left.Equals( right );

		public static bool operator !=( BackupData left, BackupData right )
			=> !left.Equals( right );
	};
};
