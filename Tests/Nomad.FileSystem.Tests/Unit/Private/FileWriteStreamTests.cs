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
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.FileSystem.Private.Services;
using Nomad.FileSystem.Private.FileStreams;

namespace Nomad.FileSystem.Tests
{
    [TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Streams.File")]
    [Category("Unit")]
    public class FileWriteStreamTests
    {
        private FileSystemService _service;
        private FileSystemServiceTestFixture _fixture;
        private string _tempDir;
        private string _filePath;

        [SetUp]
        public void SetUp()
        {
            _fixture = new FileSystemServiceTestFixture();
            _service = _fixture.Service;
            _tempDir = _fixture.TempDir;
            _filePath = _fixture.GetPath("writetest.bin");
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        private IWriteStream OpenWriteStream(bool append = false)
        {
            var config = new FileWriteConfig { FilePath = _filePath, Append = append };
            return _service.OpenWrite(config);
        }

        [Test]
        public void Create_HasCorrectMetadata()
        {
            using var stream = OpenWriteStream() as IFileWriteStream;

            Assert.That(stream, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stream, Is.InstanceOf<FileWriteStream>());
                Assert.That(stream.CanRead, Is.False);
                Assert.That(stream.CanWrite, Is.True);
            }
        }

        [Test]
        public void Create_WithInvalidStreamFormat_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileWriteStream(new FileWriteConfig{ FilePath = _filePath, Format = (StreamFormat)128 }));
        }

        [Test]
        public void Create_AndDisposeTwice_DoesNotThrow()
        {
            using var writer = OpenWriteStream();
            writer.Dispose();
            Assert.DoesNotThrow(() => writer.Dispose());
        }

        [Test]
        public void GetCanSeek_IsTrue()
        {
            using var writer = OpenWriteStream();
            Assert.That(writer.CanSeek, Is.True);
        }

