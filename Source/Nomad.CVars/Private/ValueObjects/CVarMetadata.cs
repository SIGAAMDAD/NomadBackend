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

using System.Runtime.InteropServices;
using Nomad.Core.Util;
using Nomad.Core.CVars;

namespace Nomad.CVars.Private.ValueObjects
{
	/*
	===================================================================================

	CVarMetadata

	===================================================================================
	*/
	/// <summary>
	/// Holds all a <see cref="ICVar"/>'s metadata.
	/// </summary>

	[StructLayout( LayoutKind.Explicit, Pack = 16, Size = 24 )]
	internal readonly struct CVarMetadata
	{
		[FieldOffset( 0 )] public readonly InternString Name;
		[FieldOffset( 8 )] public readonly InternString Description;
		[FieldOffset( 16 )] public readonly CVarFlags Flags;
		[FieldOffset( 20 )] public readonly CVarType Type;

		public bool IsReadOnly => Flags.HasFlag( CVarFlags.ReadOnly );
		public bool IsHidden => Flags.HasFlag( CVarFlags.Hidden );
		public bool IsSaved => Flags.HasFlag( CVarFlags.Archive );
		public bool IsUserCreated => Flags.HasFlag( CVarFlags.UserCreated );
		public bool IsDeveloper => Flags.HasFlag( CVarFlags.Developer );
		public bool IsInitializationOnly => Flags.HasFlag( CVarFlags.Init );

		/*
		===============
		CVarMetadata
		===============
		*/
		/// <summary>
		/// Creates a new CVarMetadata object.
		/// </summary>
		/// <param name="name">The CVar's name.</param>
		/// <param name="description">The CVar's description.</param>
		/// <param name="flags">The CVar's flags.</param>
		/// <param name="type">The CVar's type.</param>
		public CVarMetadata( string name, string description, CVarFlags flags, CVarType type )
		{
			Name = new InternString( name );
			Description = new InternString( description );
			Flags = flags;
			Type = type;
		}
	};
};
