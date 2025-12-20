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
	public unsafe class NativeArray<T> : IDisposable where T : unmanaged {
		public int Length => _length;
		private readonly int _length;
		
		private readonly T *_data;

		public ref T this[ int index ] {
			get {
#if DEBUG
				if ( index < 0 || index >= _length ) {
					throw new IndexOutOfRangeException();
				}
#endif
				return ref _data[ index ];
			}
		}

		public NativeArray( int length ) {
			_length = length;

			// assume a cacheline of 64
			_data = (T *)NativeMemory.AlignedAlloc( (nuint)( _length * Marshal.SizeOf<T>() ), 64 );
		}

		public void Dispose() {
			if ( _data != null ) {
				NativeMemory.AlignedFree( _data );
			}
			GC.SuppressFinalize( this );
		}
	};
};