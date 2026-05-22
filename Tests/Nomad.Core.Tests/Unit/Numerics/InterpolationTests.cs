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
using NUnit.Framework;

namespace Nomad.Core.Numerics.Tests {
	[TestFixture]
	[Category( "Nomad.Core" )]
    [Category( "Numerics" )]
    [Category( "Unit" )]
	public sealed class InterpolationTests {
		private const float FloatTolerance = 0.000001f;
		private const double DoubleTolerance = 0.000000000001d;

		/*
		================================================================================
		InverseLerp
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 10.0f, 2.5f, 0.25f )]
		[TestCase( 0.0f, 10.0f, 5.0f, 0.5f )]
		[TestCase( 0.0f, 10.0f, 10.0f, 1.0f )]
		[TestCase( 0.0f, 10.0f, -5.0f, -0.5f )]
		[TestCase( 0.0f, 10.0f, 15.0f, 1.5f )]
		[TestCase( 10.0f, 0.0f, 5.0f, 0.5f )]
		[TestCase( 10.0f, 0.0f, 15.0f, -0.5f )]
		public void InverseLerp_Float_ReturnsUnclampedNormalizedPosition(
			float a,
			float b,
			float value,
			float expected ) {
			var actual = Interpolation.InverseLerp( a, b, value );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 10.0d, 2.5d, 0.25d )]
		[TestCase( 0.0d, 10.0d, 5.0d, 0.5d )]
		[TestCase( 0.0d, 10.0d, 10.0d, 1.0d )]
		[TestCase( 0.0d, 10.0d, -5.0d, -0.5d )]
		[TestCase( 0.0d, 10.0d, 15.0d, 1.5d )]
		[TestCase( 10.0d, 0.0d, 5.0d, 0.5d )]
		[TestCase( 10.0d, 0.0d, 15.0d, -0.5d )]
		public void InverseLerp_Double_ReturnsUnclampedNormalizedPosition(
			double a,
			double b,
			double value,
			double expected ) {
			var actual = Interpolation.InverseLerp( a, b, value );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void InverseLerp_Float_WhenEndpointsAreNearlyEqual_ReturnsZero() {
			var actual = Interpolation.InverseLerp( 1.0f, 1.0f + 0.0000005f, 100.0f );

			Assert.That( actual, Is.EqualTo( 0.0f ).Within( FloatTolerance ) );
		}

		[Test]
		public void InverseLerp_Double_WhenEndpointsAreNearlyEqual_ReturnsZero() {
			var actual = Interpolation.InverseLerp( 1.0d, 1.0d + 0.0000000000005d, 100.0d );

			Assert.That( actual, Is.EqualTo( 0.0d ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Remap
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, 0.0f, 100.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 100.0f, 2.5f, 25.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 100.0f, 5.0f, 50.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 100.0f, 10.0f, 100.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 100.0f, -5.0f, -50.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 100.0f, 15.0f, 150.0f )]
		[TestCase( 10.0f, 0.0f, 0.0f, 100.0f, 5.0f, 50.0f )]
		[TestCase( 0.0f, 10.0f, 100.0f, 0.0f, 2.5f, 75.0f )]
		public void Remap_Float_MapsValueBetweenRangesWithoutClamping(
			float inMin,
			float inMax,
			float outMin,
			float outMax,
			float value,
			float expected ) {
			var actual = Interpolation.Remap( inMin, inMax, outMin, outMax, value );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, 0.0d, 100.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 100.0d, 2.5d, 25.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 100.0d, 5.0d, 50.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 100.0d, 10.0d, 100.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 100.0d, -5.0d, -50.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 100.0d, 15.0d, 150.0d )]
		[TestCase( 10.0d, 0.0d, 0.0d, 100.0d, 5.0d, 50.0d )]
		[TestCase( 0.0d, 10.0d, 100.0d, 0.0d, 2.5d, 75.0d )]
		public void Remap_Double_MapsValueBetweenRangesWithoutClamping(
			double inMin,
			double inMax,
			double outMin,
			double outMax,
			double value,
			double expected ) {
			var actual = Interpolation.Remap( inMin, inMax, outMin, outMax, value );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Remap_Float_WhenInputRangeIsNearlyZero_UsesInverseLerpZero() {
			var actual = Interpolation.Remap( 1.0f, 1.0f + 0.0000005f, 50.0f, 100.0f, 999.0f );

			Assert.That( actual, Is.EqualTo( 50.0f ).Within( FloatTolerance ) );
		}

		[Test]
		public void Remap_Double_WhenInputRangeIsNearlyZero_UsesInverseLerpZero() {
			var actual = Interpolation.Remap( 1.0d, 1.0d + 0.0000000000005d, 50.0d, 100.0d, 999.0d );

			Assert.That( actual, Is.EqualTo( 50.0d ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Lerp
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, -1.0f, 0.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 10.0f, 0.25f, 2.5f )]
		[TestCase( 0.0f, 10.0f, 0.5f, 5.0f )]
		[TestCase( 0.0f, 10.0f, 1.0f, 10.0f )]
		[TestCase( 0.0f, 10.0f, 2.0f, 10.0f )]
		[TestCase( 10.0f, 0.0f, 0.25f, 7.5f )]
		public void Lerp_Float_InterpolatesWithClampedT( float a, float b, float t, float expected ) {
			var actual = Interpolation.Lerp( a, b, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, -1.0d, 0.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 10.0d, 0.25d, 2.5d )]
		[TestCase( 0.0d, 10.0d, 0.5d, 5.0d )]
		[TestCase( 0.0d, 10.0d, 1.0d, 10.0d )]
		[TestCase( 0.0d, 10.0d, 2.0d, 10.0d )]
		[TestCase( 10.0d, 0.0d, 0.25d, 7.5d )]
		public void Lerp_Double_InterpolatesWithClampedT( double a, double b, double t, double expected ) {
			var actual = Interpolation.Lerp( a, b, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		LerpUnclamped
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, -1.0f, -10.0f )]
		[TestCase( 0.0f, 10.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 10.0f, 0.25f, 2.5f )]
		[TestCase( 0.0f, 10.0f, 0.5f, 5.0f )]
		[TestCase( 0.0f, 10.0f, 1.0f, 10.0f )]
		[TestCase( 0.0f, 10.0f, 1.5f, 15.0f )]
		[TestCase( 10.0f, 0.0f, 1.5f, -5.0f )]
		public void LerpUnclamped_Float_InterpolatesWithoutClampingT( float a, float b, float t, float expected ) {
			var actual = Interpolation.LerpUnclamped( a, b, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, -1.0d, -10.0d )]
		[TestCase( 0.0d, 10.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 10.0d, 0.25d, 2.5d )]
		[TestCase( 0.0d, 10.0d, 0.5d, 5.0d )]
		[TestCase( 0.0d, 10.0d, 1.0d, 10.0d )]
		[TestCase( 0.0d, 10.0d, 1.5d, 15.0d )]
		[TestCase( 10.0d, 0.0d, 1.5d, -5.0d )]
		public void LerpUnclamped_Double_InterpolatesWithoutClampingT( double a, double b, double t, double expected ) {
			var actual = Interpolation.LerpUnclamped( a, b, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Damp
		================================================================================
		*/

