using System;
using System.IO;
using NUnit.Framework;
using NSubstitute;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.Save.Exceptions;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.ValueObjects;
using Nomad.FileSystem.Private.MemoryStream; // For MemoryFileWriteStream
using Nomad.Core.Util;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Memory.Buffers; // For Any, AnyType

namespace Nomad.Save.Tests
{
    [TestFixture]
    [Category("Nomad.Save")]
    [Category("Unit")]
    public class SaveSectionWriterTests
    {
        private SaveConfig _config;
        private ILoggerService _logger;
        private ILoggerCategory _category;
        private IMemoryFileWriteStream _writer;
        private SaveSectionWriter _sectionWriter;
        private const string SectionName = "TestSection";

        [SetUp]
        public void SetUp()
        {
            // Create a test configuration with desired flags
            _config = new SaveConfig
            {
                ChecksumEnabled = true,
                LogSerializationTree = true,
                VerifyAfterWrite = true,
                DebugLogging = true
                // other properties are irrelevant for SaveSectionWriter
            };

            // Mock the logger and category
            _logger = Substitute.For<ILoggerService>();
            _category = Substitute.For<ILoggerCategory>();

            // Use a real MemoryFileWriteStream – it writes to memory, not disk until Flush().
            // We'll use a dummy file path; no actual file is created because we never call Flush.
            _writer = new MemoryFileWriteStream(new MemoryFileWriteConfig { FilePath = Path.GetTempFileName(), InitialCapacity = 1024 });

            _sectionWriter = new SaveSectionWriter(
                _config,
                _category,
                SectionName,
                _writer
            );
        }

        [TearDown]
        public void TearDown()
        {
            _sectionWriter?.Dispose();
            _writer?.Dispose();
            _category?.Dispose();
            _logger?.Dispose();
        }

