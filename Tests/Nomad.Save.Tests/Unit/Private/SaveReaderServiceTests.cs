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
using Moq;
using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.FileSystem.Private.Services;
using Nomad.Save.Exceptions;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.Repositories;
using Nomad.Save.Private.Services;
using Nomad.Save.Private.ValueObjects;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Tests
{
    /*
	===================================================================================

	SaveReaderServiceTests

	===================================================================================
	*/
    /// <summary>
    /// Unit tests for <see cref="SaveReaderService"/>.
    /// </summary>

    [TestFixture]
    public sealed class SaveReaderServiceTests
    {
        /*
		===============
		Constructor_WithValidDependencies_CreatesReaderWithNoSections
		===============
		*/
        /// <summary>
        /// Verifies the happy-path constructor initializes the section cache.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Constructor_WithValidDependencies_CreatesReaderWithNoSections()
        {
            using var fixture = new SaveReaderServiceFixture();

            Assert.That(fixture.Reader, Is.Not.Null);
            Assert.That(fixture.Reader.SectionCount, Is.Zero);
        }

        /*
		===============
		Constructor_WhenRequiredDependencyIsNull_ThrowsExpectedException
		===============
		*/
        /// <summary>
        /// Covers every constructor guard path.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WhenRequiredDependencyIsNull_ThrowsExpectedException()
        {
            using var fixture = new SaveReaderServiceFixture();

            Assert.That(
                () => new SaveReaderService(fixture.Config, null!, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveReaderService(fixture.Config, fixture.SlotRepository, null!, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveReaderService(null!, fixture.SlotRepository, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );

            Assert.That(
                () => new SaveReaderService(fixture.Config, fixture.SlotRepository, fixture.FileSystem, null!),
                Throws.TypeOf<NullReferenceException>()
            );
        }

        /*
		===============
		Dispose_WhenCalledMultipleTimes_DisposesCategoryOnceAndDoesNotThrow
		===============
		*/
        /// <summary>
        /// Covers both dispose branches.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Lifecycle")]
        public void Dispose_WhenCalledMultipleTimes_DisposesCategoryOnceAndDoesNotThrow()
        {
            using var fixture = new SaveReaderServiceFixture();

            fixture.Reader.Dispose();
            fixture.Reader.Dispose();

            Assert.That(fixture.Logger.CategoryDisposeCount, Is.EqualTo(1));
        }

        /*
		===============
		Load_WhenFileCannotBeOpened_LogsErrorAndLeavesCacheEmpty
		===============
		*/
        /// <summary>
        /// Covers the null reader branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Load_WhenFileCannotBeOpened_LogsErrorAndLeavesCacheEmpty()
        {
            using var fixture = new SaveReaderServiceFixture();

            fixture.Load("missing-slot");

            Assert.That(fixture.Reader.SectionCount, Is.Zero);
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("couldn't open save file"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("Loading save data from"));
        }

        /*
		===============
		Load_WhenOpenReadReturnsNonMemoryStream_LogsErrorAndLeavesCacheEmpty
		===============
		*/
        /// <summary>
        /// Covers the failed <see cref="IMemoryReadStream"/> cast path.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Load_WhenOpenReadReturnsNonMemoryStream_LogsErrorAndLeavesCacheEmpty()
        {
            using var fixture = new SaveReaderServiceFixture();
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Loose);
            fileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Array.Empty<string>());
            fileSystem.Setup(fs => fs.OpenRead(It.IsAny<IReadConfig>()))
                .Returns(new Mock<IReadStream>(MockBehavior.Loose).Object);

            using var slotRepository = new SlotRepository(fileSystem.Object, fixture.Logger, fixture.Config);
            using var reader = new SaveReaderService(fixture.Config, slotRepository, fileSystem.Object, fixture.Logger);

            ((ISaveReaderService)reader).Load("non-memory-reader");

            Assert.That(reader.SectionCount, Is.Zero);
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("couldn't open save file"));
        }

        /*
		===============
		Load_WhenSaveHasNoSections_CompletesAndLeavesCacheEmpty
		===============
		*/
        /// <summary>
        /// Covers a valid save header with zero sections.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Load_WhenSaveHasNoSections_CompletesAndLeavesCacheEmpty()
        {
            using var fixture = new SaveReaderServiceFixture();
            fixture.CreateSaveFile("empty-save", new GameVersion(1, 2, 3));

            fixture.Logger.Clear();
            fixture.Load("empty-save");

            Assert.That(fixture.Reader.SectionCount, Is.Zero);
            Assert.That(fixture.Logger.Lines, Has.Some.EqualTo("...Finished loading save data"));
            Assert.That(fixture.Logger.Errors, Is.Empty);
        }

        /*
		===============
		Load_WhenDebugLoggingIsEnabled_PrintsSaveMetadata
		===============
		*/
        /// <summary>
        /// Covers the debug logging branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("DebugLogging")]
        public void Load_WhenDebugLoggingIsEnabled_PrintsSaveMetadata()
        {
            using var fixture = new SaveReaderServiceFixture(debugLogging: true);
            fixture.CreateSaveFile("debug-save", new GameVersion(4, 5, 6));

            fixture.Logger.Clear();
            fixture.Load("debug-save");

            Assert.That(fixture.Logger.Lines, Has.Some.EqualTo("Got save file metadata:"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("SectionCount: 0"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("SaveName: debug-save"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("GameVersion: 40050006"));
        }

        /*
		===============
		Load_WhenSaveContainsSingleSection_LoadsSectionAndFields
		===============
		*/
        /// <summary>
        /// Covers section creation and lookup for a normal one-section save.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Load_WhenSaveContainsSingleSection_LoadsSectionAndFields()
        {
            using var fixture = new SaveReaderServiceFixture();
            fixture.CreateSaveFile(
                "player-save",
                new GameVersion(1, 0, 0),
                writer =>
                {
                    ISaveSectionWriter section = writer.AddSection("Player");
                    section.AddField("Health", 90);
                    section.AddField("Armor", 25);
                }
            );

            fixture.Load("player-save");
            ISaveSectionReader? player = fixture.Reader.FindSection("Player");

            Assert.That(fixture.Reader.SectionCount, Is.EqualTo(1));
            Assert.That(player, Is.Not.Null);
            Assert.That(player!.Name, Is.EqualTo("Player"));
            Assert.That(player.FieldCount, Is.EqualTo(2));
            Assert.That(player.GetField<int>("Health"), Is.EqualTo(90));
            Assert.That(player.GetField<int>("Armor"), Is.EqualTo(25));
        }

        /*
		===============
		Load_WhenSaveContainsMultipleSections_LoadsAllSections
		===============
		*/
        /// <summary>
        /// Covers the section loop for more than one section.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Load_WhenSaveContainsMultipleSections_LoadsAllSections()
        {
            using var fixture = new SaveReaderServiceFixture();
            fixture.CreateSaveFile(
                "multi-section-save",
                new GameVersion(1, 0, 0),
                writer =>
                {
                    ISaveSectionWriter player = writer.AddSection("Player");
                    player.AddField("Health", 100);

                    ISaveSectionWriter world = writer.AddSection("World");
                    world.AddField("Seed", 12345);
                }
            );

            fixture.Load("multi-section-save");

            Assert.That(fixture.Reader.SectionCount, Is.EqualTo(2));
            Assert.That(fixture.Reader.FindSection("Player")!.GetField<int>("Health"), Is.EqualTo(100));
            Assert.That(fixture.Reader.FindSection("World")!.GetField<int>("Seed"), Is.EqualTo(12345));
        }

        /*
		===============
		Load_WhenCalledAgain_ClearsPreviouslyLoadedSections
		===============
		*/
        /// <summary>
        /// Covers the cache-clear behavior before parsing a new save.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("EdgeCase")]
        public void Load_WhenCalledAgain_ClearsPreviouslyLoadedSections()
        {
            using var fixture = new SaveReaderServiceFixture();
            fixture.CreateSaveFile(
                "first-save",
                new GameVersion(1, 0, 0),
                writer => writer.AddSection("First").AddField("Value", 1)
            );
            fixture.CreateSaveFile(
                "second-save",
                new GameVersion(1, 0, 0),
                writer => writer.AddSection("Second").AddField("Value", 2)
            );

            fixture.Load("first-save");
            Assert.That(fixture.Reader.FindSection("First"), Is.Not.Null);

            fixture.Logger.Clear();
            fixture.Load("second-save");

            Assert.That(fixture.Reader.SectionCount, Is.EqualTo(1));
            Assert.That(fixture.Reader.FindSection("Second"), Is.Not.Null);
            Assert.That(fixture.Reader.FindSection("First"), Is.Null);
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("section 'First' not found"));
        }

        /*
		===============
		FindSection_WhenSectionIsMissing_ReturnsNullAndLogsError
		===============
		*/
        /// <summary>
        /// Covers the missing-section branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void FindSection_WhenSectionIsMissing_ReturnsNullAndLogsError()
        {
            using var fixture = new SaveReaderServiceFixture();

            ISaveSectionReader? section = fixture.Reader.FindSection("MissingSection");

            Assert.That(section, Is.Null);
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("section 'MissingSection' not found"));
        }

        /*
		===============
		Load_WhenHeaderSectionCountIsNegative_ThrowsSaveFileCorruptException
		===============
		*/
        /// <summary>
        /// Covers the negative section-count corruption branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Corruption")]
        [Category("ErrorHandling")]
        public void Load_WhenHeaderSectionCountIsNegative_ThrowsSaveFileCorruptException()
        {
            using var fixture = new SaveReaderServiceFixture();
            fixture.CreateHeaderOnlySaveFile("negative-section-count", sectionCount: -1);

            Assert.That(
                () => fixture.Load("negative-section-count"),
                Throws.TypeOf<SaveFileCorruptException>()
                    .With.Message.Contains("Invalid section count")
            );
        }

        /*
		===============
		Load_WhenSectionIsCorrupt_PropagatesSectionCorruptException
		===============
		*/
        /// <summary>
        /// Verifies section parser corruption is not swallowed by the reader service.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Corruption")]
        [Category("ErrorHandling")]
        public void Load_WhenSectionIsCorrupt_PropagatesSectionCorruptException()
        {
            using var fixture = new SaveReaderServiceFixture();
            fixture.CreateCorruptSectionSaveFile("bad-section");

            Assert.That(
                () => fixture.Load("bad-section"),
                Throws.TypeOf<SectionCorruptException>()
            );
        }

        /*
		===================================================================================

		SaveReaderServiceFixture

		===================================================================================
		*/
        /// <summary>
        /// Creates real save reader dependencies around temporary paths.
        /// </summary>

        private sealed class SaveReaderServiceFixture : IDisposable
        {
            public Mock<IEngineService> EngineService { get; }
            public CapturingLoggerService Logger { get; }
            public IFileSystem FileSystem { get; }
            public SaveConfig Config { get; }
            public SlotRepository SlotRepository { get; }
            public SaveReaderService Reader { get; }
            public string RootDirectory { get; }
            public string SaveDirectory { get; }
            public string BackupDirectory { get; }

            private bool _isDisposed = false;

            public SaveReaderServiceFixture(bool debugLogging = false)
            {
                RootDirectory = Path.Combine(Path.GetTempPath(), "NomadSaveReaderServiceTests", Guid.NewGuid().ToString("N"));
                SaveDirectory = Path.Combine(RootDirectory, "SaveData");
                BackupDirectory = Path.Combine(SaveDirectory, "Backups");

                Directory.CreateDirectory(RootDirectory);
                Directory.CreateDirectory(SaveDirectory);
                Directory.CreateDirectory(BackupDirectory);

                EngineService = new Mock<IEngineService>(MockBehavior.Loose);
                EngineService.Setup(service => service.GetStoragePath(StorageScope.UserData)).Returns(RootDirectory);
                EngineService.Setup(service => service.GetStoragePath(StorageScope.StreamingAssets)).Returns(RootDirectory);
                EngineService.Setup(service => service.GetStoragePath(StorageScope.Install)).Returns(RootDirectory);
                EngineService.Setup(service => service.GetStoragePath(It.IsAny<string>(), It.IsAny<StorageScope>()))
                    .Returns((string relativePath, StorageScope scope) => Path.Combine(RootDirectory, relativePath));

                Logger = new CapturingLoggerService();
                FileSystem = new FileSystemService(EngineService.Object, Logger);
                Config = CreateConfig(debugLogging);
                SlotRepository = new SlotRepository(FileSystem, Logger, Config);
                Reader = new SaveReaderService(Config, SlotRepository, FileSystem, Logger);
            }

            public void Load(string name)
            {
                ((ISaveReaderService)Reader).Load(name);
            }

            public string GetSlotPath(string name)
            {
                return SlotRepository.AddSaveFile(name, false);
            }

            public void CreateSaveFile(string name, GameVersion version, Action<ISaveWriterService>? configure = null)
            {
                var backupService = new BackupService(Config, FileSystem);
                var atomicWriter = new AtomicWriterService(EngineService.Object, FileSystem, backupService);
                using var writer = new SaveWriterService(Config, atomicWriter, SlotRepository, FileSystem, Logger);
                ISaveWriterService writerService = writer;

                writerService.BeginSave(name, version);
                configure?.Invoke(writerService);
                writerService.EndSave(name, version);
            }

            public void CreateHeaderOnlySaveFile(string name, int sectionCount)
            {
                string filePath = GetSlotPath(name);
                using var writer = FileSystem.OpenWrite(
                    new MemoryFileWriteConfig { FilePath = filePath }
                ) as IMemoryFileWriteStream ?? throw new InvalidOperationException("Expected memory file writer.");

                var header = new SaveHeader(
                    name: name,
                    version: new GameVersion(1, 0, 0),
                    sectionCount: sectionCount,
                    checksum: Checksum.Empty
                );
                header.Serialize(writer);
            }

            public void CreateCorruptSectionSaveFile(string name)
            {
                string filePath = GetSlotPath(name);
                using var writer = FileSystem.OpenWrite(
                    new MemoryFileWriteConfig { FilePath = filePath }
                ) as IMemoryFileWriteStream ?? throw new InvalidOperationException("Expected memory file writer.");

                var header = new SaveHeader(
                    name: name,
                    version: new GameVersion(1, 0, 0),
                    sectionCount: 1,
                    checksum: Checksum.Empty
                );
                header.Serialize(writer);

                // Negative byte length forces SectionHeader.Load to throw SectionCorruptException.
                writer.WriteInt32(-1);
            }

            private SaveConfig CreateConfig(bool debugLogging)
            {
                return new SaveConfig
                {
                    DataPath = SaveDirectory,
                    BackupPath = BackupDirectory,
                    MaxBackups = 3,
                    AutoSaveInterval = 5,
                    AutoSave = true,
                    ChecksumEnabled = true,
                    VerifyAfterWrite = false,
                    LogSerializationTree = debugLogging,
                    LogWriteTimings = debugLogging,
                    DebugLogging = debugLogging
                };
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Reader?.Dispose();
                    SlotRepository?.Dispose();
                    FileSystem?.Dispose();
                    Logger?.Dispose();

                    if (Directory.Exists(RootDirectory))
                    {
                        Directory.Delete(RootDirectory, true);
                    }
                }
                _isDisposed = true;
            }
        };

        /*
		===================================================================================

		CapturingLoggerService

		===================================================================================
		*/
        /// <summary>
        /// Test logger that captures root and category messages.
        /// </summary>

        private sealed class CapturingLoggerService : ILoggerService
        {
            private readonly List<string> _lines = new List<string>();
            private readonly List<string> _warnings = new List<string>();
            private readonly List<string> _errors = new List<string>();
            private readonly List<string> _debug = new List<string>();

            public IReadOnlyList<string> Lines => _lines;
            public IReadOnlyList<string> Warnings => _warnings;
            public IReadOnlyList<string> Errors => _errors;
            public IReadOnlyList<string> Debug => _debug;
            public int CategoryDisposeCount { get; private set; }

            public void AddSink(ILoggerSink sink) { }

            public void Clear()
            {
                _lines.Clear();
                _warnings.Clear();
                _errors.Clear();
                _debug.Clear();
            }

            public ILoggerCategory CreateCategory(string name, LogLevel level, bool enabled)
                => new CapturingLoggerCategory(this, name, level, enabled);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public void InitConfig(ICVarSystemService cvarSystem) { }

            public void PrintDebug(string message)
                => _debug.Add(message);

            public void PrintError(string message)
                => _errors.Add(message);

            public void PrintLine(string message)
                => _lines.Add(message);

            public void PrintWarning(string message)
                => _warnings.Add(message);

            public void NotifyCategoryDisposed()
                => CategoryDisposeCount++;
        };

        /*
		===================================================================================

		CapturingLoggerCategory

		===================================================================================
		*/
        /// <summary>
        /// Logger category that forwards output to the parent capture sink.
        /// </summary>

        private sealed class CapturingLoggerCategory : ILoggerCategory
        {
            private readonly CapturingLoggerService _logger;
            private bool _isDisposed = false;

            public string Name { get; }
            public LogLevel Level { get; }
            public bool Enabled { get; set; }

            public CapturingLoggerCategory(CapturingLoggerService logger, string name, LogLevel level, bool enabled)
            {
                _logger = logger;
                Name = name;
                Level = level;
                Enabled = enabled;
            }

            public void AddSink(ILoggerSink sink) { }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _logger.NotifyCategoryDisposed();
                }
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }

            public void PrintDebug(string message)
                => _logger.PrintDebug(message);

            public void PrintError(string message)
                => _logger.PrintError(message);

            public void PrintLine(string message)
                => _logger.PrintLine(message);

            public void PrintWarning(string message)
                => _logger.PrintWarning(message);

            public void RemoveSink(ILoggerSink sink) { }
        };
    };
};
#endif
