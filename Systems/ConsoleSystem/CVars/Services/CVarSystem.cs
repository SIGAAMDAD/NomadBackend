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

using NomadCore.Abstractions.Services;
using NomadCore.Interfaces;
using NomadCore.Utilities;
using NomadCore.Systems.ConsoleSystem.CVars.Infrastructure;
using NomadCore.Systems.ConsoleSystem.CVars.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadCore.Infrastructure;
using NomadCore.Systems.ConsoleSystem.CVars.Common;

namespace NomadCore.Systems.ConsoleSystem.CVars.Services {
	/*
	===================================================================================
	
	CVarSystem
	
	===================================================================================
	*/
	/// <summary>
	/// Global manager for CVars
	/// </summary>

	public sealed class CVarSystem : ICVarSystemService {
		private readonly ConcurrentDictionary<string, ICVar> Cvars = new ConcurrentDictionary<string, ICVar>( StringComparer.OrdinalIgnoreCase );
		private readonly HashSet<CVarGroup> Groups = new HashSet<CVarGroup>();

		private readonly IGameEventBusService EventBus = ServiceRegistry.Get<IGameEventBusService>();
		private readonly ILoggerService Logger = ServiceRegistry.Get<ILoggerService>();

		/*
		===============
		Initialize
		===============
		*/
		public void Initialize() {
		}

		/*
		===============
		Shutdown
		===============
		*/
		public void Shutdown() {
		}

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cvar"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Register( in ICVar? cvar ) {
			if ( !Cvars.ContainsKey( cvar.Name ) ) {
				Cvars.TryAdd( cvar.Name, cvar );
			}
		}

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// Registers a CVar with the global CVarSystem and adds it to the CVar cache.
		/// </summary>
		/// <typeparam name="T">The internal type of the cvar's value.</typeparam>
		/// <param name="createInfo">The cvar's creation info.</param>
		/// <returns></returns>
		public ICVar<T> Register<T>( CVarCreateInfo<T> createInfo ) {
			if ( Cvars.TryGetValue( createInfo.Name, out ICVar? var ) ) {
				if ( var is ICVar<T> value ) {
					return value;
				}
				throw new InvalidOperationException( $"CVar {createInfo.Name} found in CVarSystem cache isn't a valid CVar object!" );
			}

			ICVar<T> cvar = new CVar<T>( this, EventBus, createInfo );
			Cvars.TryAdd( createInfo.Name, cvar );

			Logger?.PrintLine( $"CVarSystem.Register: registered CVar '{createInfo.Name}' with default value {createInfo.DefaultValue} and flags {createInfo.Flags}." );

			return cvar;
		}

		/*
		===============
		Unregister
		===============
		*/
		/// <summary>
		/// Removes the given CVar from the cache.
		/// </summary>
		/// <param name="cvar">The cvar to remove</param>
		public void Unregister( in ICVar? cvar ) {
			ArgumentNullException.ThrowIfNull( cvar );

			try {
				Cvars.TryRemove( new KeyValuePair<string, ICVar>( cvar.Name, cvar ) );
			} catch ( Exception e ) {
				Logger?.PrintError( $"CVarSystem.RemoveCVar: error removing cached CVar {cvar.Name} - {e.Message}" );
			}
		}

		/*
		===============
		GetCVar
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ICVar? GetCVar<T>( string? name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			return Cvars.TryGetValue( name, out ICVar? cvar ) ? cvar as CVar<T> : null;
		}

		/*
		===============
		AddGroup
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="group"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void AddGroup( in CVarGroup? group ) {
			ArgumentNullException.ThrowIfNull( group );

			if ( Groups.Contains( group ) ) {
				throw new InvalidOperationException( $"CVarGroup {group.Name} added twice" );
			}
			Groups.Add( group );

			Logger?.PrintLine( $"CVarSystem.AddGroup: Added CVar group {group.Name}." );
		}

		/*
		===============
		Find
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ICVar? Find( string? name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			return Cvars.TryGetValue( name, out ICVar? cvar ) ? cvar : null;
		}

		/*
		===============
		TryFind
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="cvar"></param>
		/// <returns></returns>
		public bool TryFind<T>( string? name, out CVar<T>? cvar ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( Cvars.TryGetValue( name, out ICVar? var ) && var is CVar<T> typedVar ) {
				cvar = typedVar;
				return true;
			}

			cvar = null;
			return false;
		}

		/*
		===============
		Save
		===============
		*/
		/// <summary>
		/// Writes all CVars and their corresponding groups to the provided configuration file in .ini format
		/// </summary>
		/// <param name="configFile">The file to write .ini values tos</param>
		public void Save( string? configFile ) {
			ArgumentException.ThrowIfNullOrEmpty( configFile );
			ArgumentNullException.ThrowIfNull( Cvars );

			// ensure we block all access
			lock ( Cvars ) {
				ConfigFileWriter writer = new ConfigFileWriter( configFile, Groups );
			}
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		/// Loads cvar values from the provided configuration file
		/// </summary>
		/// <param name="configFile">The file to load .ini values from</param>
		public void Load( string? configFile ) {
			ArgumentException.ThrowIfNullOrEmpty( configFile );

			// make sure we actually have something to load
			if ( Cvars == null || Cvars.IsEmpty ) {
				Logger?.PrintLine( "CVarSystem.Load: no cvars yet." );
				return;
			}

			// ensure we block all access
			lock ( Cvars ) {
				ConfigFileReader reader = new ConfigFileReader( configFile );

				foreach ( var group in Groups ) {
					foreach ( var cvar in group.Cvars ) {
						ICVar var = Cvars[ cvar ];
						if ( reader.TryGetValue( cvar, out string? value ) ) {
							ArgumentException.ThrowIfNullOrEmpty( value );

							var.SetFromString( value );
						}
					}
				}
			}
		}

		/*
		===============
		GetCVars
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ICVar[] GetCVars() {
			return [ .. Cvars.Values ];
		}

		/*
		===============
		GetCVarsWithValueType
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ICVar[] GetCVarsWithValueType<T>() {
			List<ICVar> cvars = new List<ICVar>();
			Type type = typeof( T );

			foreach ( var cvar in Cvars ) {
				if ( cvar.Value.ValueType == type ) {
					cvars.Add( cvar.Value );
				}
			}

			return [ .. cvars ];
		}

		/*
		===============
		GetCVarsInGroup
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="groupName"></param>
		/// <returns></returns>
		public ICVar[] GetCVarsInGroup( string? groupName ) {
			List<ICVar> cvars = new List<ICVar>();

			return [ .. cvars ];
		}

		/*
		===============
		GroupExists
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="groupName"></param>
		/// <returns></returns>
		public bool GroupExists( string? groupName ) {
			throw new NotImplementedException();
		}

		/*
		===============
		Restart
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Restart() {
			throw new NotImplementedException();
		}

		public bool CVarExists( string? name ) {
			throw new NotImplementedException();
		}

		ICVar<T>? ICVarSystemService.GetCVar<T>( string? name ) {
			throw new NotImplementedException();
		}

		public ICVar GetCVar( string? name ) {
			throw new NotImplementedException();
		}

		ICVar<T>[] ICVarSystemService.GetCVarsWithValueType<T>() {
			throw new NotImplementedException();
		}

	};
};