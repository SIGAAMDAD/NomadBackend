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

namespace NomadCore.Systems.EntitySystem.Infrastructure.Navigation {
	/*
	===================================================================================
	
	Obstacle
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class Obstacle {
		public Rid ObstacleRid => _obstacleRid;
		private readonly Rid _obstacleRid;

		public uint AvoidanceLayers {
			get => _avoidanceLayers;
			set {
				if ( _avoidanceLayers == value ) {
					return;
				}
				_avoidanceLayers = value;
				NavigationServer2D.ObstacleSetAvoidanceLayers( _obstacleRid, _avoidanceLayers );
			}
		}
		private uint _avoidanceLayers = uint.MaxValue;

		public bool AvoidanceEnabled {
			get => _avoidanceEnabled;
			set {
				if ( _avoidanceEnabled == value ) {
					return;
				}
				NavigationServer2D.ObstacleSetAvoidanceEnabled( _obstacleRid, _avoidanceEnabled );
			}
		}
		private bool _avoidanceEnabled = false;

		public float Radius {
			get => _radius;
			set {
				if ( _radius == value ) {
					return;
				}
				_radius = value;
				NavigationServer2D.ObstacleSetRadius( _obstacleRid, _radius );
			}
		}
		private float _radius = float.MaxValue;

		public Obstacle( NavigationObstacle2D obstacle ) {
			_obstacleRid = NavigationServer2D.ObstacleCreate();

			_avoidanceLayers = obstacle.AvoidanceLayers;
			_avoidanceEnabled = obstacle.AvoidanceEnabled;
			_radius = obstacle.Radius;

			NavigationServer2D.ObstacleSetVertices( _obstacleRid, obstacle.Vertices );
			NavigationServer2D.ObstacleSetMap( _obstacleRid, obstacle.GetNavigationMap() );
			NavigationServer2D.ObstacleSetAvoidanceEnabled( _obstacleRid, _avoidanceEnabled );
			NavigationServer2D.ObstacleSetAvoidanceLayers( _obstacleRid, _avoidanceLayers );
			NavigationServer2D.ObstacleSetRadius( _obstacleRid, _radius );
			NavigationServer2D.ObstacleSetPosition( _obstacleRid, obstacle.GlobalPosition );

			obstacle.CallDeferred( NavigationObstacle2D.MethodName.QueueFree );
		}
		
		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _obstacleRid.IsValid ) {
				NavigationServer2D.FreeRid( _obstacleRid );
			}
		}
	};
};