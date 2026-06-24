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
using Nomad.Core.Logger;

public class MockLogger : ILoggerService
{
    private bool _isDisposed = false;

    public void AddSink(ILoggerSink sink)
    {
    }

    public void Clear()
    {
        Console.Clear();
    }

    public ILoggerCategory CreateCategory(string name, LogLevel level, bool enabled)
    {
        return new MockLoggerCategory(name, level, enabled);
    }

    public void Dispose()
    {
        if ( _isDisposed )
        {
            return;
        }
        GC.SuppressFinalize(this);
        _isDisposed = true;
    }

    public void InitConfig(ICVarSystemService cvarSystem)
    {
        throw new System.NotImplementedException();
    }

    public void PrintDebug(string message)
    {
        Console.WriteLine($"DEBUG: {message}");
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
}
#endif
