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
using System.Numerics;
using NUnit.Framework;

namespace Nomad.Core.Numerics.Tests {
	[TestFixture]
	[Category( "Nomad.Core" )]
    [Category( "Numerics" )]
    [Category( "Unit" )]
	public sealed class PackedVector2U32Tests {
		private const float Tolerance = 0.0000001f;

		/*
		================================================================================
		Constructor / packing layout
		================================================================================
		*/

		[Test]
		public void DefaultValue_HasZeroPackedValue() {
			var vector = default( PackedVector2U32 );

			Assert.Multiple( () => {
				Assert.That( vector.Packed, Is.EqualTo( 0u ) );
				Assert.That( vector.X, Is.EqualTo( 0 ) );
				Assert.That( vector.Y, Is.EqualTo( 0 ) );
			} );
		}

		[TestCase( -1.0f, -1.0f, 0x00000000u, 0x0000, 0x0000 )]
		[TestCase( 1.0f, -1.0f, 0x0000FFFFu, 0xFFFF, 0x0000 )]
		[TestCase( -1.0f, 1.0f, 0xFFFF0000u, 0x0000, 0xFFFF )]
		[TestCase( 1.0f, 1.0f, 0xFFFFFFFFu, 0xFFFF, 0xFFFF )]
		[TestCase( 0.0f, 0.0f, 0x80008000u, 0x8000, 0x8000 )]
		public void Constructor_PacksComponentsIntoExpectedUShortLayout(
			float x,
			float y,
			uint expectedPacked,
			int expectedX,
			int expectedY ) {
			var vector = new PackedVector2U32( x, y );

			Assert.Multiple( () => {
				Assert.That( vector.Packed, Is.EqualTo( expectedPacked ) );
				Assert.That( vector.X, Is.EqualTo( (ushort)expectedX ) );
				Assert.That( vector.Y, Is.EqualTo( (ushort)expectedY ) );
			} );
		}

		[Test]
		public void Constructor_PacksXIntoLowUShort_AndYIntoHighUShort() {
			var vector = new PackedVector2U32( 1.0f, -1.0f );

			Assert.Multiple( () => {
				Assert.That( vector.Packed & 0x0000FFFFu, Is.EqualTo( vector.X ) );
				Assert.That( ( vector.Packed >> 16 ) & 0x0000FFFFu, Is.EqualTo( vector.Y ) );
			} );
		}

		/*
		================================================================================
		Quantization
		================================================================================
		*/

		[TestCase( -1.0f, 0 )]
		[TestCase( -0.75f, 8192 )]
		[TestCase( -0.5f, 16384 )]
		[TestCase( -0.25f, 24576 )]
		[TestCase( 0.0f, 32768 )]
		[TestCase( 0.25f, 40959 )]
		[TestCase( 0.5f, 49151 )]
		[TestCase( 0.75f, 57343 )]
		[TestCase( 1.0f, 65535 )]
		public void Constructor_QuantizesXComponentAsSignedNormalized16( float x, int expectedX ) {
			var vector = new PackedVector2U32( x, -1.0f );

			Assert.That( vector.X, Is.EqualTo( (ushort)expectedX ) );
		}

		[TestCase( -1.0f, 0 )]
		[TestCase( -0.75f, 8192 )]
		[TestCase( -0.5f, 16384 )]
		[TestCase( -0.25f, 24576 )]
		[TestCase( 0.0f, 32768 )]
		[TestCase( 0.25f, 40959 )]
		[TestCase( 0.5f, 49151 )]
		[TestCase( 0.75f, 57343 )]
		[TestCase( 1.0f, 65535 )]
		public void Constructor_QuantizesYComponentAsSignedNormalized16( float y, int expectedY ) {
			var vector = new PackedVector2U32( -1.0f, y );

			Assert.That( vector.Y, Is.EqualTo( (ushort)expectedY ) );
		}

		[TestCase( -100.0f, 0 )]
		[TestCase( -2.0f, 0 )]
		[TestCase( -1.5f, 0 )]
		[TestCase( 1.5f, 65535 )]
		[TestCase( 2.0f, 65535 )]
		[TestCase( 100.0f, 65535 )]
		public void Constructor_ClampsXBeforeQuantization( float x, int expectedX ) {
			var vector = new PackedVector2U32( x, 0.0f );

			Assert.That( vector.X, Is.EqualTo( (ushort)expectedX ) );
		}

		[TestCase( -100.0f, 0 )]
		[TestCase( -2.0f, 0 )]
		[TestCase( -1.5f, 0 )]
		[TestCase( 1.5f, 65535 )]
		[TestCase( 2.0f, 65535 )]
		[TestCase( 100.0f, 65535 )]
		public void Constructor_ClampsYBeforeQuantization( float y, int expectedY ) {
			var vector = new PackedVector2U32( 0.0f, y );

			Assert.That( vector.Y, Is.EqualTo( (ushort)expectedY ) );
		}

		[Test]
		public void Constructor_UsesRoundToNearestQuantization() {
			var vector = new PackedVector2U32( 0.0f, 0.0f );

			Assert.Multiple( () => {
				Assert.That( vector.X, Is.EqualTo( 32768 ) );
				Assert.That( vector.Y, Is.EqualTo( 32768 ) );
			} );
		}

