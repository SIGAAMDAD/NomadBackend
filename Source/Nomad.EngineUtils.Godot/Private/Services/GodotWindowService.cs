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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Godot;
using Nomad.Core.Engine.Windowing;
using Nomad.Core.Engine.Globals;
using Nomad.Core.Engine.Services;
using Nomad.Core.Engine.Rendering;
using Nomad.Core.CVars;
using Nomad.CVars;

namespace Nomad.EngineUtils.Godot.Private.Services
{
	/*
	===================================================================================

	GodotWindowService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class GodotWindowService : IWindowService
	{
		private const string SWAPCHAIN_IMAGE_COUNT = "rendering/rendering_device/vsync/swapchain_image_count";

		/// <summary>
		///
		/// </summary>
		public string Title {
			get => _window.Title;
			set => _window.Title = value;
		}

		/// <summary>
		/// The window's physical dimensions.
		/// </summary>
		public WindowSize Size {
			get => new WindowSize( _window.Size.X, _window.Size.Y );
			set {

			}
		}

		/// <summary>
		///
		/// </summary>
		public int Width {
			get => _window.Size.X;
			set { }
		}

		/// <summary>
		///
		/// </summary>
		public int Height {
			get => _window.Size.Y;
			set { }
		}

		/// <summary>
		///
		/// </summary>
		public int ScreenIndex {
			get => _window.CurrentScreen;
			set {
				_window.CurrentScreen = value;
				var size = DisplayServer.ScreenGetSize( ScreenIndex );
				_screenWidth = size.X;
				_screenHeight = size.Y;
			}
		}

		/// <summary>
		///
		/// </summary>
		public VSyncMode VSyncMode {
			get => _vsyncMode;
			set {
				_vsyncMode = value;
				var mode = value switch {
					VSyncMode.Disabled => DisplayServer.VSyncMode.Disabled,
					VSyncMode.Enabled => DisplayServer.VSyncMode.Enabled,
					VSyncMode.Adaptive => DisplayServer.VSyncMode.Adaptive,
					VSyncMode.TripleBuffered => DisplayServer.VSyncMode.Enabled,
					_ => throw new ArgumentOutOfRangeException( nameof( value ) )
				};
				DisplayServer.WindowSetVsyncMode( mode );
				if ( value == VSyncMode.TripleBuffered ) {
					ProjectSettings.SetSetting( SWAPCHAIN_IMAGE_COUNT, Variant.From( 3 ) );
				} else {
					ProjectSettings.SetSetting( SWAPCHAIN_IMAGE_COUNT, Variant.From( 2 ) );
				}
			}
		}
		private VSyncMode _vsyncMode;

		/// <summary>
		///
		/// </summary>
		public int MaximumFramerate {
			get => _maximumFramerate;
			set {
				_maximumFramerate = value;
				global::Godot.Engine.MaxFps = value;
			}
		}
		private int _maximumFramerate = 60;

		/// <summary>
		///
		/// </summary>
		public float RefreshRate => _monitors[_window.CurrentScreen].RefreshRate;

		/// <summary>
		///
		/// </summary>
		public IReadOnlyList<Monitor> Monitors => _monitors;
		private readonly Monitor[] _monitors;

		/// <summary>
		///
		/// </summary>
		public WindowMode Mode {
			get => _mode;
			set => SetWindowMode( value );
		}
		private WindowMode _mode;

		/// <summary>
		///
		/// </summary>
		public bool IsFocused => _isFocused;
		private bool _isFocused = true;

		/// <summary>
		///
		/// </summary>
		public int MonitorIndex {
			get => _window.CurrentScreen;
			set {
				RangeGuard.ThrowIfOutOfRange( value, 0, _monitors.Length - 1 );
				_window.CurrentScreen = value;
			}
		}

		private int _screenWidth = 0;
		private int _screenHeight = 0;

		private readonly Window _window;

		private bool _isDisposed = false;

		/// <summary>
		///
		/// </summary>
		public IGameEvent<WindowSizeChangedEventArgs> SizeChanged => _sizeChanged;
		private readonly IGameEvent<WindowSizeChangedEventArgs> _sizeChanged;

		/// <summary>
		///
		/// </summary>
		public IGameEvent<WindowFocusChangedEventArgs> FocusChanged => _focusChanged;
		private readonly IGameEvent<WindowFocusChangedEventArgs> _focusChanged;

		/// <summary>
		///
		/// </summary>
		public IGameEvent<WindowCloseRequestedEventArgs> CloseRequested => _closeRequested;
		private readonly IGameEvent<WindowCloseRequestedEventArgs> _closeRequested;

		/*
		===============
		GodotWindowService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sceneTree"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="eventFactory"></param>
		internal GodotWindowService( SceneTree sceneTree, ICVarSystemService cvarSystem, IGameEventRegistryService eventFactory )
		{
			_window = sceneTree.Root;
			_window.FocusEntered += OnFocusEntered;
			_window.FocusExited += OnFocusExited;
			_window.SizeChanged += OnSizeChanged;
			_window.CloseRequested += OnCloseRequested;
			_window.TitleChanged += OnTitleChanged;

			_sizeChanged = eventFactory.GetEvent<WindowSizeChangedEventArgs>(
				WindowSizeChangedEventArgs.Name,
				WindowSizeChangedEventArgs.NameSpace
			);
			_closeRequested = eventFactory.GetEvent<WindowCloseRequestedEventArgs>(
				WindowCloseRequestedEventArgs.Name,
				WindowCloseRequestedEventArgs.NameSpace
			);
			_focusChanged = eventFactory.GetEvent<WindowFocusChangedEventArgs>(
				WindowFocusChangedEventArgs.Name,
				WindowFocusChangedEventArgs.NameSpace
			);

			_monitors = GetScreenList();

			// always ensure we stay in the top-left hand corner to reduce the chance of creating a window that's half off screen.
			_window.Position = Vector2I.Zero;
			_window.ContentScaleAspect = Window.ContentScaleAspectEnum.KeepHeight;
			_window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
			_window.ContentScaleSize = new Vector2I( 800, 600 );

			var maximumFramerate = cvarSystem.GetCVarOrThrow<int>( Core.Constants.CVars.EngineUtils.Display.MAX_FPS );
			MaximumFramerate = maximumFramerate.Value;
			maximumFramerate.ValueChanged.Subscribe( OnMaximumFramerateValueChanged );

			var windowMode = cvarSystem.GetCVarOrThrow<WindowMode>( Core.Constants.CVars.EngineUtils.Display.WINDOW_MODE );
			Mode = windowMode.Value;
			windowMode.ValueChanged.Subscribe( OnWindowModeValueChanged );

			var windowResolution = cvarSystem.GetCVarOrThrow<WindowResolution>( Core.Constants.CVars.EngineUtils.Display.WINDOW_RESOLUTION );
			var windowSize = (WindowSize)windowResolution.Value;
			Width = windowSize.Width;
			Height = windowSize.Height;
			windowResolution.ValueChanged.Subscribe( OnWindowResolutionValueChanged );

			var monitor = cvarSystem.GetCVarOrThrow<int>( Core.Constants.CVars.EngineUtils.Display.MONITOR );
			ScreenIndex = monitor.Value;
			monitor.ValueChanged.Subscribe( OnMonitorValueChanged );

			WindowService.Initialize( this );
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
				_sizeChanged?.Dispose();
				_closeRequested?.Dispose();
				_focusChanged?.Dispose();
				_window?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		GetScreenResolution
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void GetScreenResolution( out int width, out int height )
		{
			width = _screenWidth;
			height = _screenHeight;
		}

		/*
		===============
		GetSupportedResolutions
		===============
		*/
		/// <summary>
		/// Returns a list of monitor supported window resolutions.
		/// </summary>
		/// <param name="monitorIndex"></param>
		/// <returns></returns>
		public IReadOnlyList<WindowResolution> GetSupportedResolutions( int monitorIndex )
		{
			var monitor = _monitors[monitorIndex];
			var resolutions = new List<WindowResolution>();

			for ( var resolution = WindowResolution.Min; resolution <= WindowResolution.Max; resolution++ ) {
				if ( monitor.ScreenSize >= resolution ) {
					resolutions.Add( resolution );
				}
			}
			resolutions.Add( WindowResolution.Res_Native );

			return [.. resolutions];
		}

