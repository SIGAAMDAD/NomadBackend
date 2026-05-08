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

using System;
using Nomad.Core.Engine.SceneManagement;
using Nomad.Core.Scene.GameObjects;

namespace Nomad.EngineUtils.Godot.Private.SceneManagement
{
	/*
	===================================================================================
	
	GodotScene
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotScene : IScene
	{
		public string Path => _root.Node.SceneFilePath;

		/// <summary>
		///
		/// </summary>
		public string Name {
			get => _root.Name;
			set => _root.Name = value;
		}

		public IGameObject Root => _root;
		private readonly GodotGameObject _root;

		private bool _isDisposed = false;

		/*
		===============
		GodotScene
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="root"></param>
		public GodotScene( GodotGameObject root )
		{
			_root = root;
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_root?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		AddScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		public void AddScene( GodotGameObject scene )
		{
			_root.Node.AddChild( scene.Node );
		}
	};
};
