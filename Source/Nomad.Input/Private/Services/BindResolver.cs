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
using Nomad.Core.Input;
using Nomad.Input.Interfaces;
using Nomad.Input.Private.Repositories;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================
	
	BindResolver
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class BindResolver : IBindResolver
	{
		private readonly BindRepository _repository;
		private readonly Action _bindingsChanged;

		/*
		===============
		BindResolver
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="bindingsChanged"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public BindResolver( BindRepository repository, Action bindingsChanged )
		{
			_repository = repository ?? throw new ArgumentNullException( nameof( repository ) );
			_bindingsChanged = bindingsChanged ?? throw new ArgumentNullException( nameof( bindingsChanged ) );
		}

		/*
		===============
		GetBindMapping
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mappingName"></param>
		/// <returns></returns>
		public IReadOnlyList<InputActionDefinition>? GetBindMapping( string mappingName )
		{
			return _repository.TryGetBindMapping( mappingName, out var bindings ) ? bindings : null;
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
			return _repository.GetMappingsForScheme( scheme );
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
			return _repository.GetActiveMapping( scheme );
		}

		/*
		===============
		LoadMapping
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <param name="mappingName"></param>
		/// <returns></returns>
		public bool LoadMapping( InputScheme scheme, string mappingName )
		{
			if ( !_repository.SetActiveMapping( scheme, mappingName ) ) {
				return false;
			}

			_bindingsChanged();
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
		public void SetActionBindings( string mappingName, string actionId, ImmutableArray<InputBindingDefinition> bindings )
		{
			if ( _repository.SetActionBindings( mappingName, actionId, bindings ) ) {
				_bindingsChanged();
			}
		}
	};
};
