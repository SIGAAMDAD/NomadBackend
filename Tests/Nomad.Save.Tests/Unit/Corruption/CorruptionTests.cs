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
using System.IO;
using System.Text;
using NUnit.Framework;
using Nomad.Save.Exceptions;
using Nomad.Save.Private.ValueObjects;
using Nomad.Save.Private.Serialization.FieldSerializers;
using Nomad.FileSystem.Private.MemoryStream;
using Nomad.Core.Util;
using Nomad.Save.Private;
using System;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Memory.Buffers;

namespace Nomad.Save.Tests
{
    [TestFixture]
    [Category("Nomad.Save")]
    [Category("Corruption")]
    [Category("Unit")]
    public class SaveCorruptionTests
    {
        // Ensure all field serializers are registered (static constructor runs).
        static SaveCorruptionTests()
        {
            _ = FieldSerializerRegistry.GetSerializer<int>();
        }

        [Test]
        public void ReadField_InvalidNameLength_ThrowsFieldCorruptException()
        {
            // Arrange
            const int maxNameLength = 256;
            string longName = new string('A', maxNameLength + 10); // 266 chars

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

            // Act
            writer.Write(longName);
            writer.Write((byte)AnyType.Int32);
            writer.Write(12345);

            ms.Position = 0;
            var stream = new MemoryReadStream(new MemoryReadConfig { Buffer = new SharedBufferHandle(ms.ToArray(), (int)ms.Length), MaxCapacity = 8192 });

            var ex = Assert.Throws<FieldCorruptException>(() =>
                SaveField.Read("TestSection", 0, stream));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Does.Contain("Field name length corrupted"));
                Assert.That(ex.SectionName, Is.EqualTo("TestSection"));
                Assert.That(ex.FieldIndex, Is.Zero);
            }
        }

        [Test]
        public void ReadField_InvalidType_ThrowsFieldCorruptException()
        {
            // Arrange
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

            // Act
            string name = "ValidField";
            writer.Write(name);
            writer.Write((byte)255);

            ms.Position = 0;
            var stream = new MemoryReadStream(new MemoryReadConfig { Buffer = new SharedBufferHandle(ms.ToArray(), (int)ms.Length), MaxCapacity = 8192 });

            var ex = Assert.Throws<FieldCorruptException>(() =>
                SaveField.Read("TestSection", 1, stream));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Does.Contain("Field type '255' isn't valid"));
                Assert.That(ex.SectionName, Is.EqualTo("TestSection"));
                Assert.That(ex.FieldIndex, Is.EqualTo(1));
            }
        }

        [Test]
        public void LoadSectionHeader_InvalidNameLength_ThrowsSectionCorruptionException()
        {
            // Arrange
            const int maxSectionNameLength = Constants.SECTION_NAME_MAX_LENGTH;
            string longName = new string('B', maxSectionNameLength + 10); // 138 chars

            // Act
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

            writer.Write(0);
            writer.Write(0UL);
            writer.Write(0);
            writer.Write(longName);

            ms.Position = SectionHeader.HEADER_CHECKSUM_OFFSET;
            int byteLength = (int)writer.BaseStream.Length - SectionHeader.HEADER_CHECKSUM_OFFSET;
            ms.Position = 0;
            writer.Write(byteLength);
            writer.Write(Checksum.Compute(ms.ToArray().AsSpan().Slice(sizeof(int) + sizeof(ulong))).Value);

            ms.Position = 0;
            var stream = new MemoryReadStream(new MemoryReadConfig { Buffer = new SharedBufferHandle(ms.ToArray(), (int)ms.Length), MaxCapacity = 8192 });
            var ex = Assert.Throws<SectionCorruptException>(() => SectionHeader.Load(0, stream));

            // Assert
            Assert.That(ex.Message, Does.Contain("Section name corrupt or too long"));
        }

        [Test]
        public void LoadSectionHeader_NegativeFieldCount_ThrowsSectionCorruptionException()
        {
            // Arrange
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

            // Act
            string name = "ValidSection";
            writer.Write(0);
            writer.Write((ulong)0);
            writer.Write(name);
            writer.Write(-1);
            ms.Position = 0;
            var stream = new MemoryReadStream(new MemoryReadConfig { Buffer = new SharedBufferHandle(ms.ToArray(), (int)ms.Length), MaxCapacity = 8192 });
            var ex = Assert.Throws<SectionCorruptException>(() => SectionHeader.Load(0, stream));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Does.Contain("Field count is invalid"));
                Assert.That(ex.SectionIndex, Is.Zero);
            }
        }

        [Test]
        public void LoadSectionHeader_NegativeByteCount_ThrowsSectionCorruptionException()
        {
            // Arrange
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

            // Act
            string name = "ValidSection";
            writer.Write(-1);
            writer.Write(0x87654321UL);
            writer.Write(name);
            writer.Write(0);
            ms.Position = 0;
            var stream = new MemoryReadStream(new MemoryReadConfig { Buffer = new SharedBufferHandle(ms.ToArray(), (int)ms.Length), MaxCapacity = 8192 });

            var ex = Assert.Throws<SectionCorruptException>(() => SectionHeader.Load(0, stream));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Does.Contain("Byte length is invalid"));
                Assert.That(ex.SectionIndex, Is.Zero);
            }
        }
    }
}
#endif
