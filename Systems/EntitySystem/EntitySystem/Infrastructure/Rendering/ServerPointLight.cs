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

namespace NomadCore.Systems.EntitySystem.Infrastructure.Rendering {
	/*
	===================================================================================
	
	ServerPointLight
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class ServerPointLight : ServerLight {
		public ServerPointLight( PointLight2D light )
			: base( light, false )
		{
			RenderingServer.CanvasLightSetTexture( _lightRid.Value, light.Texture.GetRid() );
			RenderingServer.CanvasLightSetTextureOffset( _lightRid.Value, light.Offset );
			RenderingServer.CanvasLightSetTextureScale( _lightRid.Value, light.TextureScale );
			light.CallDeferred( PointLight2D.MethodName.QueueFree );
		}
	};
};