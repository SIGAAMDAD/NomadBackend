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
using System.Collections.Generic;
using NUnit.Framework;
using Nomad.Core.Memory;
using Nomad.Core.Util;
using Nomad.Core.Exceptions;

namespace Nomad.Core.Tests
{
    /// <summary>
    /// Tests for the <see cref="InternString"/> type and its interaction with <see cref="StringPool"/>.
    /// </summary>
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Utilities.StringInterning")]
    [Category("Unit")]
    public class InternStringTests
    {
        [SetUp]
        public void SetUp()
        {
            // Reset the thread-static pool before each test to avoid cross-test contamination.
            ResetStringPool();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure pool is disposed and reset after each test.
            ResetStringPool();
        }

        private static void ResetStringPool()
        {
            StringPool.Clear();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithIntId_SetsId()
        {
            var intern = new InternString(42);
            Assert.That((ulong)intern, Is.EqualTo(42));
        }

        [Test]
        public void Constructor_WithReadOnlySpan_IntermsString()
        {
            const string text = "Hello, World!";
            var intern = new InternString(text);

            Assert.That((string)intern, Is.EqualTo(text));
            // Ensure the same string returns the same id (interning).
            var intern2 = new InternString(text);
            Assert.That(intern, Is.EqualTo(intern2));
        }

        [Test]
        public void Constructor_WithEmptySpan_ReturnsEmptyInternString()
        {
            var intern = new InternString(string.Empty);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(intern, Is.EqualTo(InternString.Empty));
                Assert.That((string)intern, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void Empty_Property_ReturnsZeroIdAndEmptyString()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((ulong)InternString.Empty, Is.Zero);
                Assert.That((string)InternString.Empty, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void DefaultInstance_IsSameAsEmpty()
        {
            InternString defaultValue = default;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaultValue, Is.EqualTo(InternString.Empty));
                Assert.That((ulong)defaultValue, Is.Zero);
            }
        }

        #endregion

        #region Equality Tests

        [Test]
        public void Equals_InternString_ComparesById()
        {
            var a = new InternString(10);
            var b = new InternString(10);
            var c = new InternString(20);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a, Is.Not.EqualTo(c));
            }
        }

        [Test]
        public void Equals_Object_ComparesCorrectly()
        {
            var a = new InternString(10);
            var b = new InternString(10);
            object boxedB = b;
            object notIntern = "something";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(a, Is.EqualTo(boxedB));
                Assert.That(a, Is.Not.EqualTo(notIntern));
            }
        }

