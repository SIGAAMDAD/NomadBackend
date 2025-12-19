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
using System.Collections.Generic;

namespace NomadCore.Infrastructure.Memory {
	/*
	===================================================================================
	
	UnlockedObjectPool
	
	===================================================================================
	*/
	/// <summary>
	/// A version of the ObjectPool class that doesn't contain any thread synchronization. This is strictly meant for single-threaded optimization.
	/// </summary>
	
	public sealed class UnlockedObjectPool<T, TFactory> : IObjectPool<T> where T : class, IDisposable, new() {
		public int TotalCount => _currentSize;
		private int _currentSize = 0;
		
		public int ActiveObjectCount => _currentSize - _pool.Count;

		private readonly List<T> _pool = new List<T>();

		private readonly int _maxSize = int.MaxValue;
		private bool _isDisposed = false;

		public UnlockedObjectPool( int initialCapacity = 32, int maxCapacity = int.MaxValue ) {
			_maxSize = maxCapacity;
			_pool = new List<T>( initialCapacity );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			for ( int i = 0; i < _currentSize; i++ ) {
				_pool[ i ].Dispose();
			}
		}

		/*
		===============
		Rent
		===============
		*/
		public T Rent() {
			if ( _pool.Count > 0 ) {
				T value = _pool[ _pool.Count - 1 ];
				_pool.RemoveAt( _pool.Count - 1 );
				return value;
			}
			return new T();
		}

		/*
		===============
		Return
		===============
		*/
		public void Return( T value ) {
			_pool.Add( value );
		}
	};
};