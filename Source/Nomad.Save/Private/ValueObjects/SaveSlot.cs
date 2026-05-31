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

using System.Text;
using Nomad.Core.Util;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Private.ValueObjects
{
	/// <summary>
	///
	/// </summary>
	/// <param name="FileName"></param>
	/// <param name="Metadata"></param>
	internal record SaveSlot(
		string FileName,
		SaveFileMetadata Metadata
	)
	{
		public static string CalculateFileName( bool autoSave, SaveFileMetadata metadata )
		{
			StringBuilder sb = new StringBuilder( 256 );

			sb.AppendFormat( "{0}_{1}_{2}{3}.ngd",
				autoSave ? "AutoSave" : "Data",
				metadata.SaveName.HashFileName(),
				metadata.CreationTime.ToFileTimeUtc(),
				metadata.LastAccessTime.ToFileTimeUtc()
			);

			return sb.ToString();
		}
	}
};