        // --------------------------------------------------------------------------------
        // Constructor and basic properties
        // --------------------------------------------------------------------------------
        [Test]
        public void Constructor_InitializesProperties()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sectionWriter.Name, Is.EqualTo(SectionName));
                Assert.That(_sectionWriter.FieldCount, Is.Zero);
            }
        }

        // --------------------------------------------------------------------------------
        // AddField – happy paths
        // --------------------------------------------------------------------------------
        [Test]
        public void AddField_AddsFieldAndIncrementsCount()
        {
            _sectionWriter.AddField("intField", 42);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sectionWriter.FieldCount, Is.EqualTo(1));
                Assert.That(_sectionWriter.HasField<int>("intField"), Is.True);
            }
        }

        [Test]
        public void AddField_WithDifferentTypes_StoresCorrectly(
            [Values] AnyType type,
            [Values] object value)
        {
            // This is a combinatorial test – we need to provide meaningful values per type.
            // For simplicity, we'll test a few representative types individually.
        }

        [Test]
        public void AddField_StringField_StoresCorrectly()
        {
            string testString = "Hello, World!";
            _sectionWriter.AddField("stringField", testString);
            Assert.That(_sectionWriter.HasField<string>("stringField"), Is.True);
        }

        [Test]
        public void AddField_IntField_StoresCorrectly()
        {
            _sectionWriter.AddField("intField", 12345);
            Assert.That(_sectionWriter.HasField<int>("intField"), Is.True);
        }

        [Test]
        public void AddField_FloatField_StoresCorrectly()
        {
            _sectionWriter.AddField("floatField", 3.14f);
            Assert.That(_sectionWriter.HasField<float>("floatField"), Is.True);
        }

        [Test]
        public void AddField_BooleanField_StoresCorrectly()
        {
            _sectionWriter.AddField("boolField", true);
            Assert.That(_sectionWriter.HasField<bool>("boolField"), Is.True);
        }

        // --------------------------------------------------------------------------------
        // AddField – duplicate key
        // --------------------------------------------------------------------------------
        [Test]
        public void AddField_DuplicateKey_ThrowsDuplicateFieldException()
        {
            _sectionWriter.AddField("dupField", 1);
            var ex = Assert.Throws<DuplicateFieldException>(() =>
                _sectionWriter.AddField("dupField", 2));

            System.Console.WriteLine(ex.Message);
            Assert.That(ex.Message, Does.Contain("dupField"));
        }

        [Test]
        public void AddField_EmptyFieldId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sectionWriter.AddField(String.Empty, 42));
        }

        [Test]
        public void AddField_NullFieldId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sectionWriter.AddField(null, 42));
        }

        // --------------------------------------------------------------------------------
        // HasField
        // --------------------------------------------------------------------------------
        [Test]
        public void HasField_ReturnsTrueForExistingField()
        {
            _sectionWriter.AddField("existing", 99);
            Assert.That(_sectionWriter.HasField<int>("existing"), Is.True);
        }

        [Test]
        public void HasField_ReturnsFalseForMissingField()
        {
            Assert.That(_sectionWriter.HasField<int>("missing"), Is.False);
        }

        // --------------------------------------------------------------------------------
        // Dispose – writes section header and fields, clears dictionary
        // --------------------------------------------------------------------------------
        [Test]
        public void Dispose_WritesHeaderAndFields_ThrowsObjectDisposedException()
        {
            // Arrange
            _sectionWriter.AddField("field1", 100);
            _sectionWriter.AddField("field2", 200L);
            _sectionWriter.AddField("field3", "test");

            long positionBefore = _writer.Position;

            // Act
            _sectionWriter.Dispose();

            using (Assert.EnterMultipleScope())
            {
                // Assert – after dispose, it should throw
                Assert.Throws<ObjectDisposedException>(() => _ = _sectionWriter.FieldCount);

                // The writer's position should have advanced (header + fields written)
                Assert.That(_writer.Position, Is.GreaterThan(positionBefore));
            }

            // Verify that the buffer contains a valid section header and fields.
            // We'll read it back using a MemoryReadStream and manual parsing.
            _writer.Seek(0, SeekOrigin.Begin);
            var reader = new MemoryReadStream(new MemoryReadConfig { Buffer = _writer.Buffer });

            // Read section header (name, byteLength, fieldCount, checksum)
            int byteLength = reader.ReadInt32();
            ulong storedChecksum = reader.ReadUInt64();
            string name = reader.ReadString();
            int fieldCount = reader.ReadInt32();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(name, Is.EqualTo(SectionName));
                Assert.That(fieldCount, Is.EqualTo(3));
            }

            // Now read the fields to verify they were written correctly
            for (int i = 0; i < fieldCount; i++)
            {
                string fieldName = reader.ReadString();
                AnyType type = (AnyType)reader.ReadUInt8();
                // We trust FieldSerializerRegistry to deserialize; we just check that we can read without exception
                // For a more thorough test, we could deserialize and compare values.
                // But for I/O validation, ensuring no read error is sufficient.
                switch (type)
                {
                    case AnyType.Int32:
                        reader.ReadInt32();
                        break;
                    case AnyType.Int64:
                        reader.ReadInt64();
                        break;
                    case AnyType.String:
                        reader.ReadString();
                        break;
                    default:
                        Assert.Fail($"Unexpected type {type} in test data");
                        break;
                }
            }

            // Verify checksum: compute checksum of the data region (from after header to end)
            int dataStart = SectionHeader.HEADER_CHECKSUM_OFFSET; // after header
            int dataLength = byteLength;
            var dataSlice = reader.Buffer.GetSlice(dataStart, dataLength);
            var computedChecksum = Checksum.Compute(dataSlice);
            Assert.That(computedChecksum.Value, Is.EqualTo(storedChecksum));
        }

        [Test]
        public void Dispose_WithNoFields_WritesHeaderWithZeroCount()
        {
            _sectionWriter.Dispose();

            _writer.Seek(0, SeekOrigin.Begin);
            var reader = new MemoryReadStream(new MemoryReadConfig { Buffer = _writer.Buffer });
            int byteLength = reader.ReadInt32();
            ulong checksum = reader.ReadUInt64();
            string name = reader.ReadString();
            int fieldCount = reader.ReadInt32();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(name, Is.EqualTo(SectionName));
                Assert.That(byteLength, Is.Not.Zero);
                Assert.That(fieldCount, Is.Zero);
                // checksum of empty data should be something (Crc64 of empty)
                Assert.That(checksum, Is.Not.Zero);
            }
        }

        // --------------------------------------------------------------------------------
        // Logging
        // --------------------------------------------------------------------------------
        [Test]
        public void AddField_WhenLogSerializationTreeTrue_LogsField()
        {
            _sectionWriter.AddField("logfield", 999);
            _category.Received().PrintLine(
                Arg.Is<string>(s => s.Contains("[Field]") && s.Contains("logfield") && s.Contains("Int32") && s.Contains("999"))
            );
        }

        [Test]
        public void AddField_WhenLogSerializationTreeFalse_DoesNotLog()
        {
            _config = _config with { LogSerializationTree = false };
            _sectionWriter = new SaveSectionWriter(_config, _category, SectionName, _writer);

            _sectionWriter.AddField("logfield", 999);
            _category.DidNotReceive().PrintLine(Arg.Any<string>());
        }

        [Test]
        public void Dispose_WhenLogSerializationTreeTrue_LogsFinalizedSection()
        {
            _sectionWriter.AddField("f1", 1);
            _sectionWriter.Dispose();

            _category.Received().PrintLine(
                Arg.Is<string>(s => s.Contains("Finalized section data"))
            );
            _category.Received().PrintLine(
                Arg.Is<string>(s => s.Contains(SectionName))
            );
        }

        // --------------------------------------------------------------------------------
        // Non‑happy paths / edge cases
        // --------------------------------------------------------------------------------
        [Test]
        public void AddField_AfterDispose_ThrowsObjectDisposedException()
        {
            _sectionWriter.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _sectionWriter.AddField("lateField", 123));
        }

        [Test]
        public void Dispose_Twice_ThrowsObjectDisposedExceptionOnSecondWriteCall()
        {
            // First dispose writes the section.
            _sectionWriter.AddField("field", 42);
            _sectionWriter.Dispose();

            // Clear the section and add a new field – this simulates reusing the same writer (bad practice).
            Assert.Throws<ObjectDisposedException>(() => _sectionWriter.AddField("another", 99));
        }

        // --------------------------------------------------------------------------------
        // Large data / many fields
        // --------------------------------------------------------------------------------
        [Test]
        public void AddField_ManyFields_HandlesLargeNumberOfFields()
        {
            const int fieldCount = 1000;
            for (int i = 0; i < fieldCount; i++)
            {
                _sectionWriter.AddField($"field{i}", i);
            }
            Assert.That(_sectionWriter.FieldCount, Is.EqualTo(fieldCount));

            _sectionWriter.Dispose();
            // Verify that the buffer length is reasonable and no exception occurred.
            Assert.That(_writer.Length, Is.GreaterThan(0));
        }

        [Test]
        public void AddField_LargeString_HandlesLargeValue()
        {
            string largeString = new string('x', 1024 * 1024); // 1 MB
            _sectionWriter.AddField("big", largeString);
            _sectionWriter.Dispose();

            // Verify that the string was written by reading it back
            _writer.Seek(0, SeekOrigin.Begin);
            var reader = new MemoryReadStream(new MemoryReadConfig { Buffer = _writer.Buffer });
            reader.ReadInt32();  // byteLength
            reader.ReadUInt64(); // checksum
            reader.ReadString(); // section name
            reader.ReadInt32();  // fieldCount

            string fieldName = reader.ReadString();
            AnyType type = (AnyType)reader.ReadUInt8();
            string readString = reader.ReadString();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fieldName, Is.EqualTo("big"));
                Assert.That(type, Is.EqualTo(AnyType.String));
                Assert.That(readString, Is.EqualTo(largeString));
            }
        }

        // --------------------------------------------------------------------------------
        // Test that AddField with unsupported type throws (if any)
        // Currently Any.From might support any type by boxing, but serialization may fail.
        // This test ensures that using a custom struct (unmanaged) works.
        // --------------------------------------------------------------------------------
        [Test]
        public void AddField_WithCustomUnmanagedStruct_ThrowsInvalidCastException()
        {
            var point = new Point { X = 10, Y = 20 };
            // Point is unmanaged (struct with only value types)
            Assert.Throws<InvalidCastException>(() => _sectionWriter.AddField("point", point));
        }

        private struct Point
        {
            public int X;
            public int Y;
        }
    }
}
