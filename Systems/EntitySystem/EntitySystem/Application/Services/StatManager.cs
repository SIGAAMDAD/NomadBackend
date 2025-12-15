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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadCore.Systems.EntitySystem.Interfaces;
using NomadCore.Systems.EntitySystem.Domain;
using NomadCore.Systems.EntitySystem.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Systems.EntitySystem.Domain.Events;
using NomadCore.Infrastructure.Collections;
using NomadCore.GameServices;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;

namespace NomadCore.Systems.EntitySystem.Services {
	/*
	===================================================================================
	
	StatManager

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public class StatManager : IStatService {
		/// <summary>
		/// The maximum natural health of an entity.
		/// </summary>
		public const float BASE_MAX_HEALTH = 100.0f;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="statName"></param>
		/// <returns></returns>
		public Any this[ InternString statName ] {
			get {
				return _stats.TryGetValue( statName, out IEntityStat stat ) ? stat.GetValue() : Any.From( 0 );
			}
			set {
				if ( _stats.TryGetValue( statName, out IEntityStat stat ) ) {
					stat.SetValue( value );
				}
			}
		}

		private readonly Dictionary<InternString, IEntityStat> _stats;
		private readonly Entity _owner;

		public IGameEvent<StatChangedEventData> StatChanged => _statChanged;
		private readonly IGameEvent<StatChangedEventData> _statChanged;

		/*
		===============
		StatManager
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		public StatManager( Entity owner, IGameEventRegistryService eventFactory, Dictionary<InternString, IEntityStat> stats ) {
			_owner = owner;
			_stats = stats;

			_statChanged = eventFactory.GetEvent<StatChangedEventData>( nameof( StatChanged ), "EntitySystem:StatManager" );
		}

		/*
		===============
		SetStatValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="statName">The name of statistic.</param>
		/// <param name="newValue"></param>
		public void SetStatValue<T>( InternString statName, T newValue )
			where T : unmanaged
		{
			if ( !_stats.TryGetValue( statName, out IEntityStat? stat ) ) {
				var newStat = new EntityStat<T>( newValue, default, default );
				_stats.Add( statName, newStat );
			} else if ( stat is IEntityStat<T> typedStat ) {
				typedStat.Value = newValue;
			}
		}

		/*
		===============
		SetStatMaxValue
		===============
		*/
		/// <summary>
		/// Sets the maximum value for <paramref name="statName"/>
		/// </summary>
		/// <param name="statName">The name of the statistic.</param>
		/// <param name="maxValue"></param>
		public void SetStatMaxValue<T>( InternString statName, T maxValue )
			where T : unmanaged
		{
			string name = SceneStringPool.FromInterned( statName );
			if ( !_stats.TryGetValue( statName, out IEntityStat? stat ) ) {
				throw new KeyNotFoundException( name );
			}
			if ( stat is IEntityStat<T> typedStat ) {
				typedStat.MaxValue = maxValue;
			}
		}

		/*
		===============
		SetStatMinValue
		===============
		*/
		/// <summary>
		/// Sets the minimum value for <paramref name="statName"/>
		/// </summary>
		/// <param name="statName">The name of the statistic</param>
		/// <param name="minValue"></param>
		public void SetStatMinValue<T>( InternString statName, T minValue )
			where T : unmanaged
		{
			string name = SceneStringPool.FromInterned( statName );
			if ( !_stats.TryGetValue( statName, out IEntityStat? stat ) ) {
				throw new KeyNotFoundException( name );
			}
			if ( stat is IEntityStat<T> typedStat ) {
				typedStat.MinValue = minValue;
			}
		}

		/*
		===============
		GetStatValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="statName"></param>
		/// <returns></returns>
		public T GetStatValue<T>( InternString statName )
			where T : unmanaged
		{
			string name = SceneStringPool.FromInterned( statName );
			if ( !_stats.TryGetValue( statName, out IEntityStat? stat ) ) {
				throw new KeyNotFoundException( name );
			}
			if ( stat is IEntityStat<T> typedStat ) {
				return typedStat.MaxValue;
			}
			throw new InvalidCastException( name );
		}

		/*
		===============
		GetStatMaxValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="statName"></param>
		/// <returns></returns>
		public T GetStatMaxValue<T>( InternString statName )
			where T : unmanaged
		{
			string name = SceneStringPool.FromInterned( statName );
			if ( !_stats.TryGetValue( statName, out IEntityStat? stat ) ) {
				throw new KeyNotFoundException( name );
			}
			if ( stat is IEntityStat<T> typedStat ) {
				return typedStat.MaxValue;
			}
			throw new InvalidCastException( name );
		}

		/*
		===============
		GetStatMinValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="statName"></param>
		/// <returns></returns>
		public T GetStatMinValue<T>( InternString statName )
			where T : unmanaged
		{
			string name = SceneStringPool.FromInterned( statName );
			if ( !_stats.TryGetValue( statName, out IEntityStat? stat ) ) {
				throw new KeyNotFoundException( name );
			}
			if ( stat is IEntityStat<T> typedStat ) {
				return typedStat.MinValue;
			}
			throw new InvalidCastException( name );
		}
	};
};