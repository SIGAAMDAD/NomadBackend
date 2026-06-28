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
using Nomad.Networking.Rpc;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Rpc
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Rpc")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class RpcMethodPayloadAttributeTests
    {
        [Test]
        public void ConstructorAndInitProperties_StoreMetadata()
        {
            var attribute = new RpcMethodPayloadAttribute("Payload", typeof(TestNetworkPayload))
            {
                TypeName = "TestPayload",
                Order = 4
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attribute.Name, Is.EqualTo("Payload"));
                Assert.That(attribute.Type, Is.EqualTo(typeof(TestNetworkPayload)));
                Assert.That(attribute.TypeName, Is.EqualTo("TestPayload"));
                Assert.That(attribute.Order, Is.EqualTo(4));
            }
        }
    }
}
