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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.FileSystem;
using Nomad.Core.OnlineServices;
using Nomad.FileSystem.Private.Services;
using Nomad.OnlineServices.Steam.Private.Services;
using NUnit.Framework;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Tests
{
    [TestFixture]
    [Category("Nomad.OnlineServices.Steam")]
    [Category("Cloud")]
    [Category("Unit")]
    [Category("Steam")]
    public class SteamCloudStorageServiceTests
    {
        private MockLogger _logger;
        private IFileSystem _fileSystem;
        private ICloudStorageService _service;

        [OneTimeSetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("SteamAppId", "480");
            Environment.SetEnvironmentVariable("SteamGameId", "480");

            var initResult = SteamAPI.InitEx(out string errorMsg);
            if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Assert.Inconclusive($"Could not initialize SteamAPI: {initResult} - {errorMsg}");
            }

            PumpSteamCallbacks(1000);

            _logger = new MockLogger();
            var engineService = new MockEngineService();
            _fileSystem = new FileSystemService(engineService, _logger);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fileSystem?.Dispose();
            _logger?.Dispose();
            PumpSteamCallbacks(100); // clear any remaining callbacks

            SteamAPI.Shutdown();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        // Helper to pump Steam callbacks while a task is pending
        private static async Task PumpWhileTaskRunning(Task task, int timeoutMs = 10000)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            var pumpTask = Task.Run(async () =>
            {
                while (!task.IsCompleted && !cts.Token.IsCancellationRequested)
                {
                    SteamAPI.RunCallbacks();
                    await Task.Delay(10, cts.Token);
                }
            }, cts.Token);

            await Task.WhenAny(task, pumpTask);
            if (!task.IsCompleted)
                throw new TimeoutException("Steam operation timed out.");

            await task; // propagate exceptions
        }

        private static void PumpSteamCallbacks(int milliseconds)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
            {
                SteamAPI.RunCallbacks();
                Thread.Sleep(10);
            }
        }

        private void CreateService()
        {
            _service = new SteamCloudStorageService(_logger, _fileSystem);

            ClearAllCloudFiles();
        }

        private void ClearAllCloudFiles()
        {
            int fileCount = SteamRemoteStorage.GetFileCount();

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = SteamRemoteStorage.GetFileNameAndSize(i, out _);
                if (!SteamRemoteStorage.FileDelete(fileName))
                {
                    _logger.PrintError($"Error deleting cloud file '{fileName}'!");
                }
            }
        }

        [Test]
        public void CreateCloudStorageService_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SteamCloudStorageService(null, _fileSystem));
        }

        [Test]
        public void CreateCloudStorageService_NullFileSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SteamCloudStorageService(_logger, null));
        }

        [Test]
        public void CreateCloudStorageService_SupportsCloudStorage_IsTrue()
        {
            CreateService();

            Assert.That(_service.SupportsCloudStorage, Is.True);
        }
    }
}
