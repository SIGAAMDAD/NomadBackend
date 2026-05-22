using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.CVars.Private.ValueObjects;

namespace Nomad.CVars.Tests
{
    [TestFixture]
    [Category("Nomad.CVars")]
    [Category("Metadata")]
    [Category("Unit")]
    public class CVarMetadataTests
    {
        [Test]
        public void Constructor_ShouldSetPropertiesCorrectly()
        {
            const string name = "test.cvar";
            const string desc = "A test CVar";
            const CVarFlags flags = CVarFlags.Archive | CVarFlags.ReadOnly;
            const CVarType type = CVarType.Int;

            var metadata = new CVarMetadata(name, desc, flags, type);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metadata.Name.ToString(), Is.EqualTo(name));
                Assert.That(metadata.Description.ToString(), Is.EqualTo(desc));
                Assert.That(metadata.Flags, Is.EqualTo(flags));
                Assert.That(metadata.Type, Is.EqualTo(type));
            }
        }

        [Test]
        public void IsReadOnly_ShouldBeTrue_WhenReadOnlyFlagSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.ReadOnly, CVarType.Int);
            Assert.That(metadata.IsReadOnly, Is.True);
        }

        [Test]
        public void IsReadOnly_ShouldBeFalse_WhenReadOnlyFlagNotSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.Archive, CVarType.Int);
            Assert.That(metadata.IsReadOnly, Is.False);
        }

        [Test]
        public void IsHidden_ShouldBeTrue_WhenHiddenFlagSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.Hidden, CVarType.Int);
            Assert.That(metadata.IsHidden, Is.True);
        }

        [Test]
        public void IsSaved_ShouldBeTrue_WhenArchiveFlagSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.Archive, CVarType.Int);
            Assert.That(metadata.IsSaved, Is.True);
        }

        [Test]
        public void IsUserCreated_ShouldBeTrue_WhenUserCreatedFlagSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.UserCreated, CVarType.Int);
            Assert.That(metadata.IsUserCreated, Is.True);
        }

        [Test]
        public void IsDeveloper_ShouldBeTrue_WhenDeveloperFlagSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.Developer, CVarType.Int);
            Assert.That(metadata.IsDeveloper, Is.True);
        }

        [Test]
        public void IsInitializationOnly_ShouldBeTrue_WhenInitFlagSet()
        {
            var metadata = new CVarMetadata("name", "desc", CVarFlags.Init, CVarType.Int);
            Assert.That(metadata.IsInitializationOnly, Is.True);
        }
    }
}