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
using Nomad.Core.Logger;

namespace Nomad.Audio.Fmod
{
	/*
	===================================================================================
	
	FMODValidator
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal static class FMODValidator
	{
		private static ILoggerService? _logger;

		/*
		===============
		Initialize
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger"></param>
		public static void Initialize( ILoggerService logger )
		{
			_logger = logger;
		}

		/*
		===============
		ValidateCall
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		/// <exception cref="FMODException"></exception>
		public static void ValidateCall( FMOD.RESULT result )
		{
			if ( result != FMOD.RESULT.OK ) {
				_logger?.PrintError( $"FMODValidator.ValidateCall: FMOD Error - '{FMOD.Error.String( result )}'" );
				throw new FMODException( FMOD.Error.String( result ) );
			}
		}

		/*
		===============
		ValidateCall
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="category"></param>
		/// <param name="result"></param>
		/// <exception cref="FMODException"></exception>
		public static void ValidateCall( ILoggerCategory category, FMOD.RESULT result )
		{
			if ( result != FMOD.RESULT.OK ) {
				category.PrintLine( $"FMODValidator.ValidateCall: FMOD Error - '{FMOD.Error.String( result )}'" );
				throw new FMODException( FMOD.Error.String( result ) );
			}
		}
	};
};
