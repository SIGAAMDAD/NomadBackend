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

using NomadCore.Systems.ConsoleSystem.CVars.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadCore.Systems.ConsoleSystem.CVars.Common;
using NomadCore.GameServices;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Infrastructure.Collections;

namespace NomadCore.Systems.ConsoleSystem.CVars.Services {
	/*
	===================================================================================
	
	CVarSystem
	
	===================================================================================
	*/
	/// <summary>
	/// Global manager for CVars
	/// </summary>

	public sealed class CVarSystem( IGameEventRegistryService eventFactory, ILoggerService logger ) : ICVarSystemService {
		private readonly ConcurrentDictionary<InternString, ICVar> _cvars = new ConcurrentDictionary<InternString, ICVar>();
		private readonly HashSet<CVarGroup> _groups = new HashSet<CVarGroup>();

		private readonly IGameEventRegistryService _eventFactory = eventFactory;
		private readonly ILoggerService _logger = logger;

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_cvars.Clear();
			_groups.Clear();
		}

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvar"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Register( in ICVar cvar ) {
			ArgumentNullException.ThrowIfNull( cvar );

			if ( !_cvars.ContainsKey( cvar.Name ) ) {
				_cvars.TryAdd( cvar.Name, cvar );
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
			if ( _cvars.TryGetValue( createInfo.Name, out ICVar? var ) ) {
				if ( var is ICVar<T> value ) {
					return value;
				}
				throw new InvalidOperationException( $"CVar {createInfo.Name} found in CVarSystem cache isn't a valid CVar object!" );
			}

			ICVar<T> cvar = new CVar<T>( _eventFactory, in createInfo );
			_cvars.TryAdd( createInfo.Name, cvar );

			_logger.PrintLine( $"CVarSystem.Register: registered CVar '{StringPool.FromInterned( createInfo.Name )}' with default value {createInfo.DefaultValue} and flags {createInfo.Flags}." );

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
		public void Unregister( in ICVar cvar ) {
			ArgumentNullException.ThrowIfNull( cvar );

			try {
				_cvars.TryRemove( new KeyValuePair<InternString, ICVar>( cvar.Name, cvar ) );
			} catch ( Exception e ) {
				_logger.PrintError( $"CVarSystem.RemoveCVar: error removing cached CVar {cvar.Name} - {e.Message}" );
			}
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
		public void AddGroup( in CVarGroup group ) {
			ArgumentNullException.ThrowIfNull( group );

			if ( _groups.Contains( group ) ) {
				throw new InvalidOperationException( $"CVarGroup {group.Name} added twice" );
			}
			_groups.Add( group );

			_logger.PrintLine( $"CVarSystem.AddGroup: Added CVar group {group.Name}." );
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
		public ICVar? Find( string name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			return _cvars.TryGetValue( StringPool.Intern( name ), out ICVar? cvar ) ? cvar : null;
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
		public bool TryFind<T>( string name, out CVar<T>? cvar ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( _cvars.TryGetValue( StringPool.Intern( name ), out ICVar? var ) && var is CVar<T> typedVar ) {
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
		public void Save( string configFile ) {
			ArgumentException.ThrowIfNullOrEmpty( configFile );
			ArgumentNullException.ThrowIfNull( _cvars );

			// ensure we block all access
			lock ( _cvars ) {
				ConfigFileWriter writer = new ConfigFileWriter( configFile, _logger, this, _groups );
			}
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		/// Loads cvar values from the provided configuration file.
		/// </summary>
		/// <param name="configFile">The file to load .ini values from</param>
		public void Load( string configFile ) {
			ArgumentException.ThrowIfNullOrEmpty( configFile );

			// make sure we actually have something to load
			if ( _cvars == null || _cvars.IsEmpty ) {
				_logger?.PrintLine( "CVarSystem.Load: no cvars yet." );
				return;
			}

			// ensure we block all access
			lock ( _cvars ) {
				ConfigFileReader reader = new ConfigFileReader( _logger, configFile );

				foreach ( var group in _groups ) {
					foreach ( var cvar in group.Cvars ) {
						ICVar var = _cvars[ cvar ];
						if ( reader.TryGetValue( cvar, out string? value ) ) {
							ArgumentException.ThrowIfNullOrEmpty( value );

							var.SetFromString( StringPool.Intern( value ) );
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
			return [ .. _cvars.Values ];
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
		public ICVar[]? GetCVarsInGroup( string groupName ) {
			if ( !GetCVarGroup( groupName, out CVarGroup? group ) ) {
				return null;
			}
			ArgumentNullException.ThrowIfNull( group );

			List<ICVar> cvars = new List<ICVar>();
			foreach ( var cvarName in group.Cvars ) {
				if ( _cvars.TryGetValue( cvarName, out var cvar ) ) {
					cvars.Add( cvar );
				}
			}
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
		public bool GroupExists( string groupName ) {
			ArgumentException.ThrowIfNullOrEmpty( groupName );

			foreach ( var group in _groups ) {
				if ( string.Equals( group.Name, groupName ) ) {
					return true;
				}
			}
			return false;
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
			_logger.PrintLine( "CVarSystem.Restart: resetting all cvars..." );
			foreach ( var cvar in _cvars ) {
				cvar.Value.Reset();
			}
		}

		/*
		===============
		CVarExists
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool CVarExists( string name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			return _cvars.ContainsKey( StringPool.Intern( name ) );
		}

		/*
		===============
		GetCVar
		===============
		*/
		public ICVar<T>? GetCVar<T>( string name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( !_cvars.TryGetValue( StringPool.Intern( name ), out var cvar ) ) {
				_logger.PrintError( $"CVarSystem.GetCVar: no cvar found for name '{name}'!" );
				return null;
			}
			return cvar is ICVar<T> typedVar ? typedVar : throw new InvalidCastException( $"CVar '{name}' value type isn't '{typeof( T )}'" );
		}

		/*
		===============
		GetCVar
		===============
		*/
		public ICVar? GetCVar( string name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( !_cvars.TryGetValue( StringPool.Intern( name ), out var cvar ) ) {
				_logger.PrintError( $"CVarSystem.GetCVar: no cvar found for name '{name}'!" );
				return null;
			}
			return cvar;
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
		public ICVar<T>[] GetCVarsWithValueType<T>() {
			List<ICVar<T>> cvars = new List<ICVar<T>>();

			foreach ( var cvar in _cvars ) {
				if ( cvar.Value is ICVar<T> typedVar ) {
					cvars.Add( typedVar );
				}
			}

			return [ .. cvars ];
		}

		/*
		===============
		GetCVarGroup
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		private bool GetCVarGroup( string name, out CVarGroup? group ) {
			group = null;
			foreach ( var value in _groups ) {
				if ( string.Equals( value.Name, name ) ) {
					group = value;
					return true;
				}
			}
			return false;
		}
	};
};