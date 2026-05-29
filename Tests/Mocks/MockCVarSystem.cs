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

using System;
using System.Collections.Generic;
using Nomad.Core;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;

public class MockCVarSystem : ICVarSystemService
{
    private readonly Dictionary<string, object> _cvars = new();
    private readonly IGameEventRegistryService _eventRegistry;

    public MockCVarSystem(IGameEventRegistryService eventFactory)
    {
        _eventRegistry = eventFactory;
    }

    public void SetCVar<T>(string name, T value) where T : notnull
    {
        _cvars[name] = value!;
    }

    public ICVar<T>? GetCVar<T>(string name) where T : notnull
    {
        if (_cvars.TryGetValue(name, out var val) && val is T tVal)
        {
            return new MockCVar<T>(name, tVal, _eventRegistry);
        }
        return null;
    }

    public void Register(ICVar cvar)
    {
        throw new NotImplementedException();
    }

    public ICVar<T> Register<T>(CVarCreateInfo<T> createInfo)
    {
        var cvar = new MockCVar<T>(createInfo.Name, createInfo.DefaultValue, _eventRegistry);
        _cvars.TryAdd(createInfo.Name, cvar);
        return cvar;
    }

    public void Unregister(ICVar cvar)
    {
        throw new NotImplementedException();
    }

    public bool CVarExists(string name)
    {
        throw new NotImplementedException();
    }

    public bool CVarExists<T>(string name)
    {
        throw new NotImplementedException();
    }

    public ICVar? GetCVar(string name)
    {
        throw new NotImplementedException();
    }

    public ICVar[]? GetCVars()
    {
        throw new NotImplementedException();
    }

    public ICVar<T>[]? GetCVarsWithValueType<T>()
    {
        throw new NotImplementedException();
    }

    public ICVar[]? GetCVarsInGroup(string groupName)
    {
        throw new NotImplementedException();
    }

    public bool GroupExists(string groupName)
    {
        throw new NotImplementedException();
    }

    public void Restart()
    {
        throw new NotImplementedException();
    }

    public void Load(IFileSystem fileSystem, string configFile)
    {
        throw new NotImplementedException();
    }

    public void Save(IFileSystem fileSystem, string configFile)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public bool TryFind(string name, out ICVar? cvar)
    {
        throw new NotImplementedException();
    }

    public bool TryFind<T>(string name, out ICVar<T>? cvar)
    {
        throw new NotImplementedException();
    }

    private class MockCVar<T> : ICVar<T> where T : notnull
    {
        public string Name { get; }
        public T Value { get; set; }

        public T DefaultValue => throw new NotImplementedException();

        public IGameEvent<CVarValueChangedEventArgs<T>> ValueChanged { get; set; }

        public string Description => throw new NotImplementedException();

        public CVarType Type => throw new NotImplementedException();

        public CVarFlags Flags => throw new NotImplementedException();

        public bool IsSaved => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public bool IsUserCreated => throw new NotImplementedException();

        public bool IsHidden => throw new NotImplementedException();

        public MockCVar(string name, T value, IGameEventRegistryService eventFactory)
        {
            Name = name;
            Value = value;
            ValueChanged = eventFactory.GetEvent<CVarValueChangedEventArgs<T>>($"{name}:{CVarValueChangedEventArgs<T>.Name}", "CVarTestEvents");
        }

        public void Reset() { }
        public void SetFromString(string str) { }

        public float GetDecimalValue()
        {
            throw new NotImplementedException();
        }

        public int GetIntegerValue()
        {
            throw new NotImplementedException();
        }

        public uint GetUIntegerValue()
        {
            throw new NotImplementedException();
        }

        public string GetStringValue()
        {
            throw new NotImplementedException();
        }

        public bool GetBooleanValue()
        {
            throw new NotImplementedException();
        }

        public void SetDecimalValue(float value)
        {
            throw new NotImplementedException();
        }

        public void SetIntegerValue(int value)
        {
            throw new NotImplementedException();
        }

        public void SetUIntegerValue(uint value)
        {
            throw new NotImplementedException();
        }

        public void SetBooleanValue(bool value)
        {
            throw new NotImplementedException();
        }

        public void SetStringValue(string value)
        {
            throw new NotImplementedException();
        }
    }
}
