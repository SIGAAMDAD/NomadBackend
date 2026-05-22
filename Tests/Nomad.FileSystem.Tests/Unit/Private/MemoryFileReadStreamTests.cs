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

#if !UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using Moq;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.FileSystem.Private.Services;
using Nomad.FileSystem.Private.MemoryStream;
using System.Threading.Tasks;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Memory.Buffers;

namespace Nomad.FileSystem.Tests
{
    [TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Streams.Memory")]
    [Category("Unit")]
    public class MemoryFileReadStreamTests
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

            _testData = new byte[] { 10, 20, 30, 40, 50 };
            _filePath = _fixture.GetPath("memread.bin");
            File.WriteAllBytes(_filePath, _testData);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        private IReadStream OpenMemoryFileReadStream()
        {
            var config = new MemoryFileReadConfig { FilePath = "memread.bin", MaxCapacity = 1024 };
            return _service.OpenRead(config);
        }

        [Test]
        public void Close_ClosesFile()
        {
            using var stream = OpenMemoryFileReadStream() as IMemoryFileReadStream;
            stream.Close();
            Assert.That(stream.IsOpen, Is.False);
        }

        [Test]
        public void Create_WithInvalidAllocationStrategy_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _service.OpenRead(new MemoryFileReadConfig{ FilePath = "memread.bin", MaxCapacity = 1024, Strategy = (AllocationStrategy)99 }));
        }

        [Test]
        public void Create_WithPooledBuffer_CreatesPooledBuffer()
        {
            using var stream = _service.OpenRead(new MemoryFileReadConfig { FilePath = "memread.bin", MaxCapacity = 1024, Strategy = AllocationStrategy.Pooled }) as IMemoryFileReadStream;
            Assert.That(stream.Buffer, Is.InstanceOf<PooledBufferHandle>());
        }

        [Test]
        public void Create_WithStandardBuffer_CreatesStandardBuffer()
        {
            using var stream = _service.OpenRead(new MemoryFileReadConfig { FilePath = "memread.bin", MaxCapacity = 1024, Strategy = AllocationStrategy.Standard }) as IMemoryFileReadStream;
            Assert.That(stream.Buffer, Is.InstanceOf<StandardBufferHandle>());
        }

        [Test]
        public void Create_WithFromFileBuffer_CreatesSharedBuffer()
        {
            using var stream = _service.OpenRead(new MemoryFileReadConfig { FilePath = "memread.bin", MaxCapacity = 1024, Strategy = AllocationStrategy.FromFile }) as IMemoryFileReadStream;
            Assert.That(stream.Buffer, Is.InstanceOf<SharedBufferHandle>());
        }

        [Test]
        public void Create_HasCorrectMetadata()
        {
            using var stream = OpenMemoryFileReadStream() as IMemoryFileReadStream;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stream, Is.Not.Null);
                Assert.That(stream, Is.InstanceOf<MemoryFileReadStream>());
                Assert.That(stream.CanWrite, Is.False);
                Assert.That(stream.CanRead, Is.True);
                Assert.That(stream.CanSeek, Is.True);
                Assert.That(stream.FilePath, Does.Contain(_filePath));
            }
        }

        [Test]
        public void Dispose_DisposeAgain_DoesNotThrow()
        {
            using var stream = OpenMemoryFileReadStream();
            stream.Dispose();
            Assert.DoesNotThrow(() => stream.Dispose());
        }

        [Test]
        public void GetBuffer_AfterDisposed_ThrowsObjectDisposedException()
        {
            using var stream = OpenMemoryFileReadStream() as IMemoryFileReadStream;
            stream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = stream.Buffer);
        }

        [Test]
        public void ReadSpan_ReadsAllBytes()
        {
            using var stream = OpenMemoryFileReadStream();
            Assert.That(stream, Is.Not.Null);

            Span<byte> buffer = stackalloc byte[_testData.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.EqualTo(_testData.Length));
                Assert.That(buffer.SequenceEqual(_testData.AsSpan()));
            }
        }

        [Test]
        public async Task ReadAsync_ReadsAllBytes()
        {
            using var stream = OpenMemoryFileReadStream();
            Assert.That(stream, Is.Not.Null);

            Memory<byte> buffer = new byte[_testData.Length];
            int read = await stream.ReadAsync(buffer);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.EqualTo(_testData.Length));
                Assert.That(buffer.ToArray(), Is.EqualTo(_testData));
            }
        }

        [Test]
        public void ReadBytes_ReadsAllBytes()
        {
            using var stream = OpenMemoryFileReadStream();
            Assert.That(stream, Is.Not.Null);

            byte[] buffer = new byte[_testData.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public void ReadBytes_WithoutSpecifiedBounds_ReadsAllBytes()
        {
            using var stream = OpenMemoryFileReadStream();
            Assert.That(stream, Is.Not.Null);

            byte[] buffer = new byte[_testData.Length];
            int read = stream.Read(buffer);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public async Task ReadBytesAsync_ReadsAllBytes()
        {
            using var stream = OpenMemoryFileReadStream();
            Assert.That(stream, Is.Not.Null);

            byte[] buffer = new byte[_testData.Length];
            int read = await stream.ReadAsync(buffer, 0, buffer.Length);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.EqualTo(_testData.Length));
                Assert.That(buffer, Is.EqualTo(_testData));
            }
        }

        [Test]
        public void Read_Partial_AdvancesPosition()
        {
            using var stream = OpenMemoryFileReadStream();
            byte[] buffer = new byte[2];
            int read = stream.Read(buffer, 0, 2);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.EqualTo(2));
                Assert.That(buffer, Is.EqualTo(new byte[] { 10, 20 }));
                Assert.That(stream.Position, Is.EqualTo(2));
            }
        }

        [Test]
        public void ReadToEnd_ReadsRemaining()
        {
            using var stream = OpenMemoryFileReadStream();
            stream.Position = 2;
            byte[] remaining = stream.ReadToEnd();
            Assert.That(remaining, Is.EqualTo(new byte[] { 30, 40, 50 }));
        }

        [Test]
        public async Task ReadToEndAsync_ReadsRemaining()
        {
            using var stream = OpenMemoryFileReadStream();
            stream.Position = 2;
            byte[] remaining = await stream.ReadToEndAsync();
            Assert.That(remaining, Is.EqualTo(new byte[] { 30, 40, 50 }));
        }

        [Test]
        public void ToArray_ReturnsFullBuffer()
        {
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            byte[] full = stream.ToArray();
            Assert.That(full, Is.EqualTo(_testData));
        }

        [Test]
        public void Seek_Works()
        {
            using var stream = OpenMemoryFileReadStream();
            stream.Seek(3, SeekOrigin.Begin);
            Assert.That(stream.Position, Is.EqualTo(3));
            byte b = stream.ReadByte();
            Assert.That(b, Is.EqualTo(40));
        }

        [Test]
        public void Seek_BeyondLength_ThrowsIOException()
        {
            using var stream = OpenMemoryFileReadStream();
            Assert.Throws<IOException>(() => stream.Seek(_testData.Length + 1, SeekOrigin.Begin));
        }

        [Test]
        public void ReadInt8_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(sbyte.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            sbyte val = stream.ReadInt8();
            Assert.That(val, Is.EqualTo(sbyte.MaxValue));
        }

        [Test]
        public void ReadInt16_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(short.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            short val = stream.ReadInt16();
            Assert.That(val, Is.EqualTo(short.MaxValue));
        }

        [Test]
        public void ReadInt32_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(int.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            int val = stream.ReadInt32();
            Assert.That(val, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void ReadInt64_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(long.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            long val = stream.ReadInt64();
            Assert.That(val, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void ReadUInt8_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(byte.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            byte val = stream.ReadUInt8();
            Assert.That(val, Is.EqualTo(byte.MaxValue));
        }

        [Test]
        public void ReadUInt16_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(ushort.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            ushort val = stream.ReadUInt16();
            Assert.That(val, Is.EqualTo(ushort.MaxValue));
        }

        [Test]
        public void ReadUInt32_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(uint.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            uint val = stream.ReadUInt32();
            Assert.That(val, Is.EqualTo(uint.MaxValue));
        }

        [Test]
        public void ReadUInt64_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(ulong.MaxValue);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            ulong val = stream.ReadUInt64();
            Assert.That(val, Is.EqualTo(ulong.MaxValue));
        }

        [Test]
        public void ReadBoolean_ReadsCorrectly()
        {
            // Overwrite file with known ints
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(true);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            bool val = stream.ReadBoolean();
            Assert.That(val, Is.EqualTo(true));
        }

        [Test]
        public void ReadString_ReadsCorrectly()
        {
            string test = "Hello Memory";
            using (var bw = new BinaryWriter(File.OpenWrite(_filePath)))
            {
                bw.Write(test);
            }
            using var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            string read = stream.ReadString();
            Assert.That(read, Is.EqualTo(test));
        }

        [Test]
        public void Dispose_ReleasesBuffer()
        {
            var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            var buffer = stream.ToArray(); // internal buffer reference
            stream.Dispose();
            // After dispose, buffer is returned to pool; we can't verify directly but no exception
            Assert.DoesNotThrow(() => stream.Dispose()); // double dispose safe
        }

        [Test]
        public async Task DisposeAsync_ReleasesBuffer()
        {
            var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;
            var buffer = stream.ToArray(); // internal buffer reference
            await stream.DisposeAsync();
            // After dispose, buffer is returned to pool; we can't verify directly but no exception
            Assert.DoesNotThrowAsync(async () => await stream.DisposeAsync()); // double dispose safe
        }

        [Test]
        public void FilePath_MatchesFullFilePath()
        {
            var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;

            Assert.That(stream.FilePath, Is.EqualTo(_filePath));
        }

        [Test]
        public void Create_MetadataIsCorrect()
        {
            var stream = OpenMemoryFileReadStream() as MemoryFileReadStream;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stream.IsOpen, Is.True);
                Assert.That(stream.CanRead, Is.True);
                Assert.That(stream.CanWrite, Is.False);
            }
        }

        [Test]
        public void Flush_ThrowsNotSupportedException()
        {
            var stream = OpenMemoryFileReadStream() as IMemoryFileReadStream;
            Assert.Throws<NotSupportedException>(() => stream.Flush());
        }

        [Test]
        public async Task FlushAsync_ThrowsNotSupportedException()
        {
            var stream = OpenMemoryFileReadStream() as IMemoryFileReadStream;

            Assert.ThrowsAsync<NotSupportedException>(async () => await stream.FlushAsync());
        }
    }
}
#endif
