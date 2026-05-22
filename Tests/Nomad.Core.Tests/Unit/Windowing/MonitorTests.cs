using Nomad.Core.Engine.Windowing;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    public sealed class MonitorTests
    {
        [Test]
        public void Constructor_AssignsAllProperties()
        {
            var monitor = new Monitor(
                index: 1,
                refreshRate: 144,
                screenSize: WindowResolution.Res_2560x1440
            );

            Assert.Multiple(() =>
            {
                Assert.That(monitor.Index, Is.EqualTo(1));
                Assert.That(monitor.RefreshRate, Is.EqualTo(144));
                Assert.That(monitor.ScreenSize, Is.EqualTo(WindowResolution.Res_2560x1440));
            });
        }

        [TestCase(0, 60, WindowResolution.Res_800x600)]
        [TestCase(1, 75, WindowResolution.Res_1024x768)]
        [TestCase(2, 120, WindowResolution.Res_1920x1080)]
        [TestCase(3, 144, WindowResolution.Res_2560x1440)]
        [TestCase(4, 165, WindowResolution.Res_3440x1440)]
        [TestCase(5, 240, WindowResolution.Res_3840x2160)]
        [TestCase(-1, 0, WindowResolution.Res_Native)]
        public void Constructor_PreservesProvidedValues(
            int index,
            int refreshRate,
            WindowResolution screenSize)
        {
            var monitor = new Monitor(index, refreshRate, screenSize);

            Assert.Multiple(() =>
            {
                Assert.That(monitor.Index, Is.EqualTo(index));
                Assert.That(monitor.RefreshRate, Is.EqualTo(refreshRate));
                Assert.That(monitor.ScreenSize, Is.EqualTo(screenSize));
            });
        }

        [Test]
        public void DefaultMonitor_UsesDefaultStructValues()
        {
            var monitor = default(Monitor);

            Assert.Multiple(() =>
            {
                Assert.That(monitor.Index, Is.EqualTo(0));
                Assert.That(monitor.RefreshRate, Is.EqualTo(0));

                // WindowResolution is byte-backed, and Res_800x600 is the zero value.
                Assert.That(monitor.ScreenSize, Is.EqualTo(WindowResolution.Res_800x600));
            });
        }
    }
}