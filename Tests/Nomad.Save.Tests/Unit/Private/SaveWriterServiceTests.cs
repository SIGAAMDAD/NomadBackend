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
using System.Reflection;
using Moq;
using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.Core.Memory.Buffers;
using Nomad.FileSystem.Private.Services;
using Nomad.Save.Exceptions;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.Repositories;
using Nomad.Save.Private.Services;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Tests
{
    /*
	===================================================================================

	SaveWriterServiceTests

	===================================================================================
	*/
    /// <summary>
    /// Unit tests for <see cref="SaveWriterService"/>.
    /// </summary>

    [TestFixture]
    public sealed class SaveWriterServiceTests
    {
        /*
		===============
		Constructor_WithValidDependencies_CreatesWriterWithNoSections
		===============
		*/
        /// <summary>
        /// Verifies the constructor initializes the writer in an empty state.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Constructor_WithValidDependencies_CreatesWriterWithNoSections()
        {
            using var fixture = new SaveWriterServiceFixture();

            Assert.That(fixture.Writer, Is.Not.Null);
            Assert.That(fixture.Writer.SectionCount, Is.Zero);
        }

        /*
		===============
		Constructor_WhenRequiredDependencyIsNull_ThrowsArgumentNullException
		===============
		*/
        /// <summary>
        /// Covers every constructor guard branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WhenRequiredDependencyIsNull_ThrowsArgumentNullException()
        {
            using var fixture = new SaveWriterServiceFixture();

            Assert.That(
                () => new SaveWriterService(null!, fixture.AtomicWriter, fixture.SlotRepository, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveWriterService(fixture.Config, null!, fixture.SlotRepository, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveWriterService(fixture.Config, fixture.AtomicWriter, null!, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveWriterService(fixture.Config, fixture.AtomicWriter, fixture.SlotRepository, null!, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveWriterService(fixture.Config, fixture.AtomicWriter, fixture.SlotRepository, fixture.FileSystem, null!),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        /*
		===============
		BeginSave_WithValidName_OpensMemoryFileWriterAndWritesHeader
		===============
		*/
        /// <summary>
        /// Covers the happy-path begin-save branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void BeginSave_WithValidName_OpensMemoryFileWriterAndWritesHeader()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("begin-only", new GameVersion(1, 2, 3));

            IMemoryFileWriteStream stream = GetCurrentWriter(fixture.Writer);
            Assert.That(stream, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stream.Position, Is.GreaterThan(0));
                Assert.That(fixture.Logger.Lines, Has.Some.Contains("Writing save data to begin-only"));
            }
        }

        /*
		===============
		BeginSave_WhenOpenWriteReturnsNull_ThrowsCreateSaveFileFailed
		===============
		*/
        /// <summary>
        /// Covers the failed memory-file writer branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void BeginSave_WhenOpenWriteReturnsNull_ThrowsCreateSaveFileFailed()
        {
            using var fixture = new SaveWriterServiceFixture();
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Loose);
            fileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Array.Empty<string>());
            fileSystem.Setup(fs => fs.OpenWrite(It.IsAny<IWriteConfig>()))
                .Returns((IWriteStream?)null);

            using var slotRepository = new SlotRepository(fileSystem.Object, fixture.Logger, fixture.Config);
            var backupService = new BackupService(fixture.Config, fileSystem.Object);
            var atomicWriter = new AtomicWriterService(fixture.EngineService.Object, fileSystem.Object, backupService);
            using var writer = new SaveWriterService(fixture.Config, atomicWriter, slotRepository, fileSystem.Object, fixture.Logger);
            ISaveWriterService writerService = writer;

            Assert.That(
                () => writerService.BeginSave("open-write-fails", new GameVersion(1, 0, 0)),
                Throws.TypeOf<CreateSaveFileFailed>()
            );
        }

        /*
		===============
		AddSection_BeforeBeginSave_ThrowsInvalidOperationException
		===============
		*/
        /// <summary>
        /// Covers the null writer guard inside <see cref="SaveWriterService.AddSection"/>.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void AddSection_BeforeBeginSave_ThrowsInvalidOperationException()
        {
            using var fixture = new SaveWriterServiceFixture();

            Assert.That(
                () => fixture.Writer.AddSection("Player"),
                Throws.TypeOf<InvalidOperationException>()
            );
        }

        /*
		===============
		AddSection_AfterBeginSave_AddsSectionAndIncrementsSectionCount
		===============
		*/
        /// <summary>
        /// Covers the normal section-add branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void AddSection_AfterBeginSave_AddsSectionAndIncrementsSectionCount()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("section-count", new GameVersion(1, 0, 0));
            ISaveSectionWriter section = fixture.Writer.AddSection("Player");

            Assert.That(section, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(section.Name, Is.EqualTo("Player"));
                Assert.That(fixture.Writer.SectionCount, Is.EqualTo(1));
            }
        }

        /*
		===============
		AddSection_WhenLogSerializationTreeIsEnabled_PrintsSectionTrace
		===============
		*/
        /// <summary>
        /// Covers the section logging branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("DebugLogging")]
        public void AddSection_WhenLogSerializationTreeIsEnabled_PrintsSectionTrace()
        {
            using var fixture = new SaveWriterServiceFixture(logSerializationTree: true);
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("section-log", new GameVersion(1, 0, 0));
            fixture.Writer.AddSection("Inventory");

            Assert.That(fixture.Logger.Lines, Has.Some.Contains("[Section]"));
        }

        /*
		===============
		AddSection_WhenSectionAlreadyExists_ThrowsDuplicateSectionException
		===============
		*/
        /// <summary>
        /// Covers the duplicate-section branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void AddSection_WhenSectionAlreadyExists_ThrowsDuplicateSectionException()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("duplicate-section", new GameVersion(1, 0, 0));
            fixture.Writer.AddSection("Player");

            Assert.That(
                () => fixture.Writer.AddSection("Player"),
                Throws.TypeOf<DuplicateSectionException>()
            );
        }

        /*
		===============
		EndSave_BeforeBeginSave_ThrowsInvalidOperationException
		===============
		*/
        /// <summary>
        /// Covers the null writer guard inside <c>EndSave</c>.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void EndSave_BeforeBeginSave_ThrowsInvalidOperationException()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            Assert.That(
                () => writer.EndSave("not-started", new GameVersion(1, 0, 0)),
                Throws.TypeOf<InvalidOperationException>()
            );
        }

        /*
		===============
		EndSave_WhenMemoryWriterBufferIsNull_ThrowsInvalidOperationException
		===============
		*/
        /// <summary>
        /// Covers the explicit null-buffer branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void EndSave_WhenMemoryWriterBufferIsNull_ThrowsInvalidOperationException()
        {
            using var fixture = new SaveWriterServiceFixture();
            var memoryWriter = new Mock<IMemoryFileWriteStream>(MockBehavior.Loose);
            memoryWriter.SetupGet(writer => writer.Buffer).Returns((IBufferHandle?)null);
            SetCurrentWriter(fixture.Writer, memoryWriter.Object);
            ISaveWriterService writerService = fixture.Writer;

            Assert.That(
                () => writerService.EndSave("null-buffer", new GameVersion(1, 0, 0)),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("null write buffer")
            );
        }

        /*
		===============
		EndSave_WithNoSections_WritesSaveFileAndClearsSectionCache
		===============
		*/
        /// <summary>
        /// Covers the normal finalization path for an empty save.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void EndSave_WithNoSections_WritesSaveFileAndClearsSectionCache()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("empty-save", new GameVersion(1, 2, 3));
            writer.EndSave("empty-save", new GameVersion(1, 2, 3));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.Writer.SectionCount, Is.Zero);
                Assert.That(Directory.GetFiles(fixture.SaveDirectory, "*.ngd"), Has.Length.EqualTo(1));
                Assert.That(fixture.SlotRepository.GetMetadataList(), Has.Count.EqualTo(1));
            }
        }

        /*
		===============
		EndSave_WithSections_WritesReadableSaveDataAndClearsSectionCache
		===============
		*/
        /// <summary>
        /// Covers section disposal, section cache clearing, checksum rewrite, and atomic finalization.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void EndSave_WithSections_WritesReadableSaveDataAndClearsSectionCache()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("section-save", new GameVersion(1, 0, 0));
            ISaveSectionWriter player = fixture.Writer.AddSection("Player");
            player.AddField("Health", 100);
            player.AddField("Armor", 50);

            ISaveSectionWriter world = fixture.Writer.AddSection("World");
            world.AddField("Seed", 1337);

            writer.EndSave("section-save", new GameVersion(1, 0, 0));

            using var reader = fixture.CreateReader();
            ((ISaveReaderService)reader).Load("section-save");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.Writer.SectionCount, Is.Zero);
                Assert.That(reader.SectionCount, Is.EqualTo(2));
                Assert.That(reader.FindSection("Player")!.GetField<int>("Health"), Is.EqualTo(100));
                Assert.That(reader.FindSection("Player")!.GetField<int>("Armor"), Is.EqualTo(50));
                Assert.That(reader.FindSection("World")!.GetField<int>("Seed"), Is.EqualTo(1337));
            }
        }

        /*
		===============
		EndSave_WithSupportedFieldTypes_PreservesPrimitiveAndStringValues
		===============
		*/
        /// <summary>
        /// Verifies the writer output can be read back for representative supported field types.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Serialization")]
        public void EndSave_WithSupportedFieldTypes_PreservesPrimitiveAndStringValues()
        {
            using var fixture = new SaveWriterServiceFixture();
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("types-save", new GameVersion(2, 0, 0));
            ISaveSectionWriter section = fixture.Writer.AddSection("Types");
            section.AddField("Int32", -123);
            section.AddField("UInt32", 123u);
            section.AddField("Single", 12.5f);
            section.AddField("Boolean", true);
            section.AddField("String", "Nomad");
            writer.EndSave("types-save", new GameVersion(2, 0, 0));

            using var reader = fixture.CreateReader();
            ((ISaveReaderService)reader).Load("types-save");
            ISaveSectionReader loaded = reader.FindSection("Types")!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loaded.GetField<int>("Int32"), Is.EqualTo(-123));
                Assert.That(loaded.GetField<uint>("UInt32"), Is.EqualTo(123u));
                Assert.That(loaded.GetField<float>("Single"), Is.EqualTo(12.5f));
                Assert.That(loaded.GetField<bool>("Boolean"), Is.True);
                Assert.That(loaded.GetString("String"), Is.EqualTo("Nomad"));
            }
        }

        /*
		===============
		EndSave_WhenDebugLoggingIsEnabled_PrintsFinalSaveMetadata
		===============
		*/
        /// <summary>
        /// Covers the debug metadata logging branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("DebugLogging")]
        public void EndSave_WhenDebugLoggingIsEnabled_PrintsFinalSaveMetadata()
        {
            using var fixture = new SaveWriterServiceFixture(debugLogging: true);
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("debug-save", new GameVersion(3, 4, 5));
            writer.EndSave("debug-save", new GameVersion(3, 4, 5));

            Assert.That(fixture.Logger.Lines, Has.Some.EqualTo("Finalized save data file:"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("SectionCount: 0"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("SaveName: debug-save"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("GameVersion: 30040005"));
        }

        /*
		===============
		EndSave_WhenLogSerializationTreeIsEnabled_PrintsFinalizedSectionData
		===============
		*/
        /// <summary>
        /// Covers the section finalization logging branch invoked during <c>EndSave</c>.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("DebugLogging")]
        public void EndSave_WhenLogSerializationTreeIsEnabled_PrintsFinalizedSectionData()
        {
            using var fixture = new SaveWriterServiceFixture(logSerializationTree: true);
            ISaveWriterService writer = fixture.Writer;

            writer.BeginSave("tree-save", new GameVersion(1, 0, 0));
            ISaveSectionWriter section = fixture.Writer.AddSection("Inventory");
            section.AddField("Coins", 99);
            writer.EndSave("tree-save", new GameVersion(1, 0, 0));

            Assert.That(fixture.Logger.Lines, Has.Some.EqualTo("Finalized section data:"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("Name: Inventory"));
            Assert.That(fixture.Logger.Lines, Has.Some.Contains("FieldCount: 1"));
        }

        /*
		===============
		Dispose_WhenCalledMultipleTimes_DisposesCategorySlotRepositoryAndWriterOnce
		===============
		*/
        /// <summary>
        /// Covers both dispose branches and verifies owned collaborators are not repeatedly disposed.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Lifecycle")]
        public void Dispose_WhenCalledMultipleTimes_DisposesCategorySlotRepositoryAndWriterOnce()
        {
            using var fixture = new SaveWriterServiceFixture();
            var memoryWriter = new Mock<IMemoryFileWriteStream>(MockBehavior.Loose);
            SetCurrentWriter(fixture.Writer, memoryWriter.Object);

            fixture.Writer.Dispose();
            fixture.Writer.Dispose();

            Assert.That(fixture.Logger.CategoryDisposeCount, Is.EqualTo(1));
            memoryWriter.Verify(writer => writer.Dispose(), Times.Once);
        }

        /*
		===============
		GetCurrentWriter
		===============
		*/
        /// <summary>
        /// Reads the current private memory writer for state verification.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>

        private static IMemoryFileWriteStream GetCurrentWriter(SaveWriterService writer)
        {
            FieldInfo field = typeof(SaveWriterService).GetField("_writer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(SaveWriterService), "_writer");
            return (IMemoryFileWriteStream)field.GetValue(writer)!;
        }

        /*
		===============
		SetCurrentWriter
		===============
		*/
        /// <summary>
        /// Replaces the private memory writer to isolate error branches.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="memoryWriter"></param>

        private static void SetCurrentWriter(SaveWriterService writer, IMemoryFileWriteStream memoryWriter)
        {
            FieldInfo field = typeof(SaveWriterService).GetField("_writer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(SaveWriterService), "_writer");
            field.SetValue(writer, memoryWriter);
        }

        /*
		===================================================================================

		SaveWriterServiceFixture

		===================================================================================
		*/
        /// <summary>
        /// Creates real writer dependencies around temporary save paths.
        /// </summary>

        private sealed class SaveWriterServiceFixture : IDisposable
        {
            public Mock<IEngineService> EngineService { get; }
            public CapturingLoggerService Logger { get; }
            public IFileSystem FileSystem { get; }
            public SaveConfig Config { get; }
            public SlotRepository SlotRepository { get; }
            public AtomicWriterService AtomicWriter { get; }
            public SaveWriterService Writer { get; }
            public BackupService Backup { get; }
            public string RootDirectory { get; }
            public string SaveDirectory { get; }
            public string BackupDirectory { get; }

            private bool _isDisposed = false;

            public SaveWriterServiceFixture(bool debugLogging = false, bool logSerializationTree = false)
            {
                RootDirectory = Path.Combine(Path.GetTempPath(), "NomadSaveWriterServiceTests", Guid.NewGuid().ToString("N"));
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
                Config = CreateConfig(debugLogging, logSerializationTree);
                SlotRepository = new SlotRepository(FileSystem, Logger, Config);
                Backup = new BackupService(Config, FileSystem);

                AtomicWriter = new AtomicWriterService(EngineService.Object, FileSystem, Backup);
                Writer = new SaveWriterService(Config, AtomicWriter, SlotRepository, FileSystem, Logger);
            }

            public SaveReaderService CreateReader()
                => new SaveReaderService(Config, SlotRepository, FileSystem, Logger);

            private SaveConfig CreateConfig(bool debugLogging, bool logSerializationTree)
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
                    LogSerializationTree = logSerializationTree,
                    LogWriteTimings = debugLogging,
                    DebugLogging = debugLogging
                };
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Writer?.Dispose();
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
