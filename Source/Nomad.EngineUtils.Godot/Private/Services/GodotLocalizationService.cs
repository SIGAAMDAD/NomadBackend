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

using System.Collections.Concurrent;
using Godot;
using Nomad.Core.Engine.Globals;
using Nomad.Core.Engine.Services;
using Nomad.Core.Logger;
using Nomad.Core.Util;

namespace Nomad.EngineUtils.Godot.Private.Services
{
	/*
	===================================================================================
	
	GodotLocalizationService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotLocalizationService : ILocalizationService
	{
		/// <summary>
		///
		/// </summary>
		public string CurrentLanguage => System.Globalization.CultureInfo.CurrentCulture.Name;

		private readonly ConcurrentDictionary<InternString, string> _translations = new();

		/*
		===============
		GodotLocalizationService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public GodotLocalizationService()
		{
			TranslationServer.SetLocale( OS.GetLocale() );

			LocalizationService.Initialize( this );
		}

		/*
		===============
		Translate
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string Translate( InternString key )
		{
			return _translations.GetOrAdd( key, f => TranslationServer.Translate( (string)key ) );
		}
	}
}