		/*
		================================================================================
		Dequantization / Vector2 conversion
		================================================================================
		*/

		[TestCase( -1.0f, -1.0f )]
		[TestCase( -0.75f, -0.25f )]
		[TestCase( -0.5f, 0.5f )]
		[TestCase( 0.0f, 0.0f )]
		[TestCase( 0.25f, 0.75f )]
		[TestCase( 1.0f, 1.0f )]
		public void ImplicitVector2Conversion_ReturnsDequantizedVector( float x, float y ) {
			var packed = new PackedVector2U32( x, y );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( DequantizeSignedNormalized16( packed.X ) ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( DequantizeSignedNormalized16( packed.Y ) ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_ForMinimumPackedValue_ReturnsNegativeOneComponents() {
			var packed = new PackedVector2U32( -1.0f, -1.0f );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( -1.0f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( -1.0f ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_ForMaximumPackedValue_ReturnsPositiveOneComponents() {
			var packed = new PackedVector2U32( 1.0f, 1.0f );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( 1.0f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( 1.0f ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_ForZeroInput_ReturnsNearestRepresentableValue() {
			var packed = new PackedVector2U32( 0.0f, 0.0f );

			Vector2 actual = packed;

			var expected = DequantizeSignedNormalized16( 32768 );

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( expected ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( expected ).Within( Tolerance ) );
				Assert.That( actual.X, Is.EqualTo( 0.000015259022f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( 0.000015259022f ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_DefaultPackedValue_ReturnsNegativeOneComponents() {
			var packed = default( PackedVector2U32 );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( -1.0f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( -1.0f ).Within( Tolerance ) );
			} );
		}

		/*
		================================================================================
		Round-trip precision
		================================================================================
		*/

		[TestCase( -1.0f, -1.0f )]
		[TestCase( -0.95f, 0.95f )]
		[TestCase( -0.75f, 0.75f )]
		[TestCase( -0.5f, 0.5f )]
		[TestCase( -0.25f, 0.25f )]
		[TestCase( 0.0f, 0.0f )]
		[TestCase( 0.25f, -0.25f )]
		[TestCase( 0.5f, -0.5f )]
		[TestCase( 0.75f, -0.75f )]
		[TestCase( 0.95f, -0.95f )]
		[TestCase( 1.0f, 1.0f )]
		public void RoundTrip_ForNormalizedInputs_StaysWithinSixteenBitQuantizationError( float x, float y ) {
			var packed = new PackedVector2U32( x, y );

			Vector2 actual = packed;

			var maxError = 1.0f / ushort.MaxValue;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( x ).Within( maxError ) );
				Assert.That( actual.Y, Is.EqualTo( y ).Within( maxError ) );
			} );
		}

		[TestCase( -5.0f, -5.0f, -1.0f, -1.0f )]
		[TestCase( -2.0f, 2.0f, -1.0f, 1.0f )]
		[TestCase( 2.0f, -2.0f, 1.0f, -1.0f )]
		[TestCase( 5.0f, 5.0f, 1.0f, 1.0f )]
		public void RoundTrip_ForOutOfRangeInputs_ReturnsClampedDequantizedValues(
			float x,
			float y,
			float expectedX,
			float expectedY ) {
			var packed = new PackedVector2U32( x, y );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( expectedX ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( expectedY ).Within( Tolerance ) );
			} );
		}

		/*
		================================================================================
		Precision comparison behavior
		================================================================================
		*/

		[TestCase( -0.95f, 0.95f )]
		[TestCase( -0.75f, 0.75f )]
		[TestCase( -0.5f, 0.5f )]
		[TestCase( -0.25f, 0.25f )]
		[TestCase( 0.0f, 0.0f )]
		[TestCase( 0.25f, -0.25f )]
		[TestCase( 0.5f, -0.5f )]
		[TestCase( 0.75f, -0.75f )]
		[TestCase( 0.95f, -0.95f )]
		public void RoundTrip_HasHighPrecisionExpectedOfSixteenBitComponents( float x, float y ) {
			var packed = new PackedVector2U32( x, y );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( MathF.Abs( actual.X - x ), Is.LessThanOrEqualTo( 1.0f / ushort.MaxValue ) );
				Assert.That( MathF.Abs( actual.Y - y ), Is.LessThanOrEqualTo( 1.0f / ushort.MaxValue ) );
			} );
		}

		/*
		================================================================================
		Independence
		================================================================================
		*/

		[Test]
		public void ChangingXInput_DoesNotAffectPackedYComponent() {
			var first = new PackedVector2U32( -1.0f, 0.25f );
			var second = new PackedVector2U32( 1.0f, 0.25f );

			Assert.That( first.Y, Is.EqualTo( second.Y ) );
		}

		[Test]
		public void ChangingYInput_DoesNotAffectPackedXComponent() {
			var first = new PackedVector2U32( 0.25f, -1.0f );
			var second = new PackedVector2U32( 0.25f, 1.0f );

			Assert.That( first.X, Is.EqualTo( second.X ) );
		}

		private static float DequantizeSignedNormalized16( ushort value ) {
			var normalized = value / (float)ushort.MaxValue;
			return ( normalized * 2.0f ) - 1.0f;
		}
	}
}