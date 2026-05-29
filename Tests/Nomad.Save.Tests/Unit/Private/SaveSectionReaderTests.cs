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
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Logger;
using Nomad.Core.Memory.Buffers;
using Nomad.Events;
using Nomad.FileSystem.Private.MemoryStream;
using Nomad.FileSystem.Private.Services;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Services;
using Nomad.Save.Services;
using Nomad.Core.CVars;
using Nomad.CVars.Private.Services;
using Moq;
using Nomad.Core.Engine.Services;
using Nomad.Save.Private.Entities;

namespace Nomad.Save.Tests
{
    /// <summary>
    /// Tests specifically for SaveSectionReader functionality
    /// </summary>
    [TestFixture]
    [Category("Nomad.Save")]
    [Category("Sections.Reading")]
    [Category("Unit")]
    public class SaveSectionReaderTests
    {
        private ISaveDataProvider _dataProvider;
        private ILoggerService _logger;
        private ICVarSystemService _cvarSystem;
        private IFileSystem _fileSystem;
        private IGameEventRegistryService _eventFactory;
        private Mock<IEngineService> _engineService;
        private string _testDirectory;

        private IGameEvent<SaveBeginEventArgs> _saveBegin;
        private IGameEvent<LoadBeginEventArgs> _loadBegin;

        [SetUp]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "NomadSaveSectionReaderTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            Directory.CreateDirectory($"{_testDirectory}/SaveData");

            _logger = new MockLogger();
            _engineService = new Mock<IEngineService>();
            _engineService.Setup(e => e.GetStoragePath(StorageScope.StreamingAssets)).Returns(_testDirectory);
            _engineService.Setup(e => e.GetStoragePath(StorageScope.UserData)).Returns(_testDirectory);
            _engineService.Setup(e => e.GetStoragePath(StorageScope.Install)).Returns(_testDirectory);
            _fileSystem = new FileSystemService(_engineService.Object, _logger);
            _eventFactory = new GameEventRegistry(_logger);
            _cvarSystem = new CVarSystem(_eventFactory, _logger);
            _dataProvider = new SaveDataProvider(_engineService.Object, _eventFactory, _cvarSystem, _fileSystem, _logger);

            _saveBegin = _eventFactory.GetEvent<SaveBeginEventArgs>(SaveBeginEventArgs.Name, SaveBeginEventArgs.NameSpace);
            _loadBegin = _eventFactory.GetEvent<LoadBeginEventArgs>(LoadBeginEventArgs.Name, LoadBeginEventArgs.NameSpace);
        }

        [TearDown]
        public void TearDown()
        {
            _saveBegin?.Dispose();
            _loadBegin?.Dispose();
            _dataProvider?.Dispose();
            _cvarSystem?.Dispose();
            _logger?.Dispose();
            _fileSystem?.Dispose();
            _eventFactory?.Dispose();

            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Test]
        public void Constructor_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var config = new SaveConfig();
            using var stream = new MemoryReadStream(new MemoryReadConfig
            {
                Buffer = new SharedBufferHandle(Array.Empty<byte>(), 0),
                MaxCapacity = 8192
            });
            var category = new Mock<ILoggerCategory>().Object;

