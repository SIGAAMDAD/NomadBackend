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

#if !UNITY_EDITOR
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Events.Private;
using Nomad.Core.Util;
using System;
using Nomad.Core.Input.ValueObjects;

namespace Nomad.Events.Tests
{
    /// <summary>
    /// Tests for EventFlags and event configuration options
    /// </summary>
    [TestFixture]
    [Category("Nomad.Events")]
    [Category("Identity")]
    [Category("Unit")]
    public class EventKeyTests
    {
        [Test]
        public void CreatedWith_SameNamespaceSameArgsTypeDifferentName_IsNotEqual()
        {
            var key1 = new EventKey(new InternString("Key1"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("Key2"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_SameNamespaceDifferentArgsTypeSameName_IsNotEqual()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(int));
            var key2 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(float));

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_DifferentNamespaceSameArgsTypeSameName_IsNotEqual()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString("Namespace1"), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("SameKey"), new InternString("Namespace2"), typeof(EmptyEventArgs));

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_EmptyNamespaceSameArgsSameName_IsEqual()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString(string.Empty), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("SameKey"), new InternString(string.Empty), typeof(EmptyEventArgs));

            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_NullNamespaceSameArgsSameName_IsEqual()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString(null), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("SameKey"), new InternString(null), typeof(EmptyEventArgs));

            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_SameNamespaceSameArgsEmptyName_IsEqual()
        {
            var key1 = new EventKey(new InternString(string.Empty), new InternString("SameNamespace"), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString(string.Empty), new InternString("SameNamespace"), typeof(EmptyEventArgs));

            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_SameNamespaceSameArgsNullName_IsEqual()
        {
            var key1 = new EventKey(new InternString(null), new InternString(string.Empty), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString(null), new InternString(string.Empty), typeof(EmptyEventArgs));

            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void EmptyNamespace_AndNullNamespace_AreEqual()
        {
            var keyEmpty = new EventKey(new InternString("Key1"), new InternString(""), typeof(EmptyEventArgs));
            var keyNull = new EventKey(new InternString("Key1"), new InternString(null), typeof(EmptyEventArgs));

            Assert.That(keyEmpty, Is.EqualTo(keyNull));
        }

        [Test]
        public void CreatedWith_Identical_IsEqual()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));

            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void CreatedWith_IdenticalWhenBoxed_IsEqual()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));

            object boxedValue = key2;

			Assert.That(key1.Equals(boxedValue));
        }

        [Test]
        public void GetHashCode_IsConsistentForEqualKeys()
        {
            var key1 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));
            var key2 = new EventKey(new InternString("SameKey"), new InternString("SameNameSpace"), typeof(EmptyEventArgs));

            Assert.That(key1.GetHashCode(), Is.EqualTo(key2.GetHashCode()));
        }
    }
}
#endif
