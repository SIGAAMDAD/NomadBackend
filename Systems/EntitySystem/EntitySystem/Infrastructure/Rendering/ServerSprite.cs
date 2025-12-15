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
using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;
using NomadCore.Systems.EntitySystem.Application.Interfaces;
using NomadCore.Systems.EntitySystem.Infrastructure.Rendering;

namespace NomadCore.Infrastructure.Rendering {
	/*
	===================================================================================
	
	ServerSprite
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class ServerSprite( IGameEventRegistryService eventFactory, IGameEntity owner, Sprite2D sprite ) : ServerRenderEntity( eventFactory, owner, sprite ) {
		public Vector2 Size {
			get => _size;
			set => _size = value;
		}
		private Vector2 _size = Vector2.Zero;

		public float Scale {
			get => _scale;
			set => _scale = value;
		}
		private float _scale;

		private readonly bool _regionEnabled = sprite.RegionEnabled;
		private readonly bool _regionFilterClipEnabled = sprite.RegionFilterClipEnabled;
		private readonly Rect2 _regionRect = sprite.RegionRect;

		private readonly int _hframes = sprite.Hframes;
		private readonly int _vframes = sprite.Vframes;
		private readonly int _frame = sprite.Frame;
		private readonly Vector2 _offset = sprite.Offset;

		private readonly Texture2D _texture = sprite.Texture;

		/*
		===============
		Draw
		===============
		*/
		public override void Draw( float deltaTime ) {
			GetRects( out Rect2 srcRect, out Rect2 dstRect, out bool filterClipEnabled );
			RenderingServer.CanvasItemAddTextureRectRegion( _canvasItemRid, dstRect, _texture.GetRid(), srcRect, Colors.White, false, filterClipEnabled );
		}

		/*
		===============
		GetRects
		===============
		*/
		private void GetRects( out Rect2 srcRect, out Rect2 dstRect, out bool filterClipEnabled ) {
			Rect2 baseRect;
			if ( _regionEnabled ) {
				filterClipEnabled = _regionFilterClipEnabled;
				baseRect = _regionRect;
			} else {
				filterClipEnabled = false;
				baseRect = new Rect2( 0.0f, 0.0f, _texture.GetWidth(), _texture.GetHeight() );
			}

			Vector2 frameSize = baseRect.Size / new Vector2( _hframes, _vframes );
			Vector2 frameOffset = new Vector2( _frame % _hframes, _frame / _hframes ) * frameSize;

			srcRect = new Rect2( frameSize, baseRect.Position + frameOffset );
			Vector2 destOffset = ( _offset + new Vector2( 0.5f, 0.5f ) ).Floor();

			dstRect = new Rect2( destOffset, frameSize );
		}
	};
};