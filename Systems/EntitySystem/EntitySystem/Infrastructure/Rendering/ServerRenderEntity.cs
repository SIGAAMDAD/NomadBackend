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
using NomadCore.GameServices;
using NomadCore.Domain.Models.Interfaces;
using System;
using NomadCore.Systems.EntitySystem.Domain.Events;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;
using NomadCore.Interfaces.Common;
using NomadCore.Systems.EntitySystem.Application.Interfaces;
using NomadCore.Systems.EntitySystem.Domain.Models.Interfaces;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Rendering {
	/*
	===================================================================================
	
	ServerRenderEntity
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal abstract class ServerRenderEntity : IRenderEntity {
		public bool Visible {
			get => _visible;
			set {
				if ( _visible == value ) {
					return;
				}
				_visible = value;
				_visibilityChanged.PublishAsync( new RenderEntityVisibilityChangedEventArgs( this, value ) );
				RenderingServer.CanvasItemSetVisible( _canvasItemRid, _visible );
			}
		}
		private bool _visible;

		public uint VisibilityLayer {
			get => _visibilityLayer;
			set {
				if ( _visibilityLayer == value ) {
					return;
				}
				_visibilityLayer = value;
				RenderingServer.CanvasItemSetVisibilityLayer( _canvasItemRid, _visibilityLayer );
			}
		}
		private uint _visibilityLayer;

		public int ZIndex {
			get => _zindex;
			set {
				if ( _zindex == value ) {
					return;
				}
				_zindex = value;
				RenderingServer.CanvasItemSetZIndex( _canvasItemRid, _zindex );
			}
		}
		private int _zindex;

		public int LightMask {
			get => _lightMask;
			set {
				if ( _lightMask == value ) {
					return;
				}
				_lightMask = value;
				RenderingServer.CanvasItemSetLightMask( _canvasItemRid, _lightMask );
			}
		}
		private int _lightMask;

		public Color Modulate {
			get => _modulate;
			set {
				if ( _modulate == value ) {
					return;
				}
				_modulate = value;
				RenderingServer.CanvasItemSetModulate( _canvasItemRid, _modulate );
			}
		}
		private Color _modulate;

		public Rid Rid => _canvasItemRid;
		protected readonly Rid _canvasItemRid;

		protected readonly WeakReference<IGameEntity> _owner;

		public IGameEvent<RenderEntityVisibilityChangedEventArgs> VisibilityChanged => _visibilityChanged;

		public RenderEntityId Id => throw new NotImplementedException();

		public DateTime CreatedAt => throw new NotImplementedException();

		public DateTime? ModifiedAt => throw new NotImplementedException();

		public int Version => throw new NotImplementedException();

		protected readonly IGameEvent<RenderEntityVisibilityChangedEventArgs> _visibilityChanged;

		/*
		===============
		ServerRenderEntity
		===============
		*/
		public ServerRenderEntity( IGameEventRegistryService eventFactory, IGameEntity owner, CanvasItem canvasItem ) {
			_owner = new WeakReference<IGameEntity>( owner );

			_visibilityChanged = eventFactory.GetEvent<RenderEntityVisibilityChangedEventArgs>( nameof( VisibilityChanged ), "EntitySystem:ServerRenderEntity" );

			_zindex = canvasItem.ZIndex;
			_modulate = canvasItem.Modulate;
			_visibilityLayer = canvasItem.VisibilityLayer;
			_visible = canvasItem.Visible;
			_lightMask = canvasItem.LightMask;

			_canvasItemRid = RenderingServer.CanvasItemCreate();
			RenderingServer.CanvasItemSetZIndex( _canvasItemRid, _zindex );
			RenderingServer.CanvasItemSetModulate( _canvasItemRid, _modulate );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _canvasItemRid.IsValid ) {
				RenderingServer.FreeRid( _canvasItemRid );
			}
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Update
		===============
		*/
		public virtual void Update( float deltaTime ) {
		}

		/*
		===============
		Draw
		===============
		*/
		public virtual void Draw( float deltaTime ) {
		}

		public bool Equals( IEntity<RenderEntityId>? other ) {
			throw new NotImplementedException();
		}
	};
};