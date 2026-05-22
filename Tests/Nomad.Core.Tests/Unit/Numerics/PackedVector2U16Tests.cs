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
	public sealed class PackedVector2U16Tests {
		private const float Tolerance = 0.000001f;

		/*
		================================================================================
		Constructor / packing layout
		================================================================================
		*/

		[Test]
		public void DefaultValue_HasZeroPackedValue() {
			var vector = default( PackedVector2U16 );

			Assert.Multiple( () => {
				Assert.That( vector.Packed, Is.EqualTo( 0 ) );
				Assert.That( vector.X, Is.EqualTo( 0 ) );
				Assert.That( vector.Y, Is.EqualTo( 0 ) );
			} );
		}

		[TestCase( -1.0f, -1.0f, 0x0000, 0x00, 0x00 )]
		[TestCase( 1.0f, -1.0f, 0x00FF, 0xFF, 0x00 )]
		[TestCase( -1.0f, 1.0f, 0xFF00, 0x00, 0xFF )]
		[TestCase( 1.0f, 1.0f, 0xFFFF, 0xFF, 0xFF )]
		[TestCase( 0.0f, 0.0f, 0x8080, 0x80, 0x80 )]
		public void Constructor_PacksComponentsIntoExpectedByteLayout(
			float x,
			float y,
			int expectedPacked,
			int expectedX,
			int expectedY ) {
			var vector = new PackedVector2U16( x, y );

			Assert.Multiple( () => {
				Assert.That( vector.Packed, Is.EqualTo( (ushort)expectedPacked ) );
				Assert.That( vector.X, Is.EqualTo( (byte)expectedX ) );
				Assert.That( vector.Y, Is.EqualTo( (byte)expectedY ) );
			} );
		}

		[Test]
		public void Constructor_PacksXIntoLowByte_AndYIntoHighByte() {
			var vector = new PackedVector2U16( 1.0f, -1.0f );

			Assert.Multiple( () => {
				Assert.That( vector.Packed & 0x00FF, Is.EqualTo( vector.X ) );
				Assert.That( ( vector.Packed >> 8 ) & 0x00FF, Is.EqualTo( vector.Y ) );
			} );
		}

		/*
		================================================================================
		Quantization
		================================================================================
		*/

		[TestCase( -1.0f, 0 )]
		[TestCase( -0.75f, 32 )]
		[TestCase( -0.5f, 64 )]
		[TestCase( -0.25f, 96 )]
		[TestCase( 0.0f, 128 )]
		[TestCase( 0.25f, 159 )]
		[TestCase( 0.5f, 191 )]
		[TestCase( 0.75f, 223 )]
		[TestCase( 1.0f, 255 )]
		public void Constructor_QuantizesXComponentAsSignedNormalized8( float x, int expectedX ) {
			var vector = new PackedVector2U16( x, -1.0f );

			Assert.That( vector.X, Is.EqualTo( (byte)expectedX ) );
		}

		[TestCase( -1.0f, 0 )]
		[TestCase( -0.75f, 32 )]
		[TestCase( -0.5f, 64 )]
		[TestCase( -0.25f, 96 )]
		[TestCase( 0.0f, 128 )]
		[TestCase( 0.25f, 159 )]
		[TestCase( 0.5f, 191 )]
		[TestCase( 0.75f, 223 )]
		[TestCase( 1.0f, 255 )]
		public void Constructor_QuantizesYComponentAsSignedNormalized8( float y, int expectedY ) {
			var vector = new PackedVector2U16( -1.0f, y );

			Assert.That( vector.Y, Is.EqualTo( (byte)expectedY ) );
		}

		[TestCase( -100.0f, 0 )]
		[TestCase( -2.0f, 0 )]
		[TestCase( -1.5f, 0 )]
		[TestCase( 1.5f, 255 )]
		[TestCase( 2.0f, 255 )]
		[TestCase( 100.0f, 255 )]
		public void Constructor_ClampsXBeforeQuantization( float x, int expectedX ) {
			var vector = new PackedVector2U16( x, 0.0f );

			Assert.That( vector.X, Is.EqualTo( (byte)expectedX ) );
		}

		[TestCase( -100.0f, 0 )]
		[TestCase( -2.0f, 0 )]
		[TestCase( -1.5f, 0 )]
		[TestCase( 1.5f, 255 )]
		[TestCase( 2.0f, 255 )]
		[TestCase( 100.0f, 255 )]
		public void Constructor_ClampsYBeforeQuantization( float y, int expectedY ) {
			var vector = new PackedVector2U16( 0.0f, y );

			Assert.That( vector.Y, Is.EqualTo( (byte)expectedY ) );
		}

		[Test]
		public void Constructor_UsesRoundToNearestQuantization() {
			var vector = new PackedVector2U16( 0.0f, 0.0f );

			Assert.Multiple( () => {
				Assert.That( vector.X, Is.EqualTo( 128 ) );
				Assert.That( vector.Y, Is.EqualTo( 128 ) );
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
			var packed = new PackedVector2U16( x, y );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( DequantizeSignedNormalized8( packed.X ) ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( DequantizeSignedNormalized8( packed.Y ) ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_ForMinimumPackedValue_ReturnsNegativeOneComponents() {
			var packed = new PackedVector2U16( -1.0f, -1.0f );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( -1.0f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( -1.0f ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_ForMaximumPackedValue_ReturnsPositiveOneComponents() {
			var packed = new PackedVector2U16( 1.0f, 1.0f );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( 1.0f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( 1.0f ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_ForZeroInput_ReturnsNearestRepresentableValue() {
			var packed = new PackedVector2U16( 0.0f, 0.0f );

			Vector2 actual = packed;

			var expected = DequantizeSignedNormalized8( 128 );

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( expected ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( expected ).Within( Tolerance ) );
				Assert.That( actual.X, Is.EqualTo( 0.0039215686f ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( 0.0039215686f ).Within( Tolerance ) );
			} );
		}

		[Test]
		public void ImplicitVector2Conversion_DefaultPackedValue_ReturnsNegativeOneComponents() {
			var packed = default( PackedVector2U16 );

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
		public void RoundTrip_ForNormalizedInputs_StaysWithinEightBitQuantizationError( float x, float y ) {
			var packed = new PackedVector2U16( x, y );

			Vector2 actual = packed;

			var maxError = 1.0f / byte.MaxValue;

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
			var packed = new PackedVector2U16( x, y );

			Vector2 actual = packed;

			Assert.Multiple( () => {
				Assert.That( actual.X, Is.EqualTo( expectedX ).Within( Tolerance ) );
				Assert.That( actual.Y, Is.EqualTo( expectedY ).Within( Tolerance ) );
			} );
		}

		/*
		================================================================================
		Independence
		================================================================================
		*/

		[Test]
		public void ChangingXInput_DoesNotAffectPackedYComponent() {
			var first = new PackedVector2U16( -1.0f, 0.25f );
			var second = new PackedVector2U16( 1.0f, 0.25f );

			Assert.That( first.Y, Is.EqualTo( second.Y ) );
		}

		[Test]
		public void ChangingYInput_DoesNotAffectPackedXComponent() {
			var first = new PackedVector2U16( 0.25f, -1.0f );
			var second = new PackedVector2U16( 0.25f, 1.0f );

			Assert.That( first.X, Is.EqualTo( second.X ) );
		}

		private static float DequantizeSignedNormalized8( byte value ) {
			var normalized = value / (float)byte.MaxValue;
			return ( normalized * 2.0f ) - 1.0f;
		}
	}
}