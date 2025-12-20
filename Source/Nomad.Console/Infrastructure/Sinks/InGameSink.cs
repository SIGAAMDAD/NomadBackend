/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Godot;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using NomadCore.Systems.ConsoleSystem.Interfaces.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure {
	/*
	===================================================================================
	
	InGameSink
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class InGameSink : SinkBase {
		private readonly RichTextLabel _richLabel;

		/*
		===============
		InGameSink
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="node"></param>
		/// <param name="builder"></param>
		/// <param name="eventRegistry"></param>
		public InGameSink( Node? node, ICommandBuilder? builder, IGameEventRegistryService eventRegistry ) {
			ArgumentNullException.ThrowIfNull( node );
			ArgumentNullException.ThrowIfNull( builder );
			ArgumentNullException.ThrowIfNull( eventRegistry );

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
			node.CallDeferred( Control.MethodName.AddChild, _richLabel );

			builder.TextEntered.Subscribe( this, OnScrollToBottom );
			eventRegistry.GetEvent<EmptyEventArgs>( StringPool.Intern( "ConsoleOpened" ) ).Subscribe( this, OnConsoleOpened );
			eventRegistry.GetEvent<EmptyEventArgs>( StringPool.Intern( "PageUp" ) ).Subscribe( this, OnPageUp );
			eventRegistry.GetEvent<EmptyEventArgs>( StringPool.Intern( "PageDown" ) ).Subscribe( this, OnPageDown );
		}

		/*
		===============
		Print
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override void Print( string message ) {
			_richLabel.CallDeferred( RichTextLabel.MethodName.AppendText, message );
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override void Clear() {
			_richLabel.CallDeferred( RichTextLabel.MethodName.Clear );
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override void Flush() {
		}

		/*
		===============
		OnScrollToBottom
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="args"></param>
		private void OnScrollToBottom( in TextEnteredEventData args ) {
			VScrollBar scroll = _richLabel.GetVScrollBar();
			scroll.Value = scroll.MaxValue - scroll.Page;
		}

		/*
		===============
		OnConsoleOpened
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnConsoleOpened( in EmptyEventArgs args ) {
			OnScrollToBottom( new TextEnteredEventData() );
		}

		/*
		===============
		OnPageUp
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="args"></param>
		private void OnPageUp( in EmptyEventArgs args ) {
			VScrollBar scroll = _richLabel.GetVScrollBar();
			Tween tween = _richLabel.CreateTween();
			tween.TweenProperty( scroll, "value", scroll.Value - ( scroll.Page - scroll.Page * 0.1f ), 0.1f );
			_richLabel.GetViewport().SetInputAsHandled();
		}

		/*
		===============
		OnPageDown
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="args"></param>
		private void OnPageDown( in EmptyEventArgs args ) {
			VScrollBar scroll = _richLabel.GetVScrollBar();
			Tween tween = _richLabel.CreateTween();
			tween.TweenProperty( scroll, "value", scroll.Value + ( scroll.Page - scroll.Page * 0.1f ), 0.1f );
			_richLabel.GetViewport().SetInputAsHandled();
		}
	};
};