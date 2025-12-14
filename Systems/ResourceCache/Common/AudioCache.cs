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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Systems.ResourceCache.Application.Interfaces;

namespace NomadCore.Systems.ResourceCache.Common {
	/*
	===================================================================================
	
	AudioCache
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class AudioCache( ILoggerService logger, IGameEventRegistryService eventFactory, IResourceLoader<Godot.AudioStream, FilePath> loader ) : BaseCache<Godot.AudioStream, FilePath>( logger, eventFactory, loader ) {
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
		protected override int CalculateMemorySize( Godot.AudioStream resource ) {
			return (int)resource.GetLength() * ( resource.IsMonophonic() ? 1 : 2 );
		}
	};
};