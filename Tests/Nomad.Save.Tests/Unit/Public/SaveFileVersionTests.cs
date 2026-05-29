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
using Nomad.Core.FileSystem.Configs;
using Nomad.FileSystem.Private.MemoryStream;
using Nomad.Save.ValueObjects;
using NUnit.Framework;

namespace Nomad.Save.Tests
{
    [TestFixture]
    [Category("Unit")]
    [Category("Nomad.Save")]
    public sealed class SaveFileVersionTests
    {
        [Test]
        public void Constructor_AllValuesRemainTheSame()
        {
            var version = new SaveFileVersion(2, 24, 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(version.Major, Is.EqualTo(2));
                Assert.That(version.Minor, Is.EqualTo(24));
                Assert.That(version.Patch, Is.EqualTo(1));
            }
        }

        [Test]
        public void Serialize_WritesCorrectData()
        {
            var stream = new MemoryWriteStream(new MemoryWriteConfig { });
            var version = new SaveFileVersion(2, 24, 1);
            version.Serialize(stream);

            var buffer = stream.Buffer.ToArray();
            uint major = BitConverter.ToUInt32(buffer, 0);
            uint minor = BitConverter.ToUInt32(buffer, sizeof(uint));
            ulong patch = BitConverter.ToUInt64(buffer, sizeof(uint) * 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(major, Is.EqualTo(2));
                Assert.That(minor, Is.EqualTo(24));
                Assert.That(patch, Is.EqualTo(1));
            }
        }

        [Test]
        public void Serialize_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            var version = new SaveFileVersion();
            Assert.Throws<ArgumentNullException>(() => version.Serialize(null));
        }

        [Test]
        public void Deserialize_ReadsCorrectData()
        {
            var writeStream = new MemoryWriteStream(new MemoryWriteConfig { });
            var to = new SaveFileVersion(2, 0, 24);
            to.Serialize(writeStream);

            var stream = new MemoryReadStream(new MemoryReadConfig { Buffer = writeStream.Buffer });
            var version = SaveFileVersion.Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(version.Major, Is.EqualTo(2));
                Assert.That(version.Minor, Is.EqualTo(0));
                Assert.That(version.Patch, Is.EqualTo(24));
            }
        }

        [Test]
        public void Deserialize_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SaveFileVersion.Deserialize(null));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentMajor_AreNotEqual()
        {
            var a = new SaveFileVersion(2, 0, 1);
            var b = new SaveFileVersion(1, 0, 1);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentMinor_AreNotEqual()
        {
            var a = new SaveFileVersion(2, 0, 1);
            var b = new SaveFileVersion(2, 1, 1);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentPatch_AreNotEqual()
        {
            var a = new SaveFileVersion(2, 0, 1);
            var b = new SaveFileVersion(2, 0, 4);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Equality_TwoInstancesWithSameValues_AreEqual()
        {
            var a = new SaveFileVersion(2, 0, 1);
            var b = new SaveFileVersion(2, 0, 1);

            Assert.That(a, Is.EqualTo(b));
        }
    }
}
