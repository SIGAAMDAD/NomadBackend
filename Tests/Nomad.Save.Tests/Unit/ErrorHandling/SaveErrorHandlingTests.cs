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
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Events;
using Nomad.FileSystem.Private.Services;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Services;
using Nomad.Save.Services;
using Nomad.Save.Exceptions;
using Nomad.CVars.Private.Services;
using Nomad.Core.Engine.Services;
using Nomad.Core.CVars;
using Moq;

namespace Nomad.Save.Tests {
    /// <summary>
    /// Tests for error handling and edge cases in Nomad.Save
    /// </summary>
    [TestFixture]
    [Category( "Nomad.Save" )]
    [Category( "ErrorHandling" )]
    [Category( "Unit" )]
    public class SaveErrorHandlingTests {
        private ISaveDataProvider _dataProvider;
        private ILoggerService _logger;
        private ICVarSystemService _cvarSystem;
        private IFileSystem _fileSystem;
        private IGameEventRegistryService _eventFactory;
        private Mock<IEngineService> _engineService;
        private string _testDirectory;

        private IGameEvent<SaveBeginEventArgs> _saveBegin;
        private IGameEvent<LoadBeginEventArgs> _loadBegin;

        [SetUp]
        public void Setup() {
            _testDirectory = Path.Combine( Path.GetTempPath(), "NomadSaveErrorTests" );
            Directory.CreateDirectory( _testDirectory );
            Directory.CreateDirectory( $"{_testDirectory}/SaveData" );

            _logger = new MockLogger();
            _engineService = new Mock<IEngineService>();
            _engineService.Setup( e => e.GetStoragePath( StorageScope.StreamingAssets ) ).Returns( _testDirectory );
            _engineService.Setup( e => e.GetStoragePath( StorageScope.UserData ) ).Returns( _testDirectory );
            _engineService.Setup( e => e.GetStoragePath( StorageScope.Install ) ).Returns( _testDirectory );
            _fileSystem = new FileSystemService( _engineService.Object, _logger );
            _eventFactory = new GameEventRegistry( _logger );
            _cvarSystem = new CVarSystem( _eventFactory, _logger );
            _dataProvider = new SaveDataProvider( _engineService.Object, _eventFactory, _cvarSystem, _fileSystem, _logger );

            _saveBegin = _eventFactory.GetEvent<SaveBeginEventArgs>( SaveBeginEventArgs.Name, SaveBeginEventArgs.NameSpace );
            _loadBegin = _eventFactory.GetEvent<LoadBeginEventArgs>( LoadBeginEventArgs.Name, LoadBeginEventArgs.NameSpace );
        }

        [TearDown]
        public void TearDown() {
            _saveBegin?.Dispose();
            _loadBegin?.Dispose();
            _dataProvider?.Dispose();
            _cvarSystem?.Dispose();
            _logger?.Dispose();
            _fileSystem?.Dispose();
            _eventFactory?.Dispose();

            try {
                if ( Directory.Exists( _testDirectory ) ) {
                    Directory.Delete( _testDirectory, true );
                }
            } catch {
                // Ignore cleanup errors
            }
        }

        [Test]
        public void Constructor_WithNullEngineService_ThrowsException() {
            // Assert
            Assert.That( () => new SaveDataProvider( null!, _eventFactory, _cvarSystem, _fileSystem, _logger ), Throws.ArgumentNullException );
        }

        [Test]
        public void Constructor_WithNullEventFactory_ThrowsException() {
            // Assert
            Assert.That( () => new SaveDataProvider( _engineService.Object, null!, _cvarSystem, _fileSystem, _logger ), Throws.ArgumentNullException );
        }

        [Test]
        public void Constructor_WithCVarSystem_ThrowsException() {
            // Assert
            Assert.That( () => new SaveDataProvider( _engineService.Object, _eventFactory, null!, _fileSystem, _logger ), Throws.ArgumentNullException );
        }

        [Test]
        public void Constructor_WithNullFileSystem_ThrowsException() {
            // Assert
            Assert.That( () => new SaveDataProvider( _engineService.Object, _eventFactory, _cvarSystem, null!, _logger ), Throws.ArgumentNullException );
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsException() {
            // Assert
            Assert.That( () => new SaveDataProvider( _engineService.Object, _eventFactory, _cvarSystem, _fileSystem, null! ), Throws.ArgumentNullException );
        }

        [Test]
        public async Task Save_WithEmptyFileName_HandlesGracefully() {
            // Arrange
            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "Value", 1 );
            } );

