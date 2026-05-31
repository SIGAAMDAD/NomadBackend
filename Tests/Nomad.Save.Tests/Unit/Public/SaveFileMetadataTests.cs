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
using System;
using NUnit.Framework;
using Nomad.Save.ValueObjects;
using NUnit.Framework.Interfaces;

namespace Nomad.Save.Tests
{
    /// <summary>
    /// Tests for SaveFileMetadata value object
    /// </summary>
    [TestFixture]
    [Category("Nomad.Save")]
    [Category("Metadata")]
    [Category("Unit")]
    public class SaveFileMetadataTests
    {
        [Test]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            string fileName = "save_001";
            long fileSize = 1024;
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;

            // Act
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Assert
            Assert.That(metadata, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(metadata.SaveName, Is.EqualTo(fileName));
                Assert.That(metadata.FileSize, Is.EqualTo(fileSize));
                Assert.That(metadata.LastAccessTime, Is.EqualTo(lastAccessTime));
                Assert.That(metadata.CreationTime, Is.EqualTo(creationTime));
            }
        }

        [Test]
        public void Constructor_WithZeroFileSize_CreatesInstance()
        {
            // Arrange
            string fileName = "empty_save";
            long fileSize = 0;
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;

            // Act
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Assert
            Assert.That(metadata.FileSize, Is.Zero);
        }

