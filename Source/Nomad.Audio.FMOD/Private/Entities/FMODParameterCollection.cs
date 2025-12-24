/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System.Collections.Generic;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Entities {
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
				_parameters[ new ParameterId( new( parameter.name ) ) ] = parameter.id;
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
		public float GetParameter( ParameterId id ) {
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
		public bool ParameterExists( ParameterId id ) {
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
		public void SetParameter( ParameterId id, float value ) {
			if ( _parameters.TryGetValue( id, out var parameter ) ) {
				_instance.setParameterByID( parameter, value );
			}
		}
	};
};
