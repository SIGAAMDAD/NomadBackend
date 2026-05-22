
#if !UNITY_EDITOR
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.FileSystem.Private.Services;
using Nomad.FileSystem.Private.FileStreams;
using Nomad.FileSystem.Private.MemoryStream;
using System.Linq;
using Nomad.Core.Memory.Buffers;
using Nomad.Core.FileSystem.Configs;

namespace Nomad.FileSystem.Tests
{
    [TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Services")]
    [Category("Unit")]
    public class FileSystemServiceTests
    {
        private Mock<IEngineService> _engineMock;
        private Mock<ILoggerService> _loggerMock;
        private Mock<ILoggerCategory> _categoryMock;
        private FileSystemService _service;
        private string _tempDir;
        private string _streamingAssetsDir;
        private string _userDataDir;
        private string _installDir;

        [SetUp]
        public void SetUp()
        {
            _engineMock = new Mock<IEngineService>();
            _loggerMock = new Mock<ILoggerService>();
            _categoryMock = new Mock<ILoggerCategory>();

            // Setup logger category
            _loggerMock.Setup(l => l.CreateCategory(nameof(FileSystem), LogLevel.Info, true))
                       .Returns(_categoryMock.Object);

            // Create temp directories
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _streamingAssetsDir = Path.Combine(_tempDir, "StreamingAssets");
            _userDataDir = Path.Combine(_tempDir, "UserData");
            _installDir = Path.Combine(_tempDir, "Install");

            Directory.CreateDirectory(_streamingAssetsDir);
            Directory.CreateDirectory(_userDataDir);
            Directory.CreateDirectory(_installDir);

            _engineMock.Setup(e => e.GetStoragePath(StorageScope.StreamingAssets))
                       .Returns(_streamingAssetsDir);
            _engineMock.Setup(e => e.GetStoragePath(StorageScope.UserData))
                       .Returns(_userDataDir);
            _engineMock.Setup(e => e.GetStoragePath(StorageScope.Install))
                       .Returns(_installDir);
            _engineMock.Setup(e => e.GetStoragePath("Config", StorageScope.UserData))
                       .Returns(Path.Combine(_userDataDir, "Config"));
            _engineMock.Setup(e => e.GetStoragePath("SaveData", StorageScope.UserData))
                       .Returns(Path.Combine(_userDataDir, "SaveData"));

            _service = new FileSystemService(_engineMock.Object, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void Constructor_ValidDependencies_CreatesCategoryAndSearcher()
        {
            Assert.DoesNotThrow(() => new FileSystemService(_engineMock.Object, _loggerMock.Object));
        }

        [Test]
        public void Constructor_NullEngine_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FileSystemService(null!, _loggerMock.Object));
        }

        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FileSystemService(_engineMock.Object, null!));
        }

        #region Basic File/Directory Operations

