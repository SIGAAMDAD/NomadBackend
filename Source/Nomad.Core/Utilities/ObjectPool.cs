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

using System;
using System.Collections.Concurrent;

namespace NomadCore.Utilities {
	/*
	===================================================================================

	ObjectPool

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public class ObjectPool<T> : IDisposable where T : IDisposable, new() {
		private readonly ConcurrentBag<T> _pool = new ConcurrentBag<T>();

		/*
		===============
		Rent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public T Rent() {
			if ( _pool.TryTake( out var value ) ) {
				return value;
			}
			return new T();
		}

		/*
		===============
		Return
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public void Return( in T obj ) {
			_pool.Add( obj );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			foreach ( var value in _pool ) {
				value.Dispose();
			}
			_pool.Clear();
		}
	};
};