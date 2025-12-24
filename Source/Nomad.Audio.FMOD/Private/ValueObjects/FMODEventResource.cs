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

using System;

namespace Nomad.Audio.Fmod.Private.ValueObjects {
	/*
	===================================================================================

	FMODEventResource

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal readonly record struct FMODEventResource( FMOD.Studio.EventDescription Handle ) : IDisposable {
		public bool IsValid => Handle.isValid();

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			Unload();
		}

		/*
		===============
		Unload
		===============
		*/
		/// <summary>
		/// Unloads and deallocates all event instances bound to this event description.
		/// </summary>
		public void Unload() {
			if ( Handle.isValid() ) {
				FMODValidator.ValidateCall( Handle.unloadSampleData() );
				FMODValidator.ValidateCall( Handle.releaseAllInstances() );
				Handle.clearHandle();
			}
		}
	};
};
