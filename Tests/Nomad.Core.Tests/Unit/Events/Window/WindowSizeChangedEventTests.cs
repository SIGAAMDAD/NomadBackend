/*
===========================================================================
The Nomad MPLv2 Source Code
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.Engine.Services;
using Nomad.Core.Engine.Windowing;
using NUnit.Framework;

namespace Nomad.Core.Tests {
	[TestFixture]
    [Category( "Nomad.Core" )]
    [Category( "Events.Window" )]
    [Category( "Unit" )]
    [Category( "UnitTests" )]
	public sealed class WindowSizeChangedEventTests {
		[Test]
		public void WindowSizeChangedEvent_Constructor_ExposesAssignedValues() {
			var sizeChangedEvent = new WindowSizeChangedEventArgs( 640, 480 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sizeChangedEvent.Width, Is.EqualTo( 640 ) );
				Assert.That( sizeChangedEvent.Height, Is.EqualTo( 480 ) );
			}
		}
	}
}