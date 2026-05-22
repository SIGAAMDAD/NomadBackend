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
using Moq;
using Nomad.Core.Engine.Services;
using Nomad.Core.Logger;
using Nomad.FileSystem.Private.Services;
using Nomad.FileSystem.Private.FileStreams;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.FileSystem.Configs;

namespace Nomad.FileSystem.Tests
{
    [TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Streams.File")]
    [Category("Unit")]
    public class FileReadStreamTests
    {
        private FileSystemService _service;
        private FileSystemServiceTestFixture _fixture;
        private string _tempDir;
        private string _filePath;
        private byte[] _testData;

        [SetUp]
        public void SetUp()
        {
            _fixture = new FileSystemServiceTestFixture();
            _service = _fixture.Service;
            _tempDir = _fixture.TempDir;

            _testData = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            _filePath = _fixture.GetPath("readtest.bin");
            File.WriteAllBytes(_filePath, _testData);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        private IReadStream OpenReadStream()
        {
            var config = new FileReadConfig { FilePath = "readtest.bin" };
            return _service.OpenRead(config);
        }

        [Test]
        public void Create_HasCorrectMetadata()
        {
            using var stream = OpenReadStream() as IFileReadStream;

            Assert.That(stream, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stream.CanRead, Is.True);
                Assert.That(stream.CanWrite, Is.False);
                Assert.That(stream.CanSeek, Is.True);
                Assert.That(stream.IsOpen, Is.True);
                Assert.That(stream.FilePath, Is.EqualTo(_filePath));
            }
        }

        [Test]
        public void DisposeTwice_DoesNotThrow()
        {
            using var stream = OpenReadStream();

            stream.Dispose();
            Assert.DoesNotThrow(() => stream.Dispose());
        }

        [Test]
        public void GetLength_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenReadStream();
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.Length);
        }

