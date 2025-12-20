/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Nomad.Core.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Nomad.Core.Memory {
	/*
	===================================================================================
	
	StringPool
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public sealed class StringPool : IDisposable {
		private readonly Dictionary<string, int> _stringToIds = new Dictionary<string, int>( 2048 );
		private readonly Dictionary<int, string> _idToString = new Dictionary<int, string>( 2048 );

		[ThreadStatic]
		private static StringPool? _currentStringPool;

		private static StringPool _current => _currentStringPool ??= new();

		/*
		===============
		FromInterned
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <returns>Returns the interned string if it exists, null if not found.</returns>
		public static string? FromInterned( in InternString str ) {
			return _current._idToString.TryGetValue( str.GetHashCode(), out string? value ) ? value : null;
		}

		/*
		===============
		Intern
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static InternString Intern( ReadOnlySpan<char> str ) {
			if ( str.IsEmpty ) {
				return InternString.Empty;
			}
			ref int id = ref CollectionsMarshal.GetValueRefOrAddDefault( _current._stringToIds, new string( str ), out bool exists );
			if ( !exists ) {
				id = string.GetHashCode( str );
				_current._idToString[ id ] = new string( str );
			}
			return new InternString( id );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_stringToIds.Clear();
			_idToString.Clear();
			_currentStringPool = null;
		}
	};
};