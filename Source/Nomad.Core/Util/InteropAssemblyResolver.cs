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

#if NET5_0_OR_GREATER
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace Nomad.Core.Util
{
    /// <summary>
    /// Provides platform-specific native library resolution for DLLImport declarations.
    /// </summary>
    /// <remarks>
    /// This class allows you to hook assemblies to resolve native library imports to platform-specific binaries.
    /// For example, you can map a generic library name to 'foo.dll' on Windows and 'libfoo.so' on Linux.
    /// This is particularly useful for cross-platform interop scenarios.
    /// </remarks>
    public static class InteropAssemblyResolver
    {
        private static readonly Dictionary<Assembly, Dictionary<string, (string, string)>> _hooks = new();

        /// <summary>
        /// Registers a mapping for platform-specific native library names for an assembly.
        /// </summary>
        /// <remarks>
        /// Call this method to set up library name mappings before making DLLImport calls.
        /// The resolver will automatically select the appropriate library name based on the current platform.
        /// </remarks>
        /// <param name="assembly">The assembly to hook the resolver into.</param>
        /// <param name="libraryName">The generic library name used in DLLImport declarations.</param>
        /// <param name="libraryNameLinux">The library name to use on Linux (without extension).</param>
        /// <param name="libraryNameWindows">The library name to use on Windows (without extension).</param>
        public static void Hook(Assembly assembly, string libraryName, string libraryNameLinux, string libraryNameWindows)
        {
            if (!_hooks.TryGetValue(assembly, out var resolvers))
            {
                resolvers = new Dictionary<string, (string, string)>();
                _hooks[assembly] = resolvers;
                NativeLibrary.SetDllImportResolver(assembly, Resolve);
            }
            resolvers[libraryName] = (libraryNameLinux, libraryNameWindows);
        }

        /// <summary>
        /// Resolves a native library name to its platform-specific binary path.
        /// </summary>
        /// <remarks>
        /// This method is called automatically by the runtime when a DLLImport cannot be resolved.
        /// It searches for the library in the application's base directory, the importing assembly's directory,
        /// and the current directory.
        /// </remarks>
        /// <param name="libraryName">The generic library name being imported.</param>
        /// <param name="assembly">The assembly performing the import.</param>
        /// <param name="searchPath">The search path hint from the DLLImport attribute.</param>
        /// <returns>A handle to the loaded library, or <see cref="IntPtr.Zero"/> if not found.</returns>
        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!_hooks.TryGetValue(assembly, out var dict))
            {
                return IntPtr.Zero;
            }
            if (!dict.TryGetValue(libraryName, out var library))
            {
                return IntPtr.Zero;
            }

            string fileName =
                OperatingSystem.IsWindows() ? $"{library.Item2}.dll" :
                OperatingSystem.IsLinux() ? $"{library.Item1}.so" :
                OperatingSystem.IsMacOS() ? $"{library.Item1}.dylib" :
                throw new PlatformNotSupportedException();

            foreach (string directory in GetSearchDirectories(assembly))
            {
                string fullPath = Path.Combine(directory, fileName);
                if (File.Exists(fullPath))
                {
                    return GetLibrary(fullPath);
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr GetLibrary(string path)
        {
            return NativeLibrary.Load(path);
        }

        private static IEnumerable<string> GetSearchDirectories(Assembly assembly)
        {
            if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
            {
                yield return AppContext.BaseDirectory;
            }

            string? assemblyDirectory = Path.GetDirectoryName(assembly.Location);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                yield return assemblyDirectory;
            }

            yield return Environment.CurrentDirectory;
        }
    }
}
#endif
