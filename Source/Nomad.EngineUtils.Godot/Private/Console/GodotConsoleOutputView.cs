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
using System;

namespace Nomad.EngineUtils.Godot.Private
{
	/*
	===================================================================================

	GodotConsoleOutputView

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class GodotConsoleOutputView : IDisposable
	{
		private static readonly NodePath _valueNodePath = "value";

		public Control Node => _richLabel;
		private readonly RichTextLabel _richLabel = default;

		private bool _isDisposed = false;

		/*
		===============
		GodotConsoleOutputView
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public GodotConsoleOutputView()
		{
			_richLabel = new RichTextLabel() {
				Name = nameof( _richLabel ),
				SelectionEnabled = true,
				ContextMenuEnabled = true,
				BbcodeEnabled = true,
				ScrollFollowing = true,
				AnchorRight = 1.0f,
				AnchorBottom = 0.5f,
			};
			_richLabel.CallDeferred( RichTextLabel.MethodName.AddThemeStyleboxOverride, "normal", new StyleBoxFlat() { BgColor = new Color( 0.0f, 0.0f, 0.0f, 0.84f ) } );
			_richLabel.CallDeferred( RichTextLabel.MethodName.AddThemeFontOverride, "font", ResourceLoader.Load<Font>( "res://Assets/Fonts/SourceCodePro-ExtraLight.ttf" ) );
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
			_richLabel.Dispose();
			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Print
		===============
		*/
		/// <summary>
		/// Appends a string message on a new line into the in-game quake console.
		/// </summary>
		/// <param name="message"></param>
		public void Print( string message )
		{
			_richLabel.CallDeferred( RichTextLabel.MethodName.AppendText, $"{message}\n" );
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// Clears the in-game quake console.
		/// </summary>
		public void Clear()
		{
			_richLabel.CallDeferred( RichTextLabel.MethodName.Clear );
		}

		/*
		===============
		ScrollToBottom
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void ScrollToBottom()
		{
			VScrollBar scroll = _richLabel.GetVScrollBar();
			scroll.Value = scroll.MaxValue - scroll.Page;
		}

		/*
		===============
		PageUp
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void PageUp()
		{
			VScrollBar scroll = _richLabel.GetVScrollBar();
			Tween tween = _richLabel.CreateTween();
			tween.TweenProperty( scroll, _valueNodePath, scroll.Value - (scroll.Page - scroll.Page * 0.1f), 0.1f );
			_richLabel.GetViewport().SetInputAsHandled();
		}

		/*
		===============
		PageDown
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void PageDown()
		{
			VScrollBar scroll = _richLabel.GetVScrollBar();
			Tween tween = _richLabel.CreateTween();
			tween.TweenProperty( scroll, _valueNodePath, scroll.Value + (scroll.Page - scroll.Page * 0.1f), 0.1f );
			_richLabel.GetViewport().SetInputAsHandled();
		}
	};
};
