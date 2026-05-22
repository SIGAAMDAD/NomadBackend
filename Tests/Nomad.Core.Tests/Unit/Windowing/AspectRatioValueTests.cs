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
using Nomad.Core.Engine.Windowing;
using NUnit.Framework;

namespace Nomad.Core.Tests.Engine.Windowing
{
	[TestFixture]
	[Category("Nomad.Core")]
    [Category("Windowing")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class AspectRatioValueTests
    {
        [Test]
        [TestCase(AspectRatio.Aspect_Automatic)]
        [TestCase(AspectRatio.Aspect_4_3)]
        [TestCase(AspectRatio.Aspect_16_9)]
        [TestCase(AspectRatio.Aspect_16_10)]
        [TestCase(AspectRatio.Aspect_21_9)]
        public void Constructor_WithAspectRatio_StoresProvidedRatio(AspectRatio ratio)
        {
            var value = new AspectRatioValue(ratio);

            Assert.That(ratio, Is.EqualTo(value.Ratio));
        }

        [Test]
        public void Constructor_WithFourByThreeFloat_StoresFourByThreeRatio()
        {
            var value = new AspectRatioValue(4.0f / 3.0f);

            Assert.That(value.Ratio, Is.EqualTo(AspectRatio.Aspect_4_3));
        }

        [Test]
        public void Constructor_WithSixteenByNineFloat_StoresSixteenByNineRatio()
        {
            var value = new AspectRatioValue(16.0f / 9.0f);

            Assert.That(value.Ratio, Is.EqualTo(AspectRatio.Aspect_16_9));
        }

        [Test]
        public void Constructor_WithSixteenByTenFloat_StoresSixteenByTenRatio()
        {
            var value = new AspectRatioValue(16.0f / 10.0f);

            Assert.That(value.Ratio, Is.EqualTo(AspectRatio.Aspect_16_10));
        }

        [Test]
        public void Constructor_WithTwentyOneByNineFloat_StoresTwentyOneByNineRatio()
        {
            var value = new AspectRatioValue(21.0f / 9.0f);

            Assert.That(value.Ratio, Is.EqualTo(AspectRatio.Aspect_21_9));
        }

        [Test]
        [TestCase(0.0f)]
        [TestCase(1.0f)]
        [TestCase(2.0f)]
        [TestCase(-1.0f)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Constructor_WithUnknownFloat_StoresAutomaticRatio(float ratio)
        {
            var value = new AspectRatioValue(ratio);

            Assert.That(value.Ratio, Is.EqualTo(AspectRatio.Aspect_Automatic));
        }

        [Test]
        public void Constructor_WithNaN_StoresAutomaticRatio()
        {
            var value = new AspectRatioValue(float.NaN);

            Assert.That(value.Ratio, Is.EqualTo(AspectRatio.Aspect_Automatic));
        }

        [Test]
        public void Constructor_WithFloatNearFourByThreeButNotExactlyEqual_StoresAutomaticRatio()
        {
            var exact = 4.0f / 3.0f;
            var nearButDifferent = BitConverter.Int32BitsToSingle(
                BitConverter.SingleToInt32Bits(exact) + 1);

            var value = new AspectRatioValue(nearButDifferent);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( exact, Is.Not.EqualTo( nearButDifferent ) );
				Assert.That( value.Ratio, Is.EqualTo( AspectRatio.Aspect_Automatic ) );
			}
		}

        [Test]
        public void Constructor_WithFloatNearSixteenByNineButNotExactlyEqual_StoresAutomaticRatio()
        {
            var exact = 16.0f / 9.0f;
            var nearButDifferent = BitConverter.Int32BitsToSingle(
                BitConverter.SingleToInt32Bits(exact) + 1);

            var value = new AspectRatioValue(nearButDifferent);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( exact, Is.Not.EqualTo( nearButDifferent ) );
				Assert.That( value.Ratio, Is.EqualTo( AspectRatio.Aspect_Automatic ) );
			}
		}

        [Test]
        public void Constructor_WithFloatNearSixteenByTenButNotExactlyEqual_StoresAutomaticRatio()
        {
            var exact = 16.0f / 10.0f;
            var nearButDifferent = BitConverter.Int32BitsToSingle(
                BitConverter.SingleToInt32Bits(exact) + 1);

            var value = new AspectRatioValue(nearButDifferent);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( exact, Is.Not.EqualTo( nearButDifferent ) );
				Assert.That( value.Ratio, Is.EqualTo( AspectRatio.Aspect_Automatic ) );
			}
		}

        [Test]
        public void Constructor_WithFloatNearTwentyOneByNineButNotExactlyEqual_StoresAutomaticRatio()
        {
            var exact = 21.0f / 9.0f;
            var nearButDifferent = BitConverter.Int32BitsToSingle(
                BitConverter.SingleToInt32Bits(exact) + 1);

            var value = new AspectRatioValue(nearButDifferent);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( exact, Is.Not.EqualTo( nearButDifferent ) );
				Assert.That( value.Ratio, Is.EqualTo( AspectRatio.Aspect_Automatic ) );
			}
		}

        [Test]
        [TestCase(AspectRatio.Aspect_Automatic)]
        [TestCase(AspectRatio.Aspect_4_3)]
        [TestCase(AspectRatio.Aspect_16_9)]
        [TestCase(AspectRatio.Aspect_16_10)]
        [TestCase(AspectRatio.Aspect_21_9)]
        public void ExplicitFloatOperator_ReturnsUnderlyingAspectRatioValue(AspectRatio ratio)
        {
            var value = new AspectRatioValue(ratio);

            var converted = (float)value;

            Assert.That(ratio.GetRatio(), Is.EqualTo(converted));
        }

        [Test]
        public void ExplicitFloatOperator_AfterFloatConstructor_ReturnsMatchedAspectRatioValue()
        {
            var value = new AspectRatioValue(16.0f / 9.0f);

            var converted = (float)value;

            Assert.That(AspectRatio.Aspect_16_9.GetRatio(), Is.EqualTo(converted));
        }

        [Test]
        public void ExplicitFloatOperator_AfterUnknownFloatConstructor_ReturnsAutomaticAspectRatioValue()
        {
            var value = new AspectRatioValue(123.456f);

            var converted = (float)value;

            Assert.That(AspectRatio.Aspect_Automatic.GetRatio(), Is.EqualTo(converted));
        }

        [Test]
        public void DefaultValue_HasDefaultEnumValue()
        {
            var value = default(AspectRatioValue);

			Assert.That( value, Is.Default );
        }

        [Test]
        public void DefaultValue_ExplicitFloatOperator_ReturnsDefaultEnumRatioValue()
        {
            var value = default(AspectRatioValue);

            var converted = (float)value;

            Assert.That(default(AspectRatio).GetRatio(), Is.EqualTo(converted));
        }
    }
}