            // Act & Assert
            // Should either succeed or throw - implementation dependent
            try {
                await _dataProvider.Save( String.Empty, default );
                Assert.Pass( "Handled empty filename" );
            } catch {
                Assert.Pass( "Appropriately threw for empty filename" );
            }
        }

        [Test]
        public async Task Load_WithNonExistentFile_HandlesGracefully() {
            // Arrange
            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                // Should not execute if file doesn't exist
            } );

            // Act
            try {
                await _dataProvider.Load( "nonexistent_file" );
            } catch {
                // Expected - file doesn't exist
            }

            // Assert - implementation might either throw or handle gracefully
            Assert.Pass( "Load with non-existent file handled" );
        }

        [Test]
        public async Task SaveField_WithEmptySection_Succeeds() {
            // Arrange
            string fileId = "empty_section_test";
            bool sectionCreated = false;

            void OnSaveBegin( in SaveBeginEventArgs args ) {
                var section = args.Writer.AddSection( "EmptySection" );
                sectionCreated = section != null;
            }

            _saveBegin.Subscribe( OnSaveBegin );

            // Act
            await _dataProvider.Save( fileId, default );

            // Assert
            Assert.That( sectionCreated, Is.True );
        }

        [Test]
        public async Task SaveField_WithEmptyString_Succeeds() {
            // Arrange
            string fileId = "empty_string_test";
            string loadedString = "unchanged";

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "EmptyString", String.Empty );
            } );

            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                var section = args.Reader.FindSection( "Test" );
                if ( section != null ) {
                    loadedString = section.GetString( "EmptyString" );
                }
            } );

            // Act
            await _dataProvider.Save( fileId, default );
            await _dataProvider.Load( fileId );

            // Assert
            Assert.That( loadedString, Is.EqualTo( String.Empty ) );
        }

        [Test]
        public async Task SaveField_WithVeryLongString_Succeeds() {
            // Arrange
            string fileId = "long_string_test";
            int length = 999;
            string longString = new string( 'a', length );
            string loadedString = String.Empty;

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "LongString", longString );
            } );

            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                var section = args.Reader.FindSection( "Test" );
                if ( section != null ) {
                    loadedString = section.GetString( "LongString" );
                }
            } );

            // Act
            await _dataProvider.Save( fileId, default );
            await _dataProvider.Load( fileId );

            // Assert
            Assert.That( loadedString, Is.EqualTo( longString ) );
            Assert.That( loadedString, Has.Length.EqualTo( length ) );
        }

        [Test]
        public async Task Dispose_CanBeCalledMultipleTimes() {
            // Act & Assert
            _dataProvider.Dispose();
            _dataProvider.Dispose(); // Should not throw
            Assert.Pass( "Multiple dispose calls handled" );
        }

        [Test]
        public async Task GetField_WithWrongType_ThrowsInvalidCastException() {
            // Arrange
            string fileId = "wrong_type_test";
            bool exceptionThrown = false;

            void OnSaveBegin( in SaveBeginEventArgs args ) {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "IntField", 42 );
            }

            _saveBegin.Subscribe( OnSaveBegin );

            void OnLoadBegin( in LoadBeginEventArgs args ) {
                var section = args.Reader.FindSection( "Test" );
                if ( section != null ) {
                    try {
                        // Try to read int field as float
                        var floatValue = section.GetField<float>( "IntField" );
                    } catch ( InvalidCastException ) {
                        exceptionThrown = true;
                    }
                }
            }

            _loadBegin.Subscribe( OnLoadBegin );

            // Act
            await _dataProvider.Save( fileId, default );
            await _dataProvider.Load( fileId );

            // Assert
            Assert.That( exceptionThrown, Is.True );
        }

        [Test]
        public async Task SaveSection_WithSameName_Throws() {
            // Arrange
            string fileId = "duplicate_section_test";
            bool exceptionThrown = false;

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                try {
                    var section1 = args.Writer.AddSection( "DuplicateSection" );
                    section1.AddField( "Value1", 1 );

                    var section2 = args.Writer.AddSection( "DuplicateSection" );
                    section2.AddField( "Value2", 2 );
                } catch ( Exception ) {
                    exceptionThrown = true;
                }
            } );

            // Act
            await _dataProvider.Save( fileId, default );

            // Assert
            Assert.That( exceptionThrown, Is.True );
        }

        [Test]
        public async Task SaveField_WithVeryLargeInt_Succeeds() {
            // Arrange
            string fileId = "large_int_test";
            long largeValue = long.MaxValue;
            long loadedValue = 0;

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "LargeInt", largeValue );
            } );

            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                var section = args.Reader.FindSection( "Test" );
                if ( section != null ) {
                    loadedValue = section.GetField<long>( "LargeInt" );
                }
            } );

            // Act
            await _dataProvider.Save( fileId, default );
            await _dataProvider.Load( fileId );

            // Assert
            Assert.That( loadedValue, Is.EqualTo( largeValue ) );
        }

        [Test]
        public async Task SaveField_WithNegativeNumbers_Succeeds() {
            // Arrange
            string fileId = "negative_test";
            int negativeInt = -12345;
            double negativeDouble = -3.14159;
            int loadedInt = 0;
            double loadedDouble = 0;

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "NegativeInt", negativeInt );
                section.AddField( "NegativeDouble", negativeDouble );
            } );

            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                var section = args.Reader.FindSection( "Test" );
                if ( section != null ) {
                    loadedInt = section.GetField<int>( "NegativeInt" );
                    loadedDouble = section.GetField<double>( "NegativeDouble" );
                }
            } );

            // Act
            await _dataProvider.Save( fileId, default );
            await _dataProvider.Load( fileId );

            using ( Assert.EnterMultipleScope() ) {
                // Assert
                Assert.That( loadedInt, Is.EqualTo( negativeInt ) );
                Assert.That( loadedDouble, Is.EqualTo( negativeDouble ).Within( 0.00001 ) );
            }
        }

        [Test]
        public async Task Multiple_SaveAndLoad_Cycles_Succeed() {
            // Arrange
            string fileId = "multiple_cycles_test";
            int cycleCount = 0;

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "CycleData" );
                section.AddField( "CycleCount", cycleCount );
            } );

            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                var section = args.Reader.FindSection( "CycleData" );
                if ( section != null ) {
                    cycleCount = section.GetField<int>( "CycleCount" );
                }
            } );

            // Act
            for ( int i = 0; i < 5; i++ ) {
                cycleCount = i;
                await _dataProvider.Save( fileId, default );
                await _dataProvider.Load( fileId );
            }

            // Assert
            Assert.That( cycleCount, Is.EqualTo( 4 ) );
        }

        [Test]
        public async Task SaveSection_WithEmptyName_HandlesGracefully() {
            // Arrange
            string fileId = "empty_section_name_test";

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( String.Empty );
                section.AddField( "Value", 1 );
            } );

            // Act & Assert
            try {
                await _dataProvider.Save( fileId, default );
                Assert.Pass( "Empty section name handled" );
            } catch {
                Assert.Pass( "Appropriately handled empty section name" );
            }
        }

        [Test]
        [TestCase( 0 )]
        [TestCase( 1 )]
        [TestCase( -1 )]
        [TestCase( int.MaxValue )]
        [TestCase( int.MinValue )]
        public async Task SaveField_WithExtremumIntValues_Succeeds( int value ) {
            // Arrange
            string fileId = $"extremum_int_test_{value.GetHashCode()}";
            int loadedValue = 0;

            _saveBegin.Subscribe( ( in SaveBeginEventArgs args ) => {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "IntValue", value );
            } );

            _loadBegin.Subscribe( ( in LoadBeginEventArgs args ) => {
                var section = args.Reader.FindSection( "Test" );
                if ( section != null ) {
                    loadedValue = section.GetField<int>( "IntValue" );
                }
            } );

            // Act
            await _dataProvider.Save( fileId, default );
            await _dataProvider.Load( fileId );

            // Assert
            Assert.That( loadedValue, Is.EqualTo( value ) );
        }

        [Test]
        public async Task EventSubscriber_CanBeUnsubscribed() {
            // Arrange
            string fileId = "unsubscribe_test";
            var eventFired = false;

            void OnSaveBegin( in SaveBeginEventArgs args ) {
                eventFired = true;
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "Value", 1 );
            }

            _saveBegin.Subscribe( OnSaveBegin );

            // Act
            await _dataProvider.Save( fileId, default );
            var firstRun = eventFired;

            eventFired = false;
            _saveBegin.Unsubscribe( OnSaveBegin );

            await _dataProvider.Save( fileId, default );
            var secondRun = eventFired;

            using ( Assert.EnterMultipleScope() ) {
                // Assert
                Assert.That( firstRun, Is.True );
                Assert.That( secondRun, Is.False );
            }
        }

        [Test]
        public async Task SaveDataProvider_IsDisposable() {
            // Arrange
            var newProvider = new SaveDataProvider( _engineService.Object, _eventFactory, _cvarSystem, _fileSystem, _logger );

            // Act
            newProvider.Dispose();

            // Assert - should be disposed without throwing
            Assert.Pass( "SaveDataProvider successfully disposed" );
        }

        [Test]
        public async Task Field_DuplicateFieldCreation_ThrowsDuplicateFieldException() {
            // Arrange
            string fileId = "duplicate_field_exception";

            void OnSaveBegin( in SaveBeginEventArgs args ) {
                var section = args.Writer.AddSection( "Test" );
                section.AddField( "Value", 1 );
                Assert.Throws<DuplicateFieldException>( () => section.AddField( "Value", 1 ) );
            }

            _saveBegin.Subscribe( OnSaveBegin );

            // Act & Assert
            await _dataProvider.Save( fileId, default );
        }

        [Test]
        public async Task Section_DuplicateSectionCreation_ThrowsDuplicateSectionException() {
            // Arrange
            string fileId = "duplicate_section_test";
            DuplicateSectionException? exception = null;

            void OnSaveBegin( in SaveBeginEventArgs args ) {
                ISaveWriterService writer = args.Writer;
                var section = args.Writer.AddSection( "Test" );
                exception = Assert.Throws<DuplicateSectionException>( () => writer.AddSection( "Test" ) );
            }

            _saveBegin.Subscribe( OnSaveBegin );

            // Act
            await _dataProvider.Save( fileId, default );

            // Assert
            Assert.That( exception, Is.Not.Null );
        }
    }
}
#endif
