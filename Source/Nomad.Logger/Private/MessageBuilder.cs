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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Nomad.Core.Logger;
using Nomad.Core.Memory;

namespace Nomad.Logger.Private
{
	/*
	===================================================================================

	MessageBuilder

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class MessageBuilder
	{
		private readonly BasicObjectPool<StringBuilder> _sb = new BasicObjectPool<StringBuilder>( CreateMessageBuffer );

		/*
		===============
		FormatMessage
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="category"></param>
		/// <param name="level"></param>
		/// <param name="message"></param>
		/// <param name="addLine"></param>
		/// <returns></returns>
		public string FormatMessage( ILoggerCategory category, LogLevel level, string message, bool addLine )
		{
			var sb = _sb.Rent();
			try {
				sb.Clear();
				sb.Append( FormatLogColorBegin( level ) );
				sb.Append( $"[{DateTime.Now}] [{category.Name}] " );
				sb.Append( message );
				sb.Append( FormatLogColorEnd( level ) );
				return sb.ToString();
			} finally {
				_sb.Return( sb );
			}
		}

		/*
		===============
		CreateMessageBuffer
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static StringBuilder CreateMessageBuffer()
		{
			return new StringBuilder( 1024 );
		}

		/*
		===============
		FormatLogColorBegin
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static string FormatLogColorBegin( LogLevel level ) => level switch {
			LogLevel.Info => string.Empty,
			LogLevel.Warning => "[color=gold]",
			LogLevel.Error => "[color=red]",
			LogLevel.Debug => "[color=light_blue]",
			_ => throw new ArgumentOutOfRangeException( nameof( level ) )
		};

		/*
		===============
		FormatLogColorEnd
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static string FormatLogColorEnd( LogLevel level ) => level switch {
			LogLevel.Info => String.Empty,
			LogLevel.Warning or LogLevel.Error or LogLevel.Debug => "[/color]",
			_ => throw new ArgumentOutOfRangeException( nameof( level ) )
		};
	};
};
