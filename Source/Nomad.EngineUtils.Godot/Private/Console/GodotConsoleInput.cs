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
using Nomad.Console;
using Nomad.Console.Events;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;

namespace Nomad.EngineUtils.Godot.Private.Console
{
	/*
	===================================================================================
	
	GodotConsoleInput
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed partial class GodotConsoleInput : LineEdit, IConsoleInput
	{
		public bool IsOpen => _isOpen;

		public IGameEvent<TextEnteredEventArgs> TextEntered => _textEntered;
		private readonly IGameEvent<TextEnteredEventArgs> _textEntered = default;

		private bool _isOpen = false;

		public GodotConsoleInput( IGameEventRegistryService eventFactory )
		{
			ArgumentGuard.ThrowIfNull( eventFactory, nameof( eventFactory ) );

			_textEntered = eventFactory.GetEvent<TextEnteredEventArgs>(
				TextEnteredEventArgs.Name,
				TextEnteredEventArgs.NameSpace
			);
		}

		public void Open()
		{
			_isOpen = true;
			Show();
			GrabFocus();
		}

		public void Close()
		{
			_isOpen = false;
			Clear();
			Hide();
		}

		public void Focus()
		{
			GrabFocus();
		}

		public void ApplyHistoryText( string text )
		{
			Text = text ?? string.Empty;
			CaretColumn = Text.Length;
			GrabFocus();
		}

		public void OnTextEntered( string text )
		{
			if ( string.IsNullOrWhiteSpace( text ) ) {
				Clear();
				return;
			}
			_textEntered.Publish( new TextEnteredEventArgs( text ) );
			Clear();
		}

		public override void _Ready()
		{
			base._Ready();

			Name = nameof( GodotConsoleInput );
			AnchorTop = 0.5f;
			AnchorRight = 1.0f;
			AnchorBottom = 0.5f;
			PlaceholderText = string.Empty;

			TextSubmitted += OnTextEntered;
		}
	};
};
