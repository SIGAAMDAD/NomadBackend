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
using Nomad.Core.Compatibility.Guards;
using Nomad.Save.Interfaces;

namespace Nomad.Save.Extensions
{
    public static class SaveWriterServiceExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="name"></param>
        /// <param name="write"></param>
        public static void Section(this ISaveWriterService writer, string name, Action<ISaveSectionWriter> write)
        {
            ArgumentGuard.ThrowIfNull(writer, nameof(writer));
            ArgumentGuard.ThrowIfNull(write, nameof(write));

            using ISaveSectionWriter section = writer.AddSection(name);
            write(section);
        }
    }
}
