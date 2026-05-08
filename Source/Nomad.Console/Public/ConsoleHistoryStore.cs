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
using System.Text;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem;
using Nomad.Core.Memory.Buffers;

namespace Nomad.Console
{
    public sealed class ConsoleHistoryStore : IConsoleHistoryStore
    {
        private const string HISTORY_FILE_NAME = "history.txt";

        private readonly IFileSystem _fileSystem;
        private readonly string _historyPath;

        public ConsoleHistoryStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            _historyPath = CombinePath(fileSystem.GetUserDataPath(), HISTORY_FILE_NAME);
        }

        public IReadOnlyList<string> Load()
        {
            if (!_fileSystem.FileExists(_historyPath))
            {
                return Array.Empty<string>();
            }

            IBufferHandle? buffer = _fileSystem.LoadFile(_historyPath);

            if (buffer == null)
            {
                return Array.Empty<string>();
            }

            try
            {
                byte[] bytes = buffer.ToArray();
                string text = Encoding.UTF8.GetString(bytes);

                string[] lines = text.Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries
                );

                var history = new List<string>(lines.Length);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (line.Length > 0)
                    {
                        history.Add(line);
                    }
                }
                return history;
            }
            finally
            {
                buffer.Dispose();
            }
        }

        public void Save(IReadOnlyList<string> history)
        {
            ArgumentGuard.ThrowIfNull(history);

            var builder = new StringBuilder(history.Count * 32);

            for (int i = 0; i < history.Count; i++)
            {
                string line = history[i];

                if (!string.IsNullOrWhiteSpace(line))
                {
                    builder.AppendLine(line);
                }
            }

            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            _fileSystem.WriteFile(_historyPath, bytes, 0, bytes.Length);
        }

        private static string CombinePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left))
            {
                return right.Replace('\\', '/');
            }

            left = left.Replace('\\', '/').TrimEnd('/');
            right = right.Replace('\\', '/').TrimStart('/');

            return $"{left}/{right}";
        }
    };
};
