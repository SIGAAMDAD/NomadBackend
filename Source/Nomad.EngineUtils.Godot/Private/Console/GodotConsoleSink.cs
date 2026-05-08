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
using Nomad.Core.Logger;

namespace Nomad.EngineUtils.Godot.Private.Console
{
	internal sealed class GodotConsoleSink : SinkBase
	{
		private readonly GodotConsoleOutputView _outputView;

		public GodotConsoleSink( GodotConsoleOutputView outputView )
		{
			_outputView = outputView ?? throw new ArgumentNullException( nameof( outputView ) );
		}

		public override void Clear()
		{
			_outputView.Clear();
		}

		public override void Print( string message )
		{
			_outputView.Print( message );
		}

		public override void Flush()
		{
		}
	};
};
