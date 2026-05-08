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

using Godot;
using Nomad.CVars;
using Nomad.Core.CVars;
using System.Collections.Generic;
using Nomad.Core.Util;
using Nomad.Core.Engine.Rendering;
using Nomad.Core.Engine.Windowing;
using Nomad.Core.Engine.Services;

namespace Nomad.EngineUtils.Godot.Private.Services
{
	/*
	===================================================================================
	
	GodotDisplayService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotDisplayService : IDisplayService
	{
		public bool SupportsBloom => true;
		public bool SupportsSMAA => true;
		public bool SupportsFXAA => true;
		public bool SupportsTAA => true;

		public Dictionary<string, Any> CustomSettings => _customSettings;
		private readonly Dictionary<string, Any> _customSettings = new();

		private readonly Rid _viewportRid;
		private readonly WorldEnvironment _world;
		private readonly Environment _environment;
		private readonly ICVarSystemService _cvarSystem;
		private readonly IWindowService _windowService;

		/*
		===============
		GodotDisplayService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sceneTree"></param>
		/// <param name="windowService"></param>
		/// <param name="cvarSystem"></param>
		public GodotDisplayService( SceneTree sceneTree, IWindowService windowService, ICVarSystemService cvarSystem )
		{
			_viewportRid = sceneTree.Root.GetViewportRid();
			_environment = new Environment() {
				GlowBlendMode = Environment.GlowBlendModeEnum.Additive,
				GlowEnabled = true
			};
			_world = new WorldEnvironment() {
				Environment = _environment
			};
			sceneTree.Root.CallDeferred( Node.MethodName.AddChild, _world );

			_cvarSystem = cvarSystem;
			_windowService = windowService;

			var windowMode = _cvarSystem.GetCVarOrThrow<WindowMode>( Core.Constants.CVars.EngineUtils.Display.WINDOW_MODE );
			windowMode.ValueChanged.Subscribe( OnWindowModeChanged );

			var monitorIndex = _cvarSystem.GetCVarOrThrow<int>( Core.Constants.CVars.EngineUtils.Display.MONITOR );
			monitorIndex.ValueChanged.Subscribe( OnMonitorIndexChanged );

			var maxFps = _cvarSystem.GetCVarOrThrow<int>( Core.Constants.CVars.EngineUtils.Display.MAX_FPS );
			maxFps.ValueChanged.Subscribe( OnMaxFpsChanged );

			var brightness = _cvarSystem.GetCVarOrThrow<float>( Core.Constants.CVars.EngineUtils.Display.BRIGHTNESS );
			brightness.ValueChanged.Subscribe( OnBrightnessChanged );
			_environment.AdjustmentBrightness = brightness.Value;

			var vsyncMode = _cvarSystem.GetCVarOrThrow<VSyncMode>( Core.Constants.CVars.EngineUtils.Display.VSYNC_MODE );
			vsyncMode.ValueChanged.Subscribe( OnVSyncModeChanged );

			var windowResolution = _cvarSystem.GetCVarOrThrow<WindowResolution>( Core.Constants.CVars.EngineUtils.Display.WINDOW_RESOLUTION );
			windowResolution.ValueChanged.Subscribe( OnWindowResolutionChanged );

			var antiAliasing = _cvarSystem.GetCVarOrThrow<AntiAliasingMode>( Core.Constants.CVars.EngineUtils.Display.ANTI_ALIASING );
			antiAliasing.ValueChanged.Subscribe( OnAntiAliasingChanged );

			var aspectRatio = _cvarSystem.GetCVarOrThrow<AspectRatio>( Core.Constants.CVars.EngineUtils.Display.ASPECT_RATIO );
			aspectRatio.ValueChanged.Subscribe( OnAspectRatioChanged );
		}

		/*
		===============
		OnBrightnessChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnBrightnessChanged( in CVarValueChangedEventArgs<float> args )
		{
			_environment.AdjustmentBrightness = args.NewValue;
		}

		/*
		===============
		OnWindowResolutionChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnWindowResolutionChanged( in CVarValueChangedEventArgs<WindowResolution> args )
		{
			var size = (WindowSize)args.NewValue;
			_windowService.Width = size.Width;
			_windowService.Height = size.Height;
		}

		/*
		===============
		OnMonitorIndexChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMonitorIndexChanged( in CVarValueChangedEventArgs<int> args )
		{
			_windowService.ScreenIndex = args.NewValue;
		}

		/*
		===============
		OnAspectRatioChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnAspectRatioChanged( in CVarValueChangedEventArgs<AspectRatio> args )
		{
			ProjectSettings.SetSetting( "display/window/stretch/scale", args.NewValue.GetRatio() );
		}

		/*
		===============
		OnWindowModeChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnWindowModeChanged( in CVarValueChangedEventArgs<WindowMode> args )
		{
			_windowService.Mode = args.NewValue;
		}

		/*
		===============
		OnMaxFpsChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMaxFpsChanged( in CVarValueChangedEventArgs<int> args )
		{
			global::Godot.Engine.MaxFps = args.NewValue;
		}

		/*
		===============
		OnVSyncModeChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnVSyncModeChanged( in CVarValueChangedEventArgs<VSyncMode> args )
		{
			var vsyncMode = DisplayServer.VSyncMode.Disabled;
			int swapChainImageCount = 2;
			switch ( args.NewValue ) {
				case VSyncMode.Adaptive:
					vsyncMode = DisplayServer.VSyncMode.Adaptive;
					break;
				case VSyncMode.Enabled:
					vsyncMode = DisplayServer.VSyncMode.Enabled;
					break;
				case VSyncMode.Disabled:
					vsyncMode = DisplayServer.VSyncMode.Disabled;
					break;
				case VSyncMode.TripleBuffered:
					vsyncMode = DisplayServer.VSyncMode.Adaptive;
					swapChainImageCount = 3;
					break;
			}
			DisplayServer.WindowSetVsyncMode( vsyncMode );
			ProjectSettings.SetSetting( "rendering/rendering_device/vsync/swap_chain_image_count", swapChainImageCount );
		}

		/*
		===============
		OnAntiAliasingChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnAntiAliasingChanged( in CVarValueChangedEventArgs<AntiAliasingMode> args )
		{
			switch ( args.NewValue ) {
				case AntiAliasingMode.None:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Disabled );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Disabled );
					RenderingServer.ViewportSetUseTaa( _viewportRid, false );
					break;
				case AntiAliasingMode.FXAA:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Disabled );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Fxaa );
					RenderingServer.ViewportSetUseTaa( _viewportRid, false );
					break;
				case AntiAliasingMode.SMAA:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Disabled );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Smaa );
					RenderingServer.ViewportSetUseTaa( _viewportRid, false );
					break;
				case AntiAliasingMode.TAA:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Disabled );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Disabled );
					RenderingServer.ViewportSetUseTaa( _viewportRid, true );
					break;
				case AntiAliasingMode.MSAA_2x:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Msaa2X );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Disabled );
					RenderingServer.ViewportSetUseTaa( _viewportRid, false );
					break;
				case AntiAliasingMode.MSAA_4x:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Msaa4X );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Disabled );
					RenderingServer.ViewportSetUseTaa( _viewportRid, false );
					break;
				case AntiAliasingMode.MSAA_8x:
					RenderingServer.ViewportSetMsaa2D( _viewportRid, RenderingServer.ViewportMsaa.Msaa8X );
					RenderingServer.ViewportSetScreenSpaceAA( _viewportRid, RenderingServer.ViewportScreenSpaceAA.Disabled );
					RenderingServer.ViewportSetUseTaa( _viewportRid, false );
					break;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		///
		/*
		private void OnShadowQualityChanged(in CVarValueChangedEventArgs<uint> args)
		{
			int shadowAtlasSize = 0;
			var shadowFilter = Light2D.ShadowFilterEnum.None;
			float shadowFilterSmooth = 0.0f;
			switch ((ShadowQuality)args.NewValue)
			{
				case ShadowQuality.Low:
					shadowAtlasSize = 1 * 1024;
					break;
				case ShadowQuality.Medium:
					shadowAtlasSize = 2 * 1024;
					shadowFilter = Light2D.ShadowFilterEnum.Pcf5;
					shadowFilterSmooth = 0.010f;
					break;
				case ShadowQuality.High:
					shadowAtlasSize = 4 * 1024;
					shadowFilter = Light2D.ShadowFilterEnum.Pcf13;
					shadowFilterSmooth = 0.10f;
					break;
				case ShadowQuality.VeryHigh:
					shadowAtlasSize = 8 * 1024;
					shadowFilter = Light2D.ShadowFilterEnum.Pcf13;
					shadowFilterSmooth = 0.20f;
					break;
			}

			RenderingServer.CanvasSetShadowTextureSize(shadowAtlasSize);

			var cvarAtlasSize = _cvarSystem.GetCVarOrThrow<int>(Constants.CVars.SHADOW_ATLAS_SIZE);
			cvarAtlasSize.Value = shadowAtlasSize;

			var cvarShadowFilterType = _cvarSystem.GetCVarOrThrow<int>(Constants.CVars.SHADOW_FILTER_TYPE);
			cvarShadowFilterType.Value = (int)shadowFilter;

			var cvarShadowFilterSmooth = _cvarSystem.GetCVarOrThrow<float>(Constants.CVars.SHADOW_FILTER_SMOOTH);
			cvarShadowFilterSmooth.Value = shadowFilterSmooth;
		}
		*/
	}
}
