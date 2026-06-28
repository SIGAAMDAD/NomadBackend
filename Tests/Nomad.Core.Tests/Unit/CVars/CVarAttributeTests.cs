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
    public sealed class CVarAttributeTests
    {
        [Test]
        public void CVarAttribute_StringConstructor_AssignsMetadataDefaults()
        {
            var attribute = new CVarAttribute("g_speed", "320") {
                Description = "Player speed.",
                Group = "Movement",
                Flags = CVarFlags.Archive,
                ValueType = typeof(string),
                ValidatorExpression = "value.Length > 0",
                AccessorName = "Speed"
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attribute.Name, Is.EqualTo("g_speed"));
                Assert.That(attribute.DefaultValue, Is.EqualTo("320"));
                Assert.That(attribute.Description, Is.EqualTo("Player speed."));
                Assert.That(attribute.Group, Is.EqualTo("Movement"));
                Assert.That(attribute.Flags, Is.EqualTo(CVarFlags.Archive));
                Assert.That(attribute.ValueType, Is.EqualTo(typeof(string)));
                Assert.That(attribute.ValidatorExpression, Is.EqualTo("value.Length > 0"));
                Assert.That(attribute.AccessorName, Is.EqualTo("Speed"));
            }
        }

        [Test]
        public void CVarAttribute_PrimitiveConstructors_BoxExpectedDefaultValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(new CVarAttribute("bool", true).DefaultValue, Is.EqualTo(true));
                Assert.That(new CVarAttribute("int", 7).DefaultValue, Is.EqualTo(7));
                Assert.That(new CVarAttribute("uint", 7u).DefaultValue, Is.EqualTo(7u));
                Assert.That(new CVarAttribute("float", 1.25f).DefaultValue, Is.EqualTo(1.25f));
                Assert.That(new CVarAttribute("object", (object)42).DefaultValue, Is.EqualTo(42));
            }
        }
    }
}
