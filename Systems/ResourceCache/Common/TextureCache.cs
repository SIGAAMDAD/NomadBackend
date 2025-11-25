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

namespace NomadCore.Systems.ResourceCache.Common {
	/*
	===================================================================================
	
	TextureCache
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class TextureCache : BaseCache<Godot.Texture> {
		/*
		===============
		CalculateMemorySize
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="resource"></param>
		/// <returns></returns>
		protected override long CalculateMemorySize( Godot.Texture resource ) {
			if ( resource is Godot.Texture2D texture2D ) {
				return texture2D.GetWidth() * texture2D.GetHeight() * ( texture2D.HasAlpha() ? 4 : 3 );
			} else if ( resource is Godot.GradientTexture1D gradient1D ) {
				return gradient1D.GetWidth() * ( gradient1D.HasAlpha() ? 4 : 3 );
			} else if ( resource is Godot.GradientTexture2D gradient2D ) {
				return gradient2D.GetWidth() * gradient2D.GetHeight() * ( gradient2D.HasAlpha() ? 4 : 3 );
			}
			return 0;
		}
	};
};