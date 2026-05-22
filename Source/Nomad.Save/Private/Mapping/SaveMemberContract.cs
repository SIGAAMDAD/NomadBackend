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
using System.Reflection;
using Nomad.Save.Extensions.Attributes;

namespace Nomad.Save.Private.Mapping
{
	internal sealed class SaveMemberContract
	{
		public string SaveName { get; }
		public Type MemberType { get; }
		public MemberInfo Member { get; }
		public SaveFieldAttribute Attribute { get; }
		public Func<object, object?> Getter { get; }
		public Action<object, object?>? Setter { get; }

		public SaveMemberContract(
			string saveName,
			Type memberType,
			MemberInfo member,
			SaveFieldAttribute attribute,
			Func<object, object?> getter,
			Action<object, object?>? setter
		)
		{
			SaveName = saveName;
			MemberType = memberType;
			Member = member;
			Attribute = attribute;
			Getter = getter;
			Setter = setter;
		}
	};
};
