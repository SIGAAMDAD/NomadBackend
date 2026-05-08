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
using Godot;
using Nomad.Core.Events;

namespace Nomad.EngineUtils.Private.Godot
{
	/*
	===================================================================================
	
	GodotPauseService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotPauseService : IGamePauseService
	{
		public bool IsPaused => _sceneTree.Paused;

		private readonly SceneTree _sceneTree;

		public IGameEvent<PauseStateChangedEventArgs> PauseStateChanged => _pauseStateChanged;
		private readonly IGameEvent<PauseStateChangedEventArgs> _pauseStateChanged;

		public GodotPauseService( SceneTree sceneTree, IGameEventRegistryService eventFactory )
		{
			_sceneTree = sceneTree;
			_pauseStateChanged = eventFactory.GetEvent<PauseStateChangedEventArgs>( PauseStateChangedEventArgs.Name, PauseStateChangedEventArgs.NameSpace );
		}

		/*
		===============
		Pause
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Pause()
		{
			SetPaused( true );
		}

		/*
		===============
		Resume
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Resume()
		{
			SetPaused( false );
		}

		/*
		===============
		SetPaused
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="paused"></param>
		public void SetPaused( bool paused )
		{
			if ( _sceneTree.Paused == paused ) {
				return;
			}
			_sceneTree.Paused = paused;
			_pauseStateChanged.Publish( new PauseStateChangedEventArgs( paused ) );
		}
	};
};