        [Test]
        public void GetCanSeek_AfterDisposed_ThrowsObjectDisposedException()
        {
            using var writer = OpenWriteStream();
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.CanSeek);
        }

        [Test]
        public void GetIsOpen_IsTrue()
        {
            using var writer = OpenWriteStream() as IFileWriteStream;
            Assert.That(writer.IsOpen, Is.True);
        }

        [Test]
        public void GetLength_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenWriteStream();
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.Length);
        }

        [Test]
        public void GetPosition_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenWriteStream();
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.Position);
        }

        [Test]
        public void GetFilePath_AfterDispose_ThrowsObjectDisposedException()
        {
            using var writer = OpenWriteStream() as IFileWriteStream;
            writer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = writer.FilePath);
        }

        [Test]
        public void Write_WritesBytes()
        {
            byte[] data = [1, 2, 3, 4, 5];
            using (var stream = OpenWriteStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(data));
        }

        [Test]
        public void Write_WithoutBoundsSpecified_WritesBytes()
        {
            byte[] data = [1, 2, 3, 4, 5];
            using (var stream = OpenWriteStream())
            {
                stream.Write(data);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(data));
        }

        [Test]
        public void Write_SpanOverload_Writes()
        {
            Span<byte> data = [10, 20, 30];
            using (var stream = OpenWriteStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(new byte[] { 10, 20, 30 }));
        }

        [Test]
        public void Write_SpanOverloadWithoutBounds_Writes()
        {
            Span<byte> data = [10, 20, 30];
            using (var stream = OpenWriteStream())
            {
                stream.Write(data);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(new byte[] { 10, 20, 30 }));
        }

        [Test]
        public async Task WriteAsync_WritesBytes()
        {
            byte[] data = [100, 200, 255];
            using (var stream = OpenWriteStream())
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(data));
        }

        [Test]
        public async Task WriteAsync_WithoutBoundsSpecified_WritesBytes()
        {
            byte[] data = [100, 200, 255];
            using (var stream = OpenWriteStream())
            {
                await stream.WriteAsync(data);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(data));
        }

        [Test]
        public async Task WriteAsync_MemoryOverload_WritesBytes()
        {
            ReadOnlyMemory<byte> data = new byte[] { 100, 200, 255 };
            using (var stream = OpenWriteStream())
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(data.ToArray()));
        }

        [Test]
        public async Task WriteAsync_MemoryOverloadWithoutBoundsSpecified_WritesBytes()
        {
            ReadOnlyMemory<byte> data = new byte[] { 100, 200, 255 };
            using (var stream = OpenWriteStream())
            {
                await stream.WriteAsync(data);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(data.ToArray()));
        }

        [Test]
        public void AppendMode_AddsToEnd()
        {
            File.WriteAllBytes(_filePath, [1, 2, 3]);
            using (var stream = OpenWriteStream(append: true))
            {
                stream.Write([4, 5, 6], 0, 3);
            }

            var written = File.ReadAllBytes(_filePath);
            Assert.That(written, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void WriteFromStream_CopiesData()
        {
            // Create a source file
            string sourcePath = Path.Combine(_tempDir, "source.bin");
            byte[] sourceData = [7, 8, 9, 10];
            {
                File.WriteAllBytes(sourcePath, sourceData);
            }
            {

                var readConfig = new FileReadConfig { FilePath = sourcePath };
                using var sourceStream = _service.OpenRead(readConfig);

                using var destStream = OpenWriteStream();
                destStream.WriteFromStream(sourceStream!);
            }
            {

                var written = File.ReadAllBytes(_filePath);
                Assert.That(written, Is.EqualTo(sourceData));
            }
        }

        [Test]
        public async Task WriteFromStreamAsync_CopiesData()
        {
            string sourcePath = Path.Combine(_tempDir, "source.bin");
            byte[] sourceData = [11, 12, 13];
            {
                File.WriteAllBytes(sourcePath, sourceData);
            }
            {
                var readConfig = new FileReadConfig { FilePath = sourcePath };
                using var sourceStream = _service.OpenRead(readConfig);
                using var destStream = OpenWriteStream();
                await destStream.WriteFromStreamAsync(sourceStream!);
            }
            {
                var written = File.ReadAllBytes(_filePath);
                Assert.That(written, Is.EqualTo(sourceData));
            }
        }

        [Test]
        public void WriteUInt8_WritesCorrectValueAdvancesStream()
        {
            byte value = byte.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteUInt8(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(byte)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(byte)));
            }
            {
                byte written = File.ReadAllBytes(_filePath)[0];
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUInt16_WritesCorrectValueAdvancesStream()
        {
            ushort value = ushort.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteUInt16(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(ushort)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ushort)));
            }
            {
                var written = BitConverter.ToUInt16(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUInt32_WritesCorrectValueAdvancesStream()
        {
            uint value = uint.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteUInt32(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(uint)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(uint)));
            }
            {
                var written = BitConverter.ToUInt32(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUInt64_WritesCorrectValueAdvancesStream()
        {
            ulong value = ulong.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteUInt64(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(ulong)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ulong)));
            }
            {
                var written = BitConverter.ToUInt64(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteByte_WritesCorrectValueAdvancesStream()
        {
            byte value = byte.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteByte(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(byte)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(byte)));
            }
            {
                byte written = File.ReadAllBytes(_filePath)[0];
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUShort_WritesCorrectValueAdvancesStream()
        {
            ushort value = ushort.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteUShort(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(ushort)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ushort)));
            }
            {
                var written = BitConverter.ToUInt16(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUInt_WritesCorrectValueAdvancesStream()
        {
            uint value = uint.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteUInt(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(uint)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(uint)));
            }
            {
                var written = BitConverter.ToUInt32(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteULong_WritesCorrectValueAdvancesStream()
        {
            ulong value = ulong.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteULong(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(ulong)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(ulong)));
            }
            {
                var written = BitConverter.ToUInt64(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteInt8_WritesCorrectValueAdvancesStream()
        {
            sbyte value = sbyte.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteInt8(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(sbyte)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(sbyte)));
            }
            {
                sbyte written = (sbyte)File.ReadAllBytes(_filePath)[0];
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteInt16_WritesCorrectValueAdvancesStream()
        {
            short value = short.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteInt16(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(short)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(short)));
            }
            {
                var written = BitConverter.ToInt16(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteInt32_WritesCorrectValueAdvancesStream()
        {
            int value = int.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteInt32(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(int)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(int)));
            }
            {
                var written = BitConverter.ToInt32(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteInt64_WritesCorrectValueAdvancesStream()
        {
            long value = long.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteInt64(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(long)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(long)));
            }
            {
                var written = BitConverter.ToInt64(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteSByte_WritesCorrectValueAdvancesStream()
        {
            sbyte value = sbyte.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteSByte(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(sbyte)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(sbyte)));
            }
            {
                sbyte written = (sbyte)File.ReadAllBytes(_filePath)[0];
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteShort_WritesCorrectValueAdvancesStream()
        {
            short value = short.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteShort(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(short)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(short)));
            }
            {
                var written = BitConverter.ToInt16(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteInt_WritesCorrectValueAdvancesStream()
        {
            int value = int.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteInt(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(int)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(int)));
            }
            {
                var written = BitConverter.ToInt32(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteLong_WritesCorrectValueAdvancesStream()
        {
            long value = long.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteLong(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(long)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(long)));
            }
            {
                var written = BitConverter.ToInt64(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteFloat_WritesCorrectValueAdvancesStream()
        {
            float value = float.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteFloat(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(float)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(float)));
            }
            {
                var written = BitConverter.ToSingle(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteDouble_WritesCorrectValueAdvancesStream()
        {
            double value = double.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteDouble(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(double)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(double)));
            }
            {
                var written = BitConverter.ToDouble(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteSingle_WritesCorrectValueAdvancesStream()
        {
            float value = float.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteSingle(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(float)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(float)));
            }
            {
                var written = BitConverter.ToSingle(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteFloat32_WritesCorrectValueAdvancesStream()
        {
            float value = float.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteFloat32(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(float)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(float)));
            }
            {
                var written = BitConverter.ToSingle(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteFloat64_WritesCorrectValueAdvancesStream()
        {
            double value = double.MaxValue;
            {
                using var stream = OpenWriteStream();
                stream.WriteFloat64(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(double)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(double)));
            }
            {
                var written = BitConverter.ToDouble(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteBoolean_WritesCorrectValueAdvancesStream()
        {
            bool value = true;
            {
                using var stream = OpenWriteStream();
                stream.WriteBoolean(value);
                Assert.That(stream.Length, Is.EqualTo(sizeof(bool)));
                Assert.That(stream.Position, Is.EqualTo(sizeof(bool)));
            }
            {
                var written = BitConverter.ToBoolean(File.ReadAllBytes(_filePath));
                Assert.That(written, Is.EqualTo(value));
            }
        }

        [Test]
        public void Write7BitEncodedInt_WritesCorrectValue()
        {
            int value = 123456;
            {
                using var stream = OpenWriteStream();
                stream.Write7BitEncodedInt(value);
            }
            {
                using var reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                Assert.That(reader.Read7BitEncodedInt(), Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteString_WritesLengthPrefixedString()
        {
            {
                using var stream = OpenWriteStream();
                stream.WriteString("Test");
            }
            {
                // Read back via BinaryReader
                using var br = new BinaryReader(File.OpenRead(_filePath));
                string read = br.ReadString();
                Assert.That(read, Is.EqualTo("Test"));
            }
        }

        [Test]
        public void Flush_EnsuresDataWritten()
        {
            {
                using var stream = OpenWriteStream();
                stream.Write([1, 2, 3], 0, 3);
                stream.Flush();
            }
            {
                // Even without disposing, file should exist
                Assert.That(File.Exists(_filePath));
                var written = File.ReadAllBytes(_filePath);
                Assert.That(written, Is.EqualTo(new byte[] { 1, 2, 3 }));
            }
        }

        [Test]
        public void WriteUtf8String_WithString_WritesCorrectData()
        {
            string value = "test  \t";
            {
                using var stream = _service.OpenWrite(new FileWriteConfig { FilePath = _filePath, Format = StreamFormat.Utf8 }) as IFileWriteStream;
                stream.WriteString(value);
            }
            {
                using var stream = new StreamReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                Assert.That(stream.ReadLine(), Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUtf8Line_WithSpan_WritesCorrectData()
        {
            string value = "test  \t";
            {
                using var stream = _service.OpenWrite(new FileWriteConfig { FilePath = _filePath, Format = StreamFormat.Utf8 }) as IFileWriteStream;
                stream.WriteLine(value.AsSpan());
            }
            {
                using var stream = new StreamReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                Assert.That(stream.ReadLine(), Is.EqualTo(value));
            }
        }

        [Test]
        public void WriteUtf8Line_WithString_WritesCorrectData()
        {
            string value = "test  \t";
            {
                using var stream = _service.OpenWrite(new FileWriteConfig { FilePath = _filePath, Format = StreamFormat.Utf8 }) as IFileWriteStream;
                stream.WriteLine(value);
            }
            {
                using var stream = new StreamReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                Assert.That(stream.ReadLine(), Is.EqualTo(value));
            }
        }

        [Test]
        public async Task WriteUtf8LineAsync_WithString_WritesCorrectData()
        {
            string value = "test  \t";
            {
                using var stream = await _service.OpenWriteAsync(new FileWriteConfig { FilePath = _filePath, Format = StreamFormat.Utf8 }) as IFileWriteStream;
                await stream.WriteLineAsync(value);
            }
            {
                using var stream = new StreamReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                Assert.That(stream.ReadLine(), Is.EqualTo(value));
            }
        }

        [Test]
        public async Task WriteUtf8LineAsync_WithMemory_WritesCorrectData()
        {
            string value = "test  \t";
            {
                using var stream = await _service.OpenWriteAsync(new FileWriteConfig { FilePath = _filePath, Format = StreamFormat.Utf8 }) as IFileWriteStream;
                await stream.WriteLineAsync(value.AsMemory());
            }
            {
                using var stream = new StreamReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                Assert.That(stream.ReadLine(), Is.EqualTo(value));
            }
        }
    }
}
