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
using Nomad.Core.Engine.Windowing;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("ValueObjects")]
	[Category("Unit")]
	[Category("UnitTests")]
	public class AspectRatioTests
	{
		[TestCase(AspectRatio.Aspect_Automatic, 1.0f)]
		[TestCase(AspectRatio.Aspect_4_3, 4.0f / 3.0f)]
		[TestCase(AspectRatio.Aspect_16_10, 16.0f / 10.0f)]
		[TestCase(AspectRatio.Aspect_16_9, 16.0f / 9.0f)]
		[TestCase(AspectRatio.Aspect_21_9, 21.0f / 9.0f)]
		public void AspectRatio_GetRatio_ReturnsExpectedRatio(AspectRatio aspectRatio, float expectedRatio)
		{
			Assert.That(aspectRatio.GetRatio(), Is.EqualTo(expectedRatio));
		}

		[TestCase(AspectRatio.Count)]
		[TestCase((AspectRatio)byte.MaxValue)]
		public void AspectRatio_GetRatio_ThrowsForUnsupportedValue(AspectRatio aspectRatio)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => aspectRatio.GetRatio());
		}
	}
}