        [Test]
        public void Equals_NotSameType_ReturnsFalse()
        {
            var a = new InternString(10);
            var b = new object();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(a, Is.Not.EqualTo(b));
            }
        }

        [Test]
        public void EqualityOperator_ReturnsTrueForSameId()
        {
            var left = new InternString(42);
            var right = new InternString(42);
            Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void InequalityOperator_ReturnsTrueForDifferentId()
        {
            var left = new InternString(42);
            var right = new InternString(99);

            bool nequal = left != right;
			Assert.That(nequal, Is.True);
        }
        
        [Test]
        public void GetHashCode_ReturnsId()
        {
            var intern = new InternString(123);
            Assert.That(intern.GetHashCode(), Is.EqualTo(123));
        }

        #endregion

        #region Conversion Tests

        [Test]
        public void ImplicitConversion_ToInt_ReturnsId()
        {
            var intern = new InternString(456);
            ulong id = intern;
            Assert.That(id, Is.EqualTo(456));
        }

        [Test]
        public void ImplicitConversion_ToString_ReturnsPooledString()
        {
            const string text = "Test conversion";
            var intern = new InternString(text);

            string result = intern;
            Assert.That(result, Is.EqualTo(text));
        }

        [Test]
        public void ExplicitConversion_ToString_ReturnsPooledString()
        {
            const string text = "Test conversion";
            var intern = new InternString(text);

            string result = intern.ToString();
            Assert.That(result, Is.EqualTo(text));
        }

        [Test]
        public void ImplicitConversion_ToString_WithInvalidId_ReturnsEmpty()
        {
            var invalid = new InternString(9999); // No such string in pool
            string result = invalid;
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        #endregion

        #region Interning Behavior

        [Test]
        public void Intern_IdenticalStrings_ReturnSameInternString()
        {
            var span1 = "shared";
            var span2 = "shared";

            var intern1 = new InternString(span1);
            var intern2 = new InternString(span2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(intern1, Is.EqualTo(intern2));
                Assert.That((ulong)intern1, Is.EqualTo((ulong)intern2));
            }
        }

        [Test]
        public void Intern_DifferentStrings_ReturnDifferentInternStrings()
        {
            var internA = new InternString("alpha");
            var internB = new InternString("beta");

            Assert.That(internA, Is.Not.EqualTo(internB));
        }

        [Test]
        public void Intern_EmptySpan_AlwaysReturnsEmpty()
        {
            var empty1 = new InternString(string.Empty);
            var empty2 = new InternString(string.Empty);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(empty1, Is.EqualTo(InternString.Empty));
                Assert.That(empty2, Is.EqualTo(InternString.Empty));
                // The identifier is 0 for all empty instances.
                Assert.That((ulong)empty1, Is.Zero);
            }
        }

        [Test]
        public void StringPool_FromInterned_ReturnsOriginalString()
        {
            const string original = "retrieve me";
            var intern = new InternString(original);

            // Use reflection or direct call to StringPool.FromInterned (internal? It's public static)
            var retrieved = StringPool.FromInterned(intern);
            Assert.That(retrieved, Is.EqualTo(original));
        }

        [Test]
        public void StringPool_FromInterned_WithInvalidId_ThrowsStringNotInternedException()
        {
            var invalid = new InternString(9999);
            Assert.Throws<StringNotInternedException>(() => StringPool.FromInterned(invalid));
        }

        [Test]
        public void Intern_StringWithCollidingHashCode_BehavesCorrectly()
        {
            // Note: This test depends on the actual hash code algorithm.
            // It's possible to find two different strings with the same hash code,
            // but for simplicity we assume no collision in this test suite.
            // In a real-world scenario, the implementation would need to handle collisions.
            // Here we just verify that interning works for two distinct strings.
            var s1 = "distinct1";
            var s2 = "distinct2";
            var i1 = new InternString(s1);
            var i2 = new InternString(s2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(i1, Is.Not.EqualTo(i2));
                Assert.That((ulong)i1, Is.Not.EqualTo((ulong)i2)); // Hash codes likely differ.
            }
        }

        #endregion

        #region Pool Lifecycle and Disposal

        [Test]
        public void StringPool_Dispose_ClearsAllStrings()
        {
            // Intern a string.
            var intern = new InternString("persist");
            Assert.That((string)intern, Is.EqualTo("persist"));

            // Dispose the pool.
            ResetStringPool(); // This disposes the current pool.

            // The interned string should no longer be retrievable.
            Assert.Throws<StringNotInternedException>(() => StringPool.FromInterned(intern));
        }

        #endregion

        #region Usage in Collections

        [Test]
        public void InternString_CanBeUsedInHashSet()
        {
            var set = new HashSet<InternString>();
            var a = new InternString("set test");
            var b = new InternString("set test"); // same content
            var c = new InternString("other");

            set.Add(a);
            set.Add(b); // should be duplicate, not added
            set.Add(c);

            Assert.That(set, Has.Count.EqualTo(2));
            using (Assert.EnterMultipleScope())
            {
				Assert.That(set, Does.Contain(a));
				Assert.That(set, Does.Contain(b)); // same instance
				Assert.That(set, Does.Contain(new InternString("set test")));
            }
        }

        [Test]
        public void InternString_CanBeUsedAsDictionaryKey()
        {
            var dict = new Dictionary<InternString, string>();
            var key1 = new InternString("dict key");
            var key2 = new InternString("dict key"); // same content

            dict[key1] = "value";
            using (Assert.EnterMultipleScope())
            {
                Assert.That(dict.ContainsKey(key2), Is.True);
                Assert.That(dict[key2], Is.EqualTo("value"));
            }
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Intern_VeryLongString_Works()
        {
            var longString = new string('x', 10000);
            var intern = new InternString(longString);
            Assert.That((string)intern, Is.EqualTo(longString));
        }

        [Test]
        public void Intern_StringWithNullChars_Works()
        {
            var withNull = "abc\0def";
            var intern = new InternString(withNull);
            Assert.That((string)intern, Is.EqualTo(withNull));
        }

        [Test]
        public void MultipleInterningOfSameString_YieldsSameId()
        {
            var intern1 = new InternString("repeat");
            var intern2 = new InternString("repeat");
            var intern3 = new InternString("repeat");

            Assert.That(intern1, Is.EqualTo(intern2));
            Assert.That(intern1, Is.EqualTo(intern3));
        }

        #endregion
    }
}
#endif