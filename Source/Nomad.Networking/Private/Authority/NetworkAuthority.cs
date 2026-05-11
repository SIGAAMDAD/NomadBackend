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
using Nomad.Core.Compatibility.Guards;
using Nomad.Networking.Authority;

namespace Nomad.Networking.Private.Authority
{
	/*
	===================================================================================

	NetworkAuthority

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class NetworkAuthority : INetworkAuthority
	{
		private readonly List<INetworkAuthorityRule> _rules = new();

		public NetworkAuthorityDecision DefaultDecision { get; set; } = NetworkAuthorityDecision.Deny;

		/*
		===============
		AddRule
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="rule"></param>
		public void AddRule( INetworkAuthorityRule rule )
		{
			ArgumentGuard.ThrowIfNull( rule, nameof( rule ) );
			_rules.Add( rule );
		}

		/*
		===============
		Evaluate
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public bool Evaluate( in NetworkAuthorityContext context )
		{
			bool allowed = false;

			for ( int i = 0; i < _rules.Count; i++ ) {
				NetworkAuthorityDecision decision = _rules[i].Evaluate( in context );

				if ( decision == NetworkAuthorityDecision.Deny ) {
					return false;
				}

				if ( decision == NetworkAuthorityDecision.Allow ) {
					allowed = true;
				}
			}

			if ( allowed ) {
				return true;
			}

			return DefaultDecision == NetworkAuthorityDecision.Allow;
		}
	};
};
