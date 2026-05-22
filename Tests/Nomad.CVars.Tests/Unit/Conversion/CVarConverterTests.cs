using System;
using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.CVars.Exceptions;
using Nomad.CVars.Private.Entities;

namespace Nomad.CVars.Tests
{
    [TestFixture]
    [Category("Nomad.CVars")]
    [Category("Conversion")]
    [Category("Unit")]
    public class CVarConverterTests
    {
        [Test]
        public void Constructor_ShouldSetValueAndType()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 42);
            Assert.That(converter.Value, Is.EqualTo(42));
            // _type is private; cannot assert directly
        }

        #region GetStringValue

        [Test]
        public void GetStringValue_WhenTypeIsString_ReturnsToStringOfValue()
        {
            var converter = new CVarConverter<int>(CVarType.String, 123);
            Assert.That(converter.GetStringValue(), Is.EqualTo("123"));
        }

        [Test]
        public void GetStringValue_WhenTypeIsNotString_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 42);
            Assert.Throws<CVarTypeMismatchException>(() => converter.GetStringValue());
        }

        [Test]
        public void GetStringValue_WhenValueIsNull_ReturnsNull()
        {
            var converter = new CVarConverter<string>(CVarType.String, null!);
            Assert.That(converter.GetStringValue(), Is.Null);
        }

        #endregion

        #region GetIntegerValue

        [Test]
        public void GetIntegerValue_WhenTypeIsInt_ReturnsValue()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 123);
            Assert.That(converter.GetIntegerValue(), Is.EqualTo(123));
        }

        [Test]
        public void GetIntegerValue_WhenTypeIsNotInt_Throws()
        {
            var converter = new CVarConverter<float>(CVarType.Decimal, 1.5f);
            Assert.Throws<CVarTypeMismatchException>(() => converter.GetIntegerValue());
        }

        #endregion

        #region GetUIntegerValue

        [Test]
        public void GetUIntegerValue_WhenTypeIsUInt_ReturnsValue()
        {
            var converter = new CVarConverter<uint>(CVarType.UInt, 123u);
            Assert.That(converter.GetUIntegerValue(), Is.EqualTo(123u));
        }

        [Test]
        public void GetUIntegerValue_WhenTypeIsNotUInt_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 123);
            Assert.Throws<CVarTypeMismatchException>(() => converter.GetUIntegerValue());
        }

        #endregion

        #region GetDecimalValue

        [Test]
        public void GetDecimalValue_WhenTypeIsDecimal_ReturnsValue()
        {
            var converter = new CVarConverter<float>(CVarType.Decimal, 1.5f);
            Assert.That(converter.GetDecimalValue(), Is.EqualTo(1.5f));
        }

        [Test]
        public void GetDecimalValue_WhenTypeIsNotDecimal_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 123);
            Assert.Throws<CVarTypeMismatchException>(() => converter.GetDecimalValue());
        }

        #endregion

        #region GetBooleanValue

        [Test]
        public void GetBooleanValue_WhenTypeIsBoolean_ReturnsValue()
        {
            var converter = new CVarConverter<bool>(CVarType.Boolean, true);
            Assert.That(converter.GetBooleanValue(), Is.True);
        }

        [Test]
        public void GetBooleanValue_WhenTypeIsNotBoolean_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 123);
            Assert.Throws<CVarTypeMismatchException>(() => converter.GetBooleanValue());
        }

        #endregion

        #region SetIntegerValue

        [Test]
        public void SetIntegerValue_WhenTypeMatches_SetsValue()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            converter.SetIntegerValue(42);
            Assert.That(converter.Value, Is.EqualTo(42));
        }

        [Test]
        public void SetIntegerValue_WhenTypeMismatch_Throws()
        {
            var converter = new CVarConverter<float>(CVarType.Decimal, 0f);
            Assert.Throws<CVarTypeMismatchException>(() => converter.SetIntegerValue(42));
        }

        #endregion

        #region SetUIntegerValue

        [Test]
        public void SetUIntegerValue_WhenTypeMatches_SetsValue()
        {
            var converter = new CVarConverter<uint>(CVarType.UInt, 0u);
            converter.SetUIntegerValue(42u);
            Assert.That(converter.Value, Is.EqualTo(42u));
        }

        [Test]
        public void SetUIntegerValue_WhenTypeMismatch_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            Assert.Throws<CVarTypeMismatchException>(() => converter.SetUIntegerValue(42u));
        }

        #endregion

        #region SetBooleanValue

        [Test]
        public void SetBooleanValue_WhenTypeMatches_SetsValue()
        {
            var converter = new CVarConverter<bool>(CVarType.Boolean, false);
            converter.SetBooleanValue(true);
            Assert.That(converter.Value, Is.True);
        }

        [Test]
        public void SetBooleanValue_WhenTypeMismatch_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            Assert.Throws<CVarTypeMismatchException>(() => converter.SetBooleanValue(true));
        }

        #endregion

        #region SetDecimalValue

        [Test]
        public void SetDecimalValue_WhenTypeMatches_SetsValue()
        {
            var converter = new CVarConverter<float>(CVarType.Decimal, 0f);
            converter.SetDecimalValue(1.5f);
            Assert.That(converter.Value, Is.EqualTo(1.5f));
        }

        [Test]
        public void SetDecimalValue_WhenTypeMismatch_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            Assert.Throws<CVarTypeMismatchException>(() => converter.SetDecimalValue(1.5f));
        }

        #endregion

        #region SetStringValue

        [Test]
        public void SetStringValue_WhenTypeMatches_SetsValue()
        {
            var converter = new CVarConverter<string>(CVarType.String, null!);
            converter.SetStringValue("test");
            Assert.That(converter.Value, Is.EqualTo("test"));
        }

        [Test]
        public void SetStringValue_WhenTypeMismatch_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            Assert.Throws<CVarTypeMismatchException>(() => converter.SetStringValue("test"));
        }

        #endregion

        #region SetValue<T1>

        [Test]
        public void SetValue_WhenTypesMatch_AssignsViaUnsafeAs()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            converter.SetValue(42); // T1 = int
            Assert.That(converter.Value, Is.EqualTo(42));
        }

        [Test]
        public void SetValue_WhenTypesAreDifferentButConvertible_UsesIsCheck()
        {
            // int can be assigned to object? Not directly, but we can test with base types.
            var converter = new CVarConverter<string>(CVarType.String, null!);
            converter.SetValue("hello"); // T1 = string, T = object
            Assert.That(converter.Value, Is.EqualTo("hello"));

            Assert.Throws<CVarTypeMismatchException>(() => converter.SetValue(42L));
        }

        [Test]
        public void SetValue_WhenValueIsNotAssignable_Throws()
        {
            var converter = new CVarConverter<int>(CVarType.Int, 0);
            Assert.Throws<CVarTypeMismatchException>(() => converter.SetValue("not an int"));
        }

        #endregion
    }
}