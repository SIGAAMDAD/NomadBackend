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

namespace Nomad.Core.OnlineServices
{
	public sealed record NetworkSessionJoinResult
	{
		public bool Success { get; init; }
		public NetworkSessionInfo? Session { get; init; }
		public NetworkSessionFailureReason Reason { get; init; }

		public static NetworkSessionJoinResult Failed( NetworkSessionFailureReason reason )
		{
			return new NetworkSessionJoinResult { Success = false, Reason = reason };
		}

		public static NetworkSessionJoinResult Joined( NetworkSessionInfo session )
		{
			return new NetworkSessionJoinResult { Success = true, Session = session };
		}
	}
}
