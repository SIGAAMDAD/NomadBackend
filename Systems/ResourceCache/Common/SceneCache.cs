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

namespace NomadCore.Systems.ResourceCache.Common {
	/*
	===================================================================================
	
	SceneCache
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class SceneCache : BaseCache<PackedScene> {
		/*
		===============
		CalculateMemorySize
		===============
		*/
		/// <summary>
		/// Calculates the total memory usage of a <see cref="PackedScene"/>
		/// </summary>
		/// <param name="resource"></param>
		/// <returns></returns>
		protected override long CalculateMemorySize( PackedScene resource ) {
			// FIXME: is this the best way of doing this?

			double memoryUsage = Performance.GetMonitor( Performance.Monitor.MemoryStatic );
			Node scene = resource.Instantiate( PackedScene.GenEditState.Instance );
			double newUsage = Performance.GetMonitor( Performance.Monitor.MemoryStatic );
			scene.Free();

			return (long)( newUsage - memoryUsage );
		}
	};
};