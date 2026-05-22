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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nomad.Core.Numerics;
using NUnit.Framework;

namespace Nomad.Core.Numerics.Tests {
	[TestFixture]
	[Category( "Nomad.Core" )]
    [Category( "Numerics" )]
    [Category( "Unit" )]
	public sealed class AngleMathTests {
		private const float FloatTolerance = 0.0001f;
		private const double DoubleTolerance = 0.0000000001d;

		private const double Tau = Math.PI * 2.0d;

		/*
		================================================================================
		Conversion tests
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f )]
		[TestCase( 45.0f, 0.7853982f )]
		[TestCase( 90.0f, 1.5707964f )]
		[TestCase( 180.0f, 3.1415927f )]
		[TestCase( 270.0f, 4.7123890f )]
		[TestCase( 360.0f, 6.2831855f )]
		[TestCase( -90.0f, -1.5707964f )]
		public void ToRadians_Float_ConvertsDegreesToRadians( float degrees, float expectedRadians ) {
			var actual = AngleMath.ToRadians( degrees );

			Assert.That( actual, Is.EqualTo( expectedRadians ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d )]
		[TestCase( 45.0d, Math.PI / 4.0d )]
		[TestCase( 90.0d, Math.PI / 2.0d )]
		[TestCase( 180.0d, Math.PI )]
		[TestCase( 270.0d, Math.PI * 1.5d )]
		[TestCase( 360.0d, Math.PI * 2.0d )]
		[TestCase( -90.0d, -Math.PI / 2.0d )]
		public void ToRadians_Double_ConvertsDegreesToRadians( double degrees, double expectedRadians ) {
			var actual = AngleMath.ToRadians( degrees );

			Assert.That( actual, Is.EqualTo( expectedRadians ).Within( DoubleTolerance ) );
		}

		[TestCase( 0.0f, 0.0f )]
		[TestCase( 0.7853982f, 45.0f )]
		[TestCase( 1.5707964f, 90.0f )]
		[TestCase( 3.1415927f, 180.0f )]
		[TestCase( 4.7123890f, 270.0f )]
		[TestCase( 6.2831855f, 360.0f )]
		[TestCase( -1.5707964f, -90.0f )]
		public void ToDegrees_Float_ConvertsRadiansToDegrees( float radians, float expectedDegrees ) {
			var actual = AngleMath.ToDegrees( radians );

			Assert.That( actual, Is.EqualTo( expectedDegrees ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d )]
		[TestCase( Math.PI / 4.0d, 45.0d )]
		[TestCase( Math.PI / 2.0d, 90.0d )]
		[TestCase( Math.PI, 180.0d )]
		[TestCase( Math.PI * 1.5d, 270.0d )]
		[TestCase( Math.PI * 2.0d, 360.0d )]
		[TestCase( -Math.PI / 2.0d, -90.0d )]
		public void ToDegrees_Double_ConvertsRadiansToDegrees( double radians, double expectedDegrees ) {
			var actual = AngleMath.ToDegrees( radians );

			Assert.That( actual, Is.EqualTo( expectedDegrees ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( DegreeRoundTripCases ) )]
		public void DegreeRadianRoundTrip_Float_ReturnsOriginalValue( float degrees ) {
			var actual = AngleMath.ToDegrees( AngleMath.ToRadians( degrees ) );

			Assert.That( actual, Is.EqualTo( degrees ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( DoubleDegreeRoundTripCases ) )]
		public void DegreeRadianRoundTrip_Double_ReturnsOriginalValue( double degrees ) {
			var actual = AngleMath.ToDegrees( AngleMath.ToRadians( degrees ) );

			Assert.That( actual, Is.EqualTo( degrees ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Unsigned degree normalization tests
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f )]
		[TestCase( 1.0f, 1.0f )]
		[TestCase( 359.0f, 359.0f )]
		[TestCase( 360.0f, 0.0f )]
		[TestCase( 361.0f, 1.0f )]
		[TestCase( 720.0f, 0.0f )]
		[TestCase( 721.0f, 1.0f )]
		[TestCase( -1.0f, 359.0f )]
		[TestCase( -90.0f, 270.0f )]
		[TestCase( -360.0f, 0.0f )]
		[TestCase( -361.0f, 359.0f )]
		public void NormalizeDegrees_Float_WrapsToZeroInclusiveThreeSixtyExclusive( float degrees, float expected ) {
			var actual = AngleMath.NormalizeDegrees( degrees );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d )]
		[TestCase( 1.0d, 1.0d )]
		[TestCase( 359.0d, 359.0d )]
		[TestCase( 360.0d, 0.0d )]
		[TestCase( 361.0d, 1.0d )]
		[TestCase( 720.0d, 0.0d )]
		[TestCase( 721.0d, 1.0d )]
		[TestCase( -1.0d, 359.0d )]
		[TestCase( -90.0d, 270.0d )]
		[TestCase( -360.0d, 0.0d )]
		[TestCase( -361.0d, 359.0d )]
		public void NormalizeDegrees_Double_WrapsToZeroInclusiveThreeSixtyExclusive( double degrees, double expected ) {
			var actual = AngleMath.NormalizeDegrees( degrees );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( DegreeRangeCases ) )]
		public void NormalizeDegrees_Float_ResultIsAlwaysInUnsignedDegreeRange( float degrees ) {
			var actual = AngleMath.NormalizeDegrees( degrees );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( 0.0f ) );
				Assert.That( actual, Is.LessThan( 360.0f ) );
			} );
		}

		[TestCaseSource( nameof( DoubleDegreeRangeCases ) )]
		public void NormalizeDegrees_Double_ResultIsAlwaysInUnsignedDegreeRange( double degrees ) {
			var actual = AngleMath.NormalizeDegrees( degrees );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( 0.0d ) );
				Assert.That( actual, Is.LessThan( 360.0d ) );
			} );
		}

		/*
		================================================================================
		Signed degree normalization tests
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f )]
		[TestCase( 1.0f, 1.0f )]
		[TestCase( 179.0f, 179.0f )]
		[TestCase( 180.0f, -180.0f )]
		[TestCase( 181.0f, -179.0f )]
		[TestCase( 359.0f, -1.0f )]
		[TestCase( 360.0f, 0.0f )]
		[TestCase( 540.0f, -180.0f )]
		[TestCase( -1.0f, -1.0f )]
		[TestCase( -179.0f, -179.0f )]
		[TestCase( -180.0f, -180.0f )]
		[TestCase( -181.0f, 179.0f )]
		[TestCase( -359.0f, 1.0f )]
		[TestCase( -360.0f, 0.0f )]
		public void NormalizeDegreesSigned_Float_WrapsToMinusOneEightyInclusiveOneEightyExclusive(
			float degrees,
			float expected ) {
			var actual = AngleMath.NormalizeDegreesSigned( degrees );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d )]
		[TestCase( 1.0d, 1.0d )]
		[TestCase( 179.0d, 179.0d )]
		[TestCase( 180.0d, -180.0d )]
		[TestCase( 181.0d, -179.0d )]
		[TestCase( 359.0d, -1.0d )]
		[TestCase( 360.0d, 0.0d )]
		[TestCase( 540.0d, -180.0d )]
		[TestCase( -1.0d, -1.0d )]
		[TestCase( -179.0d, -179.0d )]
		[TestCase( -180.0d, -180.0d )]
		[TestCase( -181.0d, 179.0d )]
		[TestCase( -359.0d, 1.0d )]
		[TestCase( -360.0d, 0.0d )]
		public void NormalizeDegreesSigned_Double_WrapsToMinusOneEightyInclusiveOneEightyExclusive(
			double degrees,
			double expected ) {
			var actual = AngleMath.NormalizeDegreesSigned( degrees );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( DegreeRangeCases ) )]
		public void NormalizeDegreesSigned_Float_ResultIsAlwaysInSignedDegreeRange( float degrees ) {
			var actual = AngleMath.NormalizeDegreesSigned( degrees );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( -180.0f ) );
				Assert.That( actual, Is.LessThan( 180.0f ) );
			} );
		}

		[TestCaseSource( nameof( DoubleDegreeRangeCases ) )]
		public void NormalizeDegreesSigned_Double_ResultIsAlwaysInSignedDegreeRange( double degrees ) {
			var actual = AngleMath.NormalizeDegreesSigned( degrees );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( -180.0d ) );
				Assert.That( actual, Is.LessThan( 180.0d ) );
			} );
		}

		/*
		================================================================================
		Unsigned radian normalization tests
		================================================================================
		*/

