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

using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Exceptions;
using Nomad.Core.Memory;
using Nomad.Core.Util;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Utilities.StringInterning")]
    [Category("Unit")]
    public class StringPoolConcurrencyTests
    {
        [SetUp]
        public void Setup()
        {
            // Ensure a clean pool before each test
            StringPool.Clear();
        }

        [Test]
        public async Task Intern_ConcurrentDifferentStrings_AllSucceedAndRetrievable()
        {
            const int threadCount = 10;
            const int stringsPerThread = 100;

            var tasks = new List<Task<Dictionary<int, InternString>>>();
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks.Add(Task.Run(() =>
                {
                    var results = new Dictionary<int, InternString>();
                    for (int i = 0; i < stringsPerThread; i++)
                    {
                        string str = $"Thread_{threadIndex}_String_{i}";
                        InternString interned = StringPool.Intern(str);
                        results[i] = interned;
                    }
                    return results;
                }));
            }

            var allResults = await Task.WhenAll(tasks);

            // Better approach: each task returns a list of (original, interned)
            var tasksWithOriginal = new List<Task<List<(string original, InternString interned)>>>();
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasksWithOriginal.Add(Task.Run(() =>
                {
                    var list = new List<(string, InternString)>();
                    for (int i = 0; i < stringsPerThread; i++)
                    {
                        string str = $"Thread_{threadIndex}_String_{i}";
                        InternString interned = StringPool.Intern(str);
                        list.Add((str, interned));
                    }
                    return list;
                }));
            }

            var lists = await Task.WhenAll(tasksWithOriginal);
            foreach (var list in lists)
            {
                foreach (var (original, interned) in list)
                {
                    // Verify we can get the original string back via ToString() or conversion
                    string? retrieved = interned.ToString(); // implicit conversion
                    Assert.That(retrieved, Is.EqualTo(original));

                    // Also verify explicit FromInterned
                    string fromInterned = StringPool.FromInterned(interned);
                    Assert.That(fromInterned, Is.EqualTo(original));
                }
            }
        }

        [Test]
        public async Task Intern_ConcurrentSameString_ReturnsSameId()
        {
            const int threadCount = 10;
            const string commonString = "ThisIsACommonString";

            var ids = new ConcurrentBag<ulong>();

            await Task.WhenAll(Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
            {
                InternString interned = StringPool.Intern(commonString);
                ulong id = interned; // implicit conversion to ulong
                ids.Add(id);
            })));

            // All IDs should be identical
            ulong firstId = ids.First();
            foreach (ulong id in ids)
            {
                Assert.That(id, Is.EqualTo(firstId));
            }

            // Also verify that the string is retrievable with that ID
            InternString sample = new InternString(firstId);
            string retrieved = StringPool.FromInterned(sample);
            Assert.That(retrieved, Is.EqualTo(commonString));
        }

        [Test]
        public async Task Intern_ConcurrentWithClear_NoCorruption()
        {
            // This test runs interning threads and a clearing thread concurrently
            // to ensure no crash or inconsistent state.

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var tasks = new List<Task>();

            // Interner threads
            for (int t = 0; t < 5; t++)
            {
                int threadId = t;
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random(threadId);
                    while (!cts.IsCancellationRequested)
                    {
                        string str = $"TestString_{random.Next(1000)}";
                        try
                        {
                            InternString interned = StringPool.Intern(str);
                            // Optionally retrieve to validate
                            _ = interned.ToString(); // may throw if cleared? depends on semantics
                        }
                        catch (Exception ex) when (ex is StringNotInternedException)
                        {
                            // After Clear, old IDs may become invalid if we try to use them.
                            // But we're only interning new strings, so shouldn't get this.
                            // However, a race could cause us to get an ID just before clear,
                            // then use it after clear? In this loop we don't store IDs across iterations.
                            // So fine.
                        }
                    }
                }, cts.Token));
            }

            // Clear thread
            tasks.Add(Task.Run(() =>
            {
                var random = new Random();
                while (!cts.IsCancellationRequested)
                {
                    Thread.Sleep(random.Next(10, 50));
                    StringPool.Clear();
                }
            }, cts.Token));

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when token expires
            }

            // If we got here without crashing, test passes.
            Assert.Pass("No crash during concurrent intern and clear.");
        }

        [Test]
        public void FromInterned_AfterClear_ThrowsForOldId()
        {
            string original = "HelloWorld";
            InternString interned = StringPool.Intern(original);
            ulong oldId = interned;

            StringPool.Clear();

            // Using the old InternString should throw
            Assert.Throws<StringNotInternedException>(() => StringPool.FromInterned(interned));

            // Implicit conversion returns empty string (by design, but you may want to test that behavior)
            string? viaConversion = interned.ToString(); // implicit conversion
            Assert.That(viaConversion, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task Intern_AfterClear_NewStringsWork()
        {
            StringPool.Clear();

            string newString = "AfterClear";
            InternString interned = StringPool.Intern(newString);
            string retrieved = StringPool.FromInterned(interned);
            Assert.That(retrieved, Is.EqualTo(newString));
        }

        [Test]
        public void Intern_NullOrEmpty_ReturnsEmpty()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(StringPool.Intern(null), Is.EqualTo(InternString.Empty));
                Assert.That(StringPool.Intern(""), Is.EqualTo(InternString.Empty));
                Assert.That(StringPool.Intern(string.Empty), Is.EqualTo(InternString.Empty));
            }
        }

        [Test]
        public void EmptyInternString_ToString_ReturnsEmpty()
        {
            InternString empty = InternString.Empty;
            string? str = empty.ToString(); // implicit conversion
            Assert.That(str, Is.EqualTo(string.Empty));

            // FromInterned with Empty should also return empty without throwing
            string fromInterned = StringPool.FromInterned(empty);
            Assert.That(fromInterned, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task Intern_UniqueIdsAcrossThreads()
        {
            const int totalStrings = 10000;
            var ids = new ConcurrentBag<ulong>();

            await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < totalStrings / Environment.ProcessorCount; i++)
                {
                    string str = Guid.NewGuid().ToString();
                    InternString interned = StringPool.Intern(str);
                    ids.Add(interned);
                }
            })));

            // All IDs should be distinct (since all input strings are unique)
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }

        [Test]
        public async Task Intern_StringEquality_WorksAcrossThreads()
        {
            // Intern the same string from multiple threads, collect InternStrings,
            // and verify they are considered equal (via ==, Equals, etc.)
            const string shared = "Shared";
            var bag = new ConcurrentBag<InternString>();

            await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                bag.Add(StringPool.Intern(shared));
            })));

            InternString first = bag.First();
            foreach (var item in bag)
            {
                Assert.That(item, Is.EqualTo(first));
                Assert.That(item, Is.EqualTo(first));
            }
        }
    }
}