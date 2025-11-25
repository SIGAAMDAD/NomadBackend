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

using NomadCore.Systems.SaveSystem.Interfaces;
using NomadCore.Systems.SaveSystem.Infrastructure.Sections;
using System;
using System.Collections.Concurrent;

namespace NomadCore.Systems.SaveSystem.Infrastructure {
	/*
	===================================================================================
	
	Slot
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class Slot : ISaveSlot {
		public readonly string Filepath;
		
		public int Index => _index;
		private readonly int _index;

		public ISaveSection? this[ string name ] => GetSection( name );
		
		private readonly ConcurrentDictionary<string, ISaveSection> Sections = new ConcurrentDictionary<string, ISaveSection>();

		/*
		===============
		Slot
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		public Slot( int index ) {
			ArgumentOutOfRangeException.ThrowIfLessThan( index, 0, nameof( index ) );

			_index = index;
			Filepath = FilepathCache.GetSlotPath( index );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
		}

		/*
		===============
		GetSection
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ISaveSection? GetSection( string? name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			return Sections.TryGetValue( name, out var section ) ? section : null;
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Load() {
			using Streams.SaveReaderStream reader = new Streams.SaveReaderStream( Filepath );

			var header = SaveHeader.Load( reader );

			for ( int i = 0; i < header.SectionCount; i++ ) {
				var section = new SectionReader( reader );
				try {
					Sections.TryAdd( section.Name, section );
				} catch ( Exception e ) {
				}
			}
		}

		/*
		===============
		Save
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Save() {
			using Streams.SaveStreamWriter writer = new Streams.SaveStreamWriter( Filepath );
		}
	};
};