		[TestCaseSource( nameof( UnsignedRadianFloatCases ) )]
		public void NormalizeRadians_Float_WrapsToZeroInclusiveTauExclusive( float radians, float expected ) {
			var actual = AngleMath.NormalizeRadians( radians );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( UnsignedRadianDoubleCases ) )]
		public void NormalizeRadians_Double_WrapsToZeroInclusiveTauExclusive( double radians, double expected ) {
			var actual = AngleMath.NormalizeRadians( radians );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( RadianRangeCases ) )]
		public void NormalizeRadians_Float_ResultIsAlwaysInUnsignedRadianRange( float radians ) {
			var actual = AngleMath.NormalizeRadians( radians );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( 0.0f ) );
				Assert.That( actual, Is.LessThan( MathF.PI * 2.0f ) );
			} );
		}

		[TestCaseSource( nameof( DoubleRadianRangeCases ) )]
		public void NormalizeRadians_Double_ResultIsAlwaysInUnsignedRadianRange( double radians ) {
			var actual = AngleMath.NormalizeRadians( radians );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( 0.0d ) );
				Assert.That( actual, Is.LessThan( Tau ) );
			} );
		}

		/*
		================================================================================
		Signed radian normalization tests
		================================================================================
		*/

		[TestCaseSource( nameof( SignedRadianFloatCases ) )]
		public void NormalizeRadiansSigned_Float_WrapsToMinusPiInclusivePiExclusive( float radians, float expected ) {
			var actual = AngleMath.NormalizeRadiansSigned( radians );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( SignedRadianDoubleCases ) )]
		public void NormalizeRadiansSigned_Double_WrapsToMinusPiInclusivePiExclusive( double radians, double expected ) {
			var actual = AngleMath.NormalizeRadiansSigned( radians );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( RadianRangeCases ) )]
		public void NormalizeRadiansSigned_Float_ResultIsAlwaysInSignedRadianRange( float radians ) {
			var actual = AngleMath.NormalizeRadiansSigned( radians );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( -MathF.PI ) );
				Assert.That( actual, Is.LessThan( MathF.PI ) );
			} );
		}

		[TestCaseSource( nameof( DoubleRadianRangeCases ) )]
		public void NormalizeRadiansSigned_Double_ResultIsAlwaysInSignedRadianRange( double radians ) {
			var actual = AngleMath.NormalizeRadiansSigned( radians );

			Assert.Multiple( () => {
				Assert.That( actual, Is.GreaterThanOrEqualTo( -Math.PI ) );
				Assert.That( actual, Is.LessThan( Math.PI ) );
			} );
		}

		/*
		================================================================================
		Delta tests
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 90.0f, 90.0f )]
		[TestCase( 90.0f, 0.0f, -90.0f )]
		[TestCase( 350.0f, 10.0f, 20.0f )]
		[TestCase( 10.0f, 350.0f, -20.0f )]
		[TestCase( -170.0f, 170.0f, -20.0f )]
		[TestCase( 170.0f, -170.0f, 20.0f )]
		[TestCase( 0.0f, 180.0f, -180.0f )]
		[TestCase( 180.0f, 0.0f, -180.0f )]
		public void DeltaDegrees_Float_ReturnsShortestSignedDelta( float from, float to, float expected ) {
			var actual = AngleMath.DeltaDegrees( from, to );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 90.0d, 90.0d )]
		[TestCase( 90.0d, 0.0d, -90.0d )]
		[TestCase( 350.0d, 10.0d, 20.0d )]
		[TestCase( 10.0d, 350.0d, -20.0d )]
		[TestCase( -170.0d, 170.0d, -20.0d )]
		[TestCase( 170.0d, -170.0d, 20.0d )]
		[TestCase( 0.0d, 180.0d, -180.0d )]
		[TestCase( 180.0d, 0.0d, -180.0d )]
		public void DeltaDegrees_Double_ReturnsShortestSignedDelta( double from, double to, double expected ) {
			var actual = AngleMath.DeltaDegrees( from, to );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( DeltaRadiansFloatCases ) )]
		public void DeltaRadians_Float_ReturnsShortestSignedDelta( float from, float to, float expected ) {
			var actual = AngleMath.DeltaRadians( from, to );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( DeltaRadiansDoubleCases ) )]
		public void DeltaRadians_Double_ReturnsShortestSignedDelta( double from, double to, double expected ) {
			var actual = AngleMath.DeltaRadians( from, to );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Lerp tests
		================================================================================
		*/

		[TestCase( 10.0f, 40.0f, 0.0f, 10.0f )]
		[TestCase( 10.0f, 40.0f, 0.5f, 25.0f )]
		[TestCase( 10.0f, 40.0f, 1.0f, 40.0f )]
		[TestCase( 10.0f, 40.0f, -1.0f, 10.0f )]
		[TestCase( 10.0f, 40.0f, 2.0f, 40.0f )]
		[TestCase( 350.0f, 10.0f, 0.0f, 350.0f )]
		[TestCase( 350.0f, 10.0f, 0.5f, 360.0f )]
		[TestCase( 350.0f, 10.0f, 1.0f, 370.0f )]
		[TestCase( 10.0f, 350.0f, 0.5f, 0.0f )]
		[TestCase( 10.0f, 350.0f, 1.0f, -10.0f )]
		public void LerpDegrees_Float_InterpolatesAlongShortestPath_AndClampsT(
			float from,
			float to,
			float t,
			float expected ) {
			var actual = AngleMath.LerpDegrees( from, to, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 10.0d, 40.0d, 0.0d, 10.0d )]
		[TestCase( 10.0d, 40.0d, 0.5d, 25.0d )]
		[TestCase( 10.0d, 40.0d, 1.0d, 40.0d )]
		[TestCase( 10.0d, 40.0d, -1.0d, 10.0d )]
		[TestCase( 10.0d, 40.0d, 2.0d, 40.0d )]
		[TestCase( 350.0d, 10.0d, 0.0d, 350.0d )]
		[TestCase( 350.0d, 10.0d, 0.5d, 360.0d )]
		[TestCase( 350.0d, 10.0d, 1.0d, 370.0d )]
		[TestCase( 10.0d, 350.0d, 0.5d, 0.0d )]
		[TestCase( 10.0d, 350.0d, 1.0d, -10.0d )]
		public void LerpDegrees_Double_InterpolatesAlongShortestPath_AndClampsT(
			double from,
			double to,
			double t,
			double expected ) {
			var actual = AngleMath.LerpDegrees( from, to, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( LerpRadiansFloatCases ) )]
		public void LerpRadians_Float_InterpolatesAlongShortestPath_AndClampsT(
			float from,
			float to,
			float t,
			float expected ) {
			var actual = AngleMath.LerpRadians( from, to, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( LerpRadiansDoubleCases ) )]
		public void LerpRadians_Double_InterpolatesAlongShortestPath_AndClampsT(
			double from,
			double to,
			double t,
			double expected ) {
			var actual = AngleMath.LerpRadians( from, to, t );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		MoveTowards tests
		================================================================================
		*/

		[TestCase( 10.0f, 30.0f, 5.0f, 15.0f )]
		[TestCase( 30.0f, 10.0f, 5.0f, 25.0f )]
		[TestCase( 10.0f, 30.0f, 20.0f, 30.0f )]
		[TestCase( 10.0f, 30.0f, 100.0f, 30.0f )]
		[TestCase( 350.0f, 10.0f, 5.0f, 355.0f )]
		[TestCase( 10.0f, 350.0f, 5.0f, 5.0f )]
		[TestCase( 0.0f, 180.0f, 10.0f, -10.0f )]
		public void MoveTowardsDegrees_Float_MovesByAtMostMaxDeltaAlongShortestPath(
			float current,
			float target,
			float maxDelta,
			float expected ) {
			var actual = AngleMath.MoveTowardsDegrees( current, target, maxDelta );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 10.0d, 30.0d, 5.0d, 15.0d )]
		[TestCase( 30.0d, 10.0d, 5.0d, 25.0d )]
		[TestCase( 10.0d, 30.0d, 20.0d, 30.0d )]
		[TestCase( 10.0d, 30.0d, 100.0d, 30.0d )]
		[TestCase( 350.0d, 10.0d, 5.0d, 355.0d )]
		[TestCase( 10.0d, 350.0d, 5.0d, 5.0d )]
		[TestCase( 0.0d, 180.0d, 10.0d, -10.0d )]
		public void MoveTowardsDegrees_Double_MovesByAtMostMaxDeltaAlongShortestPath(
			double current,
			double target,
			double maxDelta,
			double expected ) {
			var actual = AngleMath.MoveTowardsDegrees( current, target, maxDelta );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( MoveTowardsRadiansFloatCases ) )]
		public void MoveTowardsRadians_Float_MovesByAtMostMaxDeltaAlongShortestPath(
			float current,
			float target,
			float maxDelta,
			float expected ) {
			var actual = AngleMath.MoveTowardsRadians( current, target, maxDelta );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( MoveTowardsRadiansDoubleCases ) )]
		public void MoveTowardsRadians_Double_MovesByAtMostMaxDeltaAlongShortestPath(
			double current,
			double target,
			double maxDelta,
			double expected ) {
			var actual = AngleMath.MoveTowardsRadians( current, target, maxDelta );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		LookAt tests
		================================================================================
		*/

		[TestCase( 0.0f, 0.0f, 1.0f, 0.0f, 0.0f )]
		[TestCase( 0.0f, 0.0f, 0.0f, 1.0f, 90.0f )]
		[TestCase( 0.0f, 0.0f, -1.0f, 0.0f, 180.0f )]
		[TestCase( 0.0f, 0.0f, 0.0f, -1.0f, -90.0f )]
		[TestCase( 1.0f, 1.0f, 2.0f, 2.0f, 45.0f )]
		[TestCase( 1.0f, 1.0f, 0.0f, 2.0f, 135.0f )]
		[TestCase( 1.0f, 1.0f, 0.0f, 0.0f, -135.0f )]
		[TestCase( 1.0f, 1.0f, 2.0f, 0.0f, -45.0f )]
		public void LookAtDegrees_Float_ReturnsAtan2AngleInDegrees(
			float fromX,
			float fromY,
			float toX,
			float toY,
			float expected ) {
			var actual = AngleMath.LookAtDegrees( fromX, fromY, toX, toY );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCase( 0.0d, 0.0d, 1.0d, 0.0d, 0.0d )]
		[TestCase( 0.0d, 0.0d, 0.0d, 1.0d, 90.0d )]
		[TestCase( 0.0d, 0.0d, -1.0d, 0.0d, 180.0d )]
		[TestCase( 0.0d, 0.0d, 0.0d, -1.0d, -90.0d )]
		[TestCase( 1.0d, 1.0d, 2.0d, 2.0d, 45.0d )]
		[TestCase( 1.0d, 1.0d, 0.0d, 2.0d, 135.0d )]
		[TestCase( 1.0d, 1.0d, 0.0d, 0.0d, -135.0d )]
		[TestCase( 1.0d, 1.0d, 2.0d, 0.0d, -45.0d )]
		public void LookAtDegrees_Double_ReturnsAtan2AngleInDegrees(
			double fromX,
			double fromY,
			double toX,
			double toY,
			double expected ) {
			var actual = AngleMath.LookAtDegrees( fromX, fromY, toX, toY );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		[TestCaseSource( nameof( LookAtRadiansFloatCases ) )]
		public void LookAtRadians_Float_ReturnsAtan2AngleInRadians(
			float fromX,
			float fromY,
			float toX,
			float toY,
			float expected ) {
			var actual = AngleMath.LookAtRadians( fromX, fromY, toX, toY );

			Assert.That( actual, Is.EqualTo( expected ).Within( FloatTolerance ) );
		}

		[TestCaseSource( nameof( LookAtRadiansDoubleCases ) )]
		public void LookAtRadians_Double_ReturnsAtan2AngleInRadians(
			double fromX,
			double fromY,
			double toX,
			double toY,
			double expected ) {
			var actual = AngleMath.LookAtRadians( fromX, fromY, toX, toY );

			Assert.That( actual, Is.EqualTo( expected ).Within( DoubleTolerance ) );
		}

		/*
		================================================================================
		Quadrant tests
		================================================================================
		*/

		[TestCaseSource( nameof( GetQuadrantRadiansCases ) )]
		public void GetQuadrantRadians_ReturnsExpectedQuadrantOrZeroForAxis( float angle, int expected ) {
			var actual = AngleMath.GetQuadrantRadians( angle );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		[TestCase( 1.0f, 1.0f, 1 )]
		[TestCase( -1.0f, 1.0f, 2 )]
		[TestCase( -1.0f, -1.0f, 3 )]
		[TestCase( 1.0f, -1.0f, 4 )]
		[TestCase( 0.0f, 0.0f, 0 )]
		[TestCase( 1.0f, 0.0f, 0 )]
		[TestCase( 0.0f, 1.0f, 0 )]
		[TestCase( -1.0f, 0.0f, 0 )]
		[TestCase( 0.0f, -1.0f, 0 )]
		public void GetQuadrantFromVector_ReturnsExpectedQuadrantOrZeroForAxis( float x, float y, int expected ) {
			var actual = AngleMath.GetQuadrantFromVector( x, y );

			Assert.That( actual, Is.EqualTo( expected ) );
		}

		/*
		================================================================================
		Optional API metadata guard
		================================================================================
		*/

		[Test]
		public void PublicStaticApi_HasExpectedMethodNames() {
			var expectedNames = new[]
			{
			nameof(AngleMath.ToRadians),
			nameof(AngleMath.ToDegrees),
			nameof(AngleMath.NormalizeDegrees),
			nameof(AngleMath.NormalizeDegreesSigned),
			nameof(AngleMath.NormalizeRadians),
			nameof(AngleMath.NormalizeRadiansSigned),
			nameof(AngleMath.DeltaDegrees),
			nameof(AngleMath.DeltaRadians),
			nameof(AngleMath.LerpDegrees),
			nameof(AngleMath.LerpRadians),
			nameof(AngleMath.MoveTowardsDegrees),
			nameof(AngleMath.MoveTowardsRadians),
			nameof(AngleMath.LookAtDegrees),
			nameof(AngleMath.LookAtRadians),
			nameof(AngleMath.GetQuadrantRadians),
			nameof(AngleMath.GetQuadrantFromVector)
		};

			var actualNames = typeof( AngleMath )
				.GetMethods( BindingFlags.Public | BindingFlags.Static )
				.Where( method => !method.IsSpecialName )
				.Select( method => method.Name )
				.Distinct()
				.OrderBy( name => name )
				.ToArray();

			Assert.That( actualNames, Is.EqualTo( expectedNames.OrderBy( name => name ) ) );
		}

		[Test]
		public void PerformanceCriticalMethods_AreMarkedAggressiveInlining() {
			var methodsExpectedToBeInlined = typeof( AngleMath )
				.GetMethods( BindingFlags.Public | BindingFlags.Static )
				.Where( method => !method.IsSpecialName )
				.Where( method =>
					method.Name is not nameof( AngleMath.GetQuadrantRadians )
					&& method.Name is not nameof( AngleMath.GetQuadrantFromVector ) )
				.ToArray();

			Assert.Multiple( () => {
				foreach ( var method in methodsExpectedToBeInlined ) {
					var flags = method.GetMethodImplementationFlags();

					Assert.That(
						flags.HasFlag( MethodImplAttributes.AggressiveInlining ),
						Is.True,
						$"{method.Name} should be marked with MethodImplOptions.AggressiveInlining."
					);
				}
			} );
		}

		/*
		================================================================================
		Test case sources
		================================================================================
		*/

		private static IEnumerable<float> DegreeRoundTripCases() {
			yield return -720.0f;
			yield return -450.0f;
			yield return -360.0f;
			yield return -180.0f;
			yield return -90.0f;
			yield return -45.0f;
			yield return 0.0f;
			yield return 45.0f;
			yield return 90.0f;
			yield return 180.0f;
			yield return 360.0f;
			yield return 450.0f;
			yield return 720.0f;
		}

		private static IEnumerable<double> DoubleDegreeRoundTripCases() {
			yield return -720.0d;
			yield return -450.0d;
			yield return -360.0d;
			yield return -180.0d;
			yield return -90.0d;
			yield return -45.0d;
			yield return 0.0d;
			yield return 45.0d;
			yield return 90.0d;
			yield return 180.0d;
			yield return 360.0d;
			yield return 450.0d;
			yield return 720.0d;
		}

		private static IEnumerable<float> DegreeRangeCases() {
			yield return -1080.0f;
			yield return -721.0f;
			yield return -720.0f;
			yield return -540.0f;
			yield return -361.0f;
			yield return -360.0f;
			yield return -181.0f;
			yield return -180.0f;
			yield return -179.0f;
			yield return -1.0f;
			yield return 0.0f;
			yield return 1.0f;
			yield return 179.0f;
			yield return 180.0f;
			yield return 181.0f;
			yield return 359.0f;
			yield return 360.0f;
			yield return 361.0f;
			yield return 540.0f;
			yield return 720.0f;
			yield return 721.0f;
			yield return 1080.0f;
		}

		private static IEnumerable<double> DoubleDegreeRangeCases() {
			foreach ( var value in DegreeRangeCases() ) {
				yield return value;
			}
		}

		private static IEnumerable<TestCaseData> UnsignedRadianFloatCases() {
			var pi = MathF.PI;
			var tau = MathF.PI * 2.0f;

			yield return new TestCaseData( 0.0f, 0.0f );
			yield return new TestCaseData( pi * 0.5f, pi * 0.5f );
			yield return new TestCaseData( pi, pi );
			yield return new TestCaseData( pi * 1.5f, pi * 1.5f );
			yield return new TestCaseData( tau, 0.0f );
			yield return new TestCaseData( tau + pi * 0.5f, pi * 0.5f );
			yield return new TestCaseData( -pi * 0.5f, pi * 1.5f );
			yield return new TestCaseData( -pi, pi );
			yield return new TestCaseData( -tau, 0.0f );
			yield return new TestCaseData( -tau - pi * 0.5f, pi * 1.5f );
		}

		private static IEnumerable<TestCaseData> UnsignedRadianDoubleCases() {
			yield return new TestCaseData( 0.0d, 0.0d );
			yield return new TestCaseData( Math.PI * 0.5d, Math.PI * 0.5d );
			yield return new TestCaseData( Math.PI, Math.PI );
			yield return new TestCaseData( Math.PI * 1.5d, Math.PI * 1.5d );
			yield return new TestCaseData( Tau, 0.0d );
			yield return new TestCaseData( Tau + Math.PI * 0.5d, Math.PI * 0.5d );
			yield return new TestCaseData( -Math.PI * 0.5d, Math.PI * 1.5d );
			yield return new TestCaseData( -Math.PI, Math.PI );
			yield return new TestCaseData( -Tau, 0.0d );
			yield return new TestCaseData( -Tau - Math.PI * 0.5d, Math.PI * 1.5d );
		}

		private static IEnumerable<TestCaseData> SignedRadianFloatCases() {
			var pi = MathF.PI;
			var tau = MathF.PI * 2.0f;

			yield return new TestCaseData( 0.0f, 0.0f );
			yield return new TestCaseData( pi * 0.5f, pi * 0.5f );
			yield return new TestCaseData( pi, -pi );
			yield return new TestCaseData( pi * 1.5f, -pi * 0.5f );
			yield return new TestCaseData( tau, 0.0f );
			yield return new TestCaseData( tau + pi * 0.5f, pi * 0.5f );
			yield return new TestCaseData( -pi * 0.5f, -pi * 0.5f );
			yield return new TestCaseData( -pi, -pi );
			yield return new TestCaseData( -pi * 1.5f, pi * 0.5f );
			yield return new TestCaseData( -tau, 0.0f );
		}

		private static IEnumerable<TestCaseData> SignedRadianDoubleCases() {
			yield return new TestCaseData( 0.0d, 0.0d );
			yield return new TestCaseData( Math.PI * 0.5d, Math.PI * 0.5d );
			yield return new TestCaseData( Math.PI, -Math.PI );
			yield return new TestCaseData( Math.PI * 1.5d, -Math.PI * 0.5d );
			yield return new TestCaseData( Tau, 0.0d );
			yield return new TestCaseData( Tau + Math.PI * 0.5d, Math.PI * 0.5d );
			yield return new TestCaseData( -Math.PI * 0.5d, -Math.PI * 0.5d );
			yield return new TestCaseData( -Math.PI, -Math.PI );
			yield return new TestCaseData( -Math.PI * 1.5d, Math.PI * 0.5d );
			yield return new TestCaseData( -Tau, 0.0d );
		}

		private static IEnumerable<float> RadianRangeCases() {
			var tau = MathF.PI * 2.0f;

			yield return -tau * 3.0f;
			yield return -tau * 2.0f;
			yield return -tau;
			yield return -MathF.PI * 1.5f;
			yield return -MathF.PI;
			yield return -MathF.PI * 0.5f;
			yield return 0.0f;
			yield return MathF.PI * 0.5f;
			yield return MathF.PI;
			yield return MathF.PI * 1.5f;
			yield return tau;
			yield return tau * 2.0f;
			yield return tau * 3.0f;
		}

		private static IEnumerable<double> DoubleRadianRangeCases() {
			yield return -Tau * 3.0d;
			yield return -Tau * 2.0d;
			yield return -Tau;
			yield return -Math.PI * 1.5d;
			yield return -Math.PI;
			yield return -Math.PI * 0.5d;
			yield return 0.0d;
			yield return Math.PI * 0.5d;
			yield return Math.PI;
			yield return Math.PI * 1.5d;
			yield return Tau;
			yield return Tau * 2.0d;
			yield return Tau * 3.0d;
		}

		private static IEnumerable<TestCaseData> DeltaRadiansFloatCases() {
			var pi = MathF.PI;
			var tau = MathF.PI * 2.0f;

			yield return new TestCaseData( 0.0f, 0.0f, 0.0f );
			yield return new TestCaseData( 0.0f, pi * 0.5f, pi * 0.5f );
			yield return new TestCaseData( pi * 0.5f, 0.0f, -pi * 0.5f );
			yield return new TestCaseData( tau - DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 20.0f ) );
			yield return new TestCaseData( DegreesToRadiansF( 10.0f ), tau - DegreesToRadiansF( 10.0f ), -DegreesToRadiansF( 20.0f ) );
			yield return new TestCaseData( 0.0f, pi, -pi );
			yield return new TestCaseData( pi, 0.0f, -pi );
		}

		private static IEnumerable<TestCaseData> DeltaRadiansDoubleCases() {
			yield return new TestCaseData( 0.0d, 0.0d, 0.0d );
			yield return new TestCaseData( 0.0d, Math.PI * 0.5d, Math.PI * 0.5d );
			yield return new TestCaseData( Math.PI * 0.5d, 0.0d, -Math.PI * 0.5d );
			yield return new TestCaseData( Tau - DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 20.0d ) );
			yield return new TestCaseData( DegreesToRadiansD( 10.0d ), Tau - DegreesToRadiansD( 10.0d ), -DegreesToRadiansD( 20.0d ) );
			yield return new TestCaseData( 0.0d, Math.PI, -Math.PI );
			yield return new TestCaseData( Math.PI, 0.0d, -Math.PI );
		}

		private static IEnumerable<TestCaseData> LerpRadiansFloatCases() {
			yield return new TestCaseData( 0.0f, MathF.PI, 0.0f, 0.0f );
			yield return new TestCaseData( 0.0f, MathF.PI, 0.5f, -MathF.PI * 0.5f );
			yield return new TestCaseData( 0.0f, MathF.PI, 1.0f, -MathF.PI );
			yield return new TestCaseData( 0.0f, MathF.PI, -1.0f, 0.0f );
			yield return new TestCaseData( 0.0f, MathF.PI, 2.0f, -MathF.PI );

			var from = DegreesToRadiansF( 350.0f );
			var to = DegreesToRadiansF( 10.0f );

			yield return new TestCaseData( from, to, 0.0f, from );
			yield return new TestCaseData( from, to, 0.5f, DegreesToRadiansF( 360.0f ) );
			yield return new TestCaseData( from, to, 1.0f, DegreesToRadiansF( 370.0f ) );
		}

		private static IEnumerable<TestCaseData> LerpRadiansDoubleCases() {
			yield return new TestCaseData( 0.0d, Math.PI, 0.0d, 0.0d );
			yield return new TestCaseData( 0.0d, Math.PI, 0.5d, -Math.PI * 0.5d );
			yield return new TestCaseData( 0.0d, Math.PI, 1.0d, -Math.PI );
			yield return new TestCaseData( 0.0d, Math.PI, -1.0d, 0.0d );
			yield return new TestCaseData( 0.0d, Math.PI, 2.0d, -Math.PI );

			var from = DegreesToRadiansD( 350.0d );
			var to = DegreesToRadiansD( 10.0d );

			yield return new TestCaseData( from, to, 0.0d, from );
			yield return new TestCaseData( from, to, 0.5d, DegreesToRadiansD( 360.0d ) );
			yield return new TestCaseData( from, to, 1.0d, DegreesToRadiansD( 370.0d ) );
		}

		private static IEnumerable<TestCaseData> MoveTowardsRadiansFloatCases() {
			yield return new TestCaseData( 0.0f, DegreesToRadiansF( 90.0f ), DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 10.0f ) );
			yield return new TestCaseData( DegreesToRadiansF( 90.0f ), 0.0f, DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 80.0f ) );
			yield return new TestCaseData( 0.0f, DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 20.0f ), DegreesToRadiansF( 10.0f ) );
			yield return new TestCaseData( DegreesToRadiansF( 350.0f ), DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 5.0f ), DegreesToRadiansF( 355.0f ) );
			yield return new TestCaseData( DegreesToRadiansF( 10.0f ), DegreesToRadiansF( 350.0f ), DegreesToRadiansF( 5.0f ), DegreesToRadiansF( 5.0f ) );
		}

		private static IEnumerable<TestCaseData> MoveTowardsRadiansDoubleCases() {
			yield return new TestCaseData( 0.0d, DegreesToRadiansD( 90.0d ), DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 10.0d ) );
			yield return new TestCaseData( DegreesToRadiansD( 90.0d ), 0.0d, DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 80.0d ) );
			yield return new TestCaseData( 0.0d, DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 20.0d ), DegreesToRadiansD( 10.0d ) );
			yield return new TestCaseData( DegreesToRadiansD( 350.0d ), DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 5.0d ), DegreesToRadiansD( 355.0d ) );
			yield return new TestCaseData( DegreesToRadiansD( 10.0d ), DegreesToRadiansD( 350.0d ), DegreesToRadiansD( 5.0d ), DegreesToRadiansD( 5.0d ) );
		}

		private static IEnumerable<TestCaseData> LookAtRadiansFloatCases() {
			yield return new TestCaseData( 0.0f, 0.0f, 1.0f, 0.0f, 0.0f );
			yield return new TestCaseData( 0.0f, 0.0f, 0.0f, 1.0f, MathF.PI * 0.5f );
			yield return new TestCaseData( 0.0f, 0.0f, -1.0f, 0.0f, MathF.PI );
			yield return new TestCaseData( 0.0f, 0.0f, 0.0f, -1.0f, -MathF.PI * 0.5f );
			yield return new TestCaseData( 1.0f, 1.0f, 2.0f, 2.0f, MathF.PI * 0.25f );
			yield return new TestCaseData( 1.0f, 1.0f, 0.0f, 2.0f, MathF.PI * 0.75f );
			yield return new TestCaseData( 1.0f, 1.0f, 0.0f, 0.0f, -MathF.PI * 0.75f );
			yield return new TestCaseData( 1.0f, 1.0f, 2.0f, 0.0f, -MathF.PI * 0.25f );
		}

		private static IEnumerable<TestCaseData> LookAtRadiansDoubleCases() {
			yield return new TestCaseData( 0.0d, 0.0d, 1.0d, 0.0d, 0.0d );
			yield return new TestCaseData( 0.0d, 0.0d, 0.0d, 1.0d, Math.PI * 0.5d );
			yield return new TestCaseData( 0.0d, 0.0d, -1.0d, 0.0d, Math.PI );
			yield return new TestCaseData( 0.0d, 0.0d, 0.0d, -1.0d, -Math.PI * 0.5d );
			yield return new TestCaseData( 1.0d, 1.0d, 2.0d, 2.0d, Math.PI * 0.25d );
			yield return new TestCaseData( 1.0d, 1.0d, 0.0d, 2.0d, Math.PI * 0.75d );
			yield return new TestCaseData( 1.0d, 1.0d, 0.0d, 0.0d, -Math.PI * 0.75d );
			yield return new TestCaseData( 1.0d, 1.0d, 2.0d, 0.0d, -Math.PI * 0.25d );
		}

		private static IEnumerable<TestCaseData> GetQuadrantRadiansCases() {
			var pi = MathF.PI;
			var tau = MathF.PI * 2.0f;

			yield return new TestCaseData( 0.0f, 0 );
			yield return new TestCaseData( pi * 0.5f, 0 );
			yield return new TestCaseData( pi, 0 );
			yield return new TestCaseData( pi * 1.5f, 0 );
			yield return new TestCaseData( tau, 0 );

			yield return new TestCaseData( pi * 0.25f, 1 );
			yield return new TestCaseData( pi * 0.75f, 2 );
			yield return new TestCaseData( pi * 1.25f, 3 );
			yield return new TestCaseData( pi * 1.75f, 4 );

			yield return new TestCaseData( -pi * 0.25f, 4 );
			yield return new TestCaseData( -pi * 0.75f, 3 );
			yield return new TestCaseData( -pi * 1.25f, 2 );
			yield return new TestCaseData( -pi * 1.75f, 1 );

			yield return new TestCaseData( tau + pi * 0.25f, 1 );
			yield return new TestCaseData( tau + pi * 0.75f, 2 );
			yield return new TestCaseData( tau + pi * 1.25f, 3 );
			yield return new TestCaseData( tau + pi * 1.75f, 4 );
		}

		private static float DegreesToRadiansF( float degrees ) {
			return degrees * MathF.PI / 180.0f;
		}

		private static double DegreesToRadiansD( double degrees ) {
			return degrees * Math.PI / 180.0d;
		}
	}
}