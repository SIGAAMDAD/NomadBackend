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
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.CVars.Private.Services;
using Nomad.Events;
using Nomad.FileSystem.Private.Services;
using NUnit.Framework;

namespace Nomad.CVars.Tests;

[TestFixture]
[Category("Nomad.CVars")]
[Category("ValueObjects")]
[Category("Unit")]
public class CVarTypeTests
{
    private ICVarSystemService _cvarSystem;
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
        _logger?.Dispose();
        _registry?.Dispose();
        _fileSystem?.Dispose();
        _cvarSystem?.Dispose();
        _engineService?.Dispose();
    }

    [Test]
    [TestCase(false, CVarType.Boolean)]
    [TestCase("", CVarType.String)]
    [TestCase((uint)0, CVarType.UInt)]
    [TestCase(0, CVarType.Int)]
    [TestCase(0.0f, CVarType.Decimal)]
    public void CreateCVar_WithCorrectType_MatchesExpectedType<T>(T value, CVarType type)
    {
        // Arrange
        var createInfo = new CVarCreateInfo<T>
        {
            Name = "TestCVar",
            DefaultValue = value,
            Description = "A test cvar."
        };

        // Act
        var cvar = _cvarSystem.Register(createInfo);

        // Assert
        Assert.That(cvar.Type, Is.EqualTo(type));
    }

    [Test]
    [TestCase((ulong)0)]
    [TestCase((long)0)]
    [TestCase((double)0.0f)]
    [TestCase((byte)0)]
    [TestCase((sbyte)0)]
    [TestCase((ushort)0)]
    [TestCase((short)0)]
    public void CreateCVar_WithInvalidPrimitiveType_ThrowsInvalidCastException<T>(T value)
    {
        // Arrange
        var createInfo = new CVarCreateInfo<T>
        {
            Name = "TestCVar",
            DefaultValue = value,
            Description = "A test cvar."
        };

        // Assert
        Assert.Throws<InvalidCastException>(
            () => _cvarSystem.Register(createInfo)
        );
    }

    [Test]
    [TestCase(CVarType.Boolean, typeof(bool))]
    [TestCase(CVarType.String, typeof(string))]
    [TestCase(CVarType.Int, typeof(int))]
    [TestCase(CVarType.UInt, typeof(uint))]
    [TestCase(CVarType.Decimal, typeof(float))]
    public void Check_InternalCVarType_ReturnsCorrectSystemType(CVarType cvarType, Type systemType)
    {
        // Arrange
        var retrievedType = cvarType.GetSystemType();

        // Assert
        Assert.That(retrievedType, Is.EqualTo(systemType));
    }

    [Test]
    [TestCase(CVarType.Boolean, typeof(bool))]
    [TestCase(CVarType.String, typeof(string))]
    [TestCase(CVarType.Int, typeof(int))]
    [TestCase(CVarType.UInt, typeof(uint))]
    [TestCase(CVarType.Decimal, typeof(float))]
    public void Check_SystemType_ReturnsCorrectCVarType(CVarType cvarType, Type systemType)
    {
        // Arrange
        var retrievedType = systemType.GetCVarType();

        // Assert
        Assert.That(retrievedType, Is.EqualTo(cvarType));
    }

    [Test]
    public void Check_EnumType_ReturnsUIntType()
    {
        // Arrange
        var retrievedCVarType = typeof(TestEnum).GetCVarType();

        // Assert
        Assert.That(retrievedCVarType, Is.EqualTo(CVarType.UInt));
    }
}

enum TestEnum : uint
{
}
#endif
