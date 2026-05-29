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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Memory.Buffers;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.Services;
using Nomad.Save.Private.ValueObjects;

namespace Nomad.Save.Tests
{
    /*
	===================================================================================

	BackupServiceTests

	===================================================================================
	*/
    /// <summary>
    /// Unit tests for <see cref="BackupService"/>.
    /// </summary>

    [TestFixture]
    public sealed class BackupServiceTests
    {
        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Constructor_WithValidConfig_CreatesBackupDirectoryAndLoadsEmptyCache()
        {
            using var fixture = new BackupServiceFixture();

            Assert.That(Directory.Exists(fixture.BackupDirectory), Is.True);
            Assert.That(fixture.Service.BackupCount, Is.Zero);
            Assert.That(fixture.Service.ListBackups(), Is.Empty);
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            using var fixture = new BackupServiceFixture(createService: false);

            Assert.That(
                () => new BackupService(null!, fixture.FileSystem),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
        {
            using var fixture = new BackupServiceFixture(createService: false);

            Assert.That(
                () => new BackupService(fixture.Config, null!),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WithEmptyBackupPath_ThrowsArgumentException()
        {
            using var fixture = new BackupServiceFixture(createService: false);
            SaveConfig config = fixture.CreateConfig(backupPath: string.Empty);

            Assert.That(
                () => new BackupService(config, fixture.FileSystem),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WithNegativeMaxBackups_ThrowsArgumentOutOfRangeException()
        {
            using var fixture = new BackupServiceFixture(createService: false);
            SaveConfig config = fixture.CreateConfig(maxBackups: -1);

            Assert.That(
                () => new BackupService(config, fixture.FileSystem),
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        [Category("Unit")]
        [Category("EdgeCase")]
        public void CreateBackup_WhenMaxBackupsIsZero_ReturnsFalseAndCreatesNoBackup()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 0);
            string savePath = fixture.WriteSaveFile("slot0", "save-data");

            bool result = fixture.Service.CreateBackup(savePath);

            Assert.That(result, Is.False);
            Assert.That(fixture.Service.BackupCount, Is.Zero);
            Assert.That(Directory.GetFiles(fixture.BackupDirectory), Is.Empty);
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void TryCreateBackup_WhenSourceDoesNotExist_ReturnsFalse()
        {
            using var fixture = new BackupServiceFixture();
            string missingPath = Path.Combine(fixture.SaveDirectory, "missing.ngd");

            bool result = fixture.Service.TryCreateBackup(missingPath, out BackupData backup);

            Assert.That(result, Is.False);
            Assert.That(backup, Is.EqualTo(BackupData.Empty));
            Assert.That(fixture.Service.BackupCount, Is.Zero);
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void TryCreateBackup_WhenSourceExists_CopiesFileAndReturnsBackupData()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("slot1", "primary-data");

            bool result = fixture.Service.TryCreateBackup(savePath, out BackupData backup);

            Assert.That(result, Is.True);
            Assert.That(backup.IsValid, Is.True);
            Assert.That(backup.SourcePath, Is.EqualTo(savePath));
            Assert.That(backup.SaveName, Is.EqualTo("slot1"));
            Assert.That(backup.Order, Is.EqualTo(1));
            Assert.That(File.Exists(backup.BackupPath), Is.True);
            Assert.That(File.ReadAllText(backup.BackupPath), Is.EqualTo("primary-data"));
            Assert.That(fixture.Service.BackupCount, Is.EqualTo(1));
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void CreateBackup_WhenSourceNameHasNgdExtension_NormalizesSaveName()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("campaign", "content");

            bool result = fixture.Service.TryCreateBackup(savePath, out BackupData backup);

            Assert.That(result, Is.True);
            Assert.That(backup.SaveName, Is.EqualTo("campaign"));
            Assert.That(Path.GetFileName(backup.BackupPath), Does.StartWith("campaign.bak0001."));
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void TryCreateBackup_WhenCopyThrows_CleansPartialBackupAndReturnsFalse()
        {
            using var fixture = new BackupServiceFixture(createService: false);
            string savePath = fixture.WriteSaveFile("broken", "content");
            var fileSystem = new ThrowingCopyFileSystem(fixture.FileSystem);
            using var service = new BackupService(fixture.Config, fileSystem);

            bool result = service.TryCreateBackup(savePath, out BackupData backup);

            Assert.That(result, Is.False);
            Assert.That(backup, Is.EqualTo(BackupData.Empty));
            Assert.That(Directory.GetFiles(fixture.BackupDirectory, "*.ngd.backup"), Is.Empty);
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void TryCreateBackup_WhenMultipleBackupsAreCreated_IncrementsOrder()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 5);
            string savePath = fixture.WriteSaveFile("chain", "v1");

            fixture.Service.TryCreateBackup(savePath, out BackupData first);
            File.WriteAllText(savePath, "v2");
            fixture.Service.TryCreateBackup(savePath, out BackupData second);
            File.WriteAllText(savePath, "v3");
            fixture.Service.TryCreateBackup(savePath, out BackupData third);

            Assert.That(first.Order, Is.EqualTo(1));
            Assert.That(second.Order, Is.EqualTo(2));
            Assert.That(third.Order, Is.EqualTo(3));
            Assert.That(fixture.Service.BackupCount, Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void CreateBackup_WhenMaxBackupsExceeded_PrunesOldestBackupForThatSaveOnly()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 2);
            string player = fixture.WriteSaveFile("player", "p1");
            string world = fixture.WriteSaveFile("world", "w1");

            fixture.Service.TryCreateBackup(player, out BackupData firstPlayer);
            File.WriteAllText(player, "p2");
            fixture.Service.TryCreateBackup(player, out BackupData secondPlayer);
            File.WriteAllText(player, "p3");
            fixture.Service.TryCreateBackup(player, out BackupData thirdPlayer);

            fixture.Service.TryCreateBackup(world, out BackupData worldBackup);

            Assert.That(File.Exists(firstPlayer.BackupPath), Is.False);
            Assert.That(File.Exists(secondPlayer.BackupPath), Is.True);
            Assert.That(File.Exists(thirdPlayer.BackupPath), Is.True);
            Assert.That(File.Exists(worldBackup.BackupPath), Is.True);
            Assert.That(fixture.Service.ListBackups(player), Has.Count.EqualTo(2));
            Assert.That(fixture.Service.ListBackups(world), Has.Count.EqualTo(1));
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void TryGetLatestBackup_WhenBackupsExist_ReturnsNewestBackup()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 5);
            string savePath = fixture.WriteSaveFile("latest", "v1");

            fixture.Service.TryCreateBackup(savePath, out _);
            File.WriteAllText(savePath, "v2");
            fixture.Service.TryCreateBackup(savePath, out BackupData expected);

            bool result = fixture.Service.TryGetLatestBackup(savePath, out BackupData actual);

            Assert.That(result, Is.True);
            Assert.That(actual.BackupPath, Is.EqualTo(expected.BackupPath));
            Assert.That(File.ReadAllText(actual.BackupPath), Is.EqualTo("v2"));
        }

        [Test]
        [Category("Unit")]
        [Category("EdgeCase")]
        public void TryGetLatestBackup_WhenNoBackupExists_ReturnsFalse()
        {
            using var fixture = new BackupServiceFixture();

            bool result = fixture.Service.TryGetLatestBackup("none.ngd", out BackupData backup);

            Assert.That(result, Is.False);
            Assert.That(backup, Is.EqualTo(BackupData.Empty));
        }

        [Test]
        [Category("Unit")]
        [Category("EdgeCase")]
        public void ListBackups_ReturnsSnapshotNotLiveCollection()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("snapshot", "v1");

            IReadOnlyList<BackupData> before = fixture.Service.ListBackups();
            fixture.Service.CreateBackup(savePath);
            IReadOnlyList<BackupData> after = fixture.Service.ListBackups();

            Assert.That(before, Is.Empty);
            Assert.That(after, Has.Count.EqualTo(1));
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void RefreshBackups_WhenBackupFilesAlreadyExist_LoadsValidBackupsAndIgnoresInvalidFiles()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("refresh", "v1");

            fixture.Service.TryCreateBackup(savePath, out BackupData created);
            File.WriteAllText(Path.Combine(fixture.BackupDirectory, "invalid-file.ngd.backup"), "bad");
            fixture.Service.Dispose();

            using var reloaded = new BackupService(fixture.Config, fixture.FileSystem);

            Assert.That(reloaded.BackupCount, Is.EqualTo(1));
            Assert.That(reloaded.TryGetLatestBackup(savePath, out BackupData loaded), Is.True);
            Assert.That(loaded.BackupPath, Is.EqualTo(created.BackupPath));
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void RestoreLatestBackup_WhenBackupExists_CopiesLatestBackupToDestination()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 5);
            string savePath = fixture.WriteSaveFile("restore", "v1");

            fixture.Service.CreateBackup(savePath);
            File.WriteAllText(savePath, "v2");
            fixture.Service.CreateBackup(savePath);
            File.WriteAllText(savePath, "corrupt");

            bool result = fixture.Service.RestoreLatestBackup(savePath);

            Assert.That(result, Is.True);
            Assert.That(File.ReadAllText(savePath), Is.EqualTo("v2"));
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void RestoreBackup_WhenDestinationDirectoryDoesNotExist_CreatesDirectoryAndRestores()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("restore-dir", "payload");
            fixture.Service.TryCreateBackup(savePath, out BackupData backup);
            string destination = Path.Combine(fixture.RootDirectory, "nested", "restore-dir.ngd");

            bool result = fixture.Service.RestoreBackup(backup, destination);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(File.ReadAllText(destination), Is.EqualTo("payload"));
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void RestoreBackup_WhenBackupIsEmpty_ReturnsFalse()
        {
            using var fixture = new BackupServiceFixture();
            string destination = Path.Combine(fixture.SaveDirectory, "empty.ngd");

            bool result = fixture.Service.RestoreBackup(BackupData.Empty, destination);

            Assert.That(result, Is.False);
            Assert.That(File.Exists(destination), Is.False);
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void RestoreBackup_WhenBackupFileMissing_ReturnsFalse()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("missing-restore", "v1");
            fixture.Service.TryCreateBackup(savePath, out BackupData backup);
            File.Delete(backup.BackupPath);

            bool result = fixture.Service.RestoreBackup(backup, savePath);

            Assert.That(result, Is.False);
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void DeleteBackup_WhenBackupExists_RemovesFileAndCacheEntry()
        {
            using var fixture = new BackupServiceFixture();
            string savePath = fixture.WriteSaveFile("delete-one", "v1");
            fixture.Service.TryCreateBackup(savePath, out BackupData backup);

            bool result = fixture.Service.DeleteBackup(backup);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(backup.BackupPath), Is.False);
            Assert.That(fixture.Service.BackupCount, Is.Zero);
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void DeleteBackup_WhenBackupIsEmpty_ReturnsFalse()
        {
            using var fixture = new BackupServiceFixture();

            bool result = fixture.Service.DeleteBackup(BackupData.Empty);

            Assert.That(result, Is.False);
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void DeleteBackups_DeletesOnlyMatchingSaveBackups()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 5);
            string first = fixture.WriteSaveFile("first", "a");
            string second = fixture.WriteSaveFile("second", "b");

            fixture.Service.TryCreateBackup(first, out _);
            File.WriteAllText(first, "a2");
            fixture.Service.TryCreateBackup(first, out _);
            fixture.Service.TryCreateBackup(second, out BackupData secondBackup);

            int deleted = fixture.Service.DeleteBackups(first);

            Assert.That(deleted, Is.EqualTo(2));
            Assert.That(fixture.Service.ListBackups(first), Is.Empty);
            Assert.That(fixture.Service.ListBackups(second), Has.Count.EqualTo(1));
            Assert.That(File.Exists(secondBackup.BackupPath), Is.True);
        }

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void PruneBackups_WhenCalledManually_EnforcesMaxBackups()
        {
            using var fixture = new BackupServiceFixture(maxBackups: 10);
            string savePath = fixture.WriteSaveFile("manual-prune", "v1");

            fixture.Service.TryCreateBackup(savePath, out _);
            File.WriteAllText(savePath, "v2");
            fixture.Service.TryCreateBackup(savePath, out _);
            File.WriteAllText(savePath, "v3");
            fixture.Service.TryCreateBackup(savePath, out _);

            using var limited = new BackupService(fixture.CreateConfig(maxBackups: 1), fixture.FileSystem);
            limited.PruneBackups(savePath);

            Assert.That(limited.ListBackups(savePath), Has.Count.EqualTo(1));
            Assert.That(Directory.GetFiles(fixture.BackupDirectory, "*.ngd.backup"), Has.Length.EqualTo(1));
        }

        [Test]
        [Category("Unit")]
        [Category("Lifecycle")]
        public void PublicMethods_AfterDispose_ThrowObjectDisposedException()
        {
            using var fixture = new BackupServiceFixture();
            fixture.Service.Dispose();

            Assert.That(() => _ = fixture.Service.BackupCount, Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.RefreshBackups(), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.ListBackups(), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.ListBackups("slot.ngd"), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.CreateBackup("slot.ngd"), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.TryCreateBackup("slot.ngd", out _), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.TryGetLatestBackup("slot.ngd", out _), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.RestoreLatestBackup("slot.ngd"), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.RestoreBackup(BackupData.Empty, "slot.ngd"), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.DeleteBackup(BackupData.Empty), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.DeleteBackups("slot.ngd"), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => fixture.Service.PruneBackups("slot.ngd"), Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void PublicMethods_WithEmptyFileName_ThrowArgumentException()
        {
            using var fixture = new BackupServiceFixture();

            Assert.That(() => fixture.Service.ListBackups(""), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.CreateBackup(""), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.TryCreateBackup("", out _), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.TryGetLatestBackup("", out _), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.RestoreLatestBackup(""), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.RestoreBackup(BackupData.Empty, ""), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.DeleteBackups(""), Throws.TypeOf<ArgumentException>());
            Assert.That(() => fixture.Service.PruneBackups(""), Throws.TypeOf<ArgumentException>());
        }

        private sealed class BackupServiceFixture : IDisposable
        {
            public string RootDirectory { get; }
            public string SaveDirectory { get; }
            public string BackupDirectory { get; }
            public TestFileSystem FileSystem { get; }
            public SaveConfig Config { get; }
            public BackupService Service { get; }

            private bool _isDisposed = false;

            public BackupServiceFixture(int maxBackups = 3, bool createService = true)
            {
                RootDirectory = Path.Combine(Path.GetTempPath(), "NomadBackupServiceTests", Guid.NewGuid().ToString("N"));
                SaveDirectory = Path.Combine(RootDirectory, "SaveData");
                BackupDirectory = Path.Combine(SaveDirectory, "Backups");

                Directory.CreateDirectory(SaveDirectory);

                FileSystem = new TestFileSystem();
                Config = CreateConfig(maxBackups: maxBackups);

                Service = createService
                    ? new BackupService(Config, FileSystem)
                    : null!;
            }

            public SaveConfig CreateConfig(string? backupPath = null, int maxBackups = 3)
            {
                return new SaveConfig
                {
                    DataPath = SaveDirectory,
                    BackupPath = backupPath ?? BackupDirectory,
                    MaxBackups = maxBackups,
                    AutoSaveInterval = 5,
                    AutoSave = true,
                    ChecksumEnabled = true,
                    VerifyAfterWrite = false,
                    LogSerializationTree = false,
                    LogWriteTimings = false,
                    DebugLogging = false
                };
            }

            public string WriteSaveFile(string saveName, string content)
            {
                string path = Path.Combine(SaveDirectory, saveName + ".ngd");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, content);
                return path;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Service?.Dispose();

                    if (Directory.Exists(RootDirectory))
                    {
                        Directory.Delete(RootDirectory, true);
                    }
                }

                _isDisposed = true;
            }
        };

        private class TestFileSystem : IFileSystem
        {
            public virtual void AddSearchDirectory(string directory) { }
            public virtual void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            {
                string? directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Copy(sourcePath, destinationPath, overwrite);
            }
            public virtual IDataStream CreateStream(FileAccess accessMode, StreamType type, string outputFile = "", int length = 0) => throw new NotSupportedException();
            public virtual void CreateDirectory(string path) => Directory.CreateDirectory(path);
            public virtual void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
            public virtual void DeleteFile(string path) => File.Delete(path);
            public virtual void Dispose() => GC.SuppressFinalize(this);
            public virtual bool DirectoryExists(string path) => Directory.Exists(path);
            public virtual bool FileExists(string path) => File.Exists(path);
            public virtual string GetConfigPath() => string.Empty;
            public virtual IReadOnlyList<string> GetDirectories(string path) => Directory.GetDirectories(path);
            public virtual IReadOnlyList<string> GetFiles(string path, string searchPattern, bool recursive)
            {
                if (!Directory.Exists(path))
                {
                    return Array.Empty<string>();
                }
                return Directory.GetFiles(path, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            public virtual long GetFileSize(string path) => new FileInfo(path).Length;
            public virtual DateTime GetLastWriteTime(string path) => File.GetLastWriteTimeUtc(path);
            public virtual string GetResourcePath() => string.Empty;
            public virtual string GetSavePath() => string.Empty;
            public virtual string GetUserDataPath() => string.Empty;
            public virtual IBufferHandle? LoadFile(string path) => null;
            public virtual ValueTask<IBufferHandle?> LoadFileAsync(string path, CancellationToken ct = default) => ValueTask.FromResult<IBufferHandle?>(null);
            public virtual void MoveFile(string sourcePath, string destinationPath, bool overwrite) => File.Move(sourcePath, destinationPath, overwrite);
            public virtual IReadStream? OpenRead(IReadConfig config) => null;
            public virtual ValueTask<IReadStream?> OpenReadAsync(IReadConfig config, CancellationToken ct = default) => ValueTask.FromResult<IReadStream?>(null);
            public virtual IWriteStream? OpenWrite(IWriteConfig config) => null;
            public virtual ValueTask<IWriteStream?> OpenWriteAsync(IWriteConfig config, CancellationToken ct = default) => ValueTask.FromResult<IWriteStream?>(null);
            public virtual void ReplaceFile(string sourcePath, string destPath, string destBackupPath)
            {
                string? directory = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Copy(sourcePath, destPath, true);
                File.Delete(sourcePath);
            }
            public virtual void WriteFile(string path, byte[] buffer, int offset, int length)
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.Write(buffer, offset, length);
            }
            public virtual async ValueTask WriteFileAsync(string path, ReadOnlyMemory<byte> buffer, int offset, int length, CancellationToken ct = default)
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.WriteAsync(buffer.Slice(offset, length), ct);
            }
        };

        private sealed class ThrowingCopyFileSystem : TestFileSystem
        {
            private readonly TestFileSystem _inner;
            public ThrowingCopyFileSystem(TestFileSystem inner)
            {
                _inner = inner;
            }
            public override void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            {
                string? directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(destinationPath, "partial-backup");
                throw new IOException("Simulated copy failure.");
            }
            public override void CreateDirectory(string path) => _inner.CreateDirectory(path);
            public override void DeleteFile(string path) => _inner.DeleteFile(path);
            public override bool DirectoryExists(string path) => _inner.DirectoryExists(path);
            public override bool FileExists(string path) => _inner.FileExists(path);
            public override IReadOnlyList<string> GetFiles(string path, string searchPattern, bool recursive) => _inner.GetFiles(path, searchPattern, recursive);
            public override long GetFileSize(string path) => _inner.GetFileSize(path);
            public override DateTime GetLastWriteTime(string path) => _inner.GetLastWriteTime(path);
        };
    };
};
#endif
