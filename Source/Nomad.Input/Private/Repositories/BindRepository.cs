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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.FileSystem;
using Nomad.Core.Input;
using Nomad.Core.Logger;
using Nomad.CVars;
using Nomad.Input.Extensions;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Repositories
{
	/*
	===================================================================================
	
	BindRepository
	
	===================================================================================
	*/
	/// <summary>
	/// Loads binding databases from disk and exposes the merged action definitions used by the input pipeline.
	/// </summary>
	internal sealed class BindRepository : IDisposable
	{
		private readonly IFileSystem _fileSystem;
		private readonly ILoggerCategory _category;
		private readonly BindLoader _loader;
		private readonly string _defaultsPath;
		private readonly ICVar<string> _keyboardMouseMapping;
		private readonly ICVar<string> _gamepadMapping;

		private ImmutableArray<InputActionDefinition> _defaultBindings = ImmutableArray<InputActionDefinition>.Empty;
		private ImmutableDictionary<string, LoadedBindMapping> _loadedMappings = ImmutableDictionary<string, LoadedBindMapping>.Empty;
		private ImmutableDictionary<string, ImmutableArray<InputActionDefinition>> _bindMappings = ImmutableDictionary<string, ImmutableArray<InputActionDefinition>>.Empty;
		private ImmutableDictionary<InputScheme, string> _activeMappings = ImmutableDictionary<InputScheme, string>.Empty;
		private ImmutableArray<InputActionDefinition> _allBindings = ImmutableArray<InputActionDefinition>.Empty;

		private bool _isDisposed = false;

		/*
		===============
		BindRepository
		===============
		*/
		/// <summary>
		/// Initializes the repository and loads the default bindings plus every named binding mapping on disk.
		/// </summary>
		/// <param name="fileSystem">The file system used to discover and read binding definition files.</param>
		/// <param name="cvarSystem">The cvar system that supplies the configured defaults file path.</param>
		/// <param name="logger"></param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="fileSystem"/>, <paramref name="cvarSystem"/>, or <paramref name="logger"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="FileNotFoundException">Thrown when the configured defaults binding file cannot be found.</exception>
		public BindRepository( IFileSystem fileSystem, ICVarSystemService cvarSystem, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( cvarSystem, nameof( cvarSystem ) );
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_loader = new BindLoader( _fileSystem, logger );
			_category = logger.CreateCategory( nameof( BindRepository ), LogLevel.Info, true );

			var defaultsPath = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.DEFAULTS_PATH );
			_defaultsPath = defaultsPath.Value;
			_keyboardMouseMapping = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.KEYBOARD_MOUSE_MAPPING );
			_gamepadMapping = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.GAMEPAD_MAPPING );

			Reload();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Marks the repository as disposed so it can no longer be queried or reloaded.
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_category?.Dispose();
				_isDisposed = true;
			}
			GC.SuppressFinalize( this );
		}

		/*
		===============
		GetDefaultBindings
		===============
		*/
		/// <summary>
		/// Gets the bindings loaded from the configured defaults database.
		/// </summary>
		/// <returns>The default action definitions.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when the repository has been disposed.</exception>
		public ImmutableArray<InputActionDefinition> GetDefaultBindings()
		{
			ThrowIfDisposed();
			return _defaultBindings;
		}

		/*
		===============
		GetAllBindings
		===============
		*/
		/// <summary>
		/// Gets a merged view of the defaults database and every discovered binding mapping.
		/// </summary>
		/// <returns>All loaded action definitions merged by action id.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when the repository has been disposed.</exception>
		public ImmutableArray<InputActionDefinition> GetAllBindings()
		{
			ThrowIfDisposed();
			return _allBindings;
		}

		/*
		===============
		GetBindMappings
		===============
		*/
		/// <summary>
		/// Gets every discovered named binding mapping with the defaults already merged into each mapping.
		/// </summary>
		/// <returns>A dictionary keyed by mapping name.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when the repository has been disposed.</exception>
		public ImmutableDictionary<string, ImmutableArray<InputActionDefinition>> GetBindMappings()
		{
			ThrowIfDisposed();
			return _bindMappings;
		}

		/*
		===============
		GetMappingsForScheme
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <returns></returns>
		public IReadOnlyList<string> GetMappingsForScheme( InputScheme scheme )
		{
			ThrowIfDisposed();

			return _loadedMappings.Values
				.Where( mapping => mapping.Schemes.Contains( scheme ) )
				.Select( mapping => mapping.Name )
				.OrderBy( name => name, StringComparer.OrdinalIgnoreCase )
				.ToArray();
		}

		/*
		===============
		GetActiveMapping
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <returns></returns>
		public string? GetActiveMapping( InputScheme scheme )
		{
			ThrowIfDisposed();
			return _activeMappings.TryGetValue( scheme, out var mappingName ) ? mappingName : null;
		}

		/*
		===============
		SetActiveMapping
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <param name="mappingName"></param>
		/// <returns></returns>
		public bool SetActiveMapping( InputScheme scheme, string mappingName )
		{
			ThrowIfDisposed();

			if ( string.IsNullOrWhiteSpace( mappingName ) ) {
				return false;
			}
			if ( !_loadedMappings.TryGetValue( mappingName, out var mapping ) || !mapping.Schemes.Contains( scheme ) ) {
				return false;
			}

			SetMappingSetting( scheme, mappingName );
			_activeMappings = _activeMappings.SetItem( scheme, mappingName );
			RebuildCaches();
			return true;
		}

		/*
		===============
		TryGetBindMapping
		===============
		*/
		/// <summary>
		/// Attempts to retrieve a named binding mapping with the defaults already merged into it.
		/// </summary>
		/// <param name="mappingName">The mapping name, typically derived from the binding file name.</param>
		/// <param name="bindings">The merged bindings for the requested mapping when it exists.</param>
		/// <returns><see langword="true"/> when the mapping exists; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when the repository has been disposed.</exception>
		public bool TryGetBindMapping( string mappingName, out ImmutableArray<InputActionDefinition> bindings )
		{
			ThrowIfDisposed();

			if ( string.IsNullOrWhiteSpace( mappingName ) ) {
				bindings = ImmutableArray<InputActionDefinition>.Empty;
				return false;
			}

			if ( !_bindMappings.TryGetValue( mappingName, out bindings ) ) {
				bindings = ImmutableArray<InputActionDefinition>.Empty;
				return false;
			}
			return true;
		}

		/*
		===============
		SetActionBindings
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mappingName"></param>
		/// <param name="actionId"></param>
		/// <param name="bindings"></param>
		/// <returns></returns>
		public bool SetActionBindings( string mappingName, string actionId, ImmutableArray<InputBindingDefinition> bindings )
		{
			ThrowIfDisposed();

			if ( string.IsNullOrWhiteSpace( mappingName ) || string.IsNullOrWhiteSpace( actionId ) ) {
				return false;
			}
			if ( !_loadedMappings.TryGetValue( mappingName, out var mapping ) ) {
				return false;
			}

			var builder = mapping.Actions.ToBuilder();
			for ( int i = 0; i < builder.Count; i++ ) {
				if ( builder[i].Id.Equals( actionId, StringComparison.Ordinal ) ) {
					builder[i] = new InputActionDefinition(
						builder[i].Name,
						builder[i].Id,
						builder[i].ValueType,
						bindings.Clone()
					);

					_loadedMappings = _loadedMappings.SetItem( mappingName, new LoadedBindMapping( mappingName, builder.ToImmutable() ) );
					ValidateActiveMappings();
					RebuildCaches();
					return true;
				}
			}

			if ( !_bindMappings.TryGetValue( mappingName, out var mergedMapping ) ) {
				return false;
			}

			for ( int i = 0; i < mergedMapping.Length; i++ ) {
				if ( !mergedMapping[i].Id.Equals( actionId, StringComparison.Ordinal ) ) {
					continue;
				}

				builder.Add( new InputActionDefinition( mergedMapping[i].Name, mergedMapping[i].Id, mergedMapping[i].ValueType, bindings.Clone() ) );
				_loadedMappings = _loadedMappings.SetItem( mappingName, new LoadedBindMapping( mappingName, builder.ToImmutable() ) );
				ValidateActiveMappings();
				RebuildCaches();
				return true;
			}

			return false;
		}

		/*
		===============
		Reload
		===============
		*/
		/// <summary>
		/// Reloads the defaults database and all discovered binding mapping files from disk.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown when the repository has been disposed.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the configured defaults binding file cannot be found.</exception>
		/// <exception cref="InvalidOperationException">Thrown when two mapping files resolve to the same mapping name.</exception>
		public void Reload()
		{
			ThrowIfDisposed();

			if ( !_loader.LoadBindDatabase( _defaultsPath, out var defaultBindings ) ) {
				throw new FileNotFoundException( $"Default bindings file '{_defaultsPath}' could not be found.", _defaultsPath );
			}

			_defaultBindings = defaultBindings;
			LoadAllBindMappings();
		}

		/*
		===============
		LoadAllBindMappings
		===============
		*/
		/// <summary>
		/// Loads every binding mapping file from the configured mappings directory and rebuilds the merged caches.
		/// </summary>
		private void LoadAllBindMappings()
		{
			var mappingBuilder = ImmutableDictionary.CreateBuilder<string, LoadedBindMapping>( StringComparer.OrdinalIgnoreCase );
			var files = _fileSystem.GetFiles( Constants.BINDINGS_DIRECTORY, "*.json", true );

			for ( int i = 0; i < files.Count; i++ ) {
				string filePath = files[i];
				if ( string.Equals( filePath, _defaultsPath, StringComparison.OrdinalIgnoreCase ) ) {
					continue;
				}
				if ( !_loader.LoadBindDatabase( filePath, out var mappingBindings ) ) {
					continue;
				}

				string mappingName = GetBindMappingName( filePath );
				if ( mappingBuilder.ContainsKey( mappingName ) ) {
					throw new InvalidOperationException( $"Duplicate binding mapping name '{mappingName}' discovered while loading '{filePath}'." );
				}

				_category.PrintLine( $"Loaded bind mapping '{mappingName}'..." );

				mappingBuilder[mappingName] = new LoadedBindMapping( mappingName, mappingBindings );
			}

			_loadedMappings = mappingBuilder.ToImmutable();
			RefreshActiveMappingsFromSettings();
			ValidateActiveMappings();
			RebuildCaches();
		}

		/*
		===============
		GetBindMappingName
		===============
		*/
		/// <summary>
		/// Derives the repository mapping key from a binding file path.
		/// </summary>
		/// <param name="filePath">The binding file path.</param>
		/// <returns>The file name without its extension.</returns>
		private static string GetBindMappingName( string filePath )
		{
			return Path.GetFileNameWithoutExtension( filePath );
		}

		/*
		===============
		MergeActions
		===============
		*/
		/// <summary>
		/// Merges action definition sets by action id while preserving the order in which the sources were supplied.
		/// </summary>
		/// <param name="sources">The action definition sets to merge.</param>
		/// <returns>A single merged action definition array.</returns>
		private static ImmutableArray<InputActionDefinition> MergeActions( params ImmutableArray<InputActionDefinition>[] sources )
		{
			return MergeActions( (IEnumerable<ImmutableArray<InputActionDefinition>>)sources );
		}

		/*
		===============
		MergeActions
		===============
		*/
		/// <summary>
		/// Merges action definition sets by action id while concatenating the bindings for matching actions.
		/// </summary>
		/// <param name="sources">The action definition sets to merge.</param>
		/// <returns>A single merged action definition array.</returns>
		/// <exception cref="InvalidOperationException">Thrown when two actions share an id but disagree on value type.</exception>
		private static ImmutableArray<InputActionDefinition> MergeActions( IEnumerable<ImmutableArray<InputActionDefinition>> sources )
		{
			var actionIndices = new Dictionary<string, int>( StringComparer.Ordinal );
			var builder = ImmutableArray.CreateBuilder<InputActionDefinition>();

			foreach ( var source in sources ) {
				for ( int i = 0; i < source.Length; i++ ) {
					var action = source[i];
					if ( !actionIndices.TryGetValue( action.Id, out int actionIndex ) ) {
						actionIndices.Add( action.Id, builder.Count );
						builder.Add( action );
						continue;
					}

					var existing = builder[actionIndex];
					if ( existing.ValueType != action.ValueType ) {
						throw new InvalidOperationException( $"Binding action '{action.Id}' has conflicting value types across binding databases." );
					}

					builder[actionIndex] = new InputActionDefinition(
						existing.Name,
						existing.Id,
						existing.ValueType,
						OverrideBindingsByScheme( existing.Bindings, action.Bindings )
					);
				}
			}

			return builder.ToImmutable();
		}

		/*
		===============
		OverrideBindingsByScheme
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private static ImmutableArray<InputBindingDefinition> OverrideBindingsByScheme( ImmutableArray<InputBindingDefinition> left, ImmutableArray<InputBindingDefinition> right )
		{
			if ( right.IsDefaultOrEmpty ) {
				return left;
			}

			var overriddenSchemes = ImmutableHashSet.CreateBuilder<InputScheme>();
			for ( int i = 0; i < right.Length; i++ ) {
				overriddenSchemes.Add( right[i].Scheme );
			}

			var builder = ImmutableArray.CreateBuilder<InputBindingDefinition>( left.Length + right.Length );

			for ( int i = 0; i < left.Length; i++ ) {
				if ( overriddenSchemes.Contains( left[i].Scheme ) ) {
					continue;
				}
				builder.Add( left[i] );
			}
			for ( int i = 0; i < right.Length; i++ ) {
				if ( ContainsBinding( builder, right[i] ) ) {
					continue;
				}
				builder.Add( right[i] );
			}

			return builder.ToImmutable();
		}

		/*
		===============
		RefreshActiveMappingsFromSettings
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		private void RefreshActiveMappingsFromSettings()
		{
			var activeMappings = ImmutableDictionary.CreateBuilder<InputScheme, string>();
			AddActiveMappingFromSetting( activeMappings, InputScheme.KeyboardAndMouse, _keyboardMouseMapping.Value );
			AddActiveMappingFromSetting( activeMappings, InputScheme.Gamepad, _gamepadMapping.Value );
			_activeMappings = activeMappings.ToImmutable();
		}

		/*
		===============
		AddActiveMappingFromSetting
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="activeMappings"></param>
		/// <param name="scheme"></param>
		/// <param name="mappingName"></param>
		private void AddActiveMappingFromSetting( ImmutableDictionary<InputScheme, string>.Builder activeMappings, InputScheme scheme, string? mappingName )
		{
			if ( string.IsNullOrWhiteSpace( mappingName ) ) {
				return;
			}
			if ( !_loadedMappings.TryGetValue( mappingName, out var mapping ) || !mapping.Schemes.Contains( scheme ) ) {
				return;
			}

			activeMappings[scheme] = mappingName;
		}

		/*
		===============
		ValidateActiveMappings
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		private void ValidateActiveMappings()
		{
			var builder = _activeMappings.ToBuilder();
			foreach ( var pair in _activeMappings ) {
				if ( !_loadedMappings.TryGetValue( pair.Value, out var mapping ) || !mapping.Schemes.Contains( pair.Key ) ) {
					builder.Remove( pair.Key );
				}
			}
			_activeMappings = builder.ToImmutable();
		}

		/*
		===============
		RebuildCaches
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		private void RebuildCaches()
		{
			var mergedMappings = ImmutableDictionary.CreateBuilder<string, ImmutableArray<InputActionDefinition>>( StringComparer.OrdinalIgnoreCase );
			foreach ( var pair in _loadedMappings ) {
				mergedMappings[pair.Key] = MergeActions( _defaultBindings, pair.Value.Actions );
			}

			_bindMappings = mergedMappings.ToImmutable();

			var sources = new List<ImmutableArray<InputActionDefinition>> { _defaultBindings };
			var includedMappings = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
			foreach ( var pair in _activeMappings ) {
				if ( !includedMappings.Add( pair.Value ) ) {
					continue;
				}
				if ( _loadedMappings.TryGetValue( pair.Value, out var mapping ) ) {
					sources.Add( mapping.Actions );
				}
			}

			_allBindings = MergeActions( sources );
		}

		/*
		===============
		SetMappingSetting
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <param name="mappingName"></param>
		private void SetMappingSetting( InputScheme scheme, string mappingName )
		{
			switch ( scheme ) {
				case InputScheme.KeyboardAndMouse:
					_keyboardMouseMapping.Value = mappingName;
					break;
				case InputScheme.Gamepad:
					_gamepadMapping.Value = mappingName;
					break;
			}
		}

		/*
		===============
		ContainsBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindings"></param>
		/// <param name="candidate"></param>
		/// <returns></returns>
		private static bool ContainsBinding( ImmutableArray<InputBindingDefinition>.Builder bindings, in InputBindingDefinition candidate )
		{
			for ( int i = 0; i < bindings.Count; i++ ) {
				if ( bindings[i].ContentEquals( candidate ) ) {
					return true;
				}
			}
			return false;
		}

		/*
		===============
		ThrowIfDisposed
		===============
		*/
		/// <summary>
		/// Throws when the repository has already been disposed.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown when the repository has already been disposed.</exception>
		private void ThrowIfDisposed()
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
		}

		private sealed class LoadedBindMapping
		{
			public string Name { get; }
			public ImmutableArray<InputActionDefinition> Actions { get; }
			public ImmutableHashSet<InputScheme> Schemes { get; }

			public LoadedBindMapping( string name, ImmutableArray<InputActionDefinition> actions )
			{
				Name = name;
				Actions = actions;
				Schemes = GetSchemes( actions );
			}

			private static ImmutableHashSet<InputScheme> GetSchemes( ImmutableArray<InputActionDefinition> actions )
			{
				var builder = ImmutableHashSet.CreateBuilder<InputScheme>();
				for ( int i = 0; i < actions.Length; i++ ) {
					for ( int j = 0; j < actions[i].Bindings.Length; j++ ) {
						builder.Add( actions[i].Bindings[j].Scheme );
					}
				}
				return builder.ToImmutable();
			}
		}
	};
};