            Assert.Throws<ArgumentOutOfRangeException>(() => new SaveSectionReader(config, -1, stream, category));
        }

        [Test]
        public async Task SaveSectionReader_Name_ReturnsCorrectSectionName()
        {
            // Arrange
            string fileId = "section_name_test";
            string expectedSectionName = "MyTestSection";
            string readSectionName = String.Empty;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection(expectedSectionName);
                section.AddField("TestField", 42);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection(expectedSectionName);
                if (section != null)
                {
                    readSectionName = section.Name;
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(readSectionName, Is.EqualTo(expectedSectionName));
        }

        [Test]
        public async Task SaveSectionReader_FieldCount_ReturnsCorrectFieldCount()
        {
            // Arrange
            string fileId = "field_count_test";
            int readFieldCount = 0;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("FieldCountTest");
                section.AddField("Field1", 1);
                section.AddField("Field2", 2);
                section.AddField("Field3", 3);
                section.AddField("Field4", 4);
                section.AddField("Field5", 5);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("FieldCountTest");
                if (section != null)
                {
                    readFieldCount = section.FieldCount;
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(readFieldCount, Is.EqualTo(5));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithIntType_ReturnsCorrectValue()
        {
            // Arrange
            string fileId = "get_int_field_test";
            int expectedValue = 12345;
            int retrievedValue = 0;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("IntTest");
                section.AddField("IntValue", expectedValue);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("IntTest");
                if (section != null)
                {
                    retrievedValue = section.GetField<int>("IntValue");
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithFloatType_ReturnsCorrectValue()
        {
            // Arrange
            string fileId = "get_float_field_test";
            float expectedValue = 3.14159f;
            float retrievedValue = 0.0f;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("FloatTest");
                section.AddField("FloatValue", expectedValue);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("FloatTest");
                if (section != null)
                {
                    retrievedValue = section.GetField<float>("FloatValue");
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(expectedValue).Within(0.00001f));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithDoubleType_ReturnsCorrectValue()
        {
            // Arrange
            string fileId = "get_double_field_test";
            double expectedValue = 2.71828;
            double retrievedValue = 0d;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("DoubleTest");
                section.AddField("DoubleValue", expectedValue);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("DoubleTest");
                if (section != null)
                {
                    retrievedValue = section.GetField<double>("DoubleValue");
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(expectedValue).Within(0.00001));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithStringType_ReturnsCorrectValue()
        {
            // Arrange
            string fileId = "get_string_field_test";
            string expectedValue = "Hello, Save System!";
            string retrievedValue = String.Empty;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("StringTest");
                section.AddField("StringValue", expectedValue);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("StringTest");
                if (section != null)
                {
                    retrievedValue = section.GetString("StringValue");
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithBoolType_ReturnsCorrectValue()
        {
            // Arrange
            string fileId = "get_bool_field_test";
            bool expectedValue = true;
            bool retrievedValue = false;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("BoolTest");
                section.AddField("BoolValue", expectedValue);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("BoolTest");
                if (section != null)
                {
                    retrievedValue = section.GetField<bool>("BoolValue");
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithNonExistentField_ReturnsDefault()
        {
            // Arrange
            string fileId = "nonexistent_field_test";
            int retrievedValue = int.MinValue;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("NoFieldTest");
                section.AddField("ExistingField", 100);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("NoFieldTest");
                if (section != null)
                {
                    retrievedValue = section.GetField<int>("NonExistentField");
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.Default);
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithWrongType_ThrowsInvalidCastException()
        {
            // Arrange
            string fileId = "wrong_type_test";
            bool exceptionThrown = false;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("WrongTypeTest");
                section.AddField("IntField", 42);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                var section = args.Reader.FindSection("WrongTypeTest");
                if (section != null)
                {
                    try
                    {
                        // Try to read int as string
                        var stringValue = section.GetString("IntField");
                    }
                    catch (InvalidCastException)
                    {
                        exceptionThrown = true;
                    }
                }
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(exceptionThrown, Is.True);
        }

        [Test]
        public async Task SaveSectionReader_Dispose_PreventsFurtherAccess()
        {
            // Arrange
            string fileId = "dispose_test";
            ISaveSectionReader? sectionReader = null;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("DisposeTest");
                section.AddField("TestField", 99);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                sectionReader = args.Reader.FindSection("DisposeTest");
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            var fieldCountBeforeDispose = sectionReader?.FieldCount ?? 0;
            sectionReader?.Dispose();

            // Assert
            Assert.That(fieldCountBeforeDispose, Is.GreaterThan(0));
            Assert.Throws<ObjectDisposedException>(() => _ = sectionReader!.FieldCount);
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithAllIntegerTypes_ReturnsCorrectValues()
        {
            // Arrange
            string fileId = "all_int_types_test";
            sbyte sbyte_val = (sbyte)-50;
            short short_val = (short)-1000;
            int int_val = -100000;
            long long_val = -9000000000000000L;
            byte byte_val = (byte)200;
            ushort ushort_val = (ushort)50000;
            uint uint_val = 3000000000U;
            ulong ulong_val = 15000000000000000UL;

            ISaveSectionReader? reader = null;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("IntTypes");
                section.AddField("sbyte", sbyte_val);
                section.AddField("short", short_val);
                section.AddField("int", int_val);
                section.AddField("long", long_val);
                section.AddField("byte", byte_val);
                section.AddField("ushort", ushort_val);
                section.AddField("uint", uint_val);
                section.AddField("ulong", ulong_val);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                reader = args.Reader.FindSection("IntTypes");
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(reader, Is.Not.Null);
            if (reader != null)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(reader.GetField<sbyte>("sbyte"), Is.EqualTo(sbyte_val));
                    Assert.That(reader.GetField<short>("short"), Is.EqualTo(short_val));
                    Assert.That(reader.GetField<int>("int"), Is.EqualTo(int_val));
                    Assert.That(reader.GetField<long>("long"), Is.EqualTo(long_val));
                    Assert.That(reader.GetField<byte>("byte"), Is.EqualTo(byte_val));
                    Assert.That(reader.GetField<ushort>("ushort"), Is.EqualTo(ushort_val));
                    Assert.That(reader.GetField<uint>("uint"), Is.EqualTo(uint_val));
                    Assert.That(reader.GetField<ulong>("ulong"), Is.EqualTo(ulong_val));
                }
            }
        }

        [Test]
        public async Task SaveSectionReader_GetField_MultipleTimes_ReturnsSameValue()
        {
            // Arrange
            string fileId = "multiple_reads_test";
            ISaveSectionReader? sectionReader = null;
            int expectedValue = 777;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("MultiRead");
                section.AddField("Value", expectedValue);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                sectionReader = args.Reader.FindSection("MultiRead");
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            if (sectionReader != null)
            {
                var firstRead = sectionReader.GetField<int>("Value");
                var secondRead = sectionReader.GetField<int>("Value");
                var thirdRead = sectionReader.GetField<int>("Value");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(firstRead, Is.EqualTo(expectedValue));
                    Assert.That(secondRead, Is.EqualTo(expectedValue));
                    Assert.That(thirdRead, Is.EqualTo(expectedValue));
                }
            }
        }

        [Test]
        public async Task SaveSectionReader_Name_IsNotNull()
        {
            // Arrange
            string fileId = "name_not_null_test";
            ISaveSectionReader? sectionReader = null;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("ValidSection");
                section.AddField("TestField", 1);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                sectionReader = args.Reader.FindSection("ValidSection");
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(sectionReader, Is.Not.Null);
            if (sectionReader != null)
            {
                Assert.That(sectionReader.Name, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task SaveSectionReader_FieldCount_IsNonNegative()
        {
            // Arrange
            string fileId = "field_count_nonneg_test";
            ISaveSectionReader? sectionReader = null;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("CountTest");
                section.AddField("Field1", 1);
                section.AddField("Field2", 2);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                sectionReader = args.Reader.FindSection("CountTest");
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(sectionReader, Is.Not.Null);
            if (sectionReader != null)
            {
                Assert.That(sectionReader.FieldCount, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithEmptyStringField_ReturnsEmptyString()
        {
            // Arrange
            string fileId = "empty_string_field_test";
            string retrievedValue = "unchanged";

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("EmptyStringTest");
                section.AddField("EmptyString", String.Empty);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                var section = args.Reader.FindSection("EmptyStringTest");
                if (section != null)
                {
                    retrievedValue = section.GetString("EmptyString");
                }
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(String.Empty));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithLongString_ReturnsFullString()
        {
            // Arrange
            string fileId = "long_string_field_test";
            int length = 1000;
            string longString = new string('X', length);
            string retrievedValue = "";

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("LongStringTest");
                section.AddField("LongString", longString);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                var section = args.Reader.FindSection("LongStringTest");
                if (section != null)
                {
                    retrievedValue = section.GetString("LongString");
                }
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(longString));
            Assert.That(retrievedValue, Has.Length.EqualTo(length));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task SaveSectionReader_GetField_WithBoolValues_ReturnsExactValue(bool boolValue)
        {
            // Arrange
            string fileId = $"bool_test_{boolValue}";
            bool retrievedValue = !boolValue; // Opposite to verify it changes

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("BoolTest");
                section.AddField("BoolValue", boolValue);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                var section = args.Reader.FindSection("BoolTest");
                if (section != null)
                {
                    retrievedValue = section.GetField<bool>("BoolValue");
                }
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(boolValue));
        }

        [Test]
        public async Task SaveSectionReader_GetField_WithNegativeValues_ReturnsCorrectValues()
        {
            // Arrange
            string fileId = "negative_values_test";
            int negInt = -999;
            double negDouble = -123.456;
            int retrievedInt = 0;
            double retrievedDouble = 0;

            _saveBegin.Subscribe((in SaveBeginEventArgs args) =>
            {
                var section = args.Writer.AddSection("NegativeTest");
                section.AddField("NegInt", negInt);
                section.AddField("NegDouble", negDouble);
            });

            _loadBegin.Subscribe((in LoadBeginEventArgs args) =>
            {
                var section = args.Reader.FindSection("NegativeTest");
                if (section != null)
                {
                    retrievedInt = section.GetField<int>("NegInt");
                    retrievedDouble = section.GetField<double>("NegDouble");
                }
            });

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(retrievedInt, Is.EqualTo(negInt));
                Assert.That(retrievedDouble, Is.EqualTo(negDouble).Within(0.001));
            }
        }

        [Test]
        public async Task SaveSectionReader_Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            string fileId = "dispose_multiple_test";
            ISaveSectionReader? sectionReader = null;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("DisposeMultipleTest");
                section.AddField("TestField", 1);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                sectionReader = args.Reader.FindSection("DisposeMultipleTest");
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act & Assert
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            if (sectionReader != null)
            {
                sectionReader.Dispose();
                sectionReader.Dispose(); // Should not throw
                sectionReader.Dispose(); // Should not throw

                Assert.Pass("Multiple dispose calls handled");
            }
        }

        [Test]
        public async Task SaveSectionReader_Name_MatchesSectionWriterName()
        {
            // Arrange
            string fileId = "name_match_test";
            string writerSectionName = "NameMatchSection";
            string readerSectionName = "";

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection(writerSectionName);
                Assert.That(section.Name, Is.EqualTo(writerSectionName));
                section.AddField("TestField", 1);
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection(writerSectionName);
                if (section != null)
                {
                    readerSectionName = section.Name;
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(readerSectionName, Is.EqualTo(writerSectionName));
        }

        [Test]
        public async Task SaveSectionReader_FieldCount_MatchesSectionWriterFieldCount()
        {
            // Arrange
            string fileId = "field_count_match_test";
            int writerFieldCount = 0;
            int readerFieldCount = 0;

            void OnSaveBegin(in SaveBeginEventArgs args)
            {
                var section = args.Writer.AddSection("CountMatchTest");
                for (int i = 0; i < 10; i++)
                {
                    section.AddField($"Field_{i}", i);
                }
                writerFieldCount = section.FieldCount;
            }

            _saveBegin.Subscribe(OnSaveBegin);

            void OnLoadBegin(in LoadBeginEventArgs args)
            {
                var section = args.Reader.FindSection("CountMatchTest");
                if (section != null)
                {
                    readerFieldCount = section.FieldCount;
                }
            }

            _loadBegin.Subscribe(OnLoadBegin);

            // Act
            await _dataProvider.Save(fileId, default);
            await _dataProvider.Load(fileId);

            // Assert
            Assert.That(readerFieldCount, Is.EqualTo(writerFieldCount));
            Assert.That(readerFieldCount, Is.EqualTo(10));
        }
    }
}
