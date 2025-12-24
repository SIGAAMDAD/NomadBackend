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

using Godot;
using System;

namespace Nomad.Events.Private {
	/*
	===================================================================================

	GodotSubscription

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	public class GodotSubscription : IDisposable {
		private readonly GodotObject _owner;
		private readonly StringName _signalName;
		private readonly Callable _callback;

		public GodotSubscription( GodotObject owner, StringName signalName, Callable callback ) {
			_owner = owner;
			_signalName = signalName;
			_callback = callback;

			owner.Connect( signalName, callback );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _owner.IsConnected( _signalName, _callback ) ) {
				_owner.Disconnect( _signalName, _callback );
			}
			GC.SuppressFinalize( this );
		}
	};
};
