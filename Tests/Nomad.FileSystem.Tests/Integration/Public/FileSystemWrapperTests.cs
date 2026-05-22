using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Nomad.Core.Exceptions;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Memory.Buffers;
using Nomad.FileSystem.Global;
using NSubstitute;

namespace Nomad.FileSystem.Tests
{
    [TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Services")]
    [Category("Integration")]
    [Category("Unit")]
    public class FileSystemWrapperTests
    {
        private Mock<IFileSystem> _mockFileSystem;

        [SetUp]
        public void SetUp()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            // Initialize the static wrapper with the mock
            Global.FileSystem.Initialize(_mockFileSystem.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // Optional: reset the static instance to null using reflection
            // to ensure perfect isolation for subsequent tests.
            var field = typeof(Global.FileSystem).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }

        #region Initialization Tests

        [Test]
        public void Initialize_WithNullInstance_ThrowsArgumentNullException()
        {
            Assert.That(() => Global.FileSystem.Initialize(null), Throws.ArgumentNullException);
        }

        [Test]
        public void WhenNotInitialized_AnyStaticMethod_ThrowsSubsystemNotInitializedException()
        {
            // Reset instance to null
            var field = typeof(Global.FileSystem).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);

            // Pick a representative method
            Assert.That(() => Global.FileSystem.GetSavePath(), Throws.TypeOf<SubsystemNotInitializedException>());
        }

        #endregion

        #region Path Getter Tests

        [Test]
        public void GetSavePath_ReturnsInstanceResult()
        {
            var expected = @"C:\saves";
            _mockFileSystem.Setup(fs => fs.GetSavePath()).Returns(expected);

            var result = Global.FileSystem.GetSavePath();

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetResourcePath_ReturnsInstanceResult()
        {
            var expected = @"C:\resources";
            _mockFileSystem.Setup(fs => fs.GetResourcePath()).Returns(expected);

            var result = Global.FileSystem.GetResourcePath();

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetConfigPath_ReturnsInstanceResult()
        {
            var expected = @"C:\config";
            _mockFileSystem.Setup(fs => fs.GetConfigPath()).Returns(expected);

            var result = Global.FileSystem.GetConfigPath();

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetUserDataPath_ReturnsInstanceResult()
        {
            var expected = @"C:\userdata";
            _mockFileSystem.Setup(fs => fs.GetUserDataPath()).Returns(expected);

            var result = Global.FileSystem.GetUserDataPath();

            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region Search Directory Tests

        [Test]
        public void AddSearchDirectory_ForwardsArgument()
        {
            var directory = @"C:\search";

            Global.FileSystem.AddSearchDirectory(directory);

            _mockFileSystem.Verify(fs => fs.AddSearchDirectory(directory), Times.Once);
        }

        #endregion

        #region File Existence & Metadata Tests

        [Test]
        public void FileExists_ForwardsPath_ReturnsInstanceResult()
        {
            var path = "test.txt";
            _mockFileSystem.Setup(fs => fs.FileExists(path)).Returns(true);

            var result = Global.FileSystem.FileExists(path);

            Assert.That(result, Is.True);
        }

        [Test]
        public void GetLastWriteTime_ForwardsPath_ReturnsInstanceResult()
        {
            var path = "test.txt";
            var expected = new DateTime(2025, 1, 1);
            _mockFileSystem.Setup(fs => fs.GetLastWriteTime(path)).Returns(expected);

            var result = Global.FileSystem.GetLastWriteTime(path);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetFileSize_ForwardsPath_ReturnsInstanceResult()
        {
            var path = "test.txt";
            const long expected = 12345;
            _mockFileSystem.Setup(fs => fs.GetFileSize(path)).Returns(expected);

            var result = Global.FileSystem.GetFileSize(path);

            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region File Manipulation Tests

        [Test]
        public void DeleteFile_ForwardsPath()
        {
            var path = "to-delete.txt";

            Global.FileSystem.DeleteFile(path);

            _mockFileSystem.Verify(fs => fs.DeleteFile(path), Times.Once);
        }

        [Test]
        public void MoveFile_ForwardsArguments()
        {
            var source = "source.txt";
            var dest = "dest.txt";
            const bool overwrite = true;

            Global.FileSystem.MoveFile(source, dest, overwrite);

            _mockFileSystem.Verify(fs => fs.MoveFile(source, dest, overwrite), Times.Once);
        }

        [Test]
        public void CopyFile_ForwardsArguments()
        {
            var source = "source.txt";
            var dest = "dest.txt";
            const bool overwrite = false;

            Global.FileSystem.CopyFile(source, dest, overwrite);

            _mockFileSystem.Verify(fs => fs.CopyFile(source, dest, overwrite), Times.Once);
        }

        [Test]
        public void ReplaceFile_ForwardsArguments()
        {
            var source = "source.txt";
            var dest = "dest.txt";
            var backup = "backup.txt";

            Global.FileSystem.ReplaceFile(source, dest, backup);

            _mockFileSystem.Verify(fs => fs.ReplaceFile(source, dest, backup), Times.Once);
        }

        #endregion

        #region Directory Tests

        [Test]
        public void DirectoryExists_ForwardsPath_ReturnsInstanceResult()
        {
            var path = @"C:\folder";
            _mockFileSystem.Setup(fs => fs.DirectoryExists(path)).Returns(true);

            var result = Global.FileSystem.DirectoryExists(path);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CreateDirectory_ForwardsPath()
        {
            var path = @"C:\newfolder";

            Global.FileSystem.CreateDirectory(path);

            _mockFileSystem.Verify(fs => fs.CreateDirectory(path), Times.Once);
        }

        [Test]
        public void DeleteDirectory_ForwardsArguments()
        {
            var path = @"C:\folder";
            const bool recursive = true;

            Global.FileSystem.DeleteDirectory(path, recursive);

            _mockFileSystem.Verify(fs => fs.DeleteDirectory(path, recursive), Times.Once);
        }

        [Test]
        public void GetDirectories_ForwardsPath_ReturnsInstanceResult()
        {
            var path = @"C:\parent";
            var expected = new List<string> { @"C:\parent\sub1", @"C:\parent\sub2" };
            _mockFileSystem.Setup(fs => fs.GetDirectories(path)).Returns(expected);

            var result = Global.FileSystem.GetDirectories(path);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetFiles_ForwardsArguments_ReturnsInstanceResult()
        {
            var path = @"C:\parent";
            var pattern = "*.txt";
            const bool recursive = true;
            var expected = new List<string> { @"C:\parent\file1.txt", @"C:\parent\sub\file2.txt" };
            _mockFileSystem.Setup(fs => fs.GetFiles(path, pattern, recursive)).Returns(expected);

            var result = Global.FileSystem.GetFiles(path, pattern, recursive);

            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region Stream Opening Tests

        [Test]
        public void OpenRead_ForwardsConfig_ReturnsInstanceResult()
        {
            var config = Mock.Of<IReadConfig>();
            var expectedStream = Mock.Of<IReadStream>();
            _mockFileSystem.Setup(fs => fs.OpenRead(config)).Returns(expectedStream);

            var result = Global.FileSystem.OpenRead(config);

            Assert.That(result, Is.SameAs(expectedStream));
        }

        [Test]
        public void OpenWrite_ForwardsConfig_ReturnsInstanceResult()
        {
            var config = Mock.Of<IWriteConfig>();
            var expectedStream = Mock.Of<IWriteStream>();
            _mockFileSystem.Setup(fs => fs.OpenWrite(config)).Returns(expectedStream);

            var result = Global.FileSystem.OpenWrite(config);

            Assert.That(result, Is.SameAs(expectedStream));
        }

        [Test]
        public async Task OpenReadAsync_ForwardsConfigAndToken_ReturnsInstanceResult()
        {
            var config = Mock.Of<IReadConfig>();
            var ct = CancellationToken.None;
            var expectedStream = Mock.Of<IReadStream>();
            _mockFileSystem.Setup(fs => fs.OpenReadAsync(config, ct)).Returns(async () => await ValueTask.FromResult(expectedStream));

            var result = await Global.FileSystem.OpenReadAsync(config, ct);

            Assert.That(result, Is.SameAs(expectedStream));
        }

        [Test]
        public async Task OpenWriteAsync_ForwardsConfigAndToken_ReturnsInstanceResult()
        {
            var config = Mock.Of<IWriteConfig>();
            var ct = CancellationToken.None;
            var expectedStream = Mock.Of<IWriteStream>();
            _mockFileSystem.Setup(fs => fs.OpenWriteAsync(config, ct)).Returns(async () => await ValueTask.FromResult(expectedStream));

            var result = await Global.FileSystem.OpenWriteAsync(config, ct);

            Assert.That(result, Is.SameAs(expectedStream));
        }

        #endregion

        #region Memory Stream & Buffer Tests

        [Test]
        public void CreateStream_ForwardsArguments_ReturnsInstanceResult()
        {
            const FileAccess access = FileAccess.ReadWrite;
            const StreamType type = StreamType.Memory;
            var outputFile = "out.bin";
            const int length = 1024;
            var expectedStream = Mock.Of<IDataStream>();
            _mockFileSystem.Setup(fs => fs.CreateStream(access, type, outputFile, length)).Returns(expectedStream);

            var result = Global.FileSystem.CreateStream(access, type, outputFile, length);

            Assert.That(result, Is.SameAs(expectedStream));
        }

        [Test]
        public void LoadFile_ForwardsPath_ReturnsInstanceResult()
        {
            var path = "data.bin";
            var expectedHandle = Mock.Of<IBufferHandle>();
            _mockFileSystem.Setup(fs => fs.LoadFile(path)).Returns(expectedHandle);

            var result = Global.FileSystem.LoadFile(path);

            Assert.That(result, Is.SameAs(expectedHandle));
        }

        [Test]
        public async Task LoadFileAsync_ForwardsPath_ReturnsInstanceResult()
        {
            var path = "data.bin";
            var expectedHandle = Mock.Of<IBufferHandle>();
            _mockFileSystem.Setup(fs => fs.LoadFileAsync(path)).Returns(async() => await ValueTask.FromResult(expectedHandle));

            var result = await Global.FileSystem.LoadFileAsync(path);

            Assert.That(result, Is.SameAs(expectedHandle));
        }

        [Test]
        public void WriteFile_ForwardsArguments()
        {
            var path = "out.bin";
            var buffer = new byte[] { 1, 2, 3 };
            const int offset = 1;
            const int length = 2;
            
            Global.FileSystem.WriteFile(path, buffer, offset, length);

            _mockFileSystem.Verify(fs => fs.WriteFile(path, buffer, offset, length), Times.Once);
        }

        [Test]
        public async Task WriteFileAsync_ForwardsArguments()
        {
            var path = "out.bin";
            var buffer = new byte[] { 1, 2, 3 }.AsMemory();
            const int offset = 1;
            const int length = 2;
            var ct = CancellationToken.None;

            await Global.FileSystem.WriteFileAsync(path, buffer, offset, length, ct);

            _mockFileSystem.Verify(fs => fs.WriteFileAsync(path, buffer, offset, length, ct), Times.Once);
        }

        #endregion
    }
}