        [Test]
        public void CopyFile_SourceExists_DestinationCreated()
        {
            // Arrange
            string source = Path.Combine(_userDataDir, "source.txt");
            string dest = Path.Combine(_userDataDir, "dest.txt");
            File.WriteAllText(source, "test");

            // Act
            _service.CopyFile(source, dest, false);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(File.Exists(dest));
                Assert.That(File.ReadAllText(dest), Is.EqualTo("test"));
            }
        }

        [Test]
        public void CopyFile_OverwriteTrue_OverwritesExisting()
        {
            // Arrange
            string source = Path.Combine(_userDataDir, "source.txt");
            string dest = Path.Combine(_userDataDir, "dest.txt");
            File.WriteAllText(source, "new");
            File.WriteAllText(dest, "old");

            // Act
            _service.CopyFile(source, dest, true);

            // Assert
            Assert.That(File.ReadAllText(dest), Is.EqualTo("new"));
        }

        [Test]
        public void CopyFile_OverwriteFalse_ThrowsWhenDestExists()
        {
            // Arrange
            string source = Path.Combine(_userDataDir, "source.txt");
            string dest = Path.Combine(_userDataDir, "dest.txt");
            File.WriteAllText(source, "new");
            File.WriteAllText(dest, "old");

            // Act & Assert
            Assert.Throws<IOException>(() => _service.CopyFile(source, dest, false));
        }

        [Test]
        public void CreateDirectory_NewDirectory_Created()
        {
            string path = Path.Combine(_userDataDir, "NewFolder");
            _service.CreateDirectory(path);
            Assert.That(Directory.Exists(path));
        }

        [Test]
        public void CreateDirectory_ExistingDirectory_NoException()
        {
            string path = Path.Combine(_userDataDir, "Existing");
            Directory.CreateDirectory(path);
            Assert.DoesNotThrow(() => _service.CreateDirectory(path));
        }

        [Test]
        public void DirectoryExists_ExistingDirectory_ReturnsTrue()
        {
            string path = Path.Combine(_userDataDir, "TestDir");
            Directory.CreateDirectory(path);
            Assert.That(_service.DirectoryExists(path), Is.True);
        }

        [Test]
        public void DirectoryExists_NonExistentDirectory_ReturnsFalse()
        {
            string path = Path.Combine(_userDataDir, "NoSuchDir");
            Assert.That(_service.DirectoryExists(path), Is.False);
        }

        [Test]
        public void DirectoryExists_NullOrEmpty_ReturnsFalse()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_service.DirectoryExists(null!), Is.False);
                Assert.That(_service.DirectoryExists(""), Is.False);
            }
        }

        [Test]
        public void DeleteDirectory_EmptyDirectory_Deletes()
        {
            string path = Path.Combine(_userDataDir, "Empty");
            Directory.CreateDirectory(path);
            _service.DeleteDirectory(path, false);
            Assert.That(Directory.Exists(path), Is.False);
        }

        [Test]
        public void DeleteDirectory_NonEmptyWithoutRecursive_Throws()
        {
            string path = Path.Combine(_userDataDir, "NonEmpty");
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "file.txt"), "data");
            Assert.Throws<IOException>(() => _service.DeleteDirectory(path, false));
        }

        [Test]
        public void DeleteDirectory_NonEmptyWithRecursive_Deletes()
        {
            string path = Path.Combine(_userDataDir, "NonEmpty");
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "file.txt"), "data");
            _service.DeleteDirectory(path, true);
            Assert.That(Directory.Exists(path), Is.False);
        }

        [Test]
        public void DeleteFile_ExistingFile_Deletes()
        {
            string path = Path.Combine(_userDataDir, "file.txt");
            File.WriteAllText(path, "data");
            _service.DeleteFile(path);
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void DeleteFile_NonExistentFile_NoException()
        {
            string path = Path.Combine(_userDataDir, "nosuch.txt");
            Assert.DoesNotThrow(() => _service.DeleteFile(path));
        }

        [Test]
        public void FileExists_ExistingFile_ReturnsTrue()
        {
            string path = Path.Combine(_userDataDir, "file.txt");
            File.WriteAllText(path, "data");
            Assert.That(_service.FileExists(path), Is.True);
        }

        [Test]
        public void FileExists_NonExistentFile_ReturnsFalse()
        {
            string path = Path.Combine(_userDataDir, "nosuch.txt");
            Assert.That(_service.FileExists(path), Is.False);
        }

        [Test]
        public void FileExists_ProjectStylePathWithinSearchDirectory_ReturnsTrue()
        {
            string assetsDir = Path.Combine(_tempDir, "Assets");
            string bindingsDir = Path.Combine(assetsDir, "Config", "Bindings");
            Directory.CreateDirectory(bindingsDir);
            string file = Path.Combine(bindingsDir, "DefaultConfig.json");
            File.WriteAllText(file, "data");

            _service.AddSearchDirectory(assetsDir);

            Assert.That(_service.FileExists("Assets/Config/Bindings/DefaultConfig.json"), Is.True);
        }

        [Test]
        public void GetConfigPath_ReturnsExpected()
        {
            string expected = Path.Combine(_userDataDir, "Config");
            Assert.That(_service.GetConfigPath(), Is.EqualTo(expected));
        }

        [Test]
        public void GetResourcePath_ReturnsStreamingAssets()
        {
            Assert.That(_service.GetResourcePath(), Is.EqualTo(_streamingAssetsDir));
        }

        [Test]
        public void GetSavePath_ReturnsUserDataSaveData()
        {
            string expected = Path.Combine(_userDataDir, "SaveData");
            Assert.That(_service.GetSavePath(), Is.EqualTo(expected));
        }

        [Test]
        public void GetUserDataPath_ReturnsUserData()
        {
            Assert.That(_service.GetUserDataPath(), Is.EqualTo(_userDataDir));
        }

        [Test]
        public void GetDirectories_ReturnsSubdirectories()
        {
            Directory.CreateDirectory(Path.Combine(_userDataDir, "Sub1"));
            Directory.CreateDirectory(Path.Combine(_userDataDir, "Sub2"));
            var dirs = _service.GetDirectories(_userDataDir);
            Assert.That(dirs, Has.Count.EqualTo(2));
            Assert.That(dirs, Contains.Item(Path.Combine(_userDataDir, "Sub1")));
        }

        [Test]
        public void GetDirectories_ProjectStylePathWithinSearchDirectory_ReturnsSubdirectories()
        {
            string assetsDir = Path.Combine(_tempDir, "Assets");
            string configDir = Path.Combine(assetsDir, "Config");
            Directory.CreateDirectory(Path.Combine(configDir, "Bindings"));
            Directory.CreateDirectory(Path.Combine(configDir, "Gameplay"));

            _service.AddSearchDirectory(assetsDir);

            var directories = _service.GetDirectories("Assets/Config");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(directories, Has.Count.EqualTo(2));
                Assert.That(directories, Contains.Item(Path.Combine(configDir, "Bindings")));
                Assert.That(directories, Contains.Item(Path.Combine(configDir, "Gameplay")));
            }
        }

        [Test]
        public void GetFiles_ReturnsMatchingFiles()
        {
            File.WriteAllText(Path.Combine(_userDataDir, "a.txt"), "");
            File.WriteAllText(Path.Combine(_userDataDir, "b.txt"), "");
            File.WriteAllText(Path.Combine(_userDataDir, "c.log"), "");
            var files = _service.GetFiles(_userDataDir, "*.txt", false);
            Assert.That(files, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetFiles_Recursive_IncludesSubdirectories()
        {
            var sub = Path.Combine(_userDataDir, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "deep.txt"), "");
            var files = _service.GetFiles(_userDataDir, "*.txt", true);
            Assert.That(files, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetFiles_ProjectStylePathWithinSearchDirectory_ReturnsMatches()
        {
            string assetsDir = Path.Combine(_tempDir, "Assets");
            string bindingsDir = Path.Combine(assetsDir, "Config", "Bindings");
            Directory.CreateDirectory(bindingsDir);
            File.WriteAllText(Path.Combine(bindingsDir, "DefaultConfig.json"), "");
            File.WriteAllText(Path.Combine(bindingsDir, "KeyboardAndMouse.json"), "");

            _service.AddSearchDirectory(assetsDir);

            var files = _service.GetFiles("Assets/Config/Bindings", "*.json", false);
            Assert.That(files, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetFileSize_ReturnsCorrectBytes()
        {
            string path = Path.Combine(_userDataDir, "file.bin");
            byte[] data = new byte[123];
            new Random(42).NextBytes(data);
            File.WriteAllBytes(path, data);
            long size = _service.GetFileSize(path);
            Assert.That(size, Is.EqualTo(123));
        }

        [Test]
        public void GetLastWriteTime_ReturnsValidDateTime()
        {
            string path = Path.Combine(_userDataDir, "file.txt");
            File.WriteAllText(path, "test");
            var time = _service.GetLastWriteTime(path);
            Assert.That(time, Is.EqualTo(File.GetLastWriteTime(path)).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void MoveFile_MovesFile()
        {
            string source = Path.Combine(_userDataDir, "source.txt");
            string dest = Path.Combine(_userDataDir, "dest.txt");
            File.WriteAllText(source, "data");
            _service.MoveFile(source, dest);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(source), Is.False);
                Assert.That(File.Exists(dest), Is.True);
            }
        }

        #endregion

        #region OpenRead

        [Test]
        public void OpenRead_FileNotFound_ReturnsNull()
        {
            var config = new FileReadConfig { FilePath = "nosuch.txt" };
            var result = _service.OpenRead(config);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void OpenRead_FileFound_ReturnsFileReadStream()
        {
            string file = Path.Combine(_streamingAssetsDir, "test.txt");
            File.WriteAllText(file, "hello");
            var config = new FileReadConfig { FilePath = file };
            var result = _service.OpenRead(config);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<FileReadStream>());
            result.Dispose();
        }

        [Test]
        public void OpenRead_MemoryFileConfig_ReturnsMemoryFileReadStream()
        {
            string file = Path.Combine(_streamingAssetsDir, "test.txt");
            File.WriteAllText(file, "hello");
            var config = new MemoryFileReadConfig { FilePath = file, MaxCapacity = 1024 };
            using var result = _service.OpenRead(config);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<MemoryFileReadStream>());
        }

        [Test]
        public void OpenRead_NullPath_ThrowsArgumentException()
        {
            var config = new FileReadConfig { FilePath = null! };
            Assert.Throws<ArgumentException>(() => _service.OpenRead(config));
        }

        [Test]
        public async Task OpenReadAsync_FileFound_ReturnsStream()
        {
            string file = Path.Combine(_streamingAssetsDir, "test.txt");
            File.WriteAllText(file, "hello");
            var config = new FileReadConfig { FilePath = file };
            var result = await _service.OpenReadAsync(config);
            Assert.That(result, Is.Not.Null);
            result.Dispose();
        }

        [Test]
        public void OpenReadAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var config = new FileReadConfig { FilePath = "any.txt" };
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _service.OpenReadAsync(config, cts.Token));
        }

        #endregion

        #region OpenWrite

        [Test]
        public void OpenWrite_MemoryConfig_ReturnsMemoryWriteStream()
        {
            var config = new MemoryWriteConfig { InitialCapacity = 1024, FixedSize = false };
            var result = _service.OpenWrite(config);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<MemoryWriteStream>());
            result.Dispose();
        }

        [Test]
        public void OpenWrite_FileConfig_ReturnsFileWriteStream()
        {
            string path = Path.Combine(_userDataDir, "out.txt");
            var config = new FileWriteConfig { FilePath = path, Append = false };
            var result = _service.OpenWrite(config);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<FileWriteStream>());
            result.Dispose();
        }

        [Test]
        public void OpenWrite_MemoryFileConfig_ReturnsMemoryFileWriteStream()
        {
            string path = Path.Combine(_userDataDir, "out.bin");
            var config = new MemoryFileWriteConfig { FilePath = path, InitialCapacity = 4096, FixedSize = false };
            var result = _service.OpenWrite(config);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<MemoryFileWriteStream>());
            result.Dispose();
        }

        /* TODO: allow file format options in FileWriteConfig
		[Test]
		public void OpenWrite_AppendTrue_OpensInAppendMode()
		{
			string path = Path.Combine(_userDataDir, "append.txt");
			{
				File.WriteAllText(path, "initial");
			}
			{
				var config = new FileWriteConfig{ FilePath = path, Append = true };
				using var stream = _service.OpenWrite(config) as FileWriteStream;
				Assert.That(stream, Is.Not.Null);
				stream.WriteString(" appended");
				stream.Flush();
			}
			string content = File.ReadAllText(path);
			Assert.That(content, Is.EqualTo("initial appended"));
		}
		*/

        [Test]
        public void OpenWrite_NullPath_ThrowsArgumentException()
        {
            var config = new FileWriteConfig { FilePath = null!, Append = false };
            Assert.Throws<ArgumentException>(() => _service.OpenWrite(config));
        }

        [Test]
        public async Task OpenWriteAsync_ReturnsSameAsSync()
        {
            string path = Path.Combine(_userDataDir, "out.txt");
            var config = new FileWriteConfig { FilePath = path, Append = false };
            var result = await _service.OpenWriteAsync(config);
            Assert.That(result, Is.Not.Null);
            result.Dispose();
        }

        #endregion

        #region CreateStream (Not Implemented)

        [Test]
        public void CreateStream_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _service.CreateStream(FileAccess.Read, StreamType.File));
        }

        #endregion

        #region LoadFile

        [Test]
        public void LoadFile_ExistingFile_ReturnsBufferHandleWithContent()
        {
            string path = Path.Combine(_streamingAssetsDir, "data.bin");
            byte[] expected = [1, 2, 3, 4, 5];
            File.WriteAllBytes(path, expected);

            using var handle = _service.LoadFile(path);
            Assert.That(handle, Is.TypeOf<PooledBufferHandle>());
            Assert.That(handle.Length, Is.EqualTo(expected.Length));
            Assert.That(handle.ToArray().Take((int)handle.Length), Is.EqualTo(expected));
        }

        [Test]
        public void LoadFile_FileNotFound_ReturnsNull()
        {
            Assert.That(_service.LoadFile("missing.dat"), Is.Null);
        }

        [Test]
        public async Task LoadFileAsync_ExistingFile_ReturnsHandle()
        {
            string path = Path.Combine(_streamingAssetsDir, "data.bin");
            byte[] expected = [10, 20, 30];
            File.WriteAllBytes(path, expected);

            using var handle = await _service.LoadFileAsync(path);
            Assert.That(handle!.Length, Is.EqualTo(expected.Length));
            Assert.That(handle.ToArray().Take(handle.Length), Is.EqualTo(expected));
        }

        #endregion

        #region WriteFile

        [Test]
        public void WriteFile_WritesSpanToFile()
        {
            string path = Path.Combine(_userDataDir, "output.bin");
            byte[] data = [1, 2, 3, 4, 5];
            _service.WriteFile(path, data, 1, 3); // writes [2,3,4]

            var written = File.ReadAllBytes(path);
            Assert.That(written, Is.EqualTo(new byte[] { 2, 3, 4 }));
        }

        [Test]
        public void WriteFile_InvalidPath_ThrowsDirectoryNotFoundException()
        {
            // Path doesn't exist (directory missing)
            string badPath = Path.Combine(_userDataDir, "sub", "file.txt");
            Assert.Throws<DirectoryNotFoundException>(() =>
                _service.WriteFile(badPath, new byte[10], 0, 10));
        }

        [Test]
        public async Task WriteFileAsync_WritesMemoryToFile()
        {
            string path = Path.Combine(_userDataDir, "output.bin");
            byte[] data = [1, 2, 3, 4, 5];
            await _service.WriteFileAsync(path, data, 2, 2); // writes [3,4]

            var written = File.ReadAllBytes(path);
            Assert.That(written, Is.EqualTo(new byte[] { 3, 4 }));
        }

        [Test]
        public void WriteFileAsync_CancellationRequested_Throws()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _service.WriteFileAsync("any", new byte[10], 0, 10, cts.Token));
        }

        #endregion
    }
}
#endif
