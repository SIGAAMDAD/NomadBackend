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

using Nomad.Core.UI;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("ValueObjects")]
	[Category("Unit")]
	[Category("UnitTests")]
	public class AlignmentTests
	{
		[Test]
		public void Alignment_Constructor_AssignsHorizontalAndVertical()
		{
			var alignment = new Alignment(HorizontalAlignment.End, VerticalAlignment.Start);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(alignment.Horizontal, Is.EqualTo(HorizontalAlignment.End));
				Assert.That(alignment.Vertical, Is.EqualTo(VerticalAlignment.Start));
			}
		}

		[Test]
		public void Alignment_StaticPresets_ReturnExpectedValues()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(Alignment.Center.Horizontal, Is.EqualTo(HorizontalAlignment.Center));
				Assert.That(Alignment.Center.Vertical, Is.EqualTo(VerticalAlignment.Center));

				Assert.That(Alignment.TopLeft.Horizontal, Is.EqualTo(HorizontalAlignment.Start));
				Assert.That(Alignment.TopLeft.Vertical, Is.EqualTo(VerticalAlignment.Start));

				Assert.That(Alignment.TopRight.Horizontal, Is.EqualTo(HorizontalAlignment.End));
				Assert.That(Alignment.TopRight.Vertical, Is.EqualTo(VerticalAlignment.Start));

				Assert.That(Alignment.BottomLeft.Horizontal, Is.EqualTo(HorizontalAlignment.Start));
				Assert.That(Alignment.BottomLeft.Vertical, Is.EqualTo(VerticalAlignment.End));

				Assert.That(Alignment.BottomRight.Horizontal, Is.EqualTo(HorizontalAlignment.End));
				Assert.That(Alignment.BottomRight.Vertical, Is.EqualTo(VerticalAlignment.End));

				Assert.That(Alignment.Fill.Horizontal, Is.EqualTo(HorizontalAlignment.Stretch));
				Assert.That(Alignment.Fill.Vertical, Is.EqualTo(VerticalAlignment.Stretch));
			}
		}
	}
}
