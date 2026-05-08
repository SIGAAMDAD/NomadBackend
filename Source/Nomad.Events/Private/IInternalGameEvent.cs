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

using Nomad.Core.Events;
using Nomad.Events.Private.SubscriptionSets;

namespace Nomad.Events.Private
{
	/*
	===================================================================================
	
	IInternalGameEvent
	
	===================================================================================
	*/
	/// <summary>
	/// An internal representation of the <see cref="IGameEvent"/> interface to ensure we have equal access
	/// to the internal objects of each game event whilst keeping it modular and decoupled.
	/// </summary>

	internal interface IInternalGameEvent<TArgs> : IGameEvent<TArgs>
		where TArgs : struct
	{
		/// <summary>
		/// 
		/// </summary>
		ISubscriptionSet<TArgs> SubscriptionSet { get; }
	};
};
