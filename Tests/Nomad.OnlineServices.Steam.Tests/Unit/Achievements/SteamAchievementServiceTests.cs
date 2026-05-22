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
using NUnit.Framework;
using Steamworks;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Nomad.Events;
using System.Collections.Generic;
using System.Threading;
using Nomad.OnlineServices.Steam.Private.Stats;
using NUnit.Framework.Internal;
using Nomad.Core.Util;

namespace Nomad.OnlineServices.Steam.Tests
{
    [TestFixture]
    [Category("Nomad.OnlineServices.Steam")]
    [Category("Achievements")]
    [Category("Unit")]
    [Category("Steam")]
    public class SteamAchievementServiceTests
    {
        private IAchievementService _service;
        private List<string> _achievements;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Environment.SetEnvironmentVariable("SteamAppId", "480");
            Environment.SetEnvironmentVariable("SteamGameId", "480");

            ESteamAPIInitResult result = SteamAPI.InitEx(out string errMessage);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Assert.Inconclusive($"Couldn't initialize SteamAPI - {result}: {errMessage}");
            }

            _achievements = new List<string>
            {
                "ACH_WIN_ONE_GAME",
                "ACH_WIN_100_GAMES",
                "ACH_TRAVEL_FAR_ACCUM",
                "ACH_TRAVEL_FAR_SINGLE",
                "NEW_ACHIEVEMENT_NAME_0_4"
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SteamAPI.Shutdown();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        private void CreateService()
        {
            var engineService = new MockEngineService();
            var logger = new MockLogger();
            var eventFactory = new GameEventRegistry(logger);
            var statsRepository = new SteamStatsRepository(new SteamUserData { UserID = SteamUser.GetSteamID(), UserName = SteamFriends.GetPersonaName() }, logger, engineService);
            _service = new SteamAchievementService(statsRepository, logger, eventFactory);

            ResetAllStats();
            PumpSteamCallbacks(500);
        }

        private static void PumpSteamCallbacks(int millisecondsTimeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < millisecondsTimeout)
            {
                SteamAPI.RunCallbacks();
                System.Threading.Thread.Sleep(10); // small delay to avoid busy-wait
            }
        }

        private static void ResetAllStats()
        {
            SteamUserStats.ResetAllStats(true);
        }

        [Test]
        public void Create_SupportsAchievementsIsTrue()
        {
            CreateService();

            Assert.That(_service.SupportsAchievements, Is.True);
        }

        [Test]
        public void Create_NumAchievementsIsCorrect()
        {
            CreateService();

            Assert.That(_service.NumAchievements, Is.EqualTo(_achievements.Count));
        }

        [Test]
        public void UnlockAchievement_RaisesUnlockedEvent()
        {
            // Arrange
            const string testAchievement = "ACH_WIN_ONE_GAME";
            CreateService();

            bool eventFired = false;
            string unlockedId = null!;
            _service.Unlocked.Subscribe((in AchievementUnlockedEventArgs args) =>
            {
                eventFired = true;
                unlockedId = args.Id;
            });

            // Act
            _service.UnlockAchievement(new InternString(testAchievement)).Wait();

            int maxWait = 5000;
            int waited = 0;
            while (!eventFired && waited < maxWait)
            {
                SteamAPI.RunCallbacks();
                Thread.Sleep(50);
                waited += 50;
            }

            // Assert
            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(eventFired, Is.True);
                Assert.That(unlockedId, Is.EqualTo(testAchievement));
                Assert.That(_service.GetAchievementInfo(new InternString(testAchievement)).Achieved, Is.True);
            }
        }

        [Test]
        public void LockAchievement_LocksAchievement()
        {
            // Arrange
            const string testAchievement = "ACH_WIN_ONE_GAME";
            CreateService();

            // ACt
            _service.LockAchievement(new InternString(testAchievement));

            // Assert
            Assert.That(_service.GetAchievementInfo(new InternString(testAchievement)).Achieved, Is.False);
        }

        [Test]
        public void SetAchievementProgress_RaisesProgressChangedEvent()
        {
            // Arrange
            const string testAchievement = "ACH_TRAVEL_FAR_ACCUM";
            CreateService();

            bool eventFired = false;
            string unlockedId = null!;
            float progress = 0.0f;
            float newProgress = 50.0f;

            _service.ProgressChanged.Subscribe((in AchievementProgressChangedEventArgs args) =>
            {
                eventFired = true;
                progress = args.Progress;
                unlockedId = args.Id;
            });

            // Act
            _service.SetAchievementProgress(new InternString(testAchievement), newProgress).Wait();

            int maxWait = 5000;
            int waited = 0;
            while (!eventFired && waited < maxWait)
            {
                SteamAPI.RunCallbacks();
                Thread.Sleep(50);
                waited += 50;
            }

            // Assert
            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(eventFired, Is.True);
                Assert.That(_service.GetAchievementInfo(new InternString(testAchievement)).Achieved, Is.False);
                Assert.That(_service.GetAchievementInfo(new InternString(testAchievement)).Progress, Is.EqualTo(newProgress));
                Assert.That(progress, Is.EqualTo(newProgress));
                Assert.That(unlockedId, Is.EqualTo(testAchievement));
            }
        }
    }
}
