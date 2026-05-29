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
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.CVars.Private.Services;
using Nomad.Events;
using Nomad.FileSystem.Private.Services;
using Nomad.Save.Exceptions;
using Nomad.Save.Interfaces;
using Nomad.Save.Private;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.Services;
using Nomad.Save.ValueObjects;
using Nomad.Save.Services;

namespace Nomad.Save.Tests
{
    /*
	===================================================================================

	SaveDataProviderTests

	===================================================================================
	*/
    /// <summary>
    /// Unit tests for <see cref="SaveDataProvider"/>.
    /// </summary>

    [TestFixture]
    public sealed class SaveDataProviderTests
    {
        /*
		===============
		Constructor_WithValidDependencies_CreatesProviderAndInitializesEvents
		===============
		*/
        /// <summary>
        /// Verifies that a valid provider exposes its public event handles.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Constructor_WithValidDependencies_CreatesProviderAndInitializesEvents()
        {
            using var fixture = new SaveDataProviderFixture();

            Assert.That(fixture.Provider, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.Provider.SaveBegin, Is.Not.Null);
                Assert.That(fixture.Provider.LoadBegin, Is.Not.Null);
            }
        }

        /*
		===============
		Constructor_WhenAnyRequiredDependencyIsNull_ThrowsArgumentNullException
		===============
		*/
        /// <summary>
        /// Covers every constructor guard branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public void Constructor_WhenAnyRequiredDependencyIsNull_ThrowsArgumentNullException()
        {
            using var fixture = new SaveDataProviderFixture(createProvider: false);

            Assert.That(
                () => new SaveDataProvider(null!, fixture.EventRegistry, fixture.CVarSystem, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveDataProvider(fixture.EngineService.Object, null!, fixture.CVarSystem, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveDataProvider(fixture.EngineService.Object, fixture.EventRegistry, null!, fixture.FileSystem, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveDataProvider(fixture.EngineService.Object, fixture.EventRegistry, fixture.CVarSystem, null!, fixture.Logger),
                Throws.TypeOf<ArgumentNullException>()
            );
            Assert.That(
                () => new SaveDataProvider(fixture.EngineService.Object, fixture.EventRegistry, fixture.CVarSystem, fixture.FileSystem, null!),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        /*
		===============
		Constructor_WithValidDependencies_RegistersAllSaveCVars
		===============
		*/
        /// <summary>
        /// Ensures <c>InitConfiguration</c> registers all CVars required by the provider.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Constructor_WithValidDependencies_RegistersAllSaveCVars()
        {
            using var fixture = new SaveDataProviderFixture();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.CVarSystem.CVarExists<string>(Constants.CVars.DATA_PATH), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<string>(Constants.CVars.BACKUP_DIRECTORY), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<int>(Constants.CVars.MAX_BACKUPS), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<bool>(Constants.CVars.AUTO_SAVE_ENABLED), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<int>(Constants.CVars.AUTO_SAVE_INTERVAL), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<bool>(Constants.CVars.CHECKSUM_ENABLED), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<bool>(Constants.CVars.VERIFY_AFTER_WRITE), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<bool>(Constants.CVars.LOG_SERIALIZATION_TREE), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<bool>(Constants.CVars.LOG_WRITE_TIMINGS), Is.True);
                Assert.That(fixture.CVarSystem.CVarExists<bool>(Constants.CVars.DEBUG_LOGGING), Is.True);
            }
        }

        /*
		===============
		Constructor_WithRegisteredCVars_InitializesConfigFromCVarValues
		===============
		*/
        /// <summary>
        /// Verifies that every field copied into <see cref="SaveConfig"/> matches the registered CVar values.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void Constructor_WithRegisteredCVars_InitializesConfigFromCVarValues()
        {
            using var fixture = new SaveDataProviderFixture();

            SaveConfig config = GetConfig(fixture.Provider);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NormalizePath(config.DataPath), Is.EqualTo(NormalizePath(fixture.SaveDirectory)));
                Assert.That(NormalizePath(config.BackupPath), Is.EqualTo(NormalizePath(fixture.BackupDirectory)));
                Assert.That(config.MaxBackups, Is.EqualTo(fixture.CVarSystem.GetCVar<int>(Constants.CVars.MAX_BACKUPS)!.Value));
                Assert.That(config.AutoSave, Is.EqualTo(fixture.CVarSystem.GetCVar<bool>(Constants.CVars.AUTO_SAVE_ENABLED)!.Value));
                Assert.That(config.AutoSaveInterval, Is.EqualTo(fixture.CVarSystem.GetCVar<int>(Constants.CVars.AUTO_SAVE_INTERVAL)!.Value));
                Assert.That(config.ChecksumEnabled, Is.EqualTo(fixture.CVarSystem.GetCVar<bool>(Constants.CVars.CHECKSUM_ENABLED)!.Value));
                Assert.That(config.VerifyAfterWrite, Is.EqualTo(fixture.CVarSystem.GetCVar<bool>(Constants.CVars.VERIFY_AFTER_WRITE)!.Value));
                Assert.That(config.LogSerializationTree, Is.EqualTo(fixture.CVarSystem.GetCVar<bool>(Constants.CVars.LOG_SERIALIZATION_TREE)!.Value));
                Assert.That(config.LogWriteTimings, Is.EqualTo(fixture.CVarSystem.GetCVar<bool>(Constants.CVars.LOG_WRITE_TIMINGS)!.Value));
                Assert.That(config.DebugLogging, Is.EqualTo(fixture.CVarSystem.GetCVar<bool>(Constants.CVars.DEBUG_LOGGING)!.Value));
            }
        }

        /*
		===============
		AutoSaveEnabled_WhenCVarChanges_UpdatesProviderConfig
		===============
		*/
        /// <summary>
        /// Covers <c>OnAutoSaveEnabledChanged</c>.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("EdgeCase")]
        public void AutoSaveEnabled_WhenCVarChanges_UpdatesProviderConfig()
        {
            using var fixture = new SaveDataProviderFixture();
            ICVar<bool> cvar = fixture.CVarSystem.GetCVar<bool>(Constants.CVars.AUTO_SAVE_ENABLED)!;
            bool newValue = !cvar.Value;

            cvar.Value = newValue;

            Assert.That(GetConfig(fixture.Provider).AutoSave, Is.EqualTo(newValue));
        }

        /*
		===============
		AutoSaveInterval_WhenCVarChanges_UpdatesProviderConfig
		===============
		*/
        /// <summary>
        /// Covers <c>OnAutoSaveIntervalChanged</c>.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("EdgeCase")]
        public void AutoSaveInterval_WhenCVarChanges_UpdatesProviderConfig()
        {
            using var fixture = new SaveDataProviderFixture();
            ICVar<int> cvar = fixture.CVarSystem.GetCVar<int>(Constants.CVars.AUTO_SAVE_INTERVAL)!;
            int newValue = cvar.Value + 17;

            cvar.Value = newValue;

            Assert.That(GetConfig(fixture.Provider).AutoSaveInterval, Is.EqualTo(newValue));
        }

        /*
		===============
		ListSaveFiles_WhenNoSlotsExist_ReturnsEmptyReadOnlyList
		===============
		*/
        /// <summary>
        /// Covers the empty slot repository path.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public void ListSaveFiles_WhenNoSlotsExist_ReturnsEmptyReadOnlyList()
        {
            using var fixture = new SaveDataProviderFixture();

            IReadOnlyList<SaveFileMetadata> files = fixture.Provider.ListSaveFiles();

            Assert.That(files, Is.Not.Null);
            Assert.That(files, Is.Empty);
        }

        /*
		===============
		Save_WithNoSubscribers_CompletesAndCreatesMetadata
		===============
		*/
        /// <summary>
        /// Covers the real writer path when no external subscriber writes sections.
        /// </summary>
        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public async Task Save_WithNoSubscribers_CompletesAndCreatesMetadata()
        {
            using var fixture = new SaveDataProviderFixture();

            await fixture.Provider.Save("empty-slot", new GameVersion(1, 2, 3));
            IReadOnlyList<SaveFileMetadata> files = fixture.Provider.ListSaveFiles();

            Assert.That(files, Has.Count.EqualTo(1));
            Assert.That(files[0].SaveName, Is.EqualTo("empty-slot"));
        }

        /*
		===============
		Save_WhenSubscriberWritesSection_PublishesSaveBeginBeforeEndSave
		===============
		*/
        /// <summary>
        /// Covers normal save publication and a section write through the provided writer service.
        /// </summary>
        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public async Task Save_WhenSubscriberWritesSection_PublishesSaveBeginBeforeEndSave()
        {
            using var fixture = new SaveDataProviderFixture();
            bool published = false;

            fixture.Provider.SaveBegin.Subscribe(
                (in SaveBeginEventArgs args) =>
                {
                    published = true;
                    ISaveSectionWriter section = args.Writer.AddSection("Player");
                    section.AddField("Health", 100);
                    section.AddField("Score", 42);
                }
            );

            await fixture.Provider.Save("with-section", new GameVersion(1, 0, 0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published, Is.True);
                Assert.That(fixture.Provider.ListSaveFiles(), Has.Count.EqualTo(1));
            }
        }

        /*
		===============
		Load_WhenSaveExists_PublishesLoadBeginWithReader
		===============
		*/
        /// <summary>
        /// Covers the real load path and verifies the reader is provided to subscribers.
        /// </summary>
        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public async Task Load_WhenSaveExists_PublishesLoadBeginWithReader()
        {
            using var fixture = new SaveDataProviderFixture();
            bool loaded = false;

            fixture.Provider.SaveBegin.Subscribe(
                (in SaveBeginEventArgs args) =>
                {
                    ISaveSectionWriter section = args.Writer.AddSection("Player");
                    section.AddField("Health", 85);
                }
            );
            fixture.Provider.LoadBegin.Subscribe(
                (in LoadBeginEventArgs args) =>
                {
                    ISaveSectionReader? section = args.Reader.FindSection("Player");
                    loaded = section != null && section.GetField<int>("Health") == 85;
                }
            );

            await fixture.Provider.Save("loadable-slot", new GameVersion(1, 0, 0));
            await fixture.Provider.Load("loadable-slot");

            Assert.That(loaded, Is.True);
        }

        /*
		===============
		Load_WhenSaveFileDoesNotExist_LogsReaderFailureButStillPublishesLoadBegin
		===============
		*/
        /// <summary>
        /// Documents current provider behavior for missing files: reader logs the failure and the load event still publishes.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public async Task Load_WhenSaveFileDoesNotExist_LogsReaderFailureButStillPublishesLoadBegin()
        {
            using var fixture = new SaveDataProviderFixture();
            bool published = false;

            fixture.Provider.LoadBegin.Subscribe(
                (in LoadBeginEventArgs args) =>
                {
                    published = true;
                    Assert.That(args.Reader, Is.Not.Null);
                }
            );

            await fixture.Provider.Load("missing-slot");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published, Is.True);
                Assert.That(fixture.Logger.Errors, Has.Some.Contains("couldn't open save file"));
            }
        }

        /*
		===============
		Save_WithInjectedWriter_InvokesBeginPublishesAndEndsInOrder
		===============
		*/
        /// <summary>
        /// Covers the provider-level save flow without file-system behavior affecting branch coverage.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public async Task Save_WithInjectedWriter_InvokesBeginPublishesAndEndsInOrder()
        {
            using var fixture = new SaveDataProviderFixture();
            var writer = new FakeSaveWriterService();
            SetPrivateField(fixture.Provider, "_writerService", writer);
            bool published = false;

            fixture.Provider.SaveBegin.Subscribe(
                (in SaveBeginEventArgs args) =>
                {
                    published = true;
                    Assert.That(args.Writer, Is.SameAs(writer));
                }
            );

            await fixture.Provider.Save("fake-slot", new GameVersion(7, 8, 9));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writer.BeginSaveCount, Is.EqualTo(1));
                Assert.That(published, Is.True);
                Assert.That(writer.EndSaveCount, Is.EqualTo(1));
                Assert.That(writer.LastName, Is.EqualTo("fake-slot"));
                Assert.That(writer.LastVersion.ToInt(), Is.EqualTo(new GameVersion(7, 8, 9).ToInt()));
            }
        }

        /*
		===============
		Save_WhenBeginSaveThrows_CatchesAndLogsException
		===============
		*/
        /// <summary>
        /// Covers the save catch branch before event publication.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public async Task Save_WhenBeginSaveThrows_CatchesAndLogsException()
        {
            using var fixture = new SaveDataProviderFixture();
            var writer = new FakeSaveWriterService { BeginSaveException = new InvalidOperationException("begin failed") };
            SetPrivateField(fixture.Provider, "_writerService", writer);
            bool published = false;
            fixture.Provider.SaveBegin.Subscribe((in SaveBeginEventArgs args) => published = true);

            await fixture.Provider.Save("begin-fails", new GameVersion(1, 0, 0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writer.BeginSaveCount, Is.EqualTo(1));
                Assert.That(published, Is.False);
                Assert.That(writer.EndSaveCount, Is.Zero);
                Assert.That(fixture.Logger.Errors, Has.Some.Contains("Exception caught"));
            }
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("begin failed"));
        }

        /*
		===============
		Save_WhenEndSaveThrows_CatchesAndLogsException
		===============
		*/
        /// <summary>
        /// Covers the save catch branch after event publication.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public async Task Save_WhenEndSaveThrows_CatchesAndLogsException()
        {
            using var fixture = new SaveDataProviderFixture();
            var writer = new FakeSaveWriterService { EndSaveException = new InvalidOperationException("end failed") };
            SetPrivateField(fixture.Provider, "_writerService", writer);
            bool published = false;
            fixture.Provider.SaveBegin.Subscribe((in SaveBeginEventArgs args) => published = true);

            await fixture.Provider.Save("end-fails", new GameVersion(1, 0, 0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writer.BeginSaveCount, Is.EqualTo(1));
                Assert.That(published, Is.True);
                Assert.That(writer.EndSaveCount, Is.EqualTo(1));
                Assert.That(fixture.Logger.Errors, Has.Some.Contains("Exception caught"));
            }
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("end failed"));
        }

        /*
		===============
		Load_WithInjectedReader_InvokesLoadAndPublishesLoadBegin
		===============
		*/
        /// <summary>
        /// Covers the provider-level load flow without file-system behavior affecting branch coverage.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("HappyPath")]
        public async Task Load_WithInjectedReader_InvokesLoadAndPublishesLoadBegin()
        {
            using var fixture = new SaveDataProviderFixture();
            var reader = new FakeSaveReaderService();
            SetPrivateField(fixture.Provider, "_readerService", reader);
            bool published = false;

            fixture.Provider.LoadBegin.Subscribe(
                (in LoadBeginEventArgs args) =>
                {
                    published = true;
                    Assert.That(args.Reader, Is.SameAs(reader));
                }
            );

            await fixture.Provider.Load("fake-slot");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reader.LoadCount, Is.EqualTo(1));
                Assert.That(reader.LastName, Is.EqualTo("fake-slot"));
                Assert.That(published, Is.True);
            }
        }

        /*
		===============
		Load_WhenReaderThrowsFieldCorruptException_CatchesSpecificCorruptionBranch
		===============
		*/
        /// <summary>
        /// Covers the dedicated <see cref="FieldCorruptException"/> catch branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Corruption")]
        [Category("ErrorHandling")]
        public async Task Load_WhenReaderThrowsFieldCorruptException_CatchesSpecificCorruptionBranch()
        {
            using var fixture = new SaveDataProviderFixture();
            var reader = new FakeSaveReaderService
            {
                LoadException = new FieldCorruptException("Player", 3, 128, "bad health field")
            };
            SetPrivateField(fixture.Provider, "_readerService", reader);
            bool published = false;
            fixture.Provider.LoadBegin.Subscribe((in LoadBeginEventArgs args) => published = true);

            await fixture.Provider.Load("corrupt-slot");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reader.LoadCount, Is.EqualTo(1));
                Assert.That(published, Is.False);
                Assert.That(fixture.Logger.Errors, Has.Some.Contains("Field corruption"));
            }
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("Player"));
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("bad health field"));
        }

        /*
		===============
		Load_WhenReaderThrowsGeneralException_CatchesGenericBranch
		===============
		*/
        /// <summary>
        /// Covers the general load catch branch.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("ErrorHandling")]
        public async Task Load_WhenReaderThrowsGeneralException_CatchesGenericBranch()
        {
            using var fixture = new SaveDataProviderFixture();
            var reader = new FakeSaveReaderService { LoadException = new InvalidOperationException("reader failed") };
            SetPrivateField(fixture.Provider, "_readerService", reader);
            bool published = false;
            fixture.Provider.LoadBegin.Subscribe((in LoadBeginEventArgs args) => published = true);

            await fixture.Provider.Load("reader-fails");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reader.LoadCount, Is.EqualTo(1));
                Assert.That(published, Is.False);
                Assert.That(fixture.Logger.Errors, Has.Some.Contains("Exception caught"));
            }
            Assert.That(fixture.Logger.Errors, Has.Some.Contains("reader failed"));
        }

        /*
		===============
		Dispose_WhenCalledMultipleTimes_DisposesCollaboratorsOnceAndDoesNotThrow
		===============
		*/
        /// <summary>
        /// Covers both dispose branches.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Lifecycle")]
        public void Dispose_WhenCalledMultipleTimes_DisposesCollaboratorsOnceAndDoesNotThrow()
        {
            using var fixture = new SaveDataProviderFixture();
            var reader = new FakeSaveReaderService();
            var writer = new FakeSaveWriterService();
            SetPrivateField(fixture.Provider, "_readerService", reader);
            SetPrivateField(fixture.Provider, "_writerService", writer);

            fixture.Provider.Dispose();
            fixture.Provider.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reader.DisposeCount, Is.EqualTo(1));
                Assert.That(writer.DisposeCount, Is.EqualTo(1));
            }
        }

        /*
		===============
		Dispose_WhenAutoSaveCVarsChangeAfterDispose_DoesNotMutateConfig
		===============
		*/
        /// <summary>
        /// Verifies that dispose unsubscribes both CVar change handlers.
        /// </summary>

        [Test]
        [Category("Unit")]
        [Category("Lifecycle")]
        public void Dispose_WhenAutoSaveCVarsChangeAfterDispose_DoesNotMutateConfig()
        {
            using var fixture = new SaveDataProviderFixture();
            SaveConfig beforeDispose = GetConfig(fixture.Provider);
            ICVar<bool> autoSaveEnabled = fixture.CVarSystem.GetCVar<bool>(Constants.CVars.AUTO_SAVE_ENABLED)!;
            ICVar<int> autoSaveInterval = fixture.CVarSystem.GetCVar<int>(Constants.CVars.AUTO_SAVE_INTERVAL)!;

            fixture.Provider.Dispose();
            autoSaveEnabled.Value = !beforeDispose.AutoSave;
            autoSaveInterval.Value = beforeDispose.AutoSaveInterval + 500;

            SaveConfig afterChanges = GetConfig(fixture.Provider);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterChanges.AutoSave, Is.EqualTo(beforeDispose.AutoSave));
                Assert.That(afterChanges.AutoSaveInterval, Is.EqualTo(beforeDispose.AutoSaveInterval));
            }
        }

        private static SaveConfig GetConfig(SaveDataProvider provider)
        {
            FieldInfo field = typeof(SaveDataProvider).GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(SaveDataProvider), "_config");
            return (SaveConfig)field.GetValue(provider)!;
        }

        private static void SetPrivateField<T>(SaveDataProvider provider, string fieldName, T value)
        {
            FieldInfo field = typeof(SaveDataProvider).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(SaveDataProvider), fieldName);
            field.SetValue(provider, value);
        }

        private static string NormalizePath(string path)
            => path.Replace('\\', '/').TrimEnd('/');

        private sealed class SaveDataProviderFixture : IDisposable
        {
            public Mock<IEngineService> EngineService { get; }
            public CapturingLoggerService Logger { get; }
            public GameEventRegistry EventRegistry { get; }
            public CVarSystem CVarSystem { get; }
            public IFileSystem FileSystem { get; }
            public SaveDataProvider Provider { get; }
            public string RootDirectory { get; }
            public string SaveDirectory { get; }
            public string BackupDirectory { get; }

            private bool _isDisposed = false;

            public SaveDataProviderFixture(bool createProvider = true)
            {
                RootDirectory = Path.Combine(Path.GetTempPath(), "NomadSaveDataProviderTests", Guid.NewGuid().ToString("N"));
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
                EventRegistry = new GameEventRegistry(Logger);
                CVarSystem = new CVarSystem(EventRegistry, Logger);

                Provider = createProvider
                    ? new SaveDataProvider(EngineService.Object, EventRegistry, CVarSystem, FileSystem, Logger)
                    : null!;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Provider?.Dispose();
                    CVarSystem?.Dispose();
                    EventRegistry?.Dispose();
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
        };

        private sealed class CapturingLoggerCategory : ILoggerCategory
        {
            private readonly CapturingLoggerService _logger;

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
                GC.SuppressFinalize(this);
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

        private sealed class FakeSaveReaderService : ISaveReaderService
        {
            public int LoadCount { get; private set; }
            public int DisposeCount { get; private set; }
            public string LastName { get; private set; } = string.Empty;
            public Exception? LoadException { get; set; }

            public void Dispose()
            {
                DisposeCount++;
                GC.SuppressFinalize(this);
            }

            public ISaveSectionReader? FindSection(string sectionId)
                => null;

            void ISaveReaderService.Load(string name)
            {
                LoadCount++;
                LastName = name;
                if (LoadException != null)
                {
                    throw LoadException;
                }
            }
        };

        private sealed class FakeSaveWriterService : ISaveWriterService
        {
            public int BeginSaveCount { get; private set; }
            public int EndSaveCount { get; private set; }
            public int DisposeCount { get; private set; }
            public string LastName { get; private set; } = string.Empty;
            public GameVersion LastVersion { get; private set; }
            public Exception? BeginSaveException { get; set; }
            public Exception? EndSaveException { get; set; }

            public ISaveSectionWriter AddSection(string sectionId)
                => new FakeSaveSectionWriter(sectionId);

            public void Dispose()
            {
                DisposeCount++;
                GC.SuppressFinalize(this);
            }

            void ISaveWriterService.BeginSave(string name, GameVersion gameVersion)
            {
                BeginSaveCount++;
                LastName = name;
                LastVersion = gameVersion;
                if (BeginSaveException != null)
                {
                    throw BeginSaveException;
                }
            }

            void ISaveWriterService.EndSave(string name, GameVersion gameVersion)
            {
                EndSaveCount++;
                LastName = name;
                LastVersion = gameVersion;
                if (EndSaveException != null)
                {
                    throw EndSaveException;
                }
            }
        };

        private sealed class FakeSaveSectionWriter : ISaveSectionWriter
        {
            private readonly Dictionary<string, object?> _fields = new Dictionary<string, object?>();

            public string Name { get; }
            public int FieldCount => _fields.Count;

            public FakeSaveSectionWriter(string name)
            {
                Name = name;
            }

            public void AddField<T>(string fieldId, T value)
            {
                _fields[fieldId] = value;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public bool HasField<T>(string fieldId)
                => _fields.TryGetValue(fieldId, out object? value) && value is T;
        };
    };
};
#endif
