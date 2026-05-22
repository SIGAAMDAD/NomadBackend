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
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.CVars.Private.Services;
using Nomad.Core.CVars;
using Nomad.Events;
using Nomad.FileSystem.Private.Services;
using NUnit.Framework;
using Nomad.CVars.Exceptions;

namespace Nomad.CVars.Tests
{
    [TestFixture]
    [Category("Nomad.CVars")]
    [Category("System")]
    [Category("Unit")]
    public class CVarSystemServiceTests
    {
        private CVarSystem _cvarSystem;
        private IGameEventRegistryService _registry;
        private IEngineService _engineService;
        private IFileSystem _fileSystem;
        private ILoggerService _logger;

        [SetUp]
        public void Setup()
        {
            _logger = new MockLogger();
            _registry = new GameEventRegistry(_logger);
            _engineService = new MockEngineService();
            _fileSystem = new FileSystemService(_engineService, _logger);
            _cvarSystem = new CVarSystem(_registry, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            if (_fileSystem.FileExists("Config/config.ini"))
            {
                _fileSystem.DeleteFile("Config/config.ini");
            }

            _logger?.Dispose();
            _registry?.Dispose();
            _fileSystem?.Dispose();
            _cvarSystem?.Dispose();
            _engineService?.Dispose();
        }

        [Test]
        public void Restart_ResetsAllRegisteredCVarValues()
        {
            // Arrange
            var createInfo1 = new CVarCreateInfo<int>
            {
                Name = "TestCVar1",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo2 = new CVarCreateInfo<float>
            {
                Name = "TestCVar2",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar1 = _cvarSystem.Register(createInfo1);
            var cvar2 = _cvarSystem.Register(createInfo2);
            cvar1.Value = 12;
            cvar2.Value = 81.5f;

            _cvarSystem.Restart();

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(cvar1.Value, Is.EqualTo(cvar1.DefaultValue));
                Assert.That(cvar2.Value, Is.EqualTo(cvar2.DefaultValue));
            }
        }

        #region CVar Group Tests

        [Test]
        public void AddGroup_AddDuplicateGroup_ThrowsInvalidOperationException()
        {
            _cvarSystem.AddGroup("Group1");
            Assert.Throws<InvalidOperationException>(() => _cvarSystem.AddGroup("Group1"));
        }

        [Test]
        public void AddGroup_GroupExists()
        {
            _cvarSystem.AddGroup("Group1");
            Assert.That(_cvarSystem.GroupExists("Group1"), Is.True);
        }

        #endregion

        #region CVar Registration Tests

        [Test]
        public void RegisterCVar_WithNullParameters_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(
                () => _cvarSystem.Register(new CVarCreateInfo<int>
                {
                    Name = null!,
                    DefaultValue = default,
                    Description = null!
                })
            );
        }

        [Test]
        public void RegisterCVar_WithEmptyParameters_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(
                () => _cvarSystem.Register(new CVarCreateInfo<int>
                {
                    Name = string.Empty,
                    DefaultValue = default,
                    Description = string.Empty
                })
            );
        }

        [Test]
        public void RegisterCVar_CalledTwiceWithSameTypeAndParameters_ReturnsSameInstance()
        {
            // Arrange
            var createInfo = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar1 = _cvarSystem.Register(createInfo);
            var cvar2 = _cvarSystem.Register(createInfo);

            // Assert
            Assert.That(cvar1, Is.SameAs(cvar2));
        }

        [Test]
        public void RegisterCVar_CalledTwiceWithDifferentTypeSameParameters_ThrowsInvalidCast()
        {
            // Arrange
            var createInfo1 = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo2 = new CVarCreateInfo<float>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            _cvarSystem.Register(createInfo1);

            // Assert
            Assert.Throws<InvalidCastException>(
                () => _cvarSystem.Register(createInfo2)
            );
        }

        [Test]
        [TestCase<bool>(false)]
        [TestCase<string>("")]
        [TestCase<uint>(0)]
        [TestCase<int>(0)]
        [TestCase<float>(0.0f)]
        public void RegisterCVar_CreatedWithExplicitType_CanBeFoundWithExplicitTypeSearch<T>(T value)
        {
            // Arrange
            var createInfo = new CVarCreateInfo<T>
            {
                Name = "TestCVar",
                DefaultValue = value,
                Description = "A test cvar."
            };

            // Act
            _cvarSystem.Register(createInfo);

            // Assert
            Assert.That(_cvarSystem.CVarExists<T>("TestCVar"), Is.True);
        }

