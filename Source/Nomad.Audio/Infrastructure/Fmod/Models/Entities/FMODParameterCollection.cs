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

using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities {
	/*
	===================================================================================
	
	FMODParameterCollection
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class FMODParameterCollection : IParameterCollection {
		public int ParameterCount => _parameters.Count;

		private readonly Dictionary<ParameterId, FMOD.Studio.PARAMETER_ID> _parameters;
		private readonly FMOD.Studio.EventInstance _instance;
		
		public FMODParameterCollection( FMOD.Studio.EventDescription owner, FMOD.Studio.EventInstance instance ) {
			_instance = instance;

			FMODValidator.ValidateCall( owner.getParameterDescriptionCount( out int parameterCount ) );

			_parameters = new Dictionary<ParameterId, FMOD.Studio.PARAMETER_ID>( parameterCount );
			for ( int i = 0; i < parameterCount; i++ ) {
				FMODValidator.ValidateCall( owner.getParameterDescriptionByIndex( i, out var parameter ) );
				_parameters[ new ParameterId( StringPool.Intern( parameter.name ) ) ] = parameter.id;
			}
		}

		/*
		===============
		GetParameter
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public float GetParameter( in ParameterId id ) {
			_instance.getParameterByName( id.Name, out float value );
			return value;
		}

		/*
		===============
		ParameterExists
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool ParameterExists( in ParameterId id ) {
			return _parameters.ContainsKey( id );
		}

		/*
		===============
		SetParameter
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="value"></param>
		public void SetParameter( in ParameterId id, float value ) {
			if ( _parameters.TryGetValue( id, out var parameter ) ) {
				_instance.setParameterByID( parameter, value );
			}
		}
	};
};