using System;
using NUnit.Framework;
using Nomad.CVars.Private.ValueObjects;

namespace Nomad.CVars.Tests
{
    [TestFixture]
    [Category("Nomad.CVars")]
    [Category("Validation")]
    [Category("Unit")]
    public class CVarValidatorTests
    {
        [Test]
        public void Constructor_WithNullValidator_ShouldStoreNull()
        {
            var validator = new CVarValidator<int>(null);
            // No direct way to inspect, but ValidateValue should return true
            Assert.That(validator.ValidateValue(42), Is.True);
        }

        [Test]
        public void ValidateValue_WithValidator_ShouldReturnValidatorResult()
        {
            Func<int, bool> alwaysTrue = _ => true;
            var validator = new CVarValidator<int>(alwaysTrue);
            Assert.That(validator.ValidateValue(10), Is.True);

            Func<int, bool> alwaysFalse = _ => false;
            validator = new CVarValidator<int>(alwaysFalse);
            Assert.That(validator.ValidateValue(10), Is.False);
        }

        [Test]
        public void ValidateValue_WithNullValidator_ShouldAlwaysReturnTrue()
        {
            var validator = new CVarValidator<string>(null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(validator.ValidateValue("any"), Is.True);
                Assert.That(validator.ValidateValue(null!), Is.True);
            }
        }

        [Test]
        public void IsValidName_ValidNames_ReturnsTrue()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CVarValidator<int>.IsValidName("simple"), Is.True);
                Assert.That(CVarValidator<int>.IsValidName("with.dots"), Is.True);
                Assert.That(CVarValidator<int>.IsValidName("with_underscore"), Is.True);
                Assert.That(CVarValidator<int>.IsValidName("a1.b2_c"), Is.True);
            }
        }

        [Test]
        public void IsValidName_InvalidNames_ReturnsFalse()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CVarValidator<int>.IsValidName(null!), Is.False);
                Assert.That(CVarValidator<int>.IsValidName(""), Is.False);
                Assert.That(CVarValidator<int>.IsValidName(".start"), Is.False);
                Assert.That(CVarValidator<int>.IsValidName("end."), Is.False);
                Assert.That(CVarValidator<int>.IsValidName("invalid!"), Is.False);
                Assert.That(CVarValidator<int>.IsValidName("space in name"), Is.False);
            }
        }

        [Test]
        public void ValidateCVarType_ForSupportedTypes_ReturnsTrue()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CVarValidator<int>.ValidateCVarType(), Is.True);
                Assert.That(CVarValidator<bool>.ValidateCVarType(), Is.True);
                Assert.That(CVarValidator<float>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<double>.ValidateCVarType(), Is.True);
                Assert.That(CVarValidator<string>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<byte>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<sbyte>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<short>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<ushort>.ValidateCVarType(), Is.True);
                Assert.That(CVarValidator<uint>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<long>.ValidateCVarType(), Is.True);
                //                Assert.That(CVarValidator<ulong>.ValidateCVarType(), Is.True);
            }
        }

        [Test]
        public void ValidateCVarType_ForUnsupportedType_ReturnsFalse()
        {
            // Using a custom class which should not map to any CVarType
            Assert.That(CVarValidator<UnsupportedClass>.ValidateCVarType(), Is.False);
        }

        private class UnsupportedClass { }
    }
}