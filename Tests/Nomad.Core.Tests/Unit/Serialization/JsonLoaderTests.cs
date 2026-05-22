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

using System.IO;
using System.Text;
using System.Text.Json;
using Nomad.Core.Engine.Windowing;
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Serialization")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class JsonLoaderTests
    {
        [Test]
        public void Parse_SkipsCommentsAndAllowsTrailingCommas()
        {
            const string json = """
                {
                    // Comment
                    "Value": 42,
                }
                """;

            using JsonDocument document = ParseDocument(json);

            Assert.That(JsonLoader.GetRequired<int>(document.RootElement, "Value"), Is.EqualTo(42));
        }

        [Test]
        public void GetRequired_LoadsSupportedScalarTypes()
        {
            const string json = """
                {
                    "booleanValue": true,
                    "sbyteValue": -8,
                    "byteValue": 8,
                    "int16Value": -16,
                    "uint16Value": 16,
                    "int32Value": -32,
                    "uint32Value": 32,
                    "int64Value": -64,
                    "uint64Value": 64,
                    "singleValue": 1.25,
                    "doubleValue": 2.5,
                    "decimalValue": 3.75,
                    "charValue": "Q",
                    "stringValue": "Nomad",
                    "enumValue": "Res_1920x1080"
                }
                """;

            using JsonDocument document = ParseDocument(json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(JsonLoader.GetRequired<bool>(document.RootElement, "BooleanValue"), Is.True);
                Assert.That(JsonLoader.GetRequired<sbyte>(document.RootElement, "SByteValue"), Is.EqualTo((sbyte)-8));
                Assert.That(JsonLoader.GetRequired<byte>(document.RootElement, "ByteValue"), Is.EqualTo((byte)8));
                Assert.That(JsonLoader.GetRequired<short>(document.RootElement, "Int16Value"), Is.EqualTo((short)-16));
                Assert.That(JsonLoader.GetRequired<ushort>(document.RootElement, "UInt16Value"), Is.EqualTo((ushort)16));
                Assert.That(JsonLoader.GetRequired<int>(document.RootElement, "Int32Value"), Is.EqualTo(-32));
                Assert.That(JsonLoader.GetRequired<uint>(document.RootElement, "UInt32Value"), Is.EqualTo(32u));
                Assert.That(JsonLoader.GetRequired<long>(document.RootElement, "Int64Value"), Is.EqualTo(-64L));
                Assert.That(JsonLoader.GetRequired<ulong>(document.RootElement, "UInt64Value"), Is.EqualTo(64UL));
                Assert.That(JsonLoader.GetRequired<float>(document.RootElement, "SingleValue"), Is.EqualTo(1.25f));
                Assert.That(JsonLoader.GetRequired<double>(document.RootElement, "DoubleValue"), Is.EqualTo(2.5d));
                Assert.That(JsonLoader.GetRequired<decimal>(document.RootElement, "DecimalValue"), Is.EqualTo(3.75m));
                Assert.That(JsonLoader.GetRequired<char>(document.RootElement, "CharValue"), Is.EqualTo('Q'));
                Assert.That(JsonLoader.GetRequired<string>(document.RootElement, "StringValue"), Is.EqualTo("Nomad"));
                Assert.That(JsonLoader.GetRequired<WindowResolution>(document.RootElement, "EnumValue"), Is.EqualTo(WindowResolution.Res_1920x1080));
            }
        }

        [Test]
        public void ReadArray_LoadsSupportedArrayTypes()
        {
            const string json = """
                {
                    "BooleanValues": [ true, false ],
                    "SByteValues": [ -1, 2 ],
                    "ByteValues": [ 1, 2 ],
                    "Int16Values": [ -2, 3 ],
                    "UInt16Values": [ 2, 3 ],
                    "Int32Values": [ -3, 4 ],
                    "UInt32Values": [ 3, 4 ],
                    "Int64Values": [ -4, 5 ],
                    "UInt64Values": [ 4, 5 ],
                    "SingleValues": [ 1.5, 2.5 ],
                    "DoubleValues": [ 2.5, 3.5 ],
                    "DecimalValues": [ 3.5, 4.5 ],
                    "CharValues": [ "A", "B" ],
                    "StringValues": [ "One", "Two" ]
                }
                """;

            using JsonDocument document = ParseDocument(json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(JsonLoader.GetRequiredArray<bool>(document.RootElement, "BooleanValues"), Is.EqualTo(new[] { true, false }));
                Assert.That(JsonLoader.GetRequiredArray<sbyte>(document.RootElement, "SByteValues"), Is.EqualTo(new sbyte[] { -1, 2 }));
                Assert.That(JsonLoader.GetRequiredArray<byte>(document.RootElement, "ByteValues"), Is.EqualTo(new byte[] { 1, 2 }));
                Assert.That(JsonLoader.GetRequiredArray<short>(document.RootElement, "Int16Values"), Is.EqualTo(new short[] { -2, 3 }));
                Assert.That(JsonLoader.GetRequiredArray<ushort>(document.RootElement, "UInt16Values"), Is.EqualTo(new ushort[] { 2, 3 }));
                Assert.That(JsonLoader.GetRequiredArray<int>(document.RootElement, "Int32Values"), Is.EqualTo(new[] { -3, 4 }));
                Assert.That(JsonLoader.GetRequiredArray<uint>(document.RootElement, "UInt32Values"), Is.EqualTo(new uint[] { 3, 4 }));
                Assert.That(JsonLoader.GetRequiredArray<long>(document.RootElement, "Int64Values"), Is.EqualTo(new long[] { -4, 5 }));
                Assert.That(JsonLoader.GetRequiredArray<ulong>(document.RootElement, "UInt64Values"), Is.EqualTo(new ulong[] { 4, 5 }));
                Assert.That(JsonLoader.GetRequiredArray<float>(document.RootElement, "SingleValues"), Is.EqualTo(new[] { 1.5f, 2.5f }));
                Assert.That(JsonLoader.GetRequiredArray<double>(document.RootElement, "DoubleValues"), Is.EqualTo(new[] { 2.5d, 3.5d }));
                Assert.That(JsonLoader.GetRequiredArray<decimal>(document.RootElement, "DecimalValues"), Is.EqualTo(new[] { 3.5m, 4.5m }));
                Assert.That(JsonLoader.GetRequiredArray<char>(document.RootElement, "CharValues"), Is.EqualTo(new[] { 'A', 'B' }));
                Assert.That(JsonLoader.GetRequired<string[]>(document.RootElement, "StringValues"), Is.EqualTo(new[] { "One", "Two" }));
            }
        }

        [Test]
        public void TryGet_IsCaseInsensitiveAndReturnsFalseWhenMissing()
        {
            const string json = """
                {
                    "DisplayName": "Nomad"
                }
                """;

            using JsonDocument document = ParseDocument(json);

            bool foundName = JsonLoader.TryGet<string>(document.RootElement, "displayname", out string? displayName);
            bool foundMissing = JsonLoader.TryGet<int>(document.RootElement, "missing", out int _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundName, Is.True);
                Assert.That(displayName, Is.EqualTo("Nomad"));
                Assert.That(foundMissing, Is.False);
            }
        }

        [Test]
        public void GetOptionalArray_ReturnsProvidedDefaultWhenMissing()
        {
            const string json = """
                {
                    "Present": 1
                }
                """;

            using JsonDocument document = ParseDocument(json);
            int[] fallback = [7, 8, 9];

            int[] result = JsonLoader.GetOptionalArray(document.RootElement, "Missing", fallback);

            Assert.That(result, Is.EqualTo(fallback));
        }

        private static JsonDocument ParseDocument(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            using var stream = new MemoryStream(bytes);
            return JsonLoader.Parse(stream);
        }
    }
}