        [Test]
        public void SetLength_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenReadStream();
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => writer.Length = 0);
        }

        [Test]
        public void GetPosition_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenReadStream();
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.Position);
        }

        [Test]
        public void GetFilePath_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenReadStream() as IFileReadStream;
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.FilePath);
        }

        [Test]
        public void WriteToStream_WritesCorrectAndAllData()
        {
            {
                using var writeStream = _service.OpenWrite(new FileWriteConfig { FilePath = Path.Combine(_tempDir, "test.bin"), Format = StreamFormat.Binary });
                using var readStream = OpenReadStream();
                readStream.WriteToStream(writeStream);
            }
            Assert.That(_testData, Is.EqualTo(File.ReadAllBytes(Path.Combine(_tempDir, "test.bin"))));
        }

        [Test]
        public async Task WriteToStreamAsync_WritesCorrectAndAllData()
        {
            {
                using var writeStream = await _service.OpenWriteAsync(new FileWriteConfig { FilePath = Path.Combine(_tempDir, "test.bin"), Format = StreamFormat.Binary });
                using var readStream = OpenReadStream();
                await readStream.WriteToStreamAsync(writeStream);
            }
            Assert.That(_testData, Is.EqualTo(File.ReadAllBytes(Path.Combine(_tempDir, "test.bin"))));
        }

        [Test]
        public void Read_ReadsAllBytes()
        {
            using var stream = OpenReadStream();
            byte[] buffer = new byte[_testData.Length];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public void Read_BoundsNotSpecified_ReadsAllBytes()
        {
            using var stream = OpenReadStream();
            byte[] buffer = new byte[_testData.Length];
            int bytesRead = stream.Read(buffer);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public void Read_PartialRead_AdvancesPosition()
        {
            using var stream = OpenReadStream();
            byte[] buffer = new byte[4];
            int bytesRead = stream.Read(buffer, 0, 4);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(4));
                Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
                Assert.That(stream.Position, Is.EqualTo(4));
            }
        }

        [Test]
        public void Read_BeyondEnd_ReturnsZero()
        {
            using var stream = OpenReadStream();
            stream.Position = _testData.Length;
            byte[] buffer = new byte[4];
            int bytesRead = stream.Read(buffer, 0, 4);
            Assert.That(bytesRead, Is.Zero);
        }

        [Test]
        public void Read_WithSpan_ReadsAllBytes()
        {
            using var stream = OpenReadStream();
            Span<byte> span = new byte[5];
            int bytesRead = stream.Read(span, 0, 5);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(5));
                Assert.That(span.ToArray(), Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
            }
        }

        [Test]
        public void Read_WithSpanBoundsNotSpecified_ReadsAllBytes()
        {
            using var stream = OpenReadStream();
            Span<byte> span = new byte[5];
            int bytesRead = stream.Read(span);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(5));
                Assert.That(span.ToArray(), Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
            }
        }

        [Test]
        public async Task ReadAsync_ReadsAllBytes()
        {
            using var stream = OpenReadStream();
            byte[] buffer = new byte[_testData.Length];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public async Task ReadAsync_BoundsNotSpecified_ReadsAllBytes()
        {
            using var stream = OpenReadStream();
            byte[] buffer = new byte[_testData.Length];
            int bytesRead = await stream.ReadAsync(buffer);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bytesRead, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public void ReadToEnd_ReadsRemaining()
        {
            using var stream = OpenReadStream();
            stream.Position = 3;
            byte[] remaining = stream.ReadToEnd();
            Assert.That(remaining, Is.EqualTo(new byte[] { 3, 4, 5, 6, 7, 8, 9 }));
        }

        [Test]
        public async ValueTask ReadToEndAsync_ReadsRemaining()
        {
            using var stream = OpenReadStream();
            stream.Position = 3;
            byte[] remaining = await stream.ReadToEndAsync();
            Assert.That(remaining, Is.EqualTo(new byte[] { 3, 4, 5, 6, 7, 8, 9 }));
        }

        [Test]
        public void ReadToEnd_LengthIsGreaterThanInteger32Max_ThrowsInvalidOperationException()
        {
            Assert.Ignore("Cannot reliably test buffers bigger than 32-bit integers");
        }

        [Test]
        public async ValueTask ReadToEndAsync_LengthIsGreaterThanInteger32Max_ThrowsInvalidOperationException()
        {
            Assert.Ignore("Cannot reliably test buffers bigger than 32-bit integers");
        }

        [Test]
        public void ToArray_ReadsEntireFileWithoutMovingPosition()
        {
            using var stream = OpenReadStream();
            stream.Position = 5;
            byte[] all = stream.ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(all, Is.EqualTo(_testData));
                Assert.That(stream.Position, Is.EqualTo(5));
            }
        }

        [Test]
        public void Seek_SetsPosition()
        {
            using var stream = OpenReadStream();
            long newPos = stream.Seek(4, SeekOrigin.Begin);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(newPos, Is.EqualTo(4));
                Assert.That(stream.Position, Is.EqualTo(4));
            }

            newPos = stream.Seek(2, SeekOrigin.Current);
            Assert.That(newPos, Is.EqualTo(6));

            newPos = stream.Seek(-2, SeekOrigin.End);
            Assert.That(newPos, Is.EqualTo(8));
        }

        [Test]
        public void ReadFloat_ReturnsSameValueAndAdvancesStream()
        {
            float value = float.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            float b = stream.ReadFloat();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(float)));
            }
        }

        [Test]
        public void ReadDouble_ReturnsSameValueAndAdvancesStream()
        {
            double value = double.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            double b = stream.ReadDouble();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(double)));
            }
        }

        [Test]
        public void ReadFloat32_ReturnsSameValueAndAdvancesStream()
        {
            float value = float.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            float b = stream.ReadFloat32();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(float)));
            }
        }

        [Test]
        public void ReadSingle_ReturnsSameValueAndAdvancesStream()
        {
            float value = float.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            float b = stream.ReadSingle();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(float)));
            }
        }

        [Test]
        public void ReadFloat64_ReturnsSameValueAndAdvancesStream()
        {
            double value = double.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            double b = stream.ReadFloat64();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(double)));
            }
        }

        [Test]
        public void ReadUInt8_ReturnsSameValueAndAdvancesStream()
        {
            byte value = byte.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), [value]);
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            byte b = stream.ReadUInt8();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(byte)));
            }
        }

        [Test]
        public void ReadUInt16_ReturnsSameValueAndAdvancesStream()
        {
            ushort value = ushort.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            ushort b = stream.ReadUInt16();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ushort)));
            }
        }

        [Test]
        public void ReadUInt32_ReturnsSameValueAndAdvancesStream()
        {
            uint value = uint.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            uint b = stream.ReadUInt32();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(uint)));
            }
        }

        [Test]
        public void ReadUInt64_ReturnsSameValueAndAdvancesStream()
        {
            ulong value = ulong.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            ulong b = stream.ReadUInt64();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ulong)));
            }
        }

        [Test]
        public void ReadByte_ReturnsSameValueAndAdvancesStream()
        {
            byte value = byte.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), [value]);
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            byte b = stream.ReadByte();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(byte)));
            }
        }

        [Test]
        public void ReadUShort_ReturnsSameValueAndAdvancesStream()
        {
            ushort value = ushort.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            ushort b = stream.ReadUShort();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ushort)));
            }
        }

        [Test]
        public void ReadUInt_ReturnsSameValueAndAdvancesStream()
        {
            uint value = uint.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            uint b = stream.ReadUInt();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(uint)));
            }
        }

        [Test]
        public void ReadULong_ReturnsSameValueAndAdvancesStream()
        {
            ulong value = ulong.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            ulong b = stream.ReadULong();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ulong)));
            }
        }

        [Test]
        public void ReadInt8_ReturnsSameValueAndAdvancesStream()
        {
            sbyte value = sbyte.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), [(byte)value]);
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            sbyte b = stream.ReadInt8();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(sbyte)));
            }
        }

        [Test]
        public void ReadInt16_ReturnsSameValueAndAdvancesStream()
        {
            short value = short.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            short b = stream.ReadInt16();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(short)));
            }
        }

        [Test]
        public void ReadInt32_ReturnsSameValueAndAdvancesStream()
        {
            int value = int.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            int b = stream.ReadInt32();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(int)));
            }
        }

        [Test]
        public void ReadInt64_ReturnsSameValueAndAdvancesStream()
        {
            long value = long.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            long b = stream.ReadInt64();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(long)));
            }
        }

        [Test]
        public void ReadSByte_ReturnsSameValueAndAdvancesStream()
        {
            sbyte value = sbyte.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), [(byte)value]);
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            sbyte b = stream.ReadSByte();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(sbyte)));
            }
        }

        [Test]
        public void ReadShort_ReturnsSameValueAndAdvancesStream()
        {
            short value = short.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            short b = stream.ReadShort();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(short)));
            }
        }

        [Test]
        public void ReadInt_ReturnsSameValueAndAdvancesStream()
        {
            int value = int.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            int b = stream.ReadInt();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(int)));
            }
        }

        [Test]
        public void ReadLong_ReturnsSameValueAndAdvancesStream()
        {
            long value = long.MaxValue;
            {
                File.WriteAllBytes(Path.Combine(_tempDir, "test.bin"), BitConverter.GetBytes(value));
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            long b = stream.ReadLong();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
                Assert.That(stream.Position, Is.EqualTo(sizeof(long)));
            }
        }

        [Test]
        public void Read7BitEncodedInt_ReturnsSameValue()
        {
            int value = 128;
            {
                using var writer = new BinaryWriter(new FileStream(Path.Combine(_tempDir, "test.bin"), FileMode.Create, FileAccess.Write));
                writer.Write7BitEncodedInt(value);
            }
            using var stream = _service.OpenRead(new FileReadConfig { FilePath = "test.bin" });
            int b = stream.Read7BitEncodedInt();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(b, Is.EqualTo(value));
            }
        }

        [Test]
        public void ReadString_ReadsLengthPrefixedString()
        {
            // Write a string via BinaryWriter to a separate file
            string testString = "Hello, world!";
            string stringFilePath = Path.Combine(_tempDir, "stringtest.bin");
            using (var bw = new BinaryWriter(File.OpenWrite(stringFilePath)))
            {
                bw.Write(testString);
            }

            var config = new FileReadConfig { FilePath = stringFilePath };
            using var stream = _service.OpenRead(config) as FileReadStream;
            string read = stream.ReadString();
            Assert.That(read, Is.EqualTo(testString));
        }

        [Test]
        public void ReadBoolean_ReturnsBool()
        {
            // Prepare file with bool (1 byte)
            string boolFilePath = Path.Combine(_tempDir, "booltest.bin");
            File.WriteAllBytes(boolFilePath, [1]);
            var config = new FileReadConfig { FilePath = boolFilePath };
            using var stream = _service.OpenRead(config) as FileReadStream;
            bool val = stream.ReadBoolean();
            Assert.That(val, Is.True);
        }
    }
}
