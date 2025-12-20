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
using System.Runtime.InteropServices;

namespace NomadCore.Infrastructure.Collections {
	public unsafe struct NativeList<T> : IDisposable where T : unmanaged {
		public int Count => _length;
		private int _length = 0;
		
		public int Capacity => _capacity;
		private int _capacity = 0;

		private readonly T *_data;

		public NativeList( int size ) {
			_data = (T *)NativeMemory.AlignedAlloc( (nuint)( size * Marshal.SizeOf<T>() ), 64 );
			_length = size;
			_capacity = size;
		}
		
		public void Dispose() {
			if ( _data != null ) {
				NativeMemory.AlignedFree( _data );
			}
		}

		private void CheckCapacity( int newCapacity ) {
			
		}
	};
};