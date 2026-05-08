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
using System.Collections.Generic;
using Godot;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Engine.Globals;
using Nomad.Core.Engine.SceneManagement;
using Nomad.ResourceCache;

namespace Nomad.EngineUtils.Godot.Private.SceneManagement
{
	/*
	===================================================================================

	GodotSceneManager

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotSceneManager : ISceneManager
	{
		private const string MANAGED_SCENE_HOST_NAME = "PostProcessingContainer/PostProcessing/__NomadManagedSceneHost";

		/// <summary>
		/// The currently active runtime scene. At startup this is the persistent root scene.
		/// </summary>
		public IScene? ActiveScene {
			get => _activeScene;
			set => SetScene( _activeScene );
		}
		private GodotScene? _activeScene;

		/// <summary>
		/// The persistent startup scene. This is never unloaded by the scene manager.
		/// </summary>
		public IScene? RootScene => _baseScene;
		private readonly GodotScene _baseScene;

		/// <summary>
		/// Additively loaded scenes managed by this scene manager.
		/// </summary>
		public IReadOnlyList<IScene> LoadedScenes => _additiveScenes;
		private readonly List<IScene> _additiveScenes = new();

		private readonly IResourceCacheService<PackedScene, string> _sceneCache;
		private readonly SceneTree _sceneTree;
		private readonly Node _managedSceneHost;

		private readonly Dictionary<Node, GodotGameObject> _gameObjects = new();

		private bool _isDisposed = false;

		/*
		===============
		GodotSceneManager
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sceneTree"></param>
		/// <param name="sceneCache"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public GodotSceneManager( SceneTree sceneTree, IResourceCacheService<PackedScene, string> sceneCache )
		{
			_sceneTree = sceneTree ?? throw new ArgumentNullException( nameof( sceneTree ) );
			_sceneCache = sceneCache ?? throw new ArgumentNullException( nameof( sceneCache ) );

			if ( _sceneTree.CurrentScene == null ) {
				throw new InvalidOperationException(
					"GodotSceneManager requires SceneTree.CurrentScene to be set to the startup/base scene."
				);
			}

			var baseRootNode = _sceneTree.CurrentScene;

			_baseScene = new GodotScene( new GodotGameObject( baseRootNode ) );
			_activeScene = _baseScene;

			_gameObjects[baseRootNode] = (GodotGameObject)_baseScene.Root;
			WalkNode( baseRootNode );

			_managedSceneHost = EnsureManagedSceneHost( baseRootNode );

			// Keep Godot's current scene pointing at the persistent shell scene.
			_sceneTree.CurrentScene = baseRootNode;

			SceneManager.Initialize( this );
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
			if ( _isDisposed ) {
				return;
			}

			UnloadAllScenes();
			_sceneCache.Dispose();

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		LoadPrefab
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public IScene LoadPrefab( string path )
		{
			_sceneCache.GetCached( path ).Get( out var resource );
			return WalkSceneTree( resource );
		}

		/*
		===============
		SetScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="mode"></param>
		/// <param name="baseScene"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void SetScene( IScene scene, LoadSceneMode mode = LoadSceneMode.Single, IScene? baseScene = null )
		{
			ArgumentGuard.ThrowIfNull( scene );

			switch ( mode ) {
				case LoadSceneMode.Additive: {
						var parent = ResolveParentNode( scene );
						AttachScene( scene, parent );
						_additiveScenes.Add( scene );
						break;
					}
				case LoadSceneMode.Single:
					SetActiveScene( baseScene );
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof( mode ) );
			}
		}

		/*
		===============
		LoadScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="mode"></param>
		/// <param name="baseScene"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public IScene LoadScene( string path, LoadSceneMode mode = LoadSceneMode.Single, IScene? baseScene = null )
		{
			_sceneCache.GetCached( path ).Get( out var resource );
			var scene = WalkSceneTree( resource );

			switch ( mode ) {
				case LoadSceneMode.Additive: {
						var parent = ResolveParentNode( baseScene );
						AttachScene( scene, parent );
						_additiveScenes.Add( scene );
						break;
					}
				case LoadSceneMode.Single:
					SetActiveScene( scene );
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof( mode ) );
			}
			return scene;
		}

		/*
		===============
		SetActiveScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <exception cref="InvalidCastException"></exception>
		public void SetActiveScene( IScene scene )
		{
			ArgumentGuard.ThrowIfNull( scene );

			if ( scene is not GodotScene godotScene ) {
				throw new InvalidCastException();
			}

			// Never swap the shell scene itself.
			if ( ReferenceEquals( godotScene, _baseScene ) ) {
				_activeScene = _baseScene;
				return;
			}

			UnloadAllScenes();
			AttachScene( godotScene, _managedSceneHost );
			_activeScene = godotScene;
		}

		/*
		===============
		UnloadScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <exception cref="InvalidCastException"></exception>
		public void UnloadScene( IScene scene )
		{
			if ( scene is not GodotScene godotScene ) {
				throw new InvalidCastException();
			}

			// The persistent startup scene is not unloadable.
			if ( ReferenceEquals( godotScene, _baseScene ) ) {
				return;
			}
			if ( ReferenceEquals( godotScene, _activeScene ) ) {
				UnloadCurrentScene();
				return;
			}
			if ( _additiveScenes.Remove( godotScene ) ) {
				ReleaseSceneInstance( godotScene );
			}
		}

		/*
		===============
		UnloadAllScenes
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void UnloadAllScenes()
		{
			UnloadCurrentScene();

			for ( int i = _additiveScenes.Count - 1; i >= 0; i-- ) {
				ReleaseSceneInstance( _additiveScenes[i] );
			}
			_additiveScenes.Clear();
		}

		/*
		===============
		UnloadCurrentScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void UnloadCurrentScene()
		{
			if ( _activeScene == null || ReferenceEquals( _activeScene, _baseScene ) ) {
				return;
			}
			ReleaseSceneInstance( _activeScene );
			_activeScene = _baseScene;
		}

		/*
		===============
		WalkSceneTree
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="resource"></param>
		/// <returns></returns>
		private GodotScene WalkSceneTree( PackedScene resource )
		{
			var root = new GodotGameObject( resource.Instantiate() );
			_gameObjects[root.Node] = root;
			WalkNode( root.Node );

			return new GodotScene( root );
		}

		/*
		===============
		WalkNode
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="parentObject"></param>
		private void WalkNode( Node parentObject )
		{
			var children = parentObject.GetChildren();
			for ( int i = 0; i < children.Count; i++ ) {
				if ( children[i] is not Node childNode ) {
					continue;
				}

				if ( !_gameObjects.ContainsKey( childNode ) ) {
					_gameObjects[childNode] = new GodotGameObject( childNode );
				}

				WalkNode( childNode );
			}
		}

		/*
		===============
		EnsureManagedSceneHost
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseRoot"></param>
		/// <returns></returns>
		private static Node EnsureManagedSceneHost( Node baseRoot )
		{
			var existing = baseRoot.GetNodeOrNull<Node>( MANAGED_SCENE_HOST_NAME );
			if ( existing != null ) {
				return existing;
			}

			var host = new Node {
				Name = MANAGED_SCENE_HOST_NAME
			};

			baseRoot.AddChild( host );
			return host;
		}

		/*
		===============
		ResolveParentNode
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseScene"></param>
		/// <returns></returns>
		private Node ResolveParentNode( IScene? baseScene )
		{
			if ( baseScene is GodotScene godotScene && godotScene.Root is GodotGameObject godotObject ) {
				return godotObject.Node;
			}
			return _managedSceneHost;
		}

		/*
		===============
		AttachScene
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="parent"></param>
		private static void AttachScene( IScene scene, Node parent )
		{
			var node = ((GodotGameObject)scene.Root).Node;
			var currentParent = node.GetParent();

			if ( currentParent == parent ) {
				return;
			}

			currentParent?.RemoveChild( node );
			parent.AddChild( node );
		}

		/*
		===============
		ReleaseSceneInstance
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		private void ReleaseSceneInstance( IScene scene )
		{
			var node = ((GodotGameObject)scene.Root).Node;

			UnregisterNodeTree( node );

			var parent = node.GetParent();
			parent?.RemoveChild( node );
			node.QueueFree();

			_sceneCache.Unload( ((GodotScene)scene).Path );
		}

		/*
		===============
		UnregisterNodeTree
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="node"></param>
		private void UnregisterNodeTree( Node node )
		{
			var children = node.GetChildren();
			for ( int i = 0; i < children.Count; i++ ) {
				if ( children[i] is Node childNode ) {
					UnregisterNodeTree( childNode );
				}
			}
			_gameObjects.Remove( node );
		}
	};
};
