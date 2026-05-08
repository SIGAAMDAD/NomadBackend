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
using Godot;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.ResourceCache;
using Nomad.ResourceCache;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.Engine.Services;
using Nomad.Core.Engine.SceneManagement;
using Nomad.Core.Engine.Globals;
using Nomad.EngineUtils.Godot.Private.SceneManagement;
using Nomad.Core.Physics.Services;
using Nomad.EngineUtils.Private.Godot;
using Nomad.EngineUtils.Godot.Private.Console;
using Nomad.Core.FileSystem;
using Nomad.Core.Engine.Windowing;

namespace Nomad.EngineUtils.Godot.Private.Services {
    /*
    ===================================================================================

    GodotEngineService

    ===================================================================================
    */
    /// <summary>
    ///
    /// </summary>

    internal sealed class GodotEngineService : IEngineService {
        private readonly SceneTree _sceneTree;
        private readonly Node _root;

        private readonly GodotLoader _loader;

        private readonly IWindowService _windowService;
        private readonly ILocalizationService _localizationService;
        private readonly IDisplayService _displayService;
        private readonly ISceneManager _sceneManager;

        private readonly GodotInputPump _inputPump;

        private readonly IServiceLocator _locator;

        private bool _isDisposed = false;

        /*
        ===============
        GodotEngineService
        ===============
        */
        /// <summary>
        ///
        /// </summary>
        /// <param name="sceneTree"></param>
        /// <param name="serviceFactory"></param>
        /// <param name="locator"></param>
        public GodotEngineService( SceneTree sceneTree, IServiceRegistry serviceFactory, IServiceLocator locator ) {
            ArgumentGuard.ThrowIfNull( serviceFactory );
            ArgumentGuard.ThrowIfNull( locator );

            _locator = locator ?? throw new ArgumentNullException( nameof( locator ) );
            _sceneTree = sceneTree ?? throw new ArgumentNullException( nameof( sceneTree ) );
            _root = sceneTree.Root;

            var logger = locator.GetService<ILoggerService>();
            var eventFactory = locator.GetService<IGameEventRegistryService>();
            var cvarSystem = locator.GetService<ICVarSystemService>();

            _loader = new GodotLoader();

            _inputPump = new GodotInputPump( eventFactory );
            _root.CallDeferred( Node.MethodName.AddChild, _inputPump );

            DisplayCVars.Register( cvarSystem );

            _windowService = new GodotWindowService( _sceneTree, cvarSystem, eventFactory );
            serviceFactory.AddSingleton( _windowService );
            _windowService.CloseRequested.Subscribe( OnWindowCloseRequested );

            _localizationService = new GodotLocalizationService();
            serviceFactory.AddSingleton( _localizationService );

            _displayService = new GodotDisplayService( sceneTree, _windowService, cvarSystem );
            serviceFactory.AddSingleton( _displayService );

            _sceneManager = new GodotSceneManager( _sceneTree, new BaseCache<PackedScene, string>( logger, eventFactory, _loader ) );
            serviceFactory.AddSingleton( _sceneManager );
            serviceFactory.AddSingleton<ISplitScreenService>( new GodotSplitScreenService() );

            serviceFactory.AddSingleton<IRaycastService>( new GodotRaycastService( _sceneTree.Root.World2D ) );

            logger.AddSink( new ConsoleSink() );

            serviceFactory.AddSingleton<ITimeService>( new GodotTimeService() );
            serviceFactory.AddSingleton<IGamePauseService>( new GodotPauseService( _sceneTree, eventFactory ) );

            EngineService.Initialize( this );
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose() {
            if ( !_isDisposed ) {
                _windowService?.Dispose();
                _sceneManager?.Dispose();
            }
            GC.SuppressFinalize( this );
            _isDisposed = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ospath"></param>
        /// <returns></returns>
        public string GetLocalPath( string ospath ) {
            return ProjectSettings.LocalizePath( ospath );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localpath"></param>
        /// <returns></returns>
        public string GetOSPath( string localpath ) {
            return ProjectSettings.GlobalizePath( localpath );
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IConsoleObject CreateConsoleObject() {
            var console = new GodotConsole(
				_root,
				_locator.GetService<ILoggerService>(),
				_locator.GetService<IGameEventRegistryService>(),
				_locator.GetService<IFileSystem>()
			);
            return console;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IResourceLoader GetResourceLoader() {
            return _loader;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public bool IsApplicationFocused() {
            return _windowService.IsFocused;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string GetApplicationVersion() {
            return ProjectSettings.GetSetting( "application/config/version" ).AsString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string GetEngineVersion() {
            var version = global::Godot.Engine.GetVersionInfo();
            return version["string"].AsString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="exitCode"></param>
        public void Quit( int exitCode = 0 ) {
            System.Environment.Exit( exitCode );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public string GetStoragePath( StorageScope scope ) => scope switch {
            StorageScope.Install => ProjectSettings.GlobalizePath( "res://" ),
            StorageScope.StreamingAssets => ProjectSettings.GlobalizePath( "res://Assets/" ),
            StorageScope.UserData => ProjectSettings.GlobalizePath( "user://" ),
            _ => ProjectSettings.GlobalizePath( "user://" )
        };

        /// <summary>
        ///
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public string GetStoragePath( string relativePath, StorageScope scope ) {
            return $"{GetStoragePath( scope )}{relativePath}";
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string GetSystemRegion() {
            return System.Globalization.CultureInfo.CurrentCulture.Name;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public IDisposable CreateImageRGBA( byte[] image, int width, int height ) {
            return Image.CreateFromData( width, height, false, Image.Format.Rgba8, image );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        private void OnWindowCloseRequested( in WindowCloseRequestedEventArgs args ) {
            Quit();
        }
    }
}
