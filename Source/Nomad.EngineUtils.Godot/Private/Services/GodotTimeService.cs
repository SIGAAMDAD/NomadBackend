/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.Engine.Services;

namespace Nomad.EngineUtils.Private.Godot
{
	/*
	===================================================================================
	
	GodotTimeService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotTimeService : ITimeService
	{
		public float TimeScale => _timeScale;
		private float _timeScale;

		public GodotTimeService()
		{
			_timeScale = (float)global::Godot.Engine.TimeScale;
		}

		/*
		===============
		SetTimeScale
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="timeScale"></param>
		public void SetTimeScale( float timeScale )
		{
			if ( _timeScale == timeScale ) {
				return;
			}
			_timeScale = timeScale;
			global::Godot.Engine.TimeScale = timeScale;
		}
	};
};
