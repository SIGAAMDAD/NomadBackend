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
using NomadCore.Interfaces.Common;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;
using System;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Rendering {
	/*
	===================================================================================
	
	ServerLight
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal class ServerLight : ILight {
		public LightId Id => _lightRid;
		protected readonly LightId _lightRid;

		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => _modifiedAt;
		private DateTime _modifiedAt;

		public int Version => _version;
		private int _version = 0;

		public bool Enabled {
			get => _enabled;
			set {
				if ( _enabled == value ) {
					return;
				}
				_enabled = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetEnabled( _lightRid.Value, _enabled );
			}
		}
		private bool _enabled;

		public float Height {
			get => _height;
			set {
				if ( _height == value ) {
					return;
				}
				_height = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetHeight( _lightRid.Value, _height );
			}
		}
		private float _height;

		public bool ShadowsEnabled {
			get => _shadowsEnabled;
			set {
				if ( _shadowsEnabled == value ) {
					return;
				}
				_shadowsEnabled = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetShadowEnabled( _lightRid.Value, _shadowsEnabled );
			}
		}
		private bool _shadowsEnabled;

		public Light2D.ShadowFilterEnum ShadowFilterType {
			get => _shadowFilterType;
			set {
				if ( _shadowFilterType == value ) {
					return;
				}
				_shadowFilterType = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetShadowFilter( _lightRid.Value, (RenderingServer.CanvasLightShadowFilter)_shadowFilterType  );
			}
		}
		private Light2D.ShadowFilterEnum _shadowFilterType;

		public float ShadowFilterSmooth {
			get => _shadowFilterSmooth;
			set {
				if ( _shadowFilterSmooth == value ) {
					return;
				}
				_shadowFilterSmooth = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetShadowSmooth( _lightRid.Value, _shadowFilterSmooth );
			}
		}
		private float _shadowFilterSmooth;

		public float Energy {
			get => _energy;
			set {
				if ( _energy == value ) {
					return;
				}
				_energy = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetEnergy( _lightRid.Value, _energy );
			}
		}
		private float _energy;

		public Color Color {
			get => _color;
			set {
				if ( _color == value ) {
					return;
				}
				_color = value;
				_modifiedAt = DateTime.UtcNow;
				_version++;
				RenderingServer.CanvasLightSetColor( _lightRid.Value, _color );
			}
		}
		private Color _color;

		/*
		===============
		ServerLight
		===============
		*/
		public ServerLight( Light2D light, bool directional = false ) {
			_lightRid = new LightId( RenderingServer.CanvasLightCreate() );

			RenderingServer.CanvasLightAttachToCanvas( _lightRid.Value, light.GetCanvas() );
			RenderingServer.CanvasLightSetBlendMode( _lightRid.Value, (RenderingServer.CanvasLightBlendMode)light.BlendMode  );
			RenderingServer.CanvasLightSetColor( _lightRid.Value, light.Color );
			RenderingServer.CanvasLightSetEnabled( _lightRid.Value, light.Enabled );
			RenderingServer.CanvasLightSetEnergy( _lightRid.Value, light.Energy );
			RenderingServer.CanvasLightSetHeight( _lightRid.Value, light.GetHeight() );
			RenderingServer.CanvasLightSetTransform( _lightRid.Value, light.Transform );
			RenderingServer.CanvasLightSetMode( _lightRid.Value, directional ? RenderingServer.CanvasLightMode.Directional : RenderingServer.CanvasLightMode.Point );
			RenderingServer.CanvasLightSetShadowEnabled( _lightRid.Value, light.ShadowEnabled );
			RenderingServer.CanvasLightSetShadowColor( _lightRid.Value, light.ShadowColor );
			RenderingServer.CanvasLightSetShadowFilter( _lightRid.Value, (RenderingServer.CanvasLightShadowFilter)light.ShadowFilter );
			RenderingServer.CanvasLightSetShadowSmooth( _lightRid.Value, light.ShadowFilterSmooth );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _lightRid.Value.IsValid ) {
				RenderingServer.FreeRid( _lightRid.Value );
			}
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Equals
		===============
		*/
		public bool Equals( IEntity<LightId>? other ) {
			return other?.Id == _lightRid;
		}
	};
};