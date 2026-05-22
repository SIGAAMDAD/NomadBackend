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
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Utilities")]
    [Category("Unit")]
    public class AnyTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_Bool_StoresCorrectly()
        {
            var any = new Any(true);
            Assert.That(any.Type, Is.EqualTo(AnyType.Boolean));
            bool value = any; // implicit conversion
            Assert.That(value, Is.True);
        }

        [Test]
        public void Constructor_Int8_StoresCorrectly()
        {
            var any = new Any((sbyte)42);
            Assert.That(any.Type, Is.EqualTo(AnyType.Int8));
            sbyte value = any;
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Constructor_Int16_StoresCorrectly()
        {
            var any = new Any((short)4242);
            Assert.That(any.Type, Is.EqualTo(AnyType.Int16));
            short value = any;
            Assert.That(value, Is.EqualTo(4242));
        }

        [Test]
        public void Constructor_Int32_StoresCorrectly()
        {
            var any = new Any(424242);
            Assert.That(any.Type, Is.EqualTo(AnyType.Int32));
            int value = any;
            Assert.That(value, Is.EqualTo(424242));
        }

        [Test]
        public void Constructor_Int64_StoresCorrectly()
        {
            var any = new Any(424242424242L);
            Assert.That(any.Type, Is.EqualTo(AnyType.Int64));
            long value = any;
            Assert.That(value, Is.EqualTo(424242424242L));
        }

        [Test]
        public void Constructor_UInt8_StoresCorrectly()
        {
            var any = new Any((byte)42);
            Assert.That(any.Type, Is.EqualTo(AnyType.UInt8));
            byte value = any;
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Constructor_UInt16_StoresCorrectly()
        {
            var any = new Any((ushort)4242);
            Assert.That(any.Type, Is.EqualTo(AnyType.UInt16));
            ushort value = any;
            Assert.That(value, Is.EqualTo(4242));
        }

        [Test]
        public void Constructor_UInt32_StoresCorrectly()
        {
            var any = new Any(424242U);
            Assert.That(any.Type, Is.EqualTo(AnyType.UInt32));
            uint value = any;
            Assert.That(value, Is.EqualTo(424242U));
        }

        [Test]
        public void Constructor_UInt64_StoresCorrectly()
        {
            var any = new Any(424242424242UL);
            Assert.That(any.Type, Is.EqualTo(AnyType.UInt64));
            ulong value = any;
            Assert.That(value, Is.EqualTo(424242424242UL));
        }

        [Test]
        public void Constructor_Float32_StoresCorrectly()
        {
            var any = new Any(42.42f);
            Assert.That(any.Type, Is.EqualTo(AnyType.Float32));
            float value = any;
            Assert.That(value, Is.EqualTo(42.42f));
        }

        [Test]
        public void Constructor_Float64_StoresCorrectly()
        {
            var any = new Any(42.4242);
            Assert.That(any.Type, Is.EqualTo(AnyType.Float64));
            double value = any;
            Assert.That(value, Is.EqualTo(42.4242));
        }

        [Test]
        public void Constructor_String_StoresCorrectly()
        {
            var any = new Any("Hello, World!");
            Assert.That(any.Type, Is.EqualTo(AnyType.String));
            string value = any;
            Assert.That(value, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void Constructor_String_Null_StoresCorrectly()
        {
            var any = new Any(null!);
            Assert.That(any.Type, Is.EqualTo(AnyType.String));
            string value = any;
            Assert.That(value, Is.Null);
        }

        #endregion

        #region Implicit Conversion Tests

        [Test]
        public void ImplicitConversion_DoesNotMutateOriginalType()
        {
            var any = new Any(123); // Int32
            int converted = any;    // conversion on copy
            Assert.That(any.Type, Is.EqualTo(AnyType.Int32)); // original unchanged
        }

        [Test]
        public void ImplicitConversion_Bool_ReturnsCorrectValue()
        {
            var any = new Any(true);
            bool result = any;
            Assert.That(result, Is.True);
        }

        [Test]
        public void ImplicitConversion_Int8_ReturnsCorrectValue()
        {
            var any = new Any((sbyte)42);
            sbyte result = any;
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void ImplicitConversion_Int16_ReturnsCorrectValue()
        {
            var any = new Any((short)4242);
            short result = any;
            Assert.That(result, Is.EqualTo(4242));
        }

        [Test]
        public void ImplicitConversion_Int32_ReturnsCorrectValue()
        {
            var any = new Any(424242);
            int result = any;
            Assert.That(result, Is.EqualTo(424242));
        }

        [Test]
        public void ImplicitConversion_Int64_ReturnsCorrectValue()
        {
            var any = new Any(424242424242L);
            long result = any;
            Assert.That(result, Is.EqualTo(424242424242L));
        }

        [Test]
        public void ImplicitConversion_UInt8_ReturnsCorrectValue()
        {
            var any = new Any((byte)42);
            byte result = any;
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void ImplicitConversion_UInt16_ReturnsCorrectValue()
        {
            var any = new Any((ushort)4242);
            ushort result = any;
            Assert.That(result, Is.EqualTo(4242));
        }

        [Test]
        public void ImplicitConversion_UInt32_ReturnsCorrectValue()
        {
            var any = new Any(424242U);
            uint result = any;
            Assert.That(result, Is.EqualTo(424242U));
        }

        [Test]
        public void ImplicitConversion_UInt64_ReturnsCorrectValue()
        {
            var any = new Any(424242424242UL);
            ulong result = any;
            Assert.That(result, Is.EqualTo(424242424242UL));
        }

        [Test]
        public void ImplicitConversion_Float32_ReturnsCorrectValue()
        {
            var any = new Any(42.42f);
            float result = any;
            Assert.That(result, Is.EqualTo(42.42f));
        }

        [Test]
        public void ImplicitConversion_Float64_ReturnsCorrectValue()
        {
            var any = new Any(42.4242);
            double result = any;
            Assert.That(result, Is.EqualTo(42.4242));
        }

        [Test]
        public void ImplicitConversion_String_ReturnsCorrectValue()
        {
            var any = new Any("test");
            string result = any;
            Assert.That(result, Is.EqualTo("test"));
        }

        // The following tests verify that implicit conversion reinterprets bytes when types mismatch.
        // These are not required for correctness but document the unsafe behavior.
        [Test]
        public void ImplicitConversion_Mismatch_ReinterpretsBytes()
        {
            var any = new Any(12345); // Int32
            short mismatch = any;      // reading as Int16
            // 12345 in hex = 0x3039; on little‑endian, low word = 0x3039 = 12345
            Assert.That(mismatch, Is.EqualTo(12345));
        }

        #endregion

        #region From<T> Method Tests

        [Test]
        public void From_Bool_CreatesCorrectAny()
        {
            var any = Any.From(true);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Boolean));
                Assert.That((bool)any, Is.True);
            }
        }

        [Test]
        public void From_Int8_CreatesCorrectAny()
        {
            var any = Any.From((sbyte)42);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Int8));
                Assert.That((sbyte)any, Is.EqualTo(42));
            }
        }

        [Test]
        public void From_Int16_CreatesCorrectAny()
        {
            var any = Any.From((short)4242);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Int16));
                Assert.That((short)any, Is.EqualTo(4242));
            }
        }

        [Test]
        public void From_Int32_CreatesCorrectAny()
        {
            var any = Any.From(424242);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Int32));
                Assert.That((int)any, Is.EqualTo(424242));
            }
        }

        [Test]
        public void From_Int64_CreatesCorrectAny()
        {
            var any = Any.From(424242424242L);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Int64));
                Assert.That((long)any, Is.EqualTo(424242424242L));
            }
        }

        [Test]
        public void From_UInt8_CreatesCorrectAny()
        {
            var any = Any.From((byte)42);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.UInt8));
                Assert.That((byte)any, Is.EqualTo(42));
            }
        }

        [Test]
        public void From_UInt16_CreatesCorrectAny()
        {
            var any = Any.From((ushort)4242);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.UInt16));
                Assert.That((ushort)any, Is.EqualTo(4242));
            }
        }

        [Test]
        public void From_UInt32_CreatesCorrectAny()
        {
            var any = Any.From(424242U);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.UInt32));
                Assert.That((uint)any, Is.EqualTo(424242U));
            }
        }

        [Test]
        public void From_UInt64_CreatesCorrectAny()
        {
            var any = Any.From(424242424242UL);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.UInt64));
                Assert.That((ulong)any, Is.EqualTo(424242424242UL));
            }
        }

        [Test]
        public void From_Float32_CreatesCorrectAny()
        {
            var any = Any.From(42.42f);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Float32));
                Assert.That((float)any, Is.EqualTo(42.42f));
            }
        }

        [Test]
        public void From_Float64_CreatesCorrectAny()
        {
            var any = Any.From(42.4242);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.Float64));
                Assert.That((double)any, Is.EqualTo(42.4242));
            }
        }

        [Test]
        public void From_String_CreatesCorrectAny()
        {
            var any = Any.From("hello");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.String));
                Assert.That((string?)any, Is.EqualTo("hello"));
            }
        }

        [Test]
        public void From_String_Null_CreatesCorrectAny()
        {
            var any = Any.From<string>(null!);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(any.Type, Is.EqualTo(AnyType.String));
                Assert.That((string?)any, Is.Null);
            }
        }

        [Test]
        public void From_UnsupportedType_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Any.From(DateTime.Now));
            Assert.Throws<InvalidCastException>(() => Any.From(Guid.NewGuid()));
        }

        #endregion

        #region GetPrimitiveValue Tests

        [Test]
        public void GetPrimitiveValue_Bool_ReturnsCorrectValue()
        {
            var any = new Any(true);
            bool result = any.GetPrimitiveValue<bool>();
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetPrimitiveValue_Int8_ReturnsCorrectValue()
        {
            var any = new Any((sbyte)42);
            sbyte result = any.GetPrimitiveValue<sbyte>();
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void GetPrimitiveValue_Int16_ReturnsCorrectValue()
        {
            var any = new Any((short)4242);
            short result = any.GetPrimitiveValue<short>();
            Assert.That(result, Is.EqualTo(4242));
        }

        [Test]
        public void GetPrimitiveValue_Int32_ReturnsCorrectValue()
        {
            var any = new Any(424242);
            int result = any.GetPrimitiveValue<int>();
            Assert.That(result, Is.EqualTo(424242));
        }

        [Test]
        public void GetPrimitiveValue_Int64_ReturnsCorrectValue()
        {
            var any = new Any(424242424242L);
            long result = any.GetPrimitiveValue<long>();
            Assert.That(result, Is.EqualTo(424242424242L));
        }

        [Test]
        public void GetPrimitiveValue_UInt8_ReturnsCorrectValue()
        {
            var any = new Any((byte)42);
            byte result = any.GetPrimitiveValue<byte>();
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void GetPrimitiveValue_UInt16_ReturnsCorrectValue()
        {
            var any = new Any((ushort)4242);
            ushort result = any.GetPrimitiveValue<ushort>();
            Assert.That(result, Is.EqualTo(4242));
        }

        [Test]
        public void GetPrimitiveValue_UInt32_ReturnsCorrectValue()
        {
            var any = new Any(424242U);
            uint result = any.GetPrimitiveValue<uint>();
            Assert.That(result, Is.EqualTo(424242U));
        }

        [Test]
        public void GetPrimitiveValue_UInt64_ReturnsCorrectValue()
        {
            var any = new Any(424242424242UL);
            ulong result = any.GetPrimitiveValue<ulong>();
            Assert.That(result, Is.EqualTo(424242424242UL));
        }

        [Test]
        public void GetPrimitiveValue_Float32_ReturnsCorrectValue()
        {
            var any = new Any(42.42f);
            float result = any.GetPrimitiveValue<float>();
            Assert.That(result, Is.EqualTo(42.42f));
        }

        [Test]
        public void GetPrimitiveValue_Float64_ReturnsCorrectValue()
        {
            var any = new Any(42.4242);
            double result = any.GetPrimitiveValue<double>();
            Assert.That(result, Is.EqualTo(42.4242));
        }

        [Test]
        [TestCase((sbyte)23)]
        [TestCase((short)23)]
        [TestCase((int)23)]
        [TestCase((long)23)]
        public void GetPrimitiveValue_OnValidToString_ReturnsSameInteger<T>(T value)
            where T : unmanaged
        {
            var any = new Any("23");
            Assert.That(any.GetPrimitiveValue<T>(), Is.EqualTo(value));
        }

        [Test]
        [TestCase((byte)23)]
        [TestCase((ushort)23)]
        [TestCase((uint)23)]
        [TestCase((ulong)23)]
        public void GetPrimitiveValue_OnValidToString_ReturnsSameUInteger<T>(T value)
            where T : unmanaged
        {
            var any = new Any("23");
            Assert.That(any.GetPrimitiveValue<T>(), Is.EqualTo(value));
        }

        [Test]
        public void GetPrimitiveValue_OnValidToStringWithInvalidPrimitiveType_ThrowsInvalidOperationException()
        {
            var any = new Any("23");
            Assert.Throws<InvalidOperationException>(() => any.GetPrimitiveValue<nuint>());
        }

        [Test]
        public void GetPrimitiveValue_OnInvalidToString_ThrowsFormatException()
        {
            var any = new Any("test");
            Assert.Throws<FormatException>(() => any.GetPrimitiveValue<int>());
        }

        [Test]
        public void GetPrimitiveValue_OnInvalidType_ThrowsInvalidOperationException()
        {
            var any = new Any(0);
            Assert.Throws<InvalidOperationException>(() => any.GetPrimitiveValue<nint>());
        }

        #endregion

        #region GetReferenceValue Tests

        [Test]
        public void GetReferenceValue_String_ReturnsCorrectValue()
        {
            var any = new Any("hello");
            string result = any.GetReferenceValue<string>()!;
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void GetReferenceValue_String_Null_ReturnsNull()
        {
            var any = new Any(null!);
            string result = any.GetReferenceValue<string>()!;
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetReferenceValue_InvalidReferenceType_ThrowsInvalidOperationException()
        {
            var any = new Any("");
            Assert.Throws<InvalidOperationException>(() => any.GetReferenceValue<object>());
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_Bool_ReturnsCorrectString()
        {
            var any = new Any(true);
            Assert.That(any.ToString(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void ToString_Int8_ReturnsCorrectString()
        {
            var any = new Any((sbyte)42);
            Assert.That(any.ToString(), Is.EqualTo(42.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_Int16_ReturnsCorrectString()
        {
            var any = new Any((short)4242);
            Assert.That(any.ToString(), Is.EqualTo(4242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_Int32_ReturnsCorrectString()
        {
            var any = new Any(424242);
            Assert.That(any.ToString(), Is.EqualTo(424242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_Int64_ReturnsCorrectString()
        {
            var any = new Any(424242424242L);
            Assert.That(any.ToString(), Is.EqualTo(424242424242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_UInt8_ReturnsCorrectString()
        {
            var any = new Any((byte)42);
            Assert.That(any.ToString(), Is.EqualTo(42.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_UInt16_ReturnsCorrectString()
        {
            var any = new Any((ushort)4242);
            Assert.That(any.ToString(), Is.EqualTo(4242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_UInt32_ReturnsCorrectString()
        {
            var any = new Any(424242U);
            Assert.That(any.ToString(), Is.EqualTo(424242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_UInt64_ReturnsCorrectString()
        {
            var any = new Any(424242424242UL);
            Assert.That(any.ToString(), Is.EqualTo(424242424242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_Float32_ReturnsCorrectString()
        {
            var any = new Any(42.42f);
            Assert.That(any.ToString(), Is.EqualTo(42.42f.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_Float64_ReturnsCorrectString()
        {
            var any = new Any(42.4242);
            Assert.That(any.ToString(), Is.EqualTo(42.4242.ToString(CultureInfo.CurrentCulture)));
        }

        [Test]
        public void ToString_String_ReturnsString()
        {
            var any = new Any("hello");
            Assert.That(any.ToString(), Is.EqualTo("hello"));
        }

        [Test]
        public void ToString_String_Null_ReturnsNull()
        {
            var any = new Any(null!);
            Assert.That(any.ToString(), Is.Null);
        }

        [Test]
        public void ToString_DefaultAny_ReturnsNull()
        {
            Any any = default;
            Assert.That(() => any.ToString(), Is.Null);
        }

        #endregion

        #region Type Mapping Tests

        [Test]
        public void GetAnyType_ForEachAnyType_ReturnsCorrectSystemType()
        {
            var mappings = new Dictionary<AnyType, Type>
            {
                { AnyType.Boolean, typeof(bool) },
                { AnyType.Int8, typeof(sbyte) },
                { AnyType.Int16, typeof(short) },
                { AnyType.Int32, typeof(int) },
                { AnyType.Int64, typeof(long) },
                { AnyType.UInt8, typeof(byte) },
                { AnyType.UInt16, typeof(ushort) },
                { AnyType.UInt32, typeof(uint) },
                { AnyType.UInt64, typeof(ulong) },
                { AnyType.Float32, typeof(float) },
                { AnyType.Float64, typeof(double) },
                { AnyType.String, typeof(string) }
            };

            foreach (var kvp in mappings)
            {
                Type result = Any.GetAnyType(kvp.Key);
                Assert.That(result, Is.EqualTo(kvp.Value), $"Failed for AnyType.{kvp.Key}");
            }
        }

        [Test]
        public void GetAnyType_InvalidAnyType_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Any.GetAnyType((AnyType)byte.MaxValue));
        }

        [Test]
        public void GetType_ForEachSupportedType_ReturnsCorrectAnyType()
        {
            var mappings = new Dictionary<Type, AnyType>
            {
                { typeof(bool), AnyType.Boolean },
                { typeof(sbyte), AnyType.Int8 },
                { typeof(short), AnyType.Int16 },
                { typeof(int), AnyType.Int32 },
                { typeof(long), AnyType.Int64 },
                { typeof(byte), AnyType.UInt8 },
                { typeof(ushort), AnyType.UInt16 },
                { typeof(uint), AnyType.UInt32 },
                { typeof(ulong), AnyType.UInt64 },
                { typeof(float), AnyType.Float32 },
                { typeof(double), AnyType.Float64 },
                { typeof(string), AnyType.String }
            };

            foreach (var kvp in mappings)
            {
                // Use reflection to call generic method for each type
                var method = typeof(Any).GetMethod(nameof(Any.GetType), Type.EmptyTypes);
                var generic = method.MakeGenericMethod(kvp.Key);
                AnyType result = (AnyType)generic.Invoke(null, null)!;
                Assert.That(result, Is.EqualTo(kvp.Value), $"Failed for type {kvp.Key}");
            }
        }

        [Test]
        public void GetType_UnsupportedType_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Any.GetType<DateTime>());
        }

        #endregion

        #region Size and Layout Tests

        [Test]
        public unsafe void SizeOfAny_Is24Bytes()
        {
            int size = sizeof(Any);
            Assert.That(size, Is.EqualTo(24));
        }

        [Test]
        public void StructLayout_SequentialOrExplicit_EnsuresUnionSize()
        {
            // Verify that the union is 16 bytes (8 for value types + 8 for reference)
            int unionSize = Marshal.SizeOf<Any.Union>(); // May need to make Union public or use reflection
            Assert.That(unionSize, Is.EqualTo(16));
        }

        #endregion

        #region Default and Edge Cases

        [Test]
        public void DefaultAny_HasTypeZeroAndZeroUnion()
        {
            Any any = default;
            Assert.That(any.Type, Is.EqualTo(AnyType.None));
            // Implicit conversion to bool reads the Boolean field (offset 0) which is default false.
            bool asBool = any;
            Assert.That(asBool, Is.False);
        }

        [Test]
        public void DefaultAny_AfterImplicitConversion_TypeRemainsZero()
        {
            Any any = default;
            int asInt = any; // reads Int32 (offset 0) which is 0
            using (Assert.EnterMultipleScope())
            {
                Assert.That(asInt, Is.Zero);
                Assert.That(any.Type, Is.EqualTo(AnyType.None));
            }
        }

        #endregion
    }
}
#endif