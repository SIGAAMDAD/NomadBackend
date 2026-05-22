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
    [Category("Windowing")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class WindowSizeTests
    {
        [Test]
        public void WindowSize_Constructor_AssignsDimensions()
        {
            var size = new WindowSize(1920, 1080);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(size.Width, Is.EqualTo(1920));
                Assert.That(size.Height, Is.EqualTo(1080));
            }
        }

        [TestCase(WindowResolution.Res_800x600, 800, 600)]
        [TestCase(WindowResolution.Res_1280x720, 1280, 720)]
        [TestCase(WindowResolution.Res_1920x1080, 1920, 1080)]
        [TestCase(WindowResolution.Res_3840x2160, 3840, 2160)]
        public void WindowResolution_ImplicitWindowSizeConversion_ReturnsExpectedSize(WindowResolution resolution, int expectedWidth, int expectedHeight)
        {
            WindowSize size = resolution;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(size.Width, Is.EqualTo(expectedWidth));
                Assert.That(size.Height, Is.EqualTo(expectedHeight));
            }
        }

        [TestCase(WindowResolution.Res_Native)]
        [TestCase(WindowResolution.Count)]
        [TestCase((WindowResolution)byte.MaxValue)]
        public void WindowResolution_ImplicitWindowSizeConversion_ThrowsForNonFixedSizeResolution(WindowResolution resolution)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = (WindowSize)resolution;
            });
        }

        [TestCase(800, 600, WindowResolution.Res_800x600)]
        [TestCase(1280, 720, WindowResolution.Res_1280x720)]
        [TestCase(1920, 1080, WindowResolution.Res_1920x1080)]
        [TestCase(3440, 1440, WindowResolution.Res_3440x1440)]
        public void WindowSize_ImplicitWindowResolutionConversion_ReturnsMatchingResolution(int width, int height, WindowResolution expectedResolution)
        {
            var size = new WindowSize(width, height);
            WindowResolution resolution = size;

            Assert.That(resolution, Is.EqualTo(expectedResolution));
        }

        [Test]
        public void WindowSize_ImplicitWindowResolutionConversion_ReturnsDefaultForUnknownSize()
        {
            var size = new WindowSize(1234, 567);
            WindowResolution resolution = size;

            Assert.That(resolution, Is.EqualTo(WindowResolution.Default));
        }

        [Test]
        public void WindowSize_ComparisonOperators_CompareBothDimensions()
        {
            var smaller = new WindowSize(1280, 720);
            var larger = new WindowSize(1920, 1080);
            var mixed = new WindowSize(2560, 720);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(smaller < larger, Is.True);
                Assert.That(smaller <= larger, Is.True);
                Assert.That(larger > smaller, Is.True);
                Assert.That(larger >= smaller, Is.True);
                Assert.That(smaller <= smaller, Is.True);
                Assert.That(smaller >= smaller, Is.True);
                Assert.That(mixed < larger, Is.False);
                Assert.That(mixed > larger, Is.False);
            }
        }
    }
}
