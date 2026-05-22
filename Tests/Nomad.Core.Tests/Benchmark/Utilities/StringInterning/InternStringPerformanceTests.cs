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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Nomad.Core.Util;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Explicit("Performance comparison test. Run manually; do not include in normal CI.")]
    [Category("Performance")]
    public sealed class InternStringPerformanceTests
    {
        private const int Iterations = 5_000_000;
        private const int UniqueStringCount = 4096;

        private string[] _strings = null!;
        private string[] _equivalentStrings = null!;
        private InternString[] _internStrings = null!;
        private InternString[] _equivalentInternStrings = null!;

        private Dictionary<string, int> _stringDictionary = null!;
        private Dictionary<InternString, int> _internStringDictionary = null!;

        private static int _intSink;
        private static bool _boolSink;
        private static string? _stringSink;
        private static InternString _internStringSink;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _strings = CreateStrings(UniqueStringCount);

            // Important:
            // These have the same contents as _strings but are different string instances.
            // This avoids string equality unfairly winning via reference equality.
            _equivalentStrings = _strings
                .Select(static value => new string(value.ToCharArray()))
                .ToArray();

            _internStrings = _strings
                .Select(static value => new InternString(value))
                .ToArray();

            _equivalentInternStrings = _equivalentStrings
                .Select(static value => new InternString(value))
                .ToArray();

            _stringDictionary = new Dictionary<string, int>(UniqueStringCount);
            _internStringDictionary = new Dictionary<InternString, int>(UniqueStringCount);

            for (int i = 0; i < UniqueStringCount; i++)
            {
                _stringDictionary[_strings[i]] = i;
                _internStringDictionary[_internStrings[i]] = i;
            }

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( _strings, Has.Length.EqualTo( _internStrings.Length ) );
				Assert.That( _equivalentStrings, Has.Length.EqualTo( _equivalentInternStrings.Length ) );
			}
		}

        [Test]
        public void Compare_Equality_SameValue()
        {
            BenchmarkResult stringResult = Measure(
                "string == string, same value",
                () =>
                {
                    bool result = false;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        result ^= _strings[index] == _equivalentStrings[index];
                    }

                    Consume(result);
                });

            BenchmarkResult internResult = Measure(
                "InternString == InternString, same value",
                () =>
                {
                    bool result = false;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        result ^= _internStrings[index] == _equivalentInternStrings[index];
                    }

                    Consume(result);
                });

            Report(stringResult, internResult);
        }

        [Test]
        public void Compare_Equality_DifferentValue()
        {
            BenchmarkResult stringResult = Measure(
                "string == string, different value",
                () =>
                {
                    bool result = false;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int left = i & (UniqueStringCount - 1);
                        int right = (left + 1) & (UniqueStringCount - 1);

                        result ^= _strings[left] == _equivalentStrings[right];
                    }

                    Consume(result);
                });

            BenchmarkResult internResult = Measure(
                "InternString == InternString, different value",
                () =>
                {
                    bool result = false;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int left = i & (UniqueStringCount - 1);
                        int right = (left + 1) & (UniqueStringCount - 1);

                        result ^= _internStrings[left] == _equivalentInternStrings[right];
                    }

                    Consume(result);
                });

            Report(stringResult, internResult);
        }

        [Test]
        public void Compare_GetHashCode()
        {
            BenchmarkResult stringResult = Measure(
                "string.GetHashCode()",
                () =>
                {
                    int hash = 0;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        hash ^= _strings[index].GetHashCode(StringComparison.InvariantCulture);
                    }

                    Consume(hash);
                });

            BenchmarkResult internResult = Measure(
                "InternString.GetHashCode()",
                () =>
                {
                    int hash = 0;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        hash ^= _internStrings[index].GetHashCode();
                    }

                    Consume(hash);
                });

            Report(stringResult, internResult);
        }

        [Test]
        public void Compare_DictionaryLookup()
        {
            BenchmarkResult stringResult = Measure(
                "Dictionary<string, int>.TryGetValue",
                () =>
                {
                    int sum = 0;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);

                        if (_stringDictionary.TryGetValue(_equivalentStrings[index], out int value))
                        {
                            sum += value;
                        }
                    }

                    Consume(sum);
                });

            BenchmarkResult internResult = Measure(
                "Dictionary<InternString, int>.TryGetValue",
                () =>
                {
                    int sum = 0;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);

                        if (_internStringDictionary.TryGetValue(_equivalentInternStrings[index], out int value))
                        {
                            sum += value;
                        }
                    }

                    Consume(sum);
                });

            Report(stringResult, internResult);
        }

        [Test]
        public void Compare_ConstructionOrWrapping()
        {
            BenchmarkResult stringResult = Measure(
                "string reference assignment",
                () =>
                {
                    string? value = null;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        value = _strings[index];
                    }

                    Consume(value);
                });

            BenchmarkResult internResult = Measure(
                "new InternString(existing string)",
                () =>
                {
                    InternString value = default;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        value = new InternString(_strings[index]);
                    }

                    Consume(value);
                });

            Report(stringResult, internResult);
        }

        [Test]
        public void Compare_ToStringOrIdentity()
        {
            BenchmarkResult stringResult = Measure(
                "string identity read",
                () =>
                {
                    string? value = null;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        value = _strings[index];
                    }

                    Consume(value);
                });

            BenchmarkResult internResult = Measure(
                "InternString.ToString()",
                () =>
                {
                    string? value = null;

                    for (int i = 0; i < Iterations; i++)
                    {
                        int index = i & (UniqueStringCount - 1);
                        value = _internStrings[index].ToString();
                    }

                    Consume(value);
                });

            Report(stringResult, internResult);
        }

        private static BenchmarkResult Measure(string name, Action action)
        {
            // Warmup.
            action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long timestampBefore = Stopwatch.GetTimestamp();

            action();

            long timestampAfter = Stopwatch.GetTimestamp();
            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

            double elapsedMs = (timestampAfter - timestampBefore) * 1000.0 / Stopwatch.Frequency;

            return new BenchmarkResult(
                name,
                elapsedMs,
                allocatedAfter - allocatedBefore);
        }

        private static void Report(BenchmarkResult stringResult, BenchmarkResult internResult)
        {
            double speedRatio = stringResult.ElapsedMilliseconds / internResult.ElapsedMilliseconds;

            TestContext.Out.WriteLine(stringResult);
            TestContext.Out.WriteLine(internResult);
            TestContext.Out.WriteLine($"Ratio: {speedRatio:0.00}x");

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(stringResult.ElapsedMilliseconds, Is.GreaterThan(0.0));
                Assert.That(internResult.ElapsedMilliseconds, Is.GreaterThan(0.0));
            }
        }

        private static string[] CreateStrings(int count)
        {
            string[] values = new string[count];

            for (int i = 0; i < count; i++)
            {
                values[i] = $"nomad.test.identifier.{i:D8}";
            }

            return values;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(int value)
        {
            _intSink = value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(bool value)
        {
            _boolSink = value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(string? value)
        {
            _stringSink = value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(InternString value)
        {
            _internStringSink = value;
        }

        private readonly record struct BenchmarkResult(
            string Name,
            double ElapsedMilliseconds,
            long AllocatedBytes)
        {
            public override string ToString()
            {
                return $"{Name}: {ElapsedMilliseconds:0.000} ms, {AllocatedBytes:N0} bytes allocated";
            }
        }
    }
}