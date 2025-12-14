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

using NomadCore.GameServices;
using NomadCore.Interfaces.SaveSystem;
using NomadCore.Systems.SaveSystem.Infrastructure.Fields;
using NomadCore.Systems.SaveSystem.Infrastructure.Serialization.Streams;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Sections {
	/*
	===================================================================================
	
	SectionWriter
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class SectionWriter( ISaveFileStream fileStream, ILoggerService logger, string name ) : ISectionSerializer {
		public string Name => name;

		public int FieldCount => _fields.Count;

		public HashSet<string> Fields => _fields;
		private readonly HashSet<string> _fields = new HashSet<string>();

		private readonly ILoggerService? _logger = logger;
		private readonly SaveStreamWriter _writer = fileStream as SaveStreamWriter ?? throw new Exception( "ISaveFileStream must be writable!" );

		/*
		===============
		FieldExists
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool FieldExists( string name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			return _fields.Contains( name );
		}

		/*
		===============
		SetField
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SetField<T>( string name, T value ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			if ( !_fields.Contains( name ) ) {
				SaveField.Write( Name, name, value, _writer );
			} else {
				_logger?.PrintWarning( $"SectionWriter.SetField: field '{name}' already exists!" );
			}
		}
	};
};