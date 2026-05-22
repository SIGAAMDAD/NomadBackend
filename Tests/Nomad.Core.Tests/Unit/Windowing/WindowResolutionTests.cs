using System;
using System.Collections.Generic;
using Nomad.Core.Engine.Windowing;
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    public sealed class WindowResolutionExtensionsTests
    {
        private static IEnumerable<TestCaseData> FixedResolutionCases()
        {
            yield return new TestCaseData(WindowResolution.Res_800x600,   "800x600",   800,  600);
            yield return new TestCaseData(WindowResolution.Res_1024x768,  "1024x768",  1024, 768);
            yield return new TestCaseData(WindowResolution.Res_1280x720,  "1280x720",  1280, 720);
            yield return new TestCaseData(WindowResolution.Res_1280x768,  "1280x768",  1280, 768);
            yield return new TestCaseData(WindowResolution.Res_1280x800,  "1280x800",  1280, 800);
            yield return new TestCaseData(WindowResolution.Res_1280x1024, "1280x1024", 1280, 1024);
            yield return new TestCaseData(WindowResolution.Res_1360x768,  "1360x768",  1360, 768);
            yield return new TestCaseData(WindowResolution.Res_1366x768,  "1366x768",  1366, 768);
            yield return new TestCaseData(WindowResolution.Res_1440x900,  "1440x900",  1440, 900);
            yield return new TestCaseData(WindowResolution.Res_1536x864,  "1536x864",  1536, 864);
            yield return new TestCaseData(WindowResolution.Res_1600x900,  "1600x900",  1600, 900);
            yield return new TestCaseData(WindowResolution.Res_1600x1200, "1600x1200", 1600, 1200);
            yield return new TestCaseData(WindowResolution.Res_1680x1050, "1680x1050", 1680, 1050);
            yield return new TestCaseData(WindowResolution.Res_1920x1080, "1920x1080", 1920, 1080);
            yield return new TestCaseData(WindowResolution.Res_1920x1200, "1920x1200", 1920, 1200);
            yield return new TestCaseData(WindowResolution.Res_2048x1152, "2048x1152", 2048, 1152);
            yield return new TestCaseData(WindowResolution.Res_2048x1536, "2048x1536", 2048, 1536);
            yield return new TestCaseData(WindowResolution.Res_2560x1080, "2560x1080", 2560, 1080);
            yield return new TestCaseData(WindowResolution.Res_2560x1440, "2560x1440", 2560, 1440);
            yield return new TestCaseData(WindowResolution.Res_2560x1600, "2560x1600", 2560, 1600);
            yield return new TestCaseData(WindowResolution.Res_3440x1440, "3440x1440", 3440, 1440);
            yield return new TestCaseData(WindowResolution.Res_3840x2160, "3840x2160", 3840, 2160);
        }

        private static IEnumerable<TestCaseData> DisplayResolutionCases()
        {
            foreach (var testCase in FixedResolutionCases())
            {
                yield return testCase;
            }

            yield return new TestCaseData(
                WindowResolution.Res_Native,
                "Native Resolution",
                0,
                0
            );
        }

        [TestCaseSource(nameof(DisplayResolutionCases))]
        public void ToDisplayString_ReturnsExpectedDisplayString_ForEverySupportedResolution(
            WindowResolution resolution,
            string expectedDisplay,
            int _,
            int __)
        {
            InternString actual = resolution.ToDisplayString();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(actual, Is.EqualTo(new InternString(expectedDisplay)));
                Assert.That(actual.ToString(), Is.EqualTo(expectedDisplay));
            }
        }

        [Test]
        public void ToDisplayString_ReturnsExpectedDisplayString_ForEnumAliases()
        {
			using ( Assert.EnterMultipleScope() ) {
                Assert.That(
                    WindowResolution.Min.ToDisplayString(),
                    Is.EqualTo(new InternString("800x600"))
                );

                Assert.That(
                    WindowResolution.Max.ToDisplayString(),
                    Is.EqualTo(new InternString("3840x2160"))
                );

                Assert.That(
                    WindowResolution.Default.ToDisplayString(),
                    Is.EqualTo(new InternString("1920x1080"))
                );
            }
        }

        [TestCaseSource(nameof(FixedResolutionCases))]
        public void GetSize_ReturnsExpectedWindowSize_ForEveryFixedResolution(
            WindowResolution resolution,
            string _,
            int expectedWidth,
            int expectedHeight)
        {
            WindowSize actual = resolution.GetSize();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(actual.Width, Is.EqualTo(expectedWidth));
                Assert.That(actual.Height, Is.EqualTo(expectedHeight));
            }
        }

        [Test]
        public void GetSize_ReturnsExpectedWindowSize_ForEnumAliases()
        {
            WindowSize min = WindowResolution.Min.GetSize();
            WindowSize max = WindowResolution.Max.GetSize();
            WindowSize @default = WindowResolution.Default.GetSize();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(min.Width, Is.EqualTo(800));
                Assert.That(min.Height, Is.EqualTo(600));

                Assert.That(max.Width, Is.EqualTo(3840));
                Assert.That(max.Height, Is.EqualTo(2160));

                Assert.That(@default.Width, Is.EqualTo(1920));
                Assert.That(@default.Height, Is.EqualTo(1080));
            }
        }

        [Test]
        public void GetSize_ThrowsArgumentOutOfRangeException_ForNativeResolution()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => WindowResolution.Res_Native.GetSize()
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(ex, Is.Not.Null);
                Assert.That(ex!.ParamName, Is.EqualTo("resolution"));
                Assert.That(ex.ActualValue, Is.EqualTo(WindowResolution.Res_Native));
            }
        }

        [Test]
        public void ToDisplayString_ThrowsArgumentOutOfRangeException_ForCountSentinel()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => WindowResolution.Count.ToDisplayString()
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(ex, Is.Not.Null);
                Assert.That(ex!.ParamName, Is.EqualTo("resolution"));
                Assert.That(ex.ActualValue, Is.EqualTo(WindowResolution.Count));
            }
        }

        [Test]
        public void GetSize_ThrowsArgumentOutOfRangeException_ForCountSentinel()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => WindowResolution.Count.GetSize()
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(ex, Is.Not.Null);
                Assert.That(ex!.ParamName, Is.EqualTo("resolution"));
                Assert.That(ex.ActualValue, Is.EqualTo(WindowResolution.Count));
            }
        }

        [Test]
        public void ToDisplayString_ThrowsArgumentOutOfRangeException_ForInvalidResolutionValue()
        {
            const WindowResolution invalid = (WindowResolution)byte.MaxValue;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => invalid.ToDisplayString()
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(ex, Is.Not.Null);
                Assert.That(ex!.ParamName, Is.EqualTo("resolution"));
                Assert.That(ex.ActualValue, Is.EqualTo(invalid));
            }
        }

        [Test]
        public void GetSize_ThrowsArgumentOutOfRangeException_ForInvalidResolutionValue()
        {
            const WindowResolution invalid = (WindowResolution)byte.MaxValue;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => invalid.GetSize()
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(ex, Is.Not.Null);
                Assert.That(ex!.ParamName, Is.EqualTo("resolution"));
                Assert.That(ex.ActualValue, Is.EqualTo(invalid));
            }
        }

        [TestCaseSource(nameof(DisplayResolutionCases))]
        public void TryParse_ReturnsTrueAndExpectedResolution_ForEverySupportedDisplayString(
            WindowResolution expectedResolution,
            string displayString,
            int _,
            int __)
        {
            bool result = WindowResolutionExtensions.TryParse(
                new InternString(displayString),
                out WindowResolution actualResolution
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(result, Is.True);
                Assert.That(actualResolution, Is.EqualTo(expectedResolution));
            }
        }

        [Test]
        public void TryParse_ReturnsFalseAndDefaultResolution_ForEmptyInternString()
        {
            bool result = WindowResolutionExtensions.TryParse(
                InternString.Empty,
                out WindowResolution actualResolution
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(result, Is.False);
                Assert.That(actualResolution, Is.EqualTo(WindowResolution.Default));
            }
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("Unknown")]
        [TestCase("1920 x 1080")]
        [TestCase("1920X1080")]
        [TestCase("native resolution")]
        [TestCase("Native")]
        [TestCase("3840x2160 ")]
        [TestCase(" 3840x2160")]
        public void TryParse_ReturnsFalseAndDefaultResolution_ForUnsupportedDisplayString(
            string displayString)
        {
            bool result = WindowResolutionExtensions.TryParse(
                new InternString(displayString),
                out WindowResolution actualResolution
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(result, Is.False);
                Assert.That(actualResolution, Is.EqualTo(WindowResolution.Default));
            }
        }

        [Test]
        public void TryParse_DoesNotTreatCountAsAValidDisplayString()
        {
            bool result = WindowResolutionExtensions.TryParse(
                new InternString(nameof(WindowResolution.Count)),
                out WindowResolution actualResolution
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(result, Is.False);
                Assert.That(actualResolution, Is.EqualTo(WindowResolution.Default));
            }
        }

        [Test]
        public void EnumAliases_AreExpectedValues()
        {
			using ( Assert.EnterMultipleScope() ) {
                Assert.That(WindowResolution.Min, Is.EqualTo(WindowResolution.Res_800x600));
                Assert.That(WindowResolution.Max, Is.EqualTo(WindowResolution.Res_3840x2160));
                Assert.That(WindowResolution.Default, Is.EqualTo(WindowResolution.Res_1920x1080));

                Assert.That((byte)WindowResolution.Res_800x600, Is.EqualTo(0));
                Assert.That((byte)WindowResolution.Res_Native, Is.EqualTo(22));
                Assert.That((byte)WindowResolution.Count, Is.EqualTo(23));
            }
        }

        [Test]
        public void Count_IsNotIncludedInSupportedDisplayStrings()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => WindowResolution.Count.ToDisplayString()
            );
        }

        [Test]
        public void NativeResolution_HasDisplayStringAndParseSupportButNoFixedSize()
        {
            InternString display = WindowResolution.Res_Native.ToDisplayString();

            bool parseResult = WindowResolutionExtensions.TryParse(
                display,
                out WindowResolution parsedResolution
            );

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(display, Is.EqualTo(new InternString("Native Resolution")));
                Assert.That(parseResult, Is.True);
                Assert.That(parsedResolution, Is.EqualTo(WindowResolution.Res_Native));

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => WindowResolution.Res_Native.GetSize()
                );
            }
        }
    }
}