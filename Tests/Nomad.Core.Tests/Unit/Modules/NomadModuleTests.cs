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

using System.Reflection;
using Nomad.Core.Util.Attributes;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Modules")]
	[Category("Unit")]
	public class NomadModuleTests
	{
		private NomadModule _module;

		[SetUp]
		public void Setup()
		{
			var asm = typeof(NomadModule).Assembly;
			_module = asm.GetCustomAttribute<NomadModule>();
		}

		[Test]
		public void Module_HasCorrectMetadata()
		{
			Assert.That(_module.Name, Is.EqualTo("Nomad.Core"));
		}
	}
}