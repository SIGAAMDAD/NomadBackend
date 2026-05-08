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
using System.Runtime.CompilerServices;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	/*
	===================================================================================
	
	SteamLobbyKey
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal readonly struct SteamLobbyKey
	{
		public CSteamID Id => _id;
		private readonly CSteamID _id;

		public Guid Guid => _guid;
		private readonly Guid _guid;

		private readonly int _hashCode;

		/*
		===============
		SteamLobbyKey
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guid"></param>
		public SteamLobbyKey( CSteamID id, Guid guid )
		{
			_id = id;
			_guid = guid;
			_hashCode = HashCode.Combine(
				id.GetHashCode(),
				guid.GetHashCode()
			);
		}

		/*
		===============
		GetHashCode
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override int GetHashCode()
			=> _hashCode;

		/*
		===============
		Equals
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override bool Equals( object obj )
			=> obj is SteamLobbyKey key && key._guid == _guid && key._id == _id;
	};
};
