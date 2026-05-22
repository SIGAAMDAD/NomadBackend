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

using Nomad.Core.Engine.Rendering;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Rendering")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class TextureFilterModeTests
    {
        [Test]
        public void TextureFilterMode_Count_MatchesNumberOfConcreteModes()
        {
            Assert.That((int)TextureFilterMode.Count, Is.EqualTo(6));
        }

        [Test]
        public void TextureFilterMode_AnisotropicValues_AreOrderedByStrength()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextureFilterMode.Anisotropic_2x, Is.LessThan(TextureFilterMode.Anisotropic_4x));
                Assert.That(TextureFilterMode.Anisotropic_4x, Is.LessThan(TextureFilterMode.Anisotropic_8x));
                Assert.That(TextureFilterMode.Anisotropic_8x, Is.LessThan(TextureFilterMode.Anisotropic_16x));
            }
        }
    }
}