		[Test]
		public void Damp_WhenDeltaTimeIsZero_ReturnsCurrent() {
			var actual = Interpolation.Damp( 10.0d, 100.0d, 5.0d, 0.0d );

			Assert.That( actual, Is.EqualTo( 10.0d ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Damp_WhenSmoothingIsZero_ReturnsCurrent() {
			var actual = Interpolation.Damp( 10.0d, 100.0d, 0.0d, 1.0d );

			Assert.That( actual, Is.EqualTo( 10.0d ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Damp_UsesExponentialSmoothingFactor() {
			const double current = 0.0d;
			const double target = 10.0d;
			const double smoothing = 1.0d;
			const double deltaTime = 1.0d;

			var expected = target * ( 1.0d - Math.Exp( -smoothing * deltaTime ) );

			var actual = Interpolation.Damp( current, target, smoothing, deltaTime );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Damp_WithLargeSmoothingAndDeltaTime_MovesCloseToTarget() {
			var actual = Interpolation.Damp( 0.0d, 10.0d, 10.0d, 1.0d );

			Assert.That( actual, Is.EqualTo( 10.0d ).Within( 0.001d ) );
		}

		/*
		================================================================================
		CubicBezier
		================================================================================
		*/

		[TestCase( 0.0f, 1.0f, 2.0f, 3.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 1.0f, 2.0f, 3.0f, 1.0f, 3.0f )]
		[TestCase( 0.0f, 0.0f, 10.0f, 10.0f, 0.5f, 5.0f )]
		[TestCase( 5.0f, 5.0f, 5.0f, 5.0f, 0.0f, 5.0f )]
		[TestCase( 5.0f, 5.0f, 5.0f, 5.0f, 0.5f, 5.0f )]
		[TestCase( 5.0f, 5.0f, 5.0f, 5.0f, 1.0f, 5.0f )]
		public void CubicBezier_Float_EvaluatesCubicBezierCurve(
			float p0,
			float p1,
			float p2,
			float p3,
			float t,
			float expected ) {
			var actual = Interpolation.CubicBezier( p0, p1, p2, p3, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 1.0d, 2.0d, 3.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 1.0d, 2.0d, 3.0d, 1.0d, 3.0d )]
		[TestCase( 0.0d, 0.0d, 10.0d, 10.0d, 0.5d, 5.0d )]
		[TestCase( 5.0d, 5.0d, 5.0d, 5.0d, 0.0d, 5.0d )]
		[TestCase( 5.0d, 5.0d, 5.0d, 5.0d, 0.5d, 5.0d )]
		[TestCase( 5.0d, 5.0d, 5.0d, 5.0d, 1.0d, 5.0d )]
		public void CubicBezier_Double_EvaluatesCubicBezierCurve(
			double p0,
			double p1,
			double p2,
			double p3,
			double t,
			double expected ) {
			var actual = Interpolation.CubicBezier( p0, p1, p2, p3, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void CubicBezier_Float_AllowsUnclampedT() {
			var actual = Interpolation.CubicBezier( 0.0f, 1.0f, 2.0f, 3.0f, 2.0f );

			Assert.That( actual, Is.EqualTo( 6.0f ).Within( FloatTolerance ) );
		}

		[Test]
		public void CubicBezier_Double_AllowsUnclampedT() {
			var actual = Interpolation.CubicBezier( 0.0d, 1.0d, 2.0d, 3.0d, 2.0d );

			Assert.That( actual, Is.EqualTo( 6.0d ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Hermite
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f, 10.0f, 0.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 0.0f, 10.0f, 0.0f, 1.0f, 10.0f )]
		[TestCase( 0.0f, 0.0f, 10.0f, 0.0f, 0.5f, 5.0f )]
		[TestCase( 0.0f, 10.0f, 10.0f, 10.0f, 0.5f, 5.0f )]
		[TestCase( 5.0f, 0.0f, 5.0f, 0.0f, 0.5f, 5.0f )]
		public void Hermite_Float_EvaluatesHermiteCurve(
			float p0,
			float m0,
			float p1,
			float m1,
			float t,
			float expected ) {
			var actual = Interpolation.Hermite( p0, m0, p1, m1, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d, 10.0d, 0.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 0.0d, 10.0d, 0.0d, 1.0d, 10.0d )]
		[TestCase( 0.0d, 0.0d, 10.0d, 0.0d, 0.5d, 5.0d )]
		[TestCase( 0.0d, 10.0d, 10.0d, 10.0d, 0.5d, 5.0d )]
		[TestCase( 5.0d, 0.0d, 5.0d, 0.0d, 0.5d, 5.0d )]
		public void Hermite_Double_EvaluatesHermiteCurve(
			double p0,
			double m0,
			double p1,
			double m1,
			double t,
			double expected ) {
			var actual = Interpolation.Hermite( p0, m0, p1, m1, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void Hermite_Float_AllowsUnclampedT() {
			var actual = Interpolation.Hermite( 0.0f, 0.0f, 10.0f, 0.0f, 2.0f );

			Assert.That( actual, Is.EqualTo( -40.0f ).Within( FloatTolerance ) );
		}

		[Test]
		public void Hermite_Double_AllowsUnclampedT() {
			var actual = Interpolation.Hermite( 0.0d, 0.0d, 10.0d, 0.0d, 2.0d );

			Assert.That( actual, Is.EqualTo( -40.0d ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		CatmullRom
		================================================================================
		*/

		[TestCase( 0.0f, 10.0f, 20.0f, 30.0f, 0.0f, 10.0f )]
		[TestCase( 0.0f, 10.0f, 20.0f, 30.0f, 0.5f, 15.0f )]
		[TestCase( 0.0f, 10.0f, 20.0f, 30.0f, 1.0f, 20.0f )]
		[TestCase( 0.0f, 0.0f, 10.0f, 10.0f, 0.5f, 5.0f )]
		[TestCase( 5.0f, 5.0f, 5.0f, 5.0f, 0.0f, 5.0f )]
		[TestCase( 5.0f, 5.0f, 5.0f, 5.0f, 0.5f, 5.0f )]
		[TestCase( 5.0f, 5.0f, 5.0f, 5.0f, 1.0f, 5.0f )]
		public void CatmullRom_Float_EvaluatesCatmullRomSpline(
			float p0,
			float p1,
			float p2,
			float p3,
			float t,
			float expected ) {
			var actual = Interpolation.CatmullRom( p0, p1, p2, p3, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 10.0d, 20.0d, 30.0d, 0.0d, 10.0d )]
		[TestCase( 0.0d, 10.0d, 20.0d, 30.0d, 0.5d, 15.0d )]
		[TestCase( 0.0d, 10.0d, 20.0d, 30.0d, 1.0d, 20.0d )]
		[TestCase( 0.0d, 0.0d, 10.0d, 10.0d, 0.5d, 5.0d )]
		[TestCase( 5.0d, 5.0d, 5.0d, 5.0d, 0.0d, 5.0d )]
		[TestCase( 5.0d, 5.0d, 5.0d, 5.0d, 0.5d, 5.0d )]
		[TestCase( 5.0d, 5.0d, 5.0d, 5.0d, 1.0d, 5.0d )]
		public void CatmullRom_Double_EvaluatesCatmullRomSpline(
			double p0,
			double p1,
			double p2,
			double p3,
			double t,
			double expected ) {
			var actual = Interpolation.CatmullRom( p0, p1, p2, p3, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[Test]
		public void CatmullRom_Float_AllowsUnclampedT() {
			var actual = Interpolation.CatmullRom( 0.0f, 10.0f, 20.0f, 30.0f, 2.0f );

			Assert.That( actual, Is.EqualTo( 30.0f ).Within( FloatTolerance ) );
		}

		[Test]
		public void CatmullRom_Double_AllowsUnclampedT() {
			var actual = Interpolation.CatmullRom( 0.0d, 10.0d, 20.0d, 30.0d, 2.0d );

			Assert.That( actual, Is.EqualTo( 30.0d ).Within( DoubleTolerance ) );
		}
	}
}