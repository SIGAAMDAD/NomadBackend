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

namespace NomadCore.Infrastructure {
	/*
	===================================================================================

	StructPool

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	/// <param name="createObject"></param>
	/// <param name="initialSize"></param>
	/// <param name="maxSize"></param>

	public sealed class StructPool<T> : IDisposable where T : struct {
		public int AvailableCount => AvailableObjects.Count;

		public int TotalCount => _currentSize;
		private int _currentSize;

		public int ActiveObjectCount => _currentSize - AvailableObjects.Count;

		private readonly ConcurrentBag<T> AvailableObjects = new ConcurrentBag<T>();
		private readonly Func<T> CreateObject;

		private readonly int MaxSize;
		private bool IsDisposed;

		public StructPool( Func<T> createObject, int initialSize = 32, int maxSize = int.MaxValue ) {
			AvailableObjects = new ConcurrentBag<T>();
			CreateObject = createObject ?? throw new ArgumentNullException( nameof( createObject ) );
			MaxSize = maxSize;

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
			ObjectDisposedException.ThrowIf( IsDisposed, this );

			if ( AvailableObjects.TryTake( out T obj ) ) {
				return obj;
			}

			if ( _currentSize < MaxSize ) {
				_currentSize++;
				return CreateObject.Invoke();
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
		public void Return( in T obj ) {
			if ( IsDisposed ) {
				return;
			}

			if ( AvailableObjects.Count < MaxSize ) {
				AvailableObjects.Add( obj );
			} else {
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
			if ( IsDisposed ) {
				return;
			}

			IsDisposed = true;
			AvailableObjects.Clear();
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
			for ( int i = 0; i < initialSize && _currentSize < MaxSize; i++ ) {
				AvailableObjects.Add( CreateObject.Invoke() );
				_currentSize++;
			}
		}
	};
};