		/*
		===============
		GetNativeResolutionForMonitor
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="monitorIndex"></param>
		/// <param name="nativeSize"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void GetNativeResolutionForMonitor( int monitorIndex, out WindowSize nativeSize )
		{
			RangeGuard.ThrowIfOutOfRange( monitorIndex, 0, _monitors.Length - 1, nameof( monitorIndex ) );
			nativeSize = _monitors[monitorIndex].ScreenSize;
		}

		/*
		===============
		GetScreenList
		===============
		*/
		/// <summary>
		/// Gets a list of all the currently available monitors and fetches their sizes and other various data.
		/// </summary>u
		/// <returns></returns>
		private static Monitor[] GetScreenList()
		{
			int screenCount = DisplayServer.GetScreenCount();
			var screens = new Monitor[screenCount];

			for ( int i = 0; i < screenCount; i++ ) {
				var screenSize = DisplayServer.ScreenGetSize( i );
				var size = new WindowSize( screenSize.X, screenSize.Y );
				screens[i] = new Monitor( i, (int)DisplayServer.ScreenGetRefreshRate( i ), (WindowResolution)size );
			}

			return screens;
		}

		/*
		===============
		SetWindowMode
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="mode"></param>
		private void SetWindowMode( WindowMode mode )
		{
			bool borderless = false;
			var modeEnum = Window.ModeEnum.Windowed;
			switch ( mode ) {
				case WindowMode.Windowed:
					borderless = false;
					modeEnum = Window.ModeEnum.Windowed;
					break;
				case WindowMode.BorderlessWindowed:
					borderless = true;
					modeEnum = Window.ModeEnum.Windowed;
					break;
				case WindowMode.Fullscreen:
					borderless = false;
					modeEnum = Window.ModeEnum.Fullscreen;
					break;
				case WindowMode.BorderlessFullscreen:
					borderless = true;
					modeEnum = Window.ModeEnum.Fullscreen;
					break;
				case WindowMode.ExclusiveFullscreen:
					borderless = true;
					modeEnum = Window.ModeEnum.ExclusiveFullscreen;
					break;
			}
			_window.Borderless = borderless;
			_window.Mode = modeEnum;
			_mode = mode;
		}

		/*
		===============
		OnFocusEntered
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void OnFocusEntered()
		{
			_isFocused = true;
			_focusChanged.Publish( new WindowFocusChangedEventArgs( _isFocused ) );
		}

		/*
		===============
		OnFocusExited
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void OnFocusExited()
		{
			_isFocused = false;
			_focusChanged.Publish( new WindowFocusChangedEventArgs( _isFocused ) );
		}

		/*
		===============
		OnSizeChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void OnSizeChanged()
		{
			var size = _window.Size;
			_sizeChanged.Publish( new WindowSizeChangedEventArgs( size.X, size.Y ) );
		}

		/*
		===============
		OnCloseRequested
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void OnCloseRequested()
		{
			_closeRequested.Publish( default );
		}

		/*
		===============
		OnTitleChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void OnTitleChanged()
		{
		}

		/*
		===============
		OnWindowModeValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnWindowModeValueChanged( in CVarValueChangedEventArgs<WindowMode> args )
		{
			Mode = args.NewValue;
		}

		/*
		===============
		OnMaximumFramerateValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMaximumFramerateValueChanged( in CVarValueChangedEventArgs<int> args )
		{
			MaximumFramerate = args.NewValue;
		}

		/*
		===============
		OnWindowResolutionValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnWindowResolutionValueChanged( in CVarValueChangedEventArgs<WindowResolution> args )
		{
			var windowSize = (WindowSize)args.NewValue;
			SetWindowSize( windowSize );
		}

		/*
		===============
		OnMonitorValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMonitorValueChanged( in CVarValueChangedEventArgs<int> args )
		{
			ScreenIndex = args.NewValue;
		}

		private void SetWindowSize( WindowSize size )
		{
			if ( _mode == WindowMode.BorderlessFullscreen || _mode == WindowMode.Fullscreen || _mode == WindowMode.ExclusiveFullscreen ) {
				var baseSize = _window.ContentScaleSize;
				float scaleX = (float)baseSize.X / size.Width;
				float scaleY = (float)baseSize.Y / size.Height;
				_window.ContentScaleFactor = MathF.Min( scaleX, scaleY );

				var subviewport = _window.GetNode<SubViewport>( "ApplicationBootstrapper/PostProcessingContainer/PostProcessing" );
				subviewport.Size2DOverride = new Vector2I( size.Width, size.Height );
			} else {
				var windowSize = new Vector2I( size.Width, size.Height );
				_window.Size = windowSize;

				var subviewport = _window.GetNode<SubViewport>( "ApplicationBootstrapper/PostProcessingContainer/PostProcessing" );
				subviewport.Size2DOverride = new Vector2I( size.Width, size.Height );
			}

			_window.ContentScaleSize = new Vector2I( size.Width, size.Height );
		}
	};
};
