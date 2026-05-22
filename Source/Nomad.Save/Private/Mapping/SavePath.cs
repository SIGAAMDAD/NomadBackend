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

namespace Nomad.Save.Private.Mapping
{
	/*
	===================================================================================

	SavePath

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class SavePath
	{
		/*
		===============
		Combine
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string Combine( string prefix, string name )
		{
			return string.IsNullOrWhiteSpace( prefix )
				? name
				: $"{prefix}.{name}";
		}

		/*
		===============
		Index
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string Index( string prefix, int index )
		{
			return $"{prefix}[{index}]";
		}
	};
};
