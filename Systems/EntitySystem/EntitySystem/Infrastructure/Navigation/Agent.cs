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
using NomadCore.Systems.EntitySystem.Domain.Events;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects.Components;
using System;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Navigation {
	/*
	===================================================================================
	
	Agent
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class Agent {
		public Rid AgentRid => _agentRid;
		private readonly Rid _agentRid;

		public bool AvoidanceEnabled {
			get => _avoidanceEnabled;
			set {
				if ( _avoidanceEnabled == value ) {
					return;
				}
				_avoidanceEnabled = value;
				NavigationServer2D.AgentSetAvoidanceEnabled( _agentRid, _avoidanceEnabled );
			}
		}
		private bool _avoidanceEnabled;

		public float AvoidancePriority {
			get => _avoidancePriority;
			set {
				if ( _avoidancePriority == value ) {
					return;
				}
				_avoidancePriority = value;
				NavigationServer2D.AgentSetAvoidancePriority( _agentRid, _avoidancePriority );
			}
		}
		private float _avoidancePriority;

		public uint AvoidanceLayers {
			get => _avoidanceLayers;
			set {
				if ( _avoidanceLayers == value ) {
					return;
				}
				_avoidanceLayers = value;
				NavigationServer2D.AgentSetAvoidanceLayers( _agentRid, _avoidanceLayers );
			}
		}
		private uint _avoidanceLayers = uint.MaxValue;

		public int MaxNeighbors {
			get => _maxNeighors;
			set {
				if ( _maxNeighors == value ) {
					return;
				}
				_maxNeighors = value;
				NavigationServer2D.AgentSetMaxNeighbors( _agentRid, _maxNeighors );
			}
		}
		private int _maxNeighors;

		public float Radius {
			get => _radius;
			set {
				if ( _radius == value ) {
					return;
				}
				_radius = value;
				NavigationServer2D.AgentSetRadius( _agentRid, _radius );
			}
		}
		private float _radius;

		public float NeighborDistance {
			get => _neighborDistance;
			set {
				if ( _neighborDistance == value ) {
					return;
				}
				_neighborDistance = value;
				NavigationServer2D.AgentSetNeighborDistance( _agentRid, _neighborDistance );
			}
		}
		private float _neighborDistance;

		public float MaxSpeed {
			get => _maxSpeed;
			set {
				if ( _maxSpeed == value ) {
					return;
				}
				_maxSpeed = value;
				NavigationServer2D.AgentSetMaxSpeed( _agentRid, _maxSpeed );
			}
		}
		private float _maxSpeed;

		private readonly WeakReference<IGameEntity> _owner;

		public IGameEvent<NavigationDestinationReachedEventData> NavigationDestinationReached => _navigationDestinationReached;
		private readonly IGameEvent<NavigationDestinationReachedEventData> _navigationDestinationReached;

		/*
		===============
		Agent
		===============
		*/
		public Agent( IGameEventRegistryService eventFactory, IGameEntity owner, NavigationAgent2D agent ) {
			_owner = new WeakReference<IGameEntity>( owner );

			_navigationDestinationReached = eventFactory.GetEvent<NavigationDestinationReachedEventData>( nameof( NavigationDestinationReachedEventData ) );

			_agentRid = NavigationServer2D.AgentCreate();

			bool avoidanceEnabled = agent.AvoidanceEnabled;
			uint avoidanceLayers = agent.AvoidanceLayers;
			uint avoidanceMask = agent.AvoidanceMask;
			float avoidancePriority = agent.AvoidancePriority;
			_maxSpeed = agent.MaxSpeed;
			_neighborDistance = agent.NeighborDistance;
			_radius = agent.Radius;

			NavigationServer2D.AgentSetAvoidanceEnabled( _agentRid, avoidanceEnabled );
			NavigationServer2D.AgentSetAvoidanceLayers( _agentRid, avoidanceLayers );
			NavigationServer2D.AgentSetAvoidanceMask( _agentRid, avoidanceMask );
			NavigationServer2D.AgentSetAvoidancePriority( _agentRid, avoidancePriority );
			NavigationServer2D.AgentSetMaxSpeed( _agentRid, _maxSpeed );
			NavigationServer2D.AgentSetNeighborDistance( _agentRid, _neighborDistance );
			NavigationServer2D.AgentSetRadius( _agentRid, _radius );
			NavigationServer2D.AgentSetMap( _agentRid, agent.GetNavigationMap() );
			NavigationServer2D.AgentSetTimeHorizonAgents( _agentRid, agent.TimeHorizonAgents );
			NavigationServer2D.AgentSetTimeHorizonObstacles( _agentRid, agent.TimeHorizonObstacles );
			NavigationServer2D.AgentSetMaxNeighbors( _agentRid, agent.MaxNeighbors );

			owner.AddComponent( new NavigationComponent {
				Destination = agent.TargetPosition,
				Radius = agent.Radius,
				MaxPathDistance = agent.PathMaxDistance,
				AvoidanceEnabled = avoidanceEnabled
			} );

			agent.CallDeferred( NavigationAgent2D.MethodName.QueueFree );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _agentRid.IsValid ) {
				NavigationServer2D.FreeRid( _agentRid );
			}
		}

		/*
		===============
		Update
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Update( float deltaTime ) {
			if ( !_owner.TryGetTarget( out var owner ) ) {
				return;
			}

			ref var navigationComponent = ref owner.GetOrAddComponent<NavigationComponent>();
			ref var transformComponent = ref owner.GetOrAddComponent<TransformComponent>();
			ref var velocityComponent = ref owner.GetOrAddComponent<VelocityComponent>();

			if ( transformComponent.Position.DistanceTo( navigationComponent.Destination ) < navigationComponent.MaxPathDistance ) {
				NavigationDestinationReached.Publish( new NavigationDestinationReachedEventData() );
			} else {
				NavigationServer2D.AgentSetVelocity( _agentRid, velocityComponent.Velocity );
			}
		}
	};
};