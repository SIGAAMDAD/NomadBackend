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
    public sealed class CVarCreateInfoTests
    {
        [Test]
        public void CVarCreateInfo_ObjectInitializer_AssignsEveryProperty()
        {
            Func<int, bool> validator = value => value >= 0;

            var info = new CVarCreateInfo<int> {
                Name = "g_limit",
                DefaultValue = 12,
                Description = "Limit value.",
                Group = "Gameplay",
                Flags = CVarFlags.Archive,
                Validator = validator
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.Name, Is.EqualTo("g_limit"));
                Assert.That(info.DefaultValue, Is.EqualTo(12));
                Assert.That(info.Description, Is.EqualTo("Limit value."));
                Assert.That(info.Group, Is.EqualTo("Gameplay"));
                Assert.That(info.Flags, Is.EqualTo(CVarFlags.Archive));
                Assert.That(info.Validator, Is.SameAs(validator));
                Assert.That(info.Validator!(12), Is.True);
            }
        }
    }
}
