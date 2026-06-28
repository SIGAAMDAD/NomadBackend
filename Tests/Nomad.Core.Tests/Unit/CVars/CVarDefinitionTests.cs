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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Nomad.Core.CVars;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("CVars")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class CVarDefinitionTests
    {
        [Test]
        public void CVarDefinition_Constructor_ThrowsWhenNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CVarDefinition<int>(null!));
        }

        [Test]
        public void CVarDefinition_CreateInfo_UsesDefinitionNameAndProvidedMetadata()
        {
            var definition = new CVarDefinition<int>("g_damage");
            Func<int, bool> validator = value => value > 0;

            CVarCreateInfo<int> info = definition.CreateInfo(
                10,
                "Damage.",
                "Combat",
                CVarFlags.Developer,
                validator
            );

            using (Assert.EnterMultipleScope())
            {
                Assert.That(definition.Name, Is.EqualTo("g_damage"));
                Assert.That(definition.ToString(), Is.EqualTo("g_damage"));
                Assert.That(info.Name, Is.EqualTo("g_damage"));
                Assert.That(info.DefaultValue, Is.EqualTo(10));
                Assert.That(info.Description, Is.EqualTo("Damage."));
                Assert.That(info.Group, Is.EqualTo("Combat"));
                Assert.That(info.Flags, Is.EqualTo(CVarFlags.Developer));
                Assert.That(info.Validator, Is.SameAs(validator));
            }
        }
    }
}
