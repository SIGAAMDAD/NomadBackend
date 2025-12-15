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
using NomadCore.Interfaces.EntitySystem;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Navigation {
	/*
	===================================================================================
	
	Region
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class Region : INavigationRegion {
		public bool Enabled {
			get => NavigationServer2D.RegionGetEnabled( _regionRid );
			set => NavigationServer2D.RegionSetEnabled( _regionRid, value );
		}

		public Rid RegionRid => _regionRid;
		private readonly Rid _regionRid;

		public Region( NavigationRegion2D region ) {
			_regionRid = NavigationServer2D.RegionCreate();
			
			Enabled = region.Enabled;
			NavigationServer2D.RegionSetEnterCost( _regionRid, region.EnterCost );
			NavigationServer2D.RegionSetMap( _regionRid, region.GetNavigationMap() );
			NavigationServer2D.RegionSetNavigationLayers( _regionRid, region.NavigationLayers );
			NavigationServer2D.RegionSetNavigationPolygon( _regionRid, region.NavigationPolygon );
			NavigationServer2D.RegionSetTravelCost( _regionRid, region.TravelCost );
			NavigationServer2D.RegionSetUseAsyncIterations( _regionRid, true );
			NavigationServer2D.RegionSetUseEdgeConnections( _regionRid, true );
			
			region.CallDeferred( NavigationRegion2D.MethodName.QueueFree );
		}

		public void Dispose() {
			if ( _regionRid.IsValid ) {
				NavigationServer2D.FreeRid( _regionRid );
			}
		}
	};
};