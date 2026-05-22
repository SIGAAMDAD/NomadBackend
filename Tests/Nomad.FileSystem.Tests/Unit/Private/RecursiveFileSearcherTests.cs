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
using NUnit.Framework;
using Nomad.FileSystem.Private.Services;
using Moq;
using Nomad.Core.Logger;

namespace Nomad.FileSystem.Tests
{
    [TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Search")]
    [Category("Unit")]
    public class RecursiveFileSearcherTests
    {
        private Mock<ILoggerService> _loggerMock;
        private Mock<ILoggerCategory> _categoryMock;
        private string _tempDir;
        private string _dir1;
        private string _dir2;
        private RecursiveFileSearcher _searcher;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILoggerService>();
            _categoryMock = new Mock<ILoggerCategory>();

            _loggerMock.Setup(l => l.CreateCategory(nameof(FileSystem), LogLevel.Info, true))
                .Returns(_categoryMock.Object);

            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _dir1 = Path.Combine(_tempDir, "Search1");
            _dir2 = Path.Combine(_tempDir, "Search2");
            Directory.CreateDirectory(_dir1);
            Directory.CreateDirectory(_dir2);

            _searcher = new RecursiveFileSearcher(_categoryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);

            _searcher?.Dispose();
        }

        #region AddSearchDirectory

        [Test]
        public void AddSearchDirectory_ValidDirectory_AddsToList()
        {
            _searcher.AddSearchDirectory(_dir1);
            Assert.That(_searcher.SearchDirectories, Has.Count.EqualTo(1));
            Assert.That(_searcher.SearchDirectories[0], Does.StartWith(_dir1));
        }

        [Test]
        public void AddSearchDirectory_HighPriority_InsertsAtBeginning()
        {
            _searcher.AddSearchDirectory(_dir1);
            _searcher.AddSearchDirectory(_dir2, highPriority: true);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_searcher.SearchDirectories[0], Does.StartWith(_dir2));
                Assert.That(_searcher.SearchDirectories[1], Does.StartWith(_dir1));
            }
        }

        [Test]
        public void AddSearchDirectory_NullOrEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _searcher.AddSearchDirectory(null));
            Assert.Throws<ArgumentException>(() => _searcher.AddSearchDirectory(""));
            Assert.Throws<ArgumentException>(() => _searcher.AddSearchDirectory("   "));
        }

        [Test]
        public void AddSearchDirectory_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            string bad = Path.Combine(_tempDir, "doesnotexist");
            Assert.Throws<DirectoryNotFoundException>(() => _searcher.AddSearchDirectory(bad));
        }

        #endregion

        #region FindFile

        [Test]
        public void FindFile_ExactMatch_ReturnsFullPath()
        {
            _searcher.AddSearchDirectory(_dir1);
            string file = Path.Combine(_dir1, "test.txt");
            File.WriteAllText(file, "content");

            var result = _searcher.FindFile("test.txt");
            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_WithSubdirectories_ReturnsFullPath()
        {
            _searcher.AddSearchDirectory(_dir1);
            string subDir = Path.Combine(_dir1, "sub");
            Directory.CreateDirectory(subDir);
            string file = Path.Combine(subDir, "deep.txt");
            File.WriteAllText(file, "content");

            var result = _searcher.FindFile("sub/deep.txt");
            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_NotFound_ReturnsNull()
        {
            _searcher.AddSearchDirectory(_dir1);
            var result = _searcher.FindFile("missing.txt");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindFile_RecursiveSearch_FindsFileByName()
        {
            _searcher.AddSearchDirectory(_dir1);
            string file = Path.Combine(_dir1, "target.log");
            File.WriteAllText(file, "data");

            var result = _searcher.FindFile("target.log");
            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_WithDifferentSeparators_Normalizes()
        {
            _searcher.AddSearchDirectory(_dir1);
            string subDir = Path.Combine(_dir1, "sub");
            Directory.CreateDirectory(subDir);
            string file = Path.Combine(subDir, "file.txt");
            File.WriteAllText(file, "data");

            var result = _searcher.FindFile("sub\\file.txt"); // backslash
            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_WhenPathIncludesSearchRootFolder_ResolvesInsideRegisteredDirectory()
        {
            string assetsDir = Path.Combine(_tempDir, "Assets");
            string bindingsDir = Path.Combine(assetsDir, "Config", "Bindings");
            Directory.CreateDirectory(bindingsDir);

            _searcher.AddSearchDirectory(assetsDir);

            string file = Path.Combine(bindingsDir, "DefaultConfig.json");
            File.WriteAllText(file, "content");

            var result = _searcher.FindFile("Assets/Config/Bindings/DefaultConfig.json");
            Assert.That(result, Is.EqualTo(file));
        }

        [Test]
        public void FindFile_OnWindows_MatchesFileStreamForDifferentPathCasing()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Ignore("Windows-specific case-insensitive filesystem behavior.");
            }

            _searcher.AddSearchDirectory(_dir1);
            string subDir = Path.Combine(_dir1, "Sub");
            Directory.CreateDirectory(subDir);
            string file = Path.Combine(subDir, "File.txt");
            File.WriteAllText(file, "data");

            string requestedPath = SwapPathCase(file);

            using (var stream = new FileStream(requestedPath, FileMode.Open, FileAccess.Read))
            {
                Assert.That(stream.Length, Is.GreaterThan(0));
            }

            var result = _searcher.FindFile(requestedPath);
            Assert.That(result, Is.Not.Null);
            Assert.That(string.Equals(result, file, StringComparison.OrdinalIgnoreCase), Is.True);
        }

        #endregion

        #region FindFiles

        [Test]
        public void FindFiles_WithPattern_ReturnsMatches()
        {
            _searcher.AddSearchDirectory(_dir1);
            File.WriteAllText(Path.Combine(_dir1, "a.txt"), "");
            File.WriteAllText(Path.Combine(_dir1, "b.txt"), "");
            File.WriteAllText(Path.Combine(_dir1, "c.log"), "");

            var results = _searcher.FindFiles(_dir1, "*.txt");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Has.Count.EqualTo(2));
            }
        }

        #endregion

        #region FindAllFiles

        [Test]
        public void FindAllFiles_ReturnsAllMatchesInPriorityOrder()
        {
            _searcher.AddSearchDirectory(_dir1);
            _searcher.AddSearchDirectory(_dir2);

            string file1 = Path.Combine(_dir1, "common.txt");
            string file2 = Path.Combine(_dir2, "common.txt");
            File.WriteAllText(file1, "1");
            File.WriteAllText(file2, "2");

            var all = _searcher.FindAllFiles("common.txt");
            Assert.That(all, Has.Count.EqualTo(2));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(all[0], Is.EqualTo(file1)); // first directory added
                Assert.That(all[1], Is.EqualTo(file2));
            }
        }

        [Test]
        public void FindDirectory_WhenPathIncludesSearchRootFolder_ResolvesInsideRegisteredDirectory()
        {
            string assetsDir = Path.Combine(_tempDir, "Assets");
            string bindingsDir = Path.Combine(assetsDir, "Config", "Bindings");
            Directory.CreateDirectory(bindingsDir);

            _searcher.AddSearchDirectory(assetsDir);

            var result = _searcher.FindDirectory("Assets/Config/Bindings");
            Assert.That(result, Is.EqualTo(bindingsDir));
        }

        #endregion

        #region FindFileWithExtensions

        [Test]
        public void FindFileWithExtensions_TriesExtensionsInOrder()
        {
            _searcher.AddSearchDirectory(_dir1);
            string yamlFile = Path.Combine(_dir1, "config.yaml");
            File.WriteAllText(yamlFile, "yaml");

            var result = _searcher.FindFileWithExtensions("config", ".json", ".yaml", ".xml");
            Assert.That(result, Is.EqualTo(yamlFile));
        }

        [Test]
        public void FindFileWithExtensions_NoneFound_ReturnsNull()
        {
            _searcher.AddSearchDirectory(_dir1);
            var result = _searcher.FindFileWithExtensions("missing", ".txt");
            Assert.That(result, Is.Null);
        }

        #endregion

        private static string SwapPathCase(string path)
        {
            char[] chars = path.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetter(chars[i]))
                {
                    chars[i] = char.IsUpper(chars[i])
                        ? char.ToLowerInvariant(chars[i])
                        : char.ToUpperInvariant(chars[i]);
                }
            }

            return new string(chars);
        }
    }
}
#endif