        [Test]
        [TestCase<bool>(false)]
        [TestCase<uint>(0)]
        [TestCase<int>(0)]
        [TestCase<float>(0.0f)]
        [TestCase<string>("")]
        public void RegisterCVar_CreatedWithExplicitType_CanBeFoundWithNameOnly<T>(T value)
        {
            // Arrange
            var createInfo = new CVarCreateInfo<T>
            {
                Name = "TestCVar",
                DefaultValue = default!,
                Description = "A test cvar."
            };

            // Act
            _cvarSystem.Register(createInfo);

            // Assert
            Assert.That(_cvarSystem.CVarExists("TestCVar"), Is.True);
        }

        [Test]
        public void RegisterCVar_CalledTwiceWithDifferentTypes_ThrowsInvalidCastException()
        {
            // Arrange
            var createInfo1 = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo2 = new CVarCreateInfo<float>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            _cvarSystem.Register(createInfo1);

            // Assert
            Assert.Throws<InvalidCastException>(
                () => _cvarSystem.Register(createInfo2)
            );
        }

        #endregion

        #region CVar Unregistration Tests

        [Test]
        public void UnregisterCVar_CalledWithNullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => _cvarSystem.Unregister(null!)
            );
        }

        [Test]
        public void UnregisterCVar_CalledWithValueCVar_DoesntExist()
        {
            // Arrange
            var createInfo = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar = _cvarSystem.Register(createInfo);
            _cvarSystem.Unregister(cvar);

            // Assert
            Assert.That(!_cvarSystem.CVarExists<int>(cvar.Name));
        }

        #endregion

        #region CVar Retrieval

        [Test]
        public void TryFindCVar_ReturnsCorrectCVar()
        {
            // Arrange
            var createInfo = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar = _cvarSystem.Register(createInfo);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(_cvarSystem.TryFind<int>(cvar.Name, out var found), Is.True);
                Assert.That(found, Is.SameAs(cvar));
            }
        }

        [Test]
        public void TryFindCVar_BeforeCVarExists_ReturnsFalse()
        {
            // Assert
            Assert.That(_cvarSystem.TryFind<int>("TestCVar", out var _), Is.False);
        }

        [Test]
        public void GetCVar_WithValidCVar_ReturnsSameCVar()
        {
            // Arrange
            var createInfo = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar = _cvarSystem.Register(createInfo);

            // Assert
            Assert.That(_cvarSystem.GetCVar(cvar.Name), Is.SameAs(cvar));
        }

        [Test]
        public void GetCVar_BeforeCVarExists_ReturnsNull()
        {
            // Assert
            Assert.That(_cvarSystem.GetCVar("TestCVar"), Is.Null);
        }

        [Test]
        public void GetCVarWithType_BeforeCVarExists_ReturnsNull()
        {
            // Assert
            Assert.That(_cvarSystem.GetCVar<int>("TestCVar"), Is.Null);
        }

        [Test]
        public void GetCVarWithType_WithValidCVar_ReturnsSameCVar()
        {
            // Arrange
            var createInfo = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar = _cvarSystem.Register(createInfo);

            // Assert
            Assert.That(_cvarSystem.GetCVar<int>(cvar.Name), Is.SameAs(cvar));
        }

        [Test]
        public void GetCVarWithType_WithWrongType_ThrowsCVarTypeMismatchException()
        {
            // Arrange
            var createInfo = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar = _cvarSystem.Register(createInfo);

            // Assert
            Assert.Throws<CVarTypeMismatchException>(() => _cvarSystem.GetCVar<float>(cvar.Name));
        }

        [Test]
        public void GetCVars_ReturnsAllRegisteredCVars()
        {
            // Arrange
            var createInfo1 = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo2 = new CVarCreateInfo<float>
            {
                Name = "TestCVar2",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo3 = new CVarCreateInfo<uint>
            {
                Name = "TestCVar3",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar1 = _cvarSystem.Register(createInfo1);
            var cvar2 = _cvarSystem.Register(createInfo2);
            var cvar3 = _cvarSystem.Register(createInfo3);
            var cvars = _cvarSystem.GetCVars();

            // Assert
            Assert.That(cvars, Does.Contain(cvar1));
            Assert.That(cvars, Does.Contain(cvar2));
            Assert.That(cvars, Does.Contain(cvar3));
        }

        [Test]
        public void GetCVarsWithValueType_ReturnsOnlyCVarsWithSameValueType()
        {
            // Arrange
            var createInfo1 = new CVarCreateInfo<int>
            {
                Name = "TestCVar",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo2 = new CVarCreateInfo<float>
            {
                Name = "TestCVar2",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfo3 = new CVarCreateInfo<int>
            {
                Name = "TestCVar3",
                DefaultValue = default,
                Description = "A test cvar."
            };

            // Act
            var cvar1 = _cvarSystem.Register(createInfo1);
            var cvar2 = _cvarSystem.Register(createInfo2);
            var cvar3 = _cvarSystem.Register(createInfo3);
            var cvars = _cvarSystem.GetCVarsWithValueType<int>();

            // Assert
            Assert.That(cvars, Does.Contain(cvar1));
            Assert.That(cvars, Does.Not.Contain(cvar2));
            Assert.That(cvars, Does.Contain(cvar3));
        }

        #endregion

        #region CVar Saving & Loading

        [Test]
        public void Save_WritesAllCVars_DoesNotThrow()
        {
            // Arrange
            var createInfoUInt = new CVarCreateInfo<uint>
            {
                Name = "TestCVar.UInt",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfoFloat = new CVarCreateInfo<float>
            {
                Name = "TestCVar.Float",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfoInt = new CVarCreateInfo<int>
            {
                Name = "TestCVar.Int",
                DefaultValue = default,
                Description = "A test cvar."
            };
            var createInfoBoolean = new CVarCreateInfo<bool>
            {
                Name = "TestCVar.Boolean",
                DefaultValue = default,
                Description = "A test cvar."
            };

            var createInfoString = new CVarCreateInfo<string>
            {
                Name = "TestCVar.String",
                DefaultValue = "Testing",
                Description = "A test cvar."
            };

            // Act
            _cvarSystem.Register(createInfoInt);
            _cvarSystem.Register(createInfoUInt);
            _cvarSystem.Register(createInfoFloat);
            _cvarSystem.Register(createInfoBoolean);
            _cvarSystem.Register(createInfoString);

            Assert.DoesNotThrow(() => _cvarSystem.Save(_fileSystem, "Config/config.ini"));

            // Assert
            Assert.That(_fileSystem.FileExists("Config/config.ini"));
        }

        [Test]
        public void SaveLoad_DataPersists()
        {
            {
                // Arrange
                var createInfoUInt = new CVarCreateInfo<uint>
                {
                    Name = "TestCVar.UInt",
                    DefaultValue = default,
                    Description = "A test cvar."
                };
                var createInfoFloat = new CVarCreateInfo<float>
                {
                    Name = "TestCVar.Float",
                    DefaultValue = default,
                    Description = "A test cvar."
                };
                var createInfoInt = new CVarCreateInfo<int>
                {
                    Name = "TestCVar.Int",
                    DefaultValue = default,
                    Description = "A test cvar."
                };
                var createInfoBoolean = new CVarCreateInfo<bool>
                {
                    Name = "TestCVar.Boolean",
                    DefaultValue = default,
                    Description = "A test cvar."
                };

                var createInfoString = new CVarCreateInfo<string>
                {
                    Name = "TestCVar.String",
                    DefaultValue = "Testing",
                    Description = "A test cvar."
                };

                // Act
                var intCvar = _cvarSystem.Register(createInfoInt);
                _cvarSystem.Register(createInfoUInt);
                _cvarSystem.Register(createInfoFloat);
                _cvarSystem.Register(createInfoBoolean);
                _cvarSystem.Register(createInfoString);

                _cvarSystem.Save(_fileSystem, "Config/config.ini");

                intCvar.Value = 23;
            }

            {
                _cvarSystem.Load(_fileSystem, "config.ini");

                var intCvar = _cvarSystem.GetCVar<int>("TestCVar.Int");
                Assert.That(intCvar.Value, Is.Zero);
            }
        }

        #endregion
    }
}
#endif
