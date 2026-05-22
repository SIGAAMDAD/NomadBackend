using System;
using System.IO;
using System.Text;
using NSubstitute;
using NUnit.Framework;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.FileSystem.Private.MemoryStream;
using Nomad.Save.Exceptions;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.ValueObjects;
using Nomad.Core.FileSystem.Configs;

namespace Nomad.Save.Tests
{
    [TestFixture]
    [Category("Nomad.Save")]
    [Category("Sections.Corruption")]
    [Category("Unit")]
    public class SaveSectionCorruptionTests
    {
        private string _tempFile;
        private SaveConfig _config;
        private ILoggerService _logger;
        private ILoggerCategory _category;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.GetTempFileName(); // creates a zero-length file
            _config = new SaveConfig
            {
                LogSerializationTree = false,
                DebugLogging = false,
                ChecksumEnabled = true // enable checksums for validation
            };
            _logger = Substitute.For<ILoggerService>();
            _category = Substitute.For<ILoggerCategory>();
        }

        [TearDown]
        public void TearDown()
        {
            _category?.Dispose();
            _logger?.Dispose();
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        /// <summary>
        /// Writes a simple valid save file with one section and one field.
        /// </summary>
        private void WriteValidFile()
        {
            using var writer = new MemoryFileWriteStream(new MemoryFileWriteConfig { FilePath = _tempFile, InitialCapacity = 8192 }); // allocate initial capacity
            var sectionWriter = new SaveSectionWriter(_config, _category, "TestSection", writer);
            {
                var header = new SaveHeader("testfile", default, 1, Checksum.Empty);
                header.Serialize(writer);
            }
            sectionWriter.AddField("Health", 100);
            sectionWriter.Dispose(); // writes section header and data, then flushes to disk

            long length = writer.Position;
            {
                writer.Seek(0, SeekOrigin.Begin);
                var header = new SaveHeader("testfile", default, 1, Checksum.Compute(writer.Buffer.Span));
                header.Serialize(writer);
                writer.Seek(length, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads the first section from the file and returns the field value.
        /// </summary>
        private int ReadHealthFromFile()
        {
            using var reader = new MemoryFileReadStream(new MemoryFileReadConfig { FilePath = _tempFile, MaxCapacity = 1024 });
            var header = SaveHeader.Deserialize(reader, out _);
            var section = new SaveSectionReader(_config, 0, reader, _logger.CreateCategory(nameof(SaveSectionReader), LogLevel.Info, true));
            return section.GetField<int>("Health");
        }

        [Test]
        public void ReadWrite_ValidFile_Succeeds()
        {
            WriteValidFile();
            int health = ReadHealthFromFile();
            Assert.That(health, Is.EqualTo(100));
        }

        [Test]
        public void ReadWrite_CorruptSectionHeaderChecksum_ThrowsSectionCorruptException()
        {
            WriteValidFile();

            // Corrupt the section header checksum (bytes 4-11 after the global header)
            byte[] data = File.ReadAllBytes(_tempFile);
            // The file layout: global header (magic 8, version 3x uint, name string, sectionCount int, checksum ulong)
            // Then section header: byteLength int, checksum ulong, name string, fieldCount int
            // We need to locate the section header checksum. Simpler: just corrupt a byte in the checksum area.
            // We know the section header starts after the global header. We'll approximate by flipping a byte after position 50.
            if (data.Length > 60)
            {
                data[55] ^= 0xFF; // flip some bits
                File.WriteAllBytes(_tempFile, data);
            }

            using var reader = new MemoryFileReadStream(new MemoryFileReadConfig { FilePath = _tempFile });
            var globalHeader = SaveHeader.Deserialize(reader, out _);
            Assert.Throws<SectionCorruptException>(() =>
            {
                var section = new SaveSectionReader(_config, 0, reader, _logger.CreateCategory(nameof(SaveSectionReader), LogLevel.Info, true));
            });
        }

        [Test]
        public void ReadWrite_CorruptFieldData_ThrowsSectionCorruptException()
        {
            WriteValidFile();

            {
                // Corrupt the field data (the int value 100) inside the section
                byte[] data = File.ReadAllBytes(_tempFile);
                for (int i = 50; i < data.Length; i++)
                {
                    data[i] ^= 0xFF;
                }
                File.WriteAllBytes(_tempFile, data);
            }

            using var reader = new MemoryFileReadStream(new MemoryFileReadConfig { FilePath = _tempFile });
            var globalHeader = SaveHeader.Deserialize(reader, out _);
            // The section reader will compute the checksum of the entire section data and compare with the stored checksum.
            // Since we corrupted data, the checksum should mismatch, causing SectionCorruptException.
            Assert.Throws<SectionCorruptException>(() =>
            {
                var section = new SaveSectionReader(_config, 0, reader, _logger.CreateCategory(nameof(SaveSectionReader), LogLevel.Info, true));
            });
        }

        [Test]
        public void ReadWrite_TruncatedFile_ThrowsEndOfStreamException()
        {
            WriteValidFile();

            // Truncate the file to half its size
            byte[] data = File.ReadAllBytes(_tempFile);
            Array.Resize(ref data, data.Length / 2);
            File.WriteAllBytes(_tempFile, data);

            using var reader = new MemoryFileReadStream(new MemoryFileReadConfig { FilePath = _tempFile });
            Assert.Throws<EndOfStreamException>(() => SaveHeader.Deserialize(reader, out _));
        }

        [Test]
        public void ReadWrite_DuplicateFieldName_ThrowsDuplicateFieldException()
        {
            // Write a file with duplicate field names using SaveSectionWriter (which should prevent it,
            // but we'll manually craft a file with duplicates)
            using var writer = new MemoryFileWriteStream(new MemoryFileWriteConfig { FilePath = _tempFile, InitialCapacity = 1024 });
            var sectionWriter = new SaveSectionWriter(_config, _category, "TestSection", writer);
            sectionWriter.AddField("Health", 100);
            // Cannot add another "Health" because AddField checks for duplicates.
            // So we'll manually write the section data by accessing the underlying stream after disposal? Not easy.
            // Instead, we'll create a file with two fields with same name by using raw writes.
            // This tests the reader's duplicate detection.

            // Build the raw data manually
            var memStream = new MemoryStream();
            // Global header (placeholder)
            memStream.Write(BitConverter.GetBytes(0x5f3759df67217274UL), 0, 8);
            memStream.Write(BitConverter.GetBytes(1U), 0, 4); // major
            memStream.Write(BitConverter.GetBytes(0U), 0, 4); // minor
            memStream.Write(BitConverter.GetBytes(2U), 0, 4); // patch
            var nameBytes = Encoding.UTF8.GetBytes("TestSave");
            memStream.WriteByte((byte)nameBytes.Length);
            memStream.Write(nameBytes, 0, nameBytes.Length);
            memStream.Write(BitConverter.GetBytes(1), 0, 4); // sectionCount
            memStream.Write(BitConverter.GetBytes(0UL), 0, 8); // global checksum (placeholder)

            // Section header
            int sectionDataStart = (int)memStream.Position + 4 + 8; // after byteLength and checksum
            memStream.Write(BitConverter.GetBytes(0), 0, 4); // byteLength placeholder
            memStream.Write(BitConverter.GetBytes(0UL), 0, 8); // checksum placeholder
            var sectionNameBytes = Encoding.UTF8.GetBytes("TestSection");
            memStream.WriteByte((byte)sectionNameBytes.Length);
            memStream.Write(sectionNameBytes, 0, sectionNameBytes.Length);
            memStream.Write(BitConverter.GetBytes(2), 0, 4); // fieldCount = 2

            // Field 1
            var fieldNameBytes = Encoding.UTF8.GetBytes("Health");
            memStream.WriteByte((byte)fieldNameBytes.Length);
            memStream.Write(fieldNameBytes, 0, fieldNameBytes.Length);
            memStream.WriteByte((byte)AnyType.Int32); // type
            memStream.Write(BitConverter.GetBytes(100), 0, 4); // value

            // Field 2 (same name)
            memStream.WriteByte((byte)fieldNameBytes.Length);
            memStream.Write(fieldNameBytes, 0, fieldNameBytes.Length);
            memStream.WriteByte((byte)AnyType.Int32);
            memStream.Write(BitConverter.GetBytes(200), 0, 4);

            // Compute section checksum
            byte[] allData = memStream.ToArray();
            int sectionDataLength = allData.Length - sectionDataStart;
            var sectionChecksum = Checksum.Compute(new ReadOnlySpan<byte>(allData, sectionDataStart, sectionDataLength));

            // Write back the section header with correct length and checksum
            using (var fs = new FileStream(_tempFile, FileMode.Create, FileAccess.Write))
            {
                fs.Write(allData, 0, allData.Length);
                fs.Seek(sectionDataStart - 4 - 8, SeekOrigin.Begin); // go back to byteLength position
                fs.Write(BitConverter.GetBytes(sectionDataLength), 0, 4);
                fs.Write(BitConverter.GetBytes(sectionChecksum.Value), 0, 8);
            }

            // Now read the file
            using var reader = new MemoryFileReadStream(new MemoryFileReadConfig { FilePath = _tempFile });
            var global = SaveHeader.Deserialize(reader, out _);
            Assert.Throws<DuplicateFieldException>(() =>
            {
                var section = new SaveSectionReader(_config, 0, reader, _logger.CreateCategory(nameof(SaveSectionReader), LogLevel.Info, true));
            });
        }

        [Test]
        public void ReadWrite_CorruptGlobalMagic_ReportsMagicMismatch()
        {
            WriteValidFile();

            byte[] data = File.ReadAllBytes(_tempFile);
            // Corrupt the magic (first 8 bytes)
            data[0] ^= 0xFF;
            File.WriteAllBytes(_tempFile, data);

            using var reader = new MemoryFileReadStream(new MemoryFileReadConfig { FilePath = _tempFile });
            var header = SaveHeader.Deserialize(reader, out bool magicMatches);
            Assert.That(magicMatches, Is.False);
        }
    }
}