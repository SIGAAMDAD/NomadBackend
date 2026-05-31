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
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Save.Interfaces;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Services
{
    /// <summary>
    /// The interface contract to interacting with the save system module. Handles loading, saving, and general management of persistent game state.
    /// </summary>
    /// <remarks>
    /// This is meant to be multithreaded, so make sure you are taking the proper precautions around state management and loading the data from disk.
    ///
    /// NOTE: this is v0.1.0-alpha, expect changes in the future as the framework matures.
    /// </remarks>
    public interface ISaveDataProvider : IDisposable
    {
        [Event("Nomad.Save.Services")]
        [EventPayload("Writer", typeof(ISaveWriterService))]
        IGameEvent<SaveBeginEventArgs> SaveBegin { get; }

        [Event("Nomad.Save.Services")]
        [EventPayload("Reader", typeof(ISaveReaderService))]
        IGameEvent<LoadBeginEventArgs> LoadBegin { get; }

        /// <summary>
        /// Writes the current game state to the provided save file given by <paramref name="filepath"/> to disk.
        /// </summary>
        /// <remarks>
        /// Executes on a dedicated worker thread to ensure zero blockage of the UI. However also ensure that when you write the data to the disk
        /// that you are locking your persistent data as to not cause a dirty read.
        /// </remarks>
        /// <param name="saveName">The file to write the persistent game state to.</param>
        /// <param name="gameVersion"></param>
        Task Save(string saveName, GameVersion gameVersion);

        /// <summary>
        /// Reads a save file and loads the information into memory to be then read and processed as the new state. This save file is provided through <paramref name="filepath"/>.
        /// </summary>
        /// <remarks>
        /// Executes on a dedicated worker thread to ensure zero blockage of the UI so that you can put something like a loading screen for the wait down. Ensure that you are
        /// properly locking persistent variables to ensure you aren't getting race conditions or doing unsafe things with threads.
        /// </remarks>
        /// <param name="saveName">The file to read the persistent game data from.</param>
        Task Load(string saveName);

        /// <summary>
        /// Returns a list of all the currently available save data files in the save system.
        /// </summary>
        /// <remarks>
        /// This is not thread safe.
        /// </remarks>
        /// <returns>The complete list of save files found in the save system's data path.</returns>
        IReadOnlyList<SaveFileMetadata> ListSaveFiles();

        /// <summary>
        ///
        /// </summary>
        /// <param name="saveName"></param>
        void DeleteSaveFile(string saveName);
    }
}
