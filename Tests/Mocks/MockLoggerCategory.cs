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

using Nomad.Core.Logger;
using System;

public class MockLoggerCategory : ILoggerCategory
{
    public string Name { get; private set; }
    public LogLevel Level { get; private set; }
    public bool Enabled { get; set; }

    public MockLoggerCategory(string name, LogLevel level, bool enabled)
    {
        Name = name;
        Level = level;
        Enabled = enabled;
    }

    public void AddSink(ILoggerSink sink)
    {
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void PrintDebug(string message)
    {
    }

    public void PrintError(string message)
    {
        Console.WriteLine($"ERROR: {message}");
    }

    public void PrintLine(string message)
    {
        Console.WriteLine(message);
    }

    public void PrintWarning(string message)
    {
        Console.WriteLine($"WARNING: {message}");
    }

    public void RemoveSink(ILoggerSink sink)
    {
    }
};
