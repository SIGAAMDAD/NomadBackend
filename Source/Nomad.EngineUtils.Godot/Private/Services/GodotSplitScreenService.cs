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
using Nomad.Core.Engine.Services;
using Nomad.Core.Scene.GameObjects;

namespace Nomad.EngineUtils.Godot.Private.Services
{
	internal sealed class GodotSplitScreenService : ISplitScreenService
	{
		private readonly struct ViewportSlot
		{
			public readonly SubViewportContainer Container;
			public readonly SubViewport Viewport;
			public readonly Camera2D Camera;

			public ViewportSlot( SubViewportContainer container, SubViewport viewport, Camera2D camera )
			{
				Container = container;
				Viewport = viewport;
				Camera = camera;
			}
		}

		private const string VIEWPORT_ROOT_NAME = "NomadSplitScreenRoot";
		private const string REMOTE_TRANSFORM_NAME = "SplitScreenRemoteTransform";

		private readonly List<ViewportSlot> _slots = new();

		private Node? _boundRoot;
		private Control? _viewportRoot;
		private bool _isDisposed = false;

		public void Apply( IGameObject worldRoot, IReadOnlyList<IGameObject> participants )
		{
			ArgumentNullException.ThrowIfNull( worldRoot );
			ArgumentNullException.ThrowIfNull( participants );

			if ( worldRoot is not Node rootNode ) {
				throw new InvalidOperationException( "Godot split-screen requires the world root to be a Godot node." );
			}

			EnsureRoot( rootNode );

			int playerCount = Mathf.Clamp( participants.Count, 0, 4 );
			EnsureSlotCount( playerCount );
			ApplyLayout( playerCount );

			for ( int i = 0; i < _slots.Count; i++ ) {
				var slot = _slots[i];
				bool isActive = i < playerCount;

				slot.Container.Visible = isActive;
				slot.Camera.Enabled = isActive;

				if ( !isActive ) {
					continue;
				}

				slot.Viewport.World2D = rootNode.GetViewport().World2D;
				AssignParticipant( participants[i], slot.Camera );
			}
		}

		public void Clear()
		{
			for ( int i = 0; i < _slots.Count; i++ ) {
				_slots[i].Container.Visible = false;
				_slots[i].Camera.Enabled = false;
			}

			if ( _viewportRoot != null && GodotObject.IsInstanceValid( _viewportRoot ) ) {
				_viewportRoot.QueueFree();
			}

			_slots.Clear();
			_viewportRoot = null;
			_boundRoot = null;
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			Clear();

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		private void EnsureRoot( Node rootNode )
		{
			if ( _boundRoot == rootNode && _viewportRoot != null && GodotObject.IsInstanceValid( _viewportRoot ) ) {
				return;
			}

			Clear();

			_boundRoot = rootNode;
			_viewportRoot = new Control {
				Name = VIEWPORT_ROOT_NAME,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_viewportRoot.SetAnchorsPreset( Control.LayoutPreset.FullRect );
			rootNode.AddChild( _viewportRoot );
		}

		private void EnsureSlotCount( int playerCount )
		{
			while ( _slots.Count < playerCount ) {
				_slots.Add( CreateSlot( _slots.Count ) );
			}

			for ( int i = 0; i < _slots.Count; i++ ) {
				_slots[i].Container.Visible = i < playerCount;
			}
		}

		private ViewportSlot CreateSlot( int index )
		{
			var container = new SubViewportContainer {
				Name = $"SubViewportContainer{index + 1}",
				Stretch = true,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			container.SetAnchorsPreset( Control.LayoutPreset.FullRect );
			_viewportRoot!.AddChild( container );

			var viewport = new SubViewport {
				Name = $"SubViewport{index + 1}",
				HandleInputLocally = false,
				RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
				Size2DOverrideStretch = true,
				TransparentBg = false
			};
			container.AddChild( viewport );

			var camera = new Camera2D {
				Name = $"Camera2D{index + 1}",
				Enabled = true
			};
			viewport.AddChild( camera );

			return new ViewportSlot( container, viewport, camera );
		}

		private void ApplyLayout( int playerCount )
		{
			var layout = GetLayout( playerCount );

			for ( int i = 0; i < _slots.Count; i++ ) {
				if ( i >= layout.Count ) {
					_slots[i].Container.Visible = false;
					continue;
				}

				var slot = _slots[i];
				var rect = layout[i];

				slot.Container.AnchorLeft = rect.Left;
				slot.Container.AnchorTop = rect.Top;
				slot.Container.AnchorRight = rect.Right;
				slot.Container.AnchorBottom = rect.Bottom;
				slot.Container.OffsetLeft = 0.0f;
				slot.Container.OffsetTop = 0.0f;
				slot.Container.OffsetRight = 0.0f;
				slot.Container.OffsetBottom = 0.0f;
			}
		}

		private static IReadOnlyList<NormalizedRect> GetLayout( int playerCount )
		{
			return playerCount switch {
				0 => Array.Empty<NormalizedRect>(),
				1 => [new NormalizedRect( 0.0f, 0.0f, 1.0f, 1.0f )],
				2 => [
					new NormalizedRect( 0.0f, 0.0f, 0.5f, 1.0f ),
					new NormalizedRect( 0.5f, 0.0f, 1.0f, 1.0f )
				],
				3 => [
					new NormalizedRect( 0.0f, 0.0f, 1.0f, 0.5f ),
					new NormalizedRect( 0.0f, 0.5f, 0.5f, 1.0f ),
					new NormalizedRect( 0.5f, 0.5f, 1.0f, 1.0f )
				],
				_ => [
					new NormalizedRect( 0.0f, 0.0f, 0.5f, 0.5f ),
					new NormalizedRect( 0.5f, 0.0f, 1.0f, 0.5f ),
					new NormalizedRect( 0.0f, 0.5f, 0.5f, 1.0f ),
					new NormalizedRect( 0.5f, 0.5f, 1.0f, 1.0f )
				]
			};
		}

		private static void AssignParticipant( IGameObject participant, Camera2D targetCamera )
		{
			if ( participant is not Node participantNode ) {
				return;
			}

			var sourceCamera = participantNode.GetNodeOrNull<Camera2D>( "Camera2D" );
			if ( sourceCamera != null ) {
				sourceCamera.Enabled = false;
				targetCamera.Zoom = sourceCamera.Zoom;
			}

			var remoteTransform = participantNode.GetNodeOrNull<RemoteTransform2D>( REMOTE_TRANSFORM_NAME );
			if ( remoteTransform == null ) {
				remoteTransform = new RemoteTransform2D {
					Name = REMOTE_TRANSFORM_NAME,
					UpdatePosition = true,
					UpdateRotation = false,
					UpdateScale = false,
					UseGlobalCoordinates = true
				};
				participantNode.AddChild( remoteTransform );
			}

			remoteTransform.RemotePath = targetCamera.GetPath();
			targetCamera.ResetSmoothing();
		}

		private readonly record struct NormalizedRect( float Left, float Top, float Right, float Bottom );
	}
}
