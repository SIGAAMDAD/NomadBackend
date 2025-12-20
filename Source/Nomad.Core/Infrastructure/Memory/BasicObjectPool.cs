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

namespace NomadCore.Infrastructure.Memory {
	/*
	===================================================================================

	BasicObjectPool

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public class BasicObjectPool<T> : IObjectPool<T> where T : new() {
		public int AvailableCount => _availableObjects.Count;

		public int TotalCount => _currentSize;
		private int _currentSize = 0;

		public int ActiveObjectCount => _currentSize - _availableObjects.Count;

		private readonly ConcurrentBag<T> _availableObjects = new ConcurrentBag<T>();
		private readonly Func<T> _createObject;

		private readonly int _maxSize = 0;
		private bool _isDisposed = false;

		/*
		===============
		BasicObjectPool
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="createObject"></param>
		/// <param name="initialSize"></param>
		/// <param name="maxSize"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public BasicObjectPool( Func<T> createObject, int initialSize = 32, int maxSize = int.MaxValue ) {
			_availableObjects = new ConcurrentBag<T>();
			_createObject = createObject ?? throw new ArgumentNullException( nameof( createObject ) );
			_maxSize = maxSize;

			InitializePool( initialSize );
		}

		/*
		===============
		Rent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public T Rent() {
			ObjectDisposedException.ThrowIf( _isDisposed, this );

			if ( _availableObjects.TryTake( out T? obj ) ) {
				ArgumentNullException.ThrowIfNull( obj );
				return obj;
			}

			if ( _currentSize < _maxSize ) {
				_currentSize++;
				return _createObject.Invoke();
			}
			throw new InvalidOperationException( "Object pool exhausted and maximum size reached" );
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
		public void Return( T obj ) {
			if ( _isDisposed ) {
				if ( obj is IDisposable disposable ) {
					disposable.Dispose();
				}
				return;
			}

			ArgumentNullException.ThrowIfNull( obj );

			if ( _availableObjects.Count < _maxSize ) {
				_availableObjects.Add( obj );
			} else {
				if ( obj is IDisposable disposable ) {
					disposable.Dispose();
				}
				_currentSize--;
			}
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
			if ( _isDisposed ) {
				return;
			}

			_isDisposed = true;
			while ( _availableObjects.TryTake( out T? obj ) ) {
				ArgumentNullException.ThrowIfNull( obj );
				if ( obj is IDisposable disposable ) {
					disposable.Dispose();
				}
			}
			_currentSize = 0;
		}

		/*
		===============
		InitializePool
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="initialSize"></param>
		private void InitializePool( int initialSize ) {
			for ( int i = 0; i < initialSize && _currentSize < _maxSize; i++ ) {
				_availableObjects.Add( _createObject.Invoke() );
				_currentSize++;
			}
		}
	};
};