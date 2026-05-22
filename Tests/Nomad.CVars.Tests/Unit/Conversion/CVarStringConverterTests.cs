using System;
using NUnit.Framework;
using Nomad.CVars.Private.Entities;

namespace Nomad.CVars.Tests
{
    [TestFixture]
    [Category("Nomad.CVars")]
    [Category("Conversion")]
    [Category("Unit")]
    public class CVarStringConverterTests
    {
        private enum TestEnum
        {
            ValueA,
            ValueB
        }

        [TestCase(typeof(sbyte), "123", (sbyte)123)]
        [TestCase(typeof(short), "12345", (short)12345)]
        [TestCase(typeof(int), "123456", 123456)]
        [TestCase(typeof(long), "123456789", 123456789L)]
        [TestCase(typeof(byte), "255", (byte)255)]
        [TestCase(typeof(ushort), "65535", (ushort)65535)]
        [TestCase(typeof(uint), "4294967295", 4294967295u)]
        [TestCase(typeof(ulong), "18446744073709551615", 18446744073709551615ul)]
        [TestCase(typeof(float), "3.14", 3.14f)]
        [TestCase(typeof(double), "2.71828", 2.71828)]
        [TestCase(typeof(bool), "true", true)]
        [TestCase(typeof(bool), "false", false)]
        [TestCase(typeof(string), "hello", "hello")]
        [TestCase(typeof(TestEnum), "ValueA", TestEnum.ValueA)]
        [TestCase(typeof(TestEnum), "1", TestEnum.ValueB)] // underlying value
        public void TryParse_SupportedType_ReturnsTrueAndParsedValue(Type type, string input, object expected)
        {
            var result = CVarStringConverter.TryParse(input, type, out var parsed);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(parsed, Is.EqualTo(expected));
            }
        }

        [TestCase(typeof(int), "not a number")]
        [TestCase(typeof(float), "not a float")]
        [TestCase(typeof(bool), "maybe")]
        [TestCase(typeof(TestEnum), "ValueC")] // invalid name
        [TestCase(typeof(TestEnum), "999")]    // out of range
        public void TryParse_InvalidInput_ReturnsFalse(Type type, string input)
        {
            var result = CVarStringConverter.TryParse(input, type, out var parsed);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(parsed, Is.Null);
            }
        }

        [Test]
        public void TryParse_UnsupportedType_ThrowsNotSupportedException()
        {
            var unsupported = typeof(System.Text.StringBuilder);
            Assert.Throws<NotSupportedException>(() =>
                CVarStringConverter.TryParse("any", unsupported, out _));
        }

        [Test]
        public void TryParse_StringAlwaysSucceeds()
        {
            var result = CVarStringConverter.TryParse("anything", typeof(string), out var parsed);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(parsed, Is.EqualTo("anything"));
            }
        }
    }
}