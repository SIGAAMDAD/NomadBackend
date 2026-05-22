using System;
using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.CVars.Exceptions;
using Nomad.CVars.Private.Entities;
using Nomad.Events;

namespace Nomad.CVars.Tests
{
    [TestFixture]
    [Category("Nomad.CVars")]
    [Category("ValueObjects")]
    [Category("Unit")]
    public class CVarTests
    {
        private IGameEventRegistryService _eventFactory;
        private CVarCreateInfo<int> _intCreateInfo;
        private CVarCreateInfo<string> _stringCreateInfo;
        private CVarCreateInfo<bool> _boolCreateInfo;
        private CVarCreateInfo<float> _floatCreateInfo;
        private CVarCreateInfo<uint> _uintCreateInfo;

        [SetUp]
        public void SetUp()
        {
            var logger = new MockLogger();
            _eventFactory = new GameEventRegistry(logger);
            _intCreateInfo = new CVarCreateInfo<int>
            {
                Name = "test.int",
                Description = "Integer test",
                DefaultValue = 42,
                Validator = null
            };
            _stringCreateInfo = new CVarCreateInfo<string>
            {
                Name = "test.string",
                Description = "String test",
                DefaultValue = "default",
                Validator = null
            };
            _boolCreateInfo = new CVarCreateInfo<bool>
            {
                Name = "test.bool",
                Description = "Boolean test",
                DefaultValue = true,
                Validator = null
            };
            _floatCreateInfo = new CVarCreateInfo<float>
            {
                Name = "test.float",
                Description = "Float test",
                DefaultValue = 3.14f,
                Validator = null
            };
            _uintCreateInfo = new CVarCreateInfo<uint>
            {
                Name = "test.uint",
                Description = "UInt test",
                DefaultValue = 100u,
                Validator = null
            };
        }

        [TearDown]
        public void TearDown()
        {
            _eventFactory?.Dispose();
        }

        // -------------------------------------------------------------------------
        // Constructor Tests
        // -------------------------------------------------------------------------

        [Test]
        public void Constructor_WithValidInfo_SetsPropertiesCorrectly()
        {
            // Arrange
            var info = _intCreateInfo with { Flags = CVarFlags.Archive };

            // Act
            var cvar = new CVar<int>(_eventFactory, info);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cvar.Name, Is.EqualTo(info.Name));
                Assert.That(cvar.Description, Is.EqualTo(info.Description));
                Assert.That(cvar.DefaultValue, Is.EqualTo(info.DefaultValue));
                Assert.That(cvar.Value, Is.EqualTo(info.DefaultValue));
                Assert.That(cvar.Flags, Is.EqualTo(info.Flags));
                Assert.That(cvar.Type, Is.EqualTo(CVarType.Int));
                Assert.That(cvar.IsReadOnly, Is.False);
                Assert.That(cvar.IsSaved, Is.True);  // Archive flag set
                Assert.That(cvar.IsHidden, Is.False);
                Assert.That(cvar.IsUserCreated, Is.False);
                Assert.That(cvar.IsDeveloper, Is.False);
                Assert.That(cvar.IsInitializationOnly, Is.False);
            }
        }

        [Test]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var info = _intCreateInfo with { Name = null! };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CVar<int>(_eventFactory, info));
        }

        [Test]
        public void Constructor_WithEmptyName_ThrowsArgumentNullException()
        {
            // Arrange
            var info = _intCreateInfo with { Name = "" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new CVar<int>(_eventFactory, info));
        }

        [Test]
        public void Constructor_WithNullDescription_ThrowsArgumentNullException()
        {
            // Arrange
            var info = _intCreateInfo with { Description = null! };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CVar<int>(_eventFactory, info));
        }

        [Test]
        public void Constructor_WithUnsupportedType_ThrowsInvalidCastException()
        {
            // Arrange
            var unsupportedInfo = new CVarCreateInfo<UnsupportedClass>
            {
                Name = "bad",
                Description = "bad",
                DefaultValue = null!,
                Flags = CVarFlags.None,
                Validator = null
            };

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => new CVar<UnsupportedClass>(_eventFactory, unsupportedInfo));
        }

        private class UnsupportedClass { }

        // -------------------------------------------------------------------------
        // Property Tests (Flags and derived boolean properties)
        // -------------------------------------------------------------------------

        [Test]
        public void IsReadOnly_WhenReadOnlyFlagSet_ReturnsTrue()
        {
            // Arrange
            var info = _uintCreateInfo with { Flags = CVarFlags.ReadOnly }; // ReadOnly set

            // Act
            var cvar = new CVar<uint>(_eventFactory, info);

            // Assert
            Assert.That(cvar.IsReadOnly, Is.True);
        }

        [Test]
        public void IsSaved_WhenArchiveFlagSet_ReturnsTrue()
        {
            // Arrange
            var info = _intCreateInfo with { Flags = CVarFlags.Archive }; // Archive set

            // Act
            var cvar = new CVar<int>(_eventFactory, info);

            // Assert
            Assert.That(cvar.IsSaved, Is.True);
        }

        [Test]
        public void IsHidden_WhenHiddenFlagSet_ReturnsTrue()
        {
            // Arrange
            var info = _boolCreateInfo with { Flags = CVarFlags.Hidden }; // Hidden set

            // Act
            var cvar = new CVar<bool>(_eventFactory, info);

            // Assert
            Assert.That(cvar.IsHidden, Is.True);
        }

        [Test]
        public void IsDeveloper_WhenDeveloperFlagSet_ReturnsTrue()
        {
            // Arrange
            var info = _floatCreateInfo with { Flags = CVarFlags.Developer }; // Developer set

            // Act
            var cvar = new CVar<float>(_eventFactory, info);

            // Assert
            Assert.That(cvar.IsDeveloper, Is.True);
        }

        // (Similar tests for IsUserCreated and IsInitializationOnly can be added if needed)

        // -------------------------------------------------------------------------
        // Value Assignment Tests
        // -------------------------------------------------------------------------

        [Test]
        public void Set_NewValue_UpdatesValueAndPublishesEvent()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Value = 100;

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cvar.Value, Is.EqualTo(100));
                Assert.That(fakeEvent.PublishCount, Is.EqualTo(1));
                Assert.That(fakeEvent.LastPayload.OldValue, Is.EqualTo(42));
                Assert.That(fakeEvent.LastPayload.NewValue, Is.EqualTo(100));
            }
        }

        [Test]
        public void Set_SameValue_DoesNotChangeValueNorPublishEvent()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Value = 42;

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cvar.Value, Is.EqualTo(42));
                Assert.That(fakeEvent.PublishCount, Is.Zero);
            }
        }

        [Test]
        public void Set_WhenReadOnly_DoesNotChangeValue()
        {
            // Arrange
            var cvar = new CVar<uint>(_eventFactory, _uintCreateInfo with { Flags = CVarFlags.ReadOnly }); // ReadOnly
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Value = 200u;

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cvar.Value, Is.EqualTo(100u)); // unchanged
                Assert.That(fakeEvent.PublishCount, Is.Zero);
            }
        }

        [Test]
        public void Set_WhenValidatorRejects_DoesNotChangeValue()
        {
            // Arrange
            var info = _intCreateInfo with { Validator = x => x > 0 }; // only positive
            var cvar = new CVar<int>(_eventFactory, info);
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Value = -5;

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cvar.Value, Is.EqualTo(42)); // unchanged
                Assert.That(fakeEvent.PublishCount, Is.Zero);
            }
        }

        [Test]
        public void Set_WhenValidatorAccepts_UpdatesValue()
        {
            // Arrange
            var info = _intCreateInfo with { Validator = x => x > 0 };
            var cvar = new CVar<int>(_eventFactory, info);
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Value = 10;

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cvar.Value, Is.EqualTo(10));
                Assert.That(fakeEvent.PublishCount, Is.EqualTo(1));
            }
        }

        // -------------------------------------------------------------------------
        // Reset Tests
        // -------------------------------------------------------------------------

        [Test]
        public void Reset_SetsValueToDefault()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo)
            {
                Value = 100
            };

            // Act
            cvar.Reset();

            // Assert
            Assert.That(cvar.Value, Is.EqualTo(42));
        }

        [Test]
        public void Reset_WhenValueAlreadyDefault_DoesNotPublishEvent()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Reset();

            // Assert
            Assert.That(fakeEvent.PublishCount, Is.Zero);
        }

        [Test]
        public void Reset_WhenReadOnly_DoesNotChangeValue()
        {
            // Arrange
            var cvar = new CVar<uint>(_eventFactory, _uintCreateInfo with { Flags = CVarFlags.ReadOnly })
            {
                Value = 200u,
            };
            var fakeEvent = cvar.ValueChanged;

            // Act
            cvar.Reset();

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(cvar.Value, Is.EqualTo(100u)); // unchanged
                Assert.That(fakeEvent.PublishCount, Is.Zero);
            }
        }

        // -------------------------------------------------------------------------
        // SetFromString Tests
        // -------------------------------------------------------------------------

        [Test]
        public void SetFromString_ValidIntString_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act
            cvar.SetFromString("123");

            // Assert
            Assert.That(cvar.Value, Is.EqualTo(123));
        }

        [Test]
        public void SetFromString_InvalidIntString_ThrowsArgumentException()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cvar.SetFromString("not a number"));
        }

        [Test]
        public void SetFromString_ValidBoolString_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<bool>(_eventFactory, _boolCreateInfo);

            // Act
            cvar.SetFromString("false");

            // Assert
            Assert.That(cvar.Value, Is.False);
        }

        [Test]
        public void SetFromString_ValidFloatString_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<float>(_eventFactory, _floatCreateInfo);

            // Act
            cvar.SetFromString("2.718");

            // Assert
            Assert.That(cvar.Value, Is.EqualTo(2.718f).Within(0.001f));
        }

        [Test]
        public void SetFromString_ValidString_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<string>(_eventFactory, _stringCreateInfo);

            // Act
            cvar.SetFromString("new value");

            // Assert
            Assert.That(cvar.Value, Is.EqualTo("new value"));
        }

        [Test]
        public void SetFromString_NullString_ThrowsArgumentNullException()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cvar.SetFromString(null!));
        }

        // -------------------------------------------------------------------------
        // Typed Getter Tests
        // -------------------------------------------------------------------------

        [Test]
        public void GetIntegerValue_WhenTypeIsInt_ReturnsValue()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act
            var result = cvar.GetIntegerValue();

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void GetIntegerValue_WhenTypeIsNotInt_Throws()
        {
            // Arrange
            var cvar = new CVar<bool>(_eventFactory, _boolCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.GetIntegerValue());
        }

        [Test]
        public void GetUIntegerValue_WhenTypeIsUInt_ReturnsValue()
        {
            // Arrange
            var cvar = new CVar<uint>(_eventFactory, _uintCreateInfo);

            // Act
            var result = cvar.GetUIntegerValue();

            // Assert
            Assert.That(result, Is.EqualTo(100u));
        }

        [Test]
        public void GetUIntegerValue_WhenTypeIsNotUInt_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.GetUIntegerValue());
        }

        [Test]
        public void GetBooleanValue_WhenTypeIsBool_ReturnsValue()
        {
            // Arrange
            var cvar = new CVar<bool>(_eventFactory, _boolCreateInfo);

            // Act
            var result = cvar.GetBooleanValue();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetBooleanValue_WhenTypeIsNotBool_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.GetBooleanValue());
        }

        [Test]
        public void GetDecimalValue_WhenTypeIsFloat_ReturnsValue()
        {
            // Arrange
            var cvar = new CVar<float>(_eventFactory, _floatCreateInfo);

            // Act
            var result = cvar.GetDecimalValue();

            // Assert
            Assert.That(result, Is.EqualTo(3.14f).Within(0.001f));
        }

        [Test]
        public void GetDecimalValue_WhenTypeIsNotFloat_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.GetDecimalValue());
        }

        [Test]
        public void GetStringValue_WhenTypeIsString_ReturnsValue()
        {
            // Arrange
            var cvar = new CVar<string>(_eventFactory, _stringCreateInfo);

            // Act
            var result = cvar.GetStringValue();

            // Assert
            Assert.That(result, Is.EqualTo("default"));
        }

        [Test]
        public void GetStringValue_WhenTypeIsNotString_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.GetStringValue());
        }

        // -------------------------------------------------------------------------
        // Typed Setter Tests
        // -------------------------------------------------------------------------

        [Test]
        public void SetIntegerValue_WhenTypeIsInt_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act
            cvar.SetIntegerValue(99);

            // Assert
            Assert.That(cvar.Value, Is.EqualTo(99));
        }

        [Test]
        public void SetIntegerValue_WhenTypeIsNotInt_Throws()
        {
            // Arrange
            var cvar = new CVar<bool>(_eventFactory, _boolCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.SetIntegerValue(99));
        }

        [Test]
        public void SetUIntegerValue_WhenTypeIsUInt_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<uint>(_eventFactory, _uintCreateInfo);

            // Act
            cvar.SetUIntegerValue(200u);

            // Assert
            Assert.That(cvar.Value, Is.EqualTo(200u));
        }

        [Test]
        public void SetUIntegerValue_WhenTypeIsNotUInt_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.SetUIntegerValue(200u));
        }

        [Test]
        public void SetBooleanValue_WhenTypeIsBool_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<bool>(_eventFactory, _boolCreateInfo);

            // Act
            cvar.SetBooleanValue(false);

            // Assert
            Assert.That(cvar.Value, Is.False);
        }

        [Test]
        public void SetBooleanValue_WhenTypeIsNotBool_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.SetBooleanValue(false));
        }

        [Test]
        public void SetDecimalValue_WhenTypeIsFloat_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<float>(_eventFactory, _floatCreateInfo);

            // Act
            cvar.SetDecimalValue(1.23f);

            // Assert
            Assert.That(cvar.Value, Is.EqualTo(1.23f));
        }

        [Test]
        public void SetDecimalValue_WhenTypeIsNotFloat_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.SetDecimalValue(1.23f));
        }

        [Test]
        public void SetStringValue_WhenTypeIsString_UpdatesValue()
        {
            // Arrange
            var cvar = new CVar<string>(_eventFactory, _stringCreateInfo);

            // Act
            cvar.SetStringValue("new");

            // Assert
            Assert.That(cvar.Value, Is.EqualTo("new"));
        }

        [Test]
        public void SetStringValue_WhenTypeIsNotString_Throws()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act & Assert
            Assert.Throws<CVarTypeMismatchException>(() => cvar.SetStringValue("new"));
        }

        // -------------------------------------------------------------------------
        // Implicit Conversion Tests
        // -------------------------------------------------------------------------

        [Test]
        public void ImplicitConversion_ToT_ReturnsCurrentValue()
        {
            // Arrange
            CVar<int> cvar = new CVar<int>(_eventFactory, _intCreateInfo);

            // Act
            int value = cvar;

            // Assert
            Assert.That(value, Is.EqualTo(42));
        }

        // -------------------------------------------------------------------------
        // Event Subscription Tests (via IGameEvent)
        // -------------------------------------------------------------------------

        [Test]
        public void ValueChanged_CanBeSubscribedAndReceivesNotifications()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);
            var fakeEvent = cvar.ValueChanged;
            int receivedOld = 0, receivedNew = 0;
            EventCallback<CVarValueChangedEventArgs<int>> handler = (in CVarValueChangedEventArgs<int> args) =>
            {
                receivedOld = args.OldValue;
                receivedNew = args.NewValue;
            };
            fakeEvent.OnPublished += handler;

            // Act
            cvar.Value = 100;

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(receivedOld, Is.EqualTo(42));
                Assert.That(receivedNew, Is.EqualTo(100));
            }

            // Cleanup
            fakeEvent.OnPublished -= handler;
        }

        [Test]
        public void MultipleSubscribers_AllReceiveNotifications()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);
            var fakeEvent = cvar.ValueChanged;
            int count1 = 0, count2 = 0;
            EventCallback<CVarValueChangedEventArgs<int>> handler1 = (in CVarValueChangedEventArgs<int> _) => count1++;
            EventCallback<CVarValueChangedEventArgs<int>> handler2 = (in CVarValueChangedEventArgs<int> _) => count2++;
            fakeEvent.OnPublished += handler1;
            fakeEvent.OnPublished += handler2;

            // Act
            cvar.Value = 100;

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(count1, Is.EqualTo(1));
                Assert.That(count2, Is.EqualTo(1));
            }

            // Cleanup
            fakeEvent.OnPublished -= handler1;
            fakeEvent.OnPublished -= handler2;
        }

        [Test]
        public void UnsubscribedHandler_DoesNotReceiveNotifications()
        {
            // Arrange
            var cvar = new CVar<int>(_eventFactory, _intCreateInfo);
            var fakeEvent = cvar.ValueChanged;
            int receivedCount = 0;
            EventCallback<CVarValueChangedEventArgs<int>> handler = (in CVarValueChangedEventArgs<int> _) => receivedCount++;
            fakeEvent.OnPublished += handler;
            fakeEvent.OnPublished -= handler;

            // Act
            cvar.Value = 100;

            // Assert
            Assert.That(receivedCount, Is.Zero);
        }
    }
}