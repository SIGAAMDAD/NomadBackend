/*
===========================================================================
The Nomad MPLv2 Source Code
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
using NUnit.Framework;

namespace Nomad.Core.Numerics.Tests {
	[TestFixture]
	[Category( "Nomad.Core" )]
    [Category( "Numerics" )]
    [Category( "Unit" )]
	public sealed class ScalarMathTests {
		private const float FloatTolerance = 0.000001f;
		private const double DoubleTolerance = 0.000000000001d;

		/*
		================================================================================
		Constants
		================================================================================
		*/

		[Test]
		public void Constants_HaveExpectedValues() {
			Assert.Multiple( () => {
				Assert.That( ScalarMath.EpsilonF, Is.EqualTo( 1e-6f ) );
				Assert.That( ScalarMath.EpsilonD, Is.EqualTo( 1e-12d ) );

				Assert.That( ScalarMath.TauF, Is.EqualTo( MathF.PI * 2.0f ).Within( FloatTolerance ) );
				Assert.That( ScalarMath.TauD, Is.EqualTo( Math.PI * 2.0d ).Within( DoubleTolerance ) );

				Assert.That( ScalarMath.Deg2RadF, Is.EqualTo( MathF.PI / 180.0f ).Within( FloatTolerance ) );
				Assert.That( ScalarMath.Rad2DegF, Is.EqualTo( 180.0f / MathF.PI ).Within( FloatTolerance ) );

				Assert.That( ScalarMath.Deg2RadD, Is.EqualTo( Math.PI / 180.0d ).Within( DoubleTolerance ) );
				Assert.That( ScalarMath.Rad2DegD, Is.EqualTo( 180.0d / Math.PI ).Within( DoubleTolerance ) );
			} );
		}

		/*
		================================================================================
		Clamp01 / Saturate
		================================================================================
		*/

		[TestCase( -10.0f, 0.0f )]
		[TestCase( -1.0f, 0.0f )]
		[TestCase( 0.0f, 0.0f )]
		[TestCase( 0.25f, 0.25f )]
		[TestCase( 0.5f, 0.5f )]
		[TestCase( 1.0f, 1.0f )]
		[TestCase( 2.0f, 1.0f )]
		[TestCase( 10.0f, 1.0f )]
		public void Clamp01_Float_ClampsToZeroOneRange( float value, float expected ) {
			var actual = ScalarMath.Clamp01( value );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( -10.0d, 0.0d )]
		[TestCase( -1.0d, 0.0d )]
		[TestCase( 0.0d, 0.0d )]
		[TestCase( 0.25d, 0.25d )]
		[TestCase( 0.5d, 0.5d )]
		[TestCase( 1.0d, 1.0d )]
		[TestCase( 2.0d, 1.0d )]
		[TestCase( 10.0d, 1.0d )]
		public void Clamp01_Double_ClampsToZeroOneRange( double value, double expected ) {
			var actual = ScalarMath.Clamp01( value );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCase( -10.0f )]
		[TestCase( 0.0f )]
		[TestCase( 0.5f )]
		[TestCase( 1.0f )]
		[TestCase( 10.0f )]
		public void Saturate_Float_MatchesClamp01( float value ) {
			Assert.That( ScalarMath.Saturate( value ), Is.EqualTo( ScalarMath.Clamp01( value ) ).Within( FloatTolerance ) );
		}

		[TestCase( -10.0d )]
		[TestCase( 0.0d )]
		[TestCase( 0.5d )]
		[TestCase( 1.0d )]
		[TestCase( 10.0d )]
		public void Saturate_Double_MatchesClamp01( double value ) {
			Assert.That( ScalarMath.Saturate( value ), Is.EqualTo( ScalarMath.Clamp01( value ) ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		IsZero
		================================================================================
		*/

		[TestCase( 0.0f, true )]
		[TestCase( 0.0000005f, true )]
		[TestCase( -0.0000005f, true )]
		[TestCase( 0.000001f, true )]
		[TestCase( -0.000001f, true )]
		[TestCase( 0.0000011f, false )]
		[TestCase( -0.0000011f, false )]
		public void IsZero_Float_UsesInclusiveDefaultEpsilon( float value, bool expected ) {
			var actual = ScalarMath.IsZero( value );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		[TestCase( 0.0d, true )]
		[TestCase( 0.0000000000005d, true )]
		[TestCase( -0.0000000000005d, true )]
		[TestCase( 0.000000000001d, true )]
		[TestCase( -0.000000000001d, true )]
		[TestCase( 0.0000000000011d, false )]
		[TestCase( -0.0000000000011d, false )]
		public void IsZero_Double_UsesInclusiveDefaultEpsilon( double value, bool expected ) {
			var actual = ScalarMath.IsZero( value );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		[Test]
		public void IsZero_Float_UsesCustomEpsilon() {
			Assert.Multiple( () => {
				Assert.That( ScalarMath.IsZero( 0.25f, 0.5f ), Is.True );
				Assert.That( ScalarMath.IsZero( 0.75f, 0.5f ), Is.False );
			} );
		}

		[Test]
		public void IsZero_Double_UsesCustomEpsilon() {
			Assert.Multiple( () => {
				Assert.That( ScalarMath.IsZero( 0.25d, 0.5d ), Is.True );
				Assert.That( ScalarMath.IsZero( 0.75d, 0.5d ), Is.False );
			} );
		}

		/*
		================================================================================
		NearlyEqual
		================================================================================
		*/

		[Test]
		public void NearlyEqual_Float_ReturnsTrueForExactlyEqualValues() {
			Assert.That( ScalarMath.NearlyEqual( 123.456f, 123.456f ), Is.True );
		}

		[Test]
		public void NearlyEqual_Double_ReturnsTrueForExactlyEqualValues() {
			Assert.That( ScalarMath.NearlyEqual( 123.456d, 123.456d ), Is.True );
		}

		[Test]
		public void NearlyEqual_Float_ReturnsTrueWhenAbsoluteDifferenceIsWithinEpsilon() {
			Assert.That( ScalarMath.NearlyEqual( 1.0f, 1.0f + 0.0000005f ), Is.True );
		}

		[Test]
		public void NearlyEqual_Double_ReturnsTrueWhenAbsoluteDifferenceIsWithinEpsilon() {
			Assert.That( ScalarMath.NearlyEqual( 1.0d, 1.0d + 0.0000000000005d ), Is.True );
		}

		[Test]
		public void NearlyEqual_Float_ReturnsTrueWhenRelativeDifferenceIsWithinEpsilon() {
			Assert.That( ScalarMath.NearlyEqual( 1_000_000.0f, 1_000_000.5f ), Is.True );
		}

		[Test]
		public void NearlyEqual_Double_ReturnsTrueWhenRelativeDifferenceIsWithinEpsilon() {
			Assert.That( ScalarMath.NearlyEqual( 1_000_000_000_000.0d, 1_000_000_000_000.5d ), Is.True );
		}

		[Test]
		public void NearlyEqual_Float_ReturnsFalseWhenDifferenceExceedsAbsoluteAndRelativeTolerance() {
			Assert.That( ScalarMath.NearlyEqual( 1.0f, 1.01f ), Is.False );
		}

		[Test]
		public void NearlyEqual_Double_ReturnsFalseWhenDifferenceExceedsAbsoluteAndRelativeTolerance() {
			Assert.That( ScalarMath.NearlyEqual( 1.0d, 1.0000001d ), Is.False );
		}

		/*
		================================================================================
		Frac
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f )]
		[TestCase( 0.25f, 0.25f )]
		[TestCase( 1.25f, 0.25f )]
		[TestCase( 10.75f, 0.75f )]
		[TestCase( -0.25f, 0.75f )]
		[TestCase( -1.25f, 0.75f )]
		[TestCase( -10.75f, 0.25f )]
		public void Frac_Float_ReturnsFractionalComponentUsingFloor( float value, float expected ) {
			var actual = ScalarMath.Frac( value );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d )]
		[TestCase( 0.25d, 0.25d )]
		[TestCase( 1.25d, 0.25d )]
		[TestCase( 10.75d, 0.75d )]
		[TestCase( -0.25d, 0.75d )]
		[TestCase( -1.25d, 0.75d )]
		[TestCase( -10.75d, 0.25d )]
		public void Frac_Double_ReturnsFractionalComponentUsingFloor( double value, double expected ) {
			var actual = ScalarMath.Frac( value );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		SignNonZero
		================================================================================
		*/

		[TestCase( -10, -1 )]
		[TestCase( -1, -1 )]
		[TestCase( 0, 1 )]
		[TestCase( 1, 1 )]
		[TestCase( 10, 1 )]
		public void SignNonZero_Int_ReturnsNegativeOneOnlyForNegativeValues( int value, int expected ) {
			Assert.That( ScalarMath.SignNonZero( value ), Is.EqualTo( expected ) );
		}

		[TestCase( -10.0f, -1.0f )]
		[TestCase( -0.0001f, -1.0f )]
		[TestCase( 0.0f, 1.0f )]
		[TestCase( 0.0001f, 1.0f )]
		[TestCase( 10.0f, 1.0f )]
		public void SignNonZero_Float_ReturnsNegativeOneOnlyForNegativeValues( float value, float expected ) {
			Assert.That( ScalarMath.SignNonZero( value ), Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( -10.0d, -1.0d )]
		[TestCase( -0.0001d, -1.0d )]
		[TestCase( 0.0d, 1.0d )]
		[TestCase( 0.0001d, 1.0d )]
		[TestCase( 10.0d, 1.0d )]
		public void SignNonZero_Double_ReturnsNegativeOneOnlyForNegativeValues( double value, double expected ) {
			Assert.That( ScalarMath.SignNonZero( value ), Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Wrap
		================================================================================
		*/

		[TestCase( 0, 0, 10, 0 )]
		[TestCase( 1, 0, 10, 1 )]
		[TestCase( 9, 0, 10, 9 )]
		[TestCase( 10, 0, 10, 0 )]
		[TestCase( 11, 0, 10, 1 )]
		[TestCase( 20, 0, 10, 0 )]
		[TestCase( -1, 0, 10, 9 )]
		[TestCase( -10, 0, 10, 0 )]
		[TestCase( -11, 0, 10, 9 )]
		[TestCase( 25, 10, 20, 15 )]
		[TestCase( 9, 10, 20, 19 )]
		public void Wrap_Int_WrapsIntoMinInclusiveMaxExclusiveRange(
			int value,
			int minInclusive,
			int maxExclusive,
			int expected ) {
			var actual = ScalarMath.Wrap( value, minInclusive, maxExclusive );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		[TestCase( 0.0f, 0.0f, 10.0f, 0.0f )]
		[TestCase( 1.0f, 0.0f, 10.0f, 1.0f )]
		[TestCase( 9.0f, 0.0f, 10.0f, 9.0f )]
		[TestCase( 10.0f, 0.0f, 10.0f, 0.0f )]
		[TestCase( 11.0f, 0.0f, 10.0f, 1.0f )]
		[TestCase( -1.0f, 0.0f, 10.0f, 9.0f )]
		[TestCase( -10.0f, 0.0f, 10.0f, 0.0f )]
		[TestCase( -11.0f, 0.0f, 10.0f, 9.0f )]
		[TestCase( 25.5f, 10.0f, 20.0f, 15.5f )]
		[TestCase( 9.5f, 10.0f, 20.0f, 19.5f )]
		public void Wrap_Float_WrapsIntoMinInclusiveMaxExclusiveRange(
			float value,
			float minInclusive,
			float maxExclusive,
			float expected ) {
			var actual = ScalarMath.Wrap( value, minInclusive, maxExclusive );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d, 10.0d, 0.0d )]
		[TestCase( 1.0d, 0.0d, 10.0d, 1.0d )]
		[TestCase( 9.0d, 0.0d, 10.0d, 9.0d )]
		[TestCase( 10.0d, 0.0d, 10.0d, 0.0d )]
		[TestCase( 11.0d, 0.0d, 10.0d, 1.0d )]
		[TestCase( -1.0d, 0.0d, 10.0d, 9.0d )]
		[TestCase( -10.0d, 0.0d, 10.0d, 0.0d )]
		[TestCase( -11.0d, 0.0d, 10.0d, 9.0d )]
		[TestCase( 25.5d, 10.0d, 20.0d, 15.5d )]
		[TestCase( 9.5d, 10.0d, 20.0d, 19.5d )]
		public void Wrap_Double_WrapsIntoMinInclusiveMaxExclusiveRange(
			double value,
			double minInclusive,
			double maxExclusive,
			double expected ) {
			var actual = ScalarMath.Wrap( value, minInclusive, maxExclusive );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Wrap_Int_WhenRangeIsInvalid_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Wrap( 0, 10, 10 ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Wrap( 0, 10, 9 ) );
			} );
		}

		[Test]
		public void Wrap_Float_WhenRangeIsInvalid_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Wrap( 0.0f, 10.0f, 10.0f ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Wrap( 0.0f, 10.0f, 9.0f ) );
			} );
		}

		[Test]
		public void Wrap_Double_WhenRangeIsInvalid_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Wrap( 0.0d, 10.0d, 10.0d ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Wrap( 0.0d, 10.0d, 9.0d ) );
			} );
		}

		/*
		================================================================================
		Repeat
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, 0.0f )]
		[TestCase( 1.0f, 10.0f, 1.0f )]
		[TestCase( 9.0f, 10.0f, 9.0f )]
		[TestCase( 10.0f, 10.0f, 0.0f )]
		[TestCase( 11.0f, 10.0f, 1.0f )]
		[TestCase( 20.0f, 10.0f, 0.0f )]
		[TestCase( -1.0f, 10.0f, 9.0f )]
		[TestCase( -10.0f, 10.0f, 0.0f )]
		[TestCase( -11.0f, 10.0f, 9.0f )]
		public void Repeat_Float_RepeatsIntoZeroLengthRange( float value, float length, float expected ) {
			var actual = ScalarMath.Repeat( value, length );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, 0.0d )]
		[TestCase( 1.0d, 10.0d, 1.0d )]
		[TestCase( 9.0d, 10.0d, 9.0d )]
		[TestCase( 10.0d, 10.0d, 0.0d )]
		[TestCase( 11.0d, 10.0d, 1.0d )]
		[TestCase( 20.0d, 10.0d, 0.0d )]
		[TestCase( -1.0d, 10.0d, 9.0d )]
		[TestCase( -10.0d, 10.0d, 0.0d )]
		[TestCase( -11.0d, 10.0d, 9.0d )]
		public void Repeat_Double_RepeatsIntoZeroLengthRange( double value, double length, double expected ) {
			var actual = ScalarMath.Repeat( value, length );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Repeat_Float_WhenLengthIsNotPositive_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Repeat( 1.0f, 0.0f ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Repeat( 1.0f, -1.0f ) );
			} );
		}

		[Test]
		public void Repeat_Double_WhenLengthIsNotPositive_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Repeat( 1.0d, 0.0d ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.Repeat( 1.0d, -1.0d ) );
			} );
		}

		/*
		================================================================================
		PingPong
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, 0.0f )]
		[TestCase( 5.0f, 10.0f, 5.0f )]
		[TestCase( 10.0f, 10.0f, 10.0f )]
		[TestCase( 15.0f, 10.0f, 5.0f )]
		[TestCase( 20.0f, 10.0f, 0.0f )]
		[TestCase( 25.0f, 10.0f, 5.0f )]
		[TestCase( -5.0f, 10.0f, 5.0f )]
		[TestCase( -10.0f, 10.0f, 10.0f )]
		[TestCase( -15.0f, 10.0f, 5.0f )]
		public void PingPong_Float_OscillatesBetweenZeroAndLength( float value, float length, float expected ) {
			var actual = ScalarMath.PingPong( value, length );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, 0.0d )]
		[TestCase( 5.0d, 10.0d, 5.0d )]
		[TestCase( 10.0d, 10.0d, 10.0d )]
		[TestCase( 15.0d, 10.0d, 5.0d )]
		[TestCase( 20.0d, 10.0d, 0.0d )]
		[TestCase( 25.0d, 10.0d, 5.0d )]
		[TestCase( -5.0d, 10.0d, 5.0d )]
		[TestCase( -10.0d, 10.0d, 10.0d )]
		[TestCase( -15.0d, 10.0d, 5.0d )]
		public void PingPong_Double_OscillatesBetweenZeroAndLength( double value, double length, double expected ) {
			var actual = ScalarMath.PingPong( value, length );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void PingPong_Float_WhenLengthIsNotPositive_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.PingPong( 1.0f, 0.0f ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.PingPong( 1.0f, -1.0f ) );
			} );
		}

		[Test]
		public void PingPong_Double_WhenLengthIsNotPositive_ThrowsArgumentOutOfRangeException() {
			Assert.Multiple( () => {
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.PingPong( 1.0d, 0.0d ) );
				Assert.Throws<ArgumentOutOfRangeException>( () => ScalarMath.PingPong( 1.0d, -1.0d ) );
			} );
		}

		/*
		================================================================================
		Step
		================================================================================
		*/

		[TestCase( 10.0f, 9.999f, 0.0f )]
		[TestCase( 10.0f, 10.0f, 1.0f )]
		[TestCase( 10.0f, 10.001f, 1.0f )]
		public void Step_Float_ReturnsZeroBelowEdge_OneAtOrAboveEdge( float edge, float value, float expected ) {
			var actual = ScalarMath.Step( edge, value );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 10.0d, 9.999d, 0.0d )]
		[TestCase( 10.0d, 10.0d, 1.0d )]
		[TestCase( 10.0d, 10.001d, 1.0d )]
		public void Step_Double_ReturnsZeroBelowEdge_OneAtOrAboveEdge( double edge, double value, double expected ) {
			var actual = ScalarMath.Step( edge, value );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Snap
		================================================================================
		*/

		[TestCase( 1.24f, 0.5f, 1.0f )]
		[TestCase( 1.26f, 0.5f, 1.5f )]
		[TestCase( -1.24f, 0.5f, -1.0f )]
		[TestCase( -1.26f, 0.5f, -1.5f )]
		[TestCase( 7.4f, 2.0f, 8.0f )]
		[TestCase( 7.4f, 0.0f, 7.4f )]
		[TestCase( 7.4f, -1.0f, 7.4f )]
		public void Snap_Float_SnapsToNearestIncrement_OrReturnsValueWhenIncrementIsNotPositive(
			float value,
			float increment,
			float expected ) {
			var actual = ScalarMath.Snap( value, increment );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 1.24d, 0.5d, 1.0d )]
		[TestCase( 1.26d, 0.5d, 1.5d )]
		[TestCase( -1.24d, 0.5d, -1.0d )]
		[TestCase( -1.26d, 0.5d, -1.5d )]
		[TestCase( 7.4d, 2.0d, 8.0d )]
		[TestCase( 7.4d, 0.0d, 7.4d )]
		[TestCase( 7.4d, -1.0d, 7.4d )]
		public void Snap_Double_SnapsToNearestIncrement_OrReturnsValueWhenIncrementIsNotPositive(
			double value,
			double increment,
			double expected ) {
			var actual = ScalarMath.Snap( value, increment );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		CeilDiv
		================================================================================
		*/

		[TestCase( 0, 3, 0 )]
		[TestCase( 1, 3, 1 )]
		[TestCase( 2, 3, 1 )]
		[TestCase( 3, 3, 1 )]
		[TestCase( 4, 3, 2 )]
		[TestCase( 9, 3, 3 )]
		[TestCase( 10, 3, 4 )]
		[TestCase( 11, 3, 4 )]
		[TestCase( 12, 3, 4 )]
		public void CeilDiv_Int_ForPositiveNumeratorsAndDenominators_ReturnsCeilingQuotient(
			int numerator,
			int denominator,
			int expected ) {
			var actual = ScalarMath.CeilDiv( numerator, denominator );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		[TestCase( 0u, 3u, 0u )]
		[TestCase( 1u, 3u, 1u )]
		[TestCase( 2u, 3u, 1u )]
		[TestCase( 3u, 3u, 1u )]
		[TestCase( 4u, 3u, 2u )]
		[TestCase( 9u, 3u, 3u )]
		[TestCase( 10u, 3u, 4u )]
		[TestCase( 11u, 3u, 4u )]
		[TestCase( 12u, 3u, 4u )]
		public void CeilDiv_UInt_ReturnsCeilingQuotient( uint numerator, uint denominator, uint expected ) {
			var actual = ScalarMath.CeilDiv( numerator, denominator );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		[Test]
		public void CeilDiv_Int_WhenDenominatorIsZero_ThrowsDivideByZeroException() {
			Assert.Throws<DivideByZeroException>( () => ScalarMath.CeilDiv( 1, 0 ) );
		}

		[Test]
		public void CeilDiv_UInt_WhenDenominatorIsZero_ThrowsDivideByZeroException() {
			Assert.Throws<DivideByZeroException>( () => ScalarMath.CeilDiv( 1u, 0u ) );
		}
	}
}