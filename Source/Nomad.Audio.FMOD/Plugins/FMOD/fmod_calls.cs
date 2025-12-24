/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace FMOD
{
	/*
	===================================================================================
	
	FMODNative
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	[SuppressUnmanagedCodeSecurity]
	public static unsafe class FMODNative
	{
		private static delegate* unmanaged[Cdecl]<Studio.System**, uint, RESULT> _studio_systemCreate;
		private static delegate* unmanaged[Cdecl]<Studio.System*, RESULT> _studio_systemUpdate;
		private static delegate* unmanaged[Cdecl]<Studio.System*, RESULT> _studio_systemRelease;
		private static delegate* unmanaged[Cdecl]<Studio.System*, int, Studio.INITFLAGS, INITFLAGS, void*, RESULT> _studio_systemInitialize;
		private static delegate* unmanaged[Cdecl]<Studio.System*, RESULT> _studio_systemFlushSampleLoading;
		private static delegate* unmanaged[Cdecl]<Studio.System*, RESULT> _studio_systemFlushCommands;
		private static delegate* unmanaged[Cdecl]<Studio.System*, ADVANCEDSETTINGS*, RESULT> _studio_systemGetAdvancedSettings;
		private static delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.Bank**, RESULT> _studio_systemGetBank;
		private static delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.Bank**> _studio_systemGetBankByID;
		private static delegate* unmanaged[Cdecl]<Studio.System*, int*, RESULT> _studio_systemGetBankCount;
		private static delegate* unmanaged[Cdecl]<Studio.System*, System**, RESULT> _studio_systemGetCoreSystem;
		private static delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.EventDescription**, RESULT> _studio_systemGetEvent;
		private static delegate* unmanaged[Cdecl]<Studio.System*, int, ATTRIBUTES_3D*, VECTOR*> _studio_systemGetListenerAttributes;
		private static delegate* unmanaged[Cdecl]<Studio.System*, int, float*> _studio_systemGetListenerWeight;
		private static delegate* unmanaged[Cdecl]<Studio.System*, Studio.MEMORY_USAGE*> _studio_systemGetMemoryUsage;
		private static delegate* unmanaged[Cdecl]<Studio.System*, int*> _studio_systemGetNumListeners;
		private static delegate* unmanaged[Cdecl]<Studio.System*, char*, float*, float*> _studio_systemGetParameterByName;
		private static delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.PARAMETER_DESCRIPTION*> _studio_systemGetParameterDescriptionByName;
		private static delegate* unmanaged[Cdecl]<Studio.System*, int*> _studio_systemGetParameterDescriptionCount;

		static FMODNative()
		{

		}

		private static void LoadFunctionProcs()
		{
			// Load both the studio and core libraries so callers can resolve Studio and Core functions.
			IntPtr moduleStudio = NativeLibrary.Load(GetPlatformLibraryName("fmodstudio"));
			IntPtr moduleCore = NativeLibrary.Load(GetPlatformLibraryName("fmod"));

			_studio_systemCreate = (delegate* unmanaged[Cdecl]<Studio.System**, uint, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_Create");
			_studio_systemUpdate = (delegate* unmanaged[Cdecl]<Studio.System*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_Update");
			_studio_systemRelease = (delegate* unmanaged[Cdecl]<Studio.System*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_Release");
			_studio_systemInitialize = (delegate* unmanaged[Cdecl]<Studio.System*, int, Studio.INITFLAGS, INITFLAGS, void*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_Initialize");
			_studio_systemFlushSampleLoading = (delegate* unmanaged[Cdecl]<Studio.System*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_FlushSampleLoading");
			_studio_systemFlushCommands = (delegate* unmanaged[Cdecl]<Studio.System*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_FlushCommands");
			_studio_systemGetAdvancedSettings = (delegate* unmanaged[Cdecl]<Studio.System*, ADVANCEDSETTINGS*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_GetAdvancedSettings");
			_studio_systemGetBank = (delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.Bank**, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_GetBank");
			_studio_systemGetBankCount = (delegate* unmanaged[Cdecl]<Studio.System*, int*, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_GetBankCount");
			_studio_systemGetCoreSystem = (delegate* unmanaged[Cdecl]<Studio.System*, System**, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_GetCoreSystem");
			_studio_systemGetEvent = (delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.EventDescription**, RESULT>)GetProc(moduleStudio, "FMOD_Studio_System_GetEvent");
			_studio_systemGetListenerAttributes = (delegate* unmanaged[Cdecl]<Studio.System*, int, ATTRIBUTES_3D*, VECTOR*>)GetProc(moduleStudio, "FMOD_Studio_System_GetListenerAttributes");
			_studio_systemGetListenerWeight = (delegate* unmanaged[Cdecl]<Studio.System*, int, float*>)GetProc(moduleStudio, "FMOD_Studio_System_GetListenerWeight");
			_studio_systemGetMemoryUsage = (delegate* unmanaged[Cdecl]<Studio.System*, Studio.MEMORY_USAGE*>)GetProc(moduleStudio, "FMOD_Studio_System_GetMemoryUsage");
			_studio_systemGetNumListeners = (delegate* unmanaged[Cdecl]<Studio.System*, int*>)GetProc(moduleStudio, "FMOD_Studio_System_GetNumListeners");
			_studio_systemGetParameterByName = (delegate* unmanaged[Cdecl]<Studio.System*, char*, float*, float*>)GetProc(moduleStudio, "FMOD_Studio_System_GetParameterByName");
			_studio_systemGetParameterDescriptionByName = (delegate* unmanaged[Cdecl]<Studio.System*, char*, Studio.PARAMETER_DESCRIPTION*>)GetProc(moduleStudio, "FMOD_Studio_System_GetParameterDescriptionByName");
			_studio_systemGetParameterDescriptionCount = (delegate* unmanaged[Cdecl]<Studio.System*, int*>)GetProc(moduleStudio, "FMOD_Studio_System_GetParameterDescriptionCount");
		}

		private static IntPtr GetProc(IntPtr module, string name)
		{
			return NativeLibrary.GetExport(module, name);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RESULT Studio_System_Create(ref Studio.System system, uint version)
		{
			fixed (Studio.System* pSystem = &system)
			{
				return _studio_systemCreate(&pSystem, version);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RESULT Studio_System_Update(ref Studio.System system)
		{
			fixed (Studio.System* pSystem = &system)
			{
				return _studio_systemUpdate(pSystem);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RESULT Studio_System_Release(ref Studio.System system)
		{
			fixed (Studio.System* pSystem = &system)
			{
				return _studio_systemRelease(pSystem);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RESULT Studio_System_GetCoreSystem(ref Studio.System system, ref System coreSystem)
		{
			fixed (Studio.System* pSystem = &system)
			fixed (System* pCoreSystem = &coreSystem)
			{
				return _studio_systemGetCoreSystem(pSystem, &pCoreSystem);
			}
		}

		/// <summary>
		/// Gets the native dll name extension.
		/// </summary>
		/// <param name="baseName"></param>
		/// <returns></returns>
		private static string GetPlatformLibraryName(string baseName)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return $"{baseName}.dll";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return $"{baseName}.dylib";
			}
			else
			{
				if (IsAndroid())
				{
					return $"lib{baseName}.so";
				}

				string arch = RuntimeInformation.ProcessArchitecture switch
				{
					Architecture.X64 => "x86_64",
					Architecture.Arm64 => "aarch64",
					Architecture.Armv6 => "aarch32", // TODO: check if this is ACTUALLY what it is
					Architecture.X86 => "i386",
					_ => throw new Exception("...what the fuck are you running this on")
				};

				return $"lib{baseName}-{arch}.so";
			}
		}

		/// <summary>
		/// Checks if we're on android.
		/// </summary>
		/// <returns></returns>
		private static bool IsAndroid()
		{
			try
			{
				return File.Exists("/system/build.prop")
					|| Directory.Exists("/system/app")
					|| Environment.GetEnvironmentVariable("ANDROID_ROOT") != null;
			}
			catch
			{
				return false;
			}
		}
	}
}