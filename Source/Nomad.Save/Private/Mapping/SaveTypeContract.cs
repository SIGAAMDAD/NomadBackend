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

namespace Nomad.Save.Private.Mapping
{
	internal sealed class SaveTypeContract
	{
		public Type Type { get; }
		public string SectionName { get; }
		public int Version { get; }
		public IReadOnlyList<SaveMemberContract> Members { get; }

		public SaveTypeContract(
			Type type,
			string sectionName,
			int version,
			IReadOnlyList<SaveMemberContract> members
		)
		{
			Type = type;
			SectionName = sectionName;
			Version = version;
			Members = members;
		}
	};
};
