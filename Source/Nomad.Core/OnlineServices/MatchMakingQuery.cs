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

namespace Nomad.Core.OnlineServices
{
    public sealed record MatchMakingQuery
	{
		public IReadOnlyList<string>? Maps { get; init; }
		public IReadOnlyList<string>? GameModes { get; init; }
		public IReadOnlyDictionary<string, string>? Metadata { get; init; }
		public bool FriendsOnly { get; init; }
		public ServerRange Range { get; init; }
		public int MaxResults { get; init; } = 50;
	}
}
