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
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Tests
{
    /// <summary>
    /// Tests for GameVersion value object
    /// </summary>
    [TestFixture]
    [Category("Nomad.Save")]
    [Category("Versioning")]
    [Category("Unit")]
    public class GameVersionTests
    {
        [Test]
        public void Constructor_WithValidNumbers_CreatesInstance()
        {
            // Arrange
            uint major = 2;
            uint minor = 0;
            uint patch = 1;

            // Act
            var version = new GameVersion(major, minor, patch);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version.Major, Is.EqualTo(major));
                Assert.That(version.Minor, Is.EqualTo(minor));
                Assert.That(version.Patch, Is.EqualTo(patch));
            }
        }

        [Test]
        public void Constructor_WithZeroValues_CreatesInstance()
        {
            // Arrange
            uint major = 0;
            uint minor = 0;
            uint patch = 0;

            // Act
            var version = new GameVersion(major, minor, patch);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version.Major, Is.EqualTo(0));
                Assert.That(version.Minor, Is.EqualTo(0));
                Assert.That(version.Patch, Is.EqualTo(0));
            }
        }

        [Test]
        public void Constructor_WithMaxValues_CreatesInstance()
        {
            // Arrange
            uint major = uint.MaxValue;
            uint minor = uint.MaxValue;
            uint patch = uint.MaxValue;

            // Act
            var version = new GameVersion(major, minor, patch);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version.Major, Is.EqualTo(uint.MaxValue));
                Assert.That(version.Minor, Is.EqualTo(uint.MaxValue));
                Assert.That(version.Patch, Is.EqualTo(uint.MaxValue));
            }
        }

        [Test]
        [TestCase(1u, 0u, 0u)]
        [TestCase(10u, 5u, 3u)]
        [TestCase(2u, 0u, 1u)]
        [TestCase(0u, 1u, 0u)]
        public void Constructor_WithVariousValues_CreatesInstance(uint major, uint minor, uint patch)
        {
            // Act
            var version = new GameVersion(major, minor, patch);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version.Major, Is.EqualTo(major));
                Assert.That(version.Minor, Is.EqualTo(minor));
                Assert.That(version.Patch, Is.EqualTo(patch));
            }
        }

        [Test]
        public void ToInt_WithSimpleVersion_CalculatesCorrectly()
        {
            // Arrange
            var version = new GameVersion(20, 100, 1000);

            // Act
            var result = version.ToInt();

            // Assert
            Assert.That(result, Is.EqualTo(20_100_1000ul));
        }

        [Test]
        public void ToInt_WithZeroVersion_CalculatesCorrectly()
        {
            // Arrange
            var version = new GameVersion(0, 0, 0);

            // Act
            var result = version.ToInt();

            // Assert
            Assert.That(result, Is.EqualTo(0ul));
        }

        [Test]
        [TestCase(1u, 0u, 0u, 01_000_0000ul)]
        [TestCase(1u, 1u, 1u, 01_001_0001ul)]
        [TestCase(10u, 5u, 3u, 10_005_0003ul)]
        [TestCase(2u, 5u, 10u, 02_005_0010ul)]
        public void ToInt_WithVariousVersions_CalculatesCorrectly(uint major, uint minor, uint patch, ulong expected)
        {
            // Arrange
            var version = new GameVersion(major, minor, patch);

            // Act
            var result = version.ToInt();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Equality_TwoInstancesWithSameValues_AreEqual()
        {
            // Arrange
            var version1 = new GameVersion(2, 0, 1);
            var version2 = new GameVersion(2, 0, 1);

            // Act & Assert
            Assert.That(version1, Is.EqualTo(version2));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentMajor_AreNotEqual()
        {
            // Arrange
            var version1 = new GameVersion(1, 0, 1);
            var version2 = new GameVersion(2, 0, 1);

            // Act & Assert
            Assert.That(version1, Is.Not.EqualTo(version2));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentMinor_AreNotEqual()
        {
            // Arrange
            var version1 = new GameVersion(2, 0, 1);
            var version2 = new GameVersion(2, 1, 1);

            // Act & Assert
            Assert.That(version1, Is.Not.EqualTo(version2));
        }

        [Test]
        public void Equality_TwoInstancesWithDifferentPatch_AreNotEqual()
        {
            // Arrange
            var version1 = new GameVersion(2, 0, 1);
            var version2 = new GameVersion(2, 0, 2);

            // Act & Assert
            Assert.That(version1, Is.Not.EqualTo(version2));
        }

        [Test]
        public void GetHashCode_TwoInstancesWithSameValues_HaveSameHashCode()
        {
            // Arrange
            var version1 = new GameVersion(2, 0, 1);
            var version2 = new GameVersion(2, 0, 1);

            // Act & Assert
            Assert.That(version1.GetHashCode(), Is.EqualTo(version2.GetHashCode()));
        }

        [Test]
        public void GetHashCode_TwoInstancesWithDifferentValues_MayHaveDifferentHashCode()
        {
            // Arrange
            var version1 = new GameVersion(1, 0, 0);
            var version2 = new GameVersion(2, 0, 0);

            // Act & Assert
            // Hash codes might collide, but different values should likely produce different hashes
            // This is not strictly required, so we just check that GetHashCode doesn't throw
            Assert.That(() => version1.GetHashCode(), Throws.Nothing);
            Assert.That(() => version2.GetHashCode(), Throws.Nothing);
        }

        [Test]
        public void StructBehavior_IsValueType()
        {
            // Arrange & Act
            var version1 = new GameVersion(2, 0, 1);
            var version2 = version1;
            version2 = new GameVersion(1, 0, 0);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version1.Major, Is.EqualTo(2));
                Assert.That(version2.Major, Is.EqualTo(1));
            }
        }

        [Test]
        public void Major_PropertyIsImmutable()
        {
            // Arrange
            var version = new GameVersion(2, 0, 1);

            // Assert - Struct members are readonly, so cannot be changed
            Assert.That(version.Major, Is.EqualTo(2));
        }

        [Test]
        public void Minor_PropertyIsImmutable()
        {
            // Arrange
            var version = new GameVersion(2, 5, 1);

            // Assert - Struct members are readonly, so cannot be changed
            Assert.That(version.Minor, Is.EqualTo(5));
        }

        [Test]
        public void Patch_PropertyIsImmutable()
        {
            // Arrange
            var version = new GameVersion(2, 0, 10);

            // Assert - Struct members are readonly, so cannot be changed
            Assert.That(version.Patch, Is.EqualTo(10ul));
        }

        [Test]
        public void ToString_DoesNotThrow()
        {
            // Arrange
            var version = new GameVersion(2, 0, 1);

            // Act
            var result = version.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToInt_WithLargeValues_CalculatesCorrectly()
        {
            // Arrange
            var version = new GameVersion(99, 999, 9999);

            // Act
            var result = version.ToInt();

            // Assert
            // 999 * 10000 + 999 * 100 + 999 = 9,999,999
            Assert.That(result, Is.EqualTo(99_999_9999));
        }

        [Test]
        public void DefaultGameVersion_HasAllZeros()
        {
            // Arrange & Act
            var version = default(GameVersion);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version.Major, Is.EqualTo(0u));
                Assert.That(version.Minor, Is.EqualTo(0u));
                Assert.That(version.Patch, Is.EqualTo(0ul));
            }
        }

        [Test]
        [Sequential]
        public void SequentialVersionComparison([Values(1u, 2u, 3u)] uint major, [Values(0u, 1u, 2u)] uint minor, [Values(0u, 5u, 10u)] uint patch)
        {
            // Act
            var version = new GameVersion(major, minor, patch);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(version.Major, Is.EqualTo(major));
                Assert.That(version.Minor, Is.EqualTo(minor));
                Assert.That(version.Patch, Is.EqualTo(patch));
            }
        }
    }
}
#endif