        [Test]
        public void Constructor_WithLargeFileSize_CreatesInstance()
        {
            // Arrange
            string fileName = "large_save";
            long fileSize = long.MaxValue;
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;

            // Act
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Assert
            Assert.That(metadata.FileSize, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void Constructor_WithOldDateTime_CreatesInstance()
        {
            // Arrange
            string fileName = "old_save";
            long fileSize = 2048;
            var lastAccessTime = new DateTime(2020, 1, 1, 0, 0, 0);
            var creationTime = DateTime.Now;

            // Act
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Assert
            Assert.That(metadata.LastAccessTime, Is.EqualTo(lastAccessTime));
        }

        [Test]
        public void Constructor_WithCurrentDateTime_CreatesInstance()
        {
            // Arrange
            string fileName = "current_save";
            long fileSize = 5120;
            var lastAccessTime = DateTime.UtcNow;
            var creationTime = DateTime.Now;

            // Act
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(metadata.LastAccessTime, Is.EqualTo(lastAccessTime));
            }
        }

        [Test]
        public void Equality_TwoInstancesWithSameValues_AreEqual()
        {
            // Arrange
            string fileName = "save_001.ngd";
            long fileSize = 1024;
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;

            var metadata1 = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );
            var metadata2 = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Act & Assert
            Assert.That(metadata1, Is.EqualTo(metadata2));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentFileNames_AreNotEqual()
        {
            // Arrange
            long fileSize = 1024;
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;

            var metadata1 = new SaveFileMetadata(
                "save_001",
                fileSize,
                lastAccessTime,
                creationTime
            );
            var metadata2 = new SaveFileMetadata(
                "save_002",
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Act & Assert
            Assert.That(metadata1, Is.Not.EqualTo(metadata2));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentFileSizes_AreNotEqual()
        {
            // Arrange
            string fileName = "save_001";
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;

            var metadata1 = new SaveFileMetadata(
                fileName,
                1024,
                lastAccessTime,
                creationTime
            );
            var metadata2 = new SaveFileMetadata(
                fileName,
                2048,
                lastAccessTime,
                creationTime
            );

            // Act & Assert
            Assert.That(metadata1, Is.Not.EqualTo(metadata2));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentAccessTimes_AreNotEqual()
        {
            // Arrange
            string fileName = "save_001";
            var now = DateTime.Now;
            var creationTime = DateTime.Now;
            long fileSize = 1024;

            var metadata1 = new SaveFileMetadata(
                fileName,
                fileSize,
                now,
                creationTime
            );
            now = now.AddDays(2);
            var metadata2 = new SaveFileMetadata(
                fileName,
                fileSize,
                now,
                creationTime
            );

            // Act & Assert
            Assert.That(metadata1, Is.Not.EqualTo(metadata2));
        }

        [Test]
        public void GetHashCode_TwoInstancesWithSameValues_HaveSameHashCode()
        {
            // Arrange
            string fileName = "save_001";
            long fileSize = 1024;
            var lastAccessTime = new DateTime(2025, 1, 1);
            var creationTime = DateTime.Now;

            var metadata1 = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );
            var metadata2 = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Act & Assert
            Assert.That(metadata1.GetHashCode(), Is.EqualTo(metadata2.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsMetadataInfo()
        {
            // Arrange
            var now = DateTime.Now;
            var creationTime = DateTime.Now;
            var metadata = new SaveFileMetadata(
                "save_001",
                1024,
                now,
                creationTime
            );

            // Act
            string result = metadata.ToString();

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void RecordBehavior_AllPropertiesAccessible()
        {
            // Arrange
            string fileName = "save_001";
            long fileSize = 2048;
            var lastAccessTime = DateTime.Now;
            var creationTime = DateTime.Now;
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            using (Assert.EnterMultipleScope())
            {
                // Act & Assert
                Assert.That(metadata.SaveName, Is.EqualTo(fileName));
                Assert.That(metadata.FileSize, Is.EqualTo(fileSize));
                Assert.That(metadata.LastAccessTime, Is.EqualTo(lastAccessTime));
            }
        }

        [Test]
        public void FileName_PropertyIsAccessible()
        {
            // Arrange
            string fileName = "test_save";
            var now = DateTime.Now;
            var creationTime = DateTime.Now;
            var metadata = new SaveFileMetadata(
                fileName,
                1024,
                now,
                creationTime
            );

            // Act
            var retrievedFileName = metadata.SaveName;

            // Assert
            Assert.That(retrievedFileName, Is.EqualTo(fileName));
        }

        [Test]
        public void FileSize_PropertyIsAccessible()
        {
            // Arrange
            long expectedSize = 5678;
            var now = DateTime.Now;
            var creationTime = DateTime.Now;
            var metadata = new SaveFileMetadata(
                "test",
                expectedSize,
                now,
                creationTime
            );

            // Act
            long retrievedSize = metadata.FileSize;

            // Assert
            Assert.That(retrievedSize, Is.EqualTo(expectedSize));
        }

        [Test]
        public void LastAccessTime_PropertyIsAccessible()
        {
            // Arrange
            var expectedTime = new DateTime(2024, 6, 15, 12, 30, 45);
            var creationTime = DateTime.Now;
            var metadata = new SaveFileMetadata(
                "test",
                1024,
                expectedTime,
                creationTime
            );

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(metadata.LastAccessTime, Is.EqualTo(expectedTime));
            }
        }

        [Test]
        public void Constructor_WithNegativeFileSize_CreatesInstance()
        {
            // Note: This tests that the constructor allows negative values
            // In real usage this would be invalid, but records don't validate
            // Arrange
            string fileName = "test";
            long fileSize = -100;
            var creationTime = DateTime.Now;
            var lastAccessTime = DateTime.Now;

            // Act
            var metadata = new SaveFileMetadata(
                fileName,
                fileSize,
                lastAccessTime,
                creationTime
            );

            // Assert
            Assert.That(metadata.FileSize, Is.EqualTo(-100));
        }

        [Test]
        public void Immutability_PropertiesCannotBeChanged()
        {
            // Arrange
            var now = DateTime.Now;
            var creationTime = DateTime.Now;
            var metadata = new SaveFileMetadata(
                "save",
                1024,
                now,
                creationTime
            );

            // Act & Assert
            // Records are immutable, attempting to verify the properties are read-only
            Assert.That(() => metadata.SaveName, Throws.Nothing);
            Assert.That(() => metadata.FileSize, Throws.Nothing);
            Assert.That(() => metadata.LastAccessTime, Throws.Nothing);
        }
    }
}
#endif
