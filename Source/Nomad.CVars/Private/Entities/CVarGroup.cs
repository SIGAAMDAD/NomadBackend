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

using System.Collections.Generic;
using Nomad.Core.Util;
using Nomad.Core.Logger;
using Nomad.Core.CVars;
using Nomad.Core.Compatibility.Guards;
using System.Collections.Immutable;

namespace Nomad.CVars.Private.Entities
{
	/*
	===================================================================================

	CVarGroup

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class CVarGroup : ICVarGroup
	{
		public string Name => _name;
		private readonly InternString _name;

		public IImmutableList<ICVar> CVars => _cvars.ToImmutableList();
		private readonly HashSet<ICVar> _cvars = new();

		private readonly ILoggerCategory _category;

		/*
		===============
		CVarGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="category"></param>
		public CVarGroup( string name, ILoggerCategory category )
		{
			_name = new InternString( name );
			_category = category;
		}

		/*
		===============
		AddCVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="cvar"></param>
		public void AddCVar( ICVar cvar )
		{
			ArgumentGuard.ThrowIfNull( cvar );

			if ( _cvars.Contains( cvar ) ) {
				_category.PrintWarning( $"CVarGroup.AddCVar: CVar '{cvar.Name}' already in group '{Name}'!" );
				return;
			}
			_cvars.Add( cvar );
		}

		/*
		===============
		RemoveCVar
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvar"></param>
		public void RemoveCVar( ICVar cvar )
		{
			ArgumentGuard.ThrowIfNull( cvar );

			if ( !_cvars.Contains( cvar ) ) {
				_category.PrintWarning( $"CVarGroup.RemoveCVar: CVar '{cvar.Name}' not in group '{Name}'!" );
				return;
			}
			_cvars.Remove( cvar );
		}
	};
};
