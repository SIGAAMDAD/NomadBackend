/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Collections.Concurrent;
using System.Linq;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.CVars.Private.Entities;
using Nomad.CVars.Private.Repositories;
using Nomad.CVars.Private.Serialization;

namespace Nomad.CVars.Private.Services
{
	/*
	===================================================================================

	CVarSystem

	===================================================================================
	*/
	/// <summary>
	/// Global manager for CVars.
	/// </summary>

	internal sealed class CVarSystem : ICVarSystemService
	{
		private readonly ConcurrentDictionary<string, CVarGroup> _groups = new();

		private readonly CVarRepository _repository;
		private readonly IGameEventRegistryService _eventFactory;
		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;

		/*
		===============
		CVarSystem
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventFactory"></param>
		/// <param name="logger"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public CVarSystem( IGameEventRegistryService eventFactory, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
			_repository = new CVarRepository( logger, eventFactory );

			_category = logger.CreateCategory( nameof( CVarSystem ), LogLevel.Info, true );

			AddGroup( "Default" );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_category.Dispose();
				_groups.Clear();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
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
		/// <returns></returns>/
		public ICVar<T> Register<T>( CVarCreateInfo<T> createInfo )
		{
			var cvar = _repository.AddCVar( createInfo, _eventFactory );
			if ( !GetCVarGroup( createInfo.Group ?? "Default", out var group ) ) {
				var newGroup = new CVarGroup( createInfo.Group, _category );
				_groups.TryAdd( newGroup.Name, newGroup );
				group = newGroup;
			}
			group.AddCVar( cvar );

			_category.PrintLine( $"CVarSystem.Register: registered CVar '{createInfo.Name}' with default value {createInfo.DefaultValue} and flags {createInfo.Flags}." );
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
		public void Unregister( ICVar cvar )
		{
			_repository.Unregister( cvar );
		}

		/*
		===============
		AddGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="groupName"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void AddGroup( string groupName )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( groupName );

			if ( _groups.ContainsKey( groupName ) ) {
				throw new InvalidOperationException( $"CVarGroup {groupName} added twice" );
			}
			_groups.TryAdd( groupName, new CVarGroup( groupName, _category ) );

			_category.PrintLine( $"CVarSystem.AddGroup: Added CVar group {groupName}." );
		}

		/*
		===============
		Save
		===============
		*/
		/// <summary>
		/// Writes all CVars and their corresponding groups to the provided configuration file in .ini format
		/// </summary>
		/// <param name="fileSystem"></param>
		/// <param name="configFile">The file to write .ini values tos</param>
		public void Save( IFileSystem fileSystem, string configFile )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( configFile );

			// ensure we block all access
			lock ( _groups ) {
				ConfigFileWriter writer = new ConfigFileWriter( configFile, _category, this, fileSystem, _groups.Values );
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
		/// <param name="fileSystem"></param>
		/// <param name="configFile">The file to load .ini values from</param>
		public void Load( IFileSystem fileSystem, string configFile )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( configFile );

			// ensure we block all access
			lock ( _repository ) {
				_category.PrintLine( "Loading cvar configuration..." );
				IniLoader reader = new IniLoader( configFile, _category, fileSystem );

				foreach ( var group in _groups ) {
					foreach ( var cvar in group.Value.CVars ) {
						string name = $"{group.Key}:{cvar.Name}";
						_category.PrintLine( $"Loading CVar '{cvar.Name}'" );
						switch ( cvar.Type ) {
							case CVarType.Int:
								if ( reader.LoadConfigValue( name, out int intValue ) ) {
									cvar.SetIntegerValue( intValue );
								}
								break;
							case CVarType.UInt:
								if ( reader.LoadConfigValue( name, out uint uintValue ) ) {
									cvar.SetUIntegerValue( uintValue );
								}
								break;
							case CVarType.Decimal:
								if ( reader.LoadConfigValue( name, out float floatValue ) ) {
									cvar.SetDecimalValue( floatValue );
								}
								break;
							case CVarType.String:
								if ( reader.LoadConfigValue( name, out string strValue ) ) {
									cvar.SetStringValue( strValue );
								}
								break;
							case CVarType.Boolean:
								if ( reader.LoadConfigValue( name, out bool boolValue ) ) {
									cvar.SetBooleanValue( boolValue );
								}
								break;
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
		public ICVar[] GetCVars()
		{
			return _repository.CVars.ToArray();
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
		public ICVar[]? GetCVarsInGroup( string groupName )
		{
			if ( !GetCVarGroup( groupName, out var group ) ) {
				return null;
			}
			return group.CVars.ToArray();
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
		public bool GroupExists( string groupName )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( groupName );

			foreach ( var group in _groups ) {
				if ( string.Equals( group.Key, groupName, StringComparison.CurrentCultureIgnoreCase ) ) {
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
		public void Restart()
		{
			_category.PrintLine( "CVarSystem.Restart: resetting all cvars..." );
			foreach ( var cvar in _repository.CVars ) {
				cvar.Reset();
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
		public bool CVarExists( string name )
			=> _repository.CVarExists( name );

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
		public bool CVarExists<T>( string name )
			=> _repository.CVarExists<T>( name );

		/*
		===============
		GetCVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException"></exception>
		public ICVar<T>? GetCVar<T>( string name )
			=> _repository.GetCVar<T>( name );

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
		public ICVar? GetCVar( string name )
			=> _repository.GetCVar( name );

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
		public ICVar<T>[] GetCVarsWithValueType<T>()
			=> _repository.GetCVarsWithValueType<T>();

		/*
		===============
		TryFind
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="cvar"></param>
		/// <returns></returns>
		public bool TryFind( string name, out ICVar? cvar )
			=> _repository.TryFind( name, out cvar );

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
		public bool TryFind<T>( string name, out ICVar<T>? cvar )
			=> _repository.TryFind( name, out cvar );

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
		private bool GetCVarGroup( string name, out ICVarGroup? group )
		{
			group = null;
			foreach ( var value in _groups ) {
				if ( string.Equals( value.Key, name, StringComparison.CurrentCultureIgnoreCase ) ) {
					group = value.Value;
					return true;
				}
			}
			return false;
		}
	};
};
