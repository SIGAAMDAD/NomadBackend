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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Nomad.EngineUtils.Settings
{
    /// <summary>
    /// 
    /// </summary>
    public static class MonitorDisplayNames
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IReadOnlyList<string> GetDisplayNames()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsDisplayNames();
            }
            /*
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacDisplayNames();
            }
            */
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxDisplayNames();
            }
            return Array.Empty<string>();
        }

        // ----------------------------
        // Windows
        // ----------------------------

        private static IReadOnlyList<string> GetWindowsDisplayNames()
        {
            var results = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            uint adapterIndex = 0;
            while (true)
            {
                var adapter = new DISPLAY_DEVICE()
                {
                    cb = Marshal.SizeOf<DISPLAY_DEVICE>()
                };

                if (!EnumDisplayDevices(null, adapterIndex, ref adapter, 0))
                {
                    break;
                }

                string adapterName = adapter.DeviceName;
                uint monitorIndex = 0;
                while (true)
                {
                    var monitor = new DISPLAY_DEVICE()
                    {
                        cb = Marshal.SizeOf<DISPLAY_DEVICE>()
                    };

                    if (!EnumDisplayDevices(adapterName, monitorIndex, ref monitor, 0))
                    {
                        break;
                    }

                    var name = (monitor.DeviceString ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(name) && seen.Add(name))
                    {
                        results.Add(name);
                    }
                    monitorIndex++;
                }
                adapterIndex++;
            }
            return results;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public int StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumDisplayDevices(
            string? lpDevice,
            uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags
        );

        /*
        // ----------------------------
        // macOS
        // ----------------------------

        private static IReadOnlyList<string> GetMacDisplayNames()
        {
            var results = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Load AppKit so NSScreen is available.
            NativeLibrary.TryLoad("/System/Library/Frameworks/AppKit.framework/AppKit", out _);

            IntPtr nsScreenClass = objc_getClass("NSScreen");
            if (nsScreenClass == IntPtr.Zero)
            {
                return results;
            }

            IntPtr screensSel = sel_registerName("screens");
            IntPtr countSel = sel_registerName("count");
            IntPtr objectAtIndexSel = sel_registerName("objectAtIndex:");
            IntPtr localizedNameSel = sel_registerName("localizedName");

            IntPtr screensArray = objc_msgSend_IntPtr(nsScreenClass, screensSel);
            if (screensArray == IntPtr.Zero)
            {
                return results;
            }

            nuint count = objc_msgSend_nuint(screensArray, countSel);

            for (nuint i = 0; i < count; i++)
            {
                IntPtr screen = objc_msgSend_IntPtr_nuint(screensArray, objectAtIndexSel, i);
                if (screen == IntPtr.Zero)
                {
                    continue;
                }

                IntPtr nsString = objc_msgSend_IntPtr(screen, localizedNameSel);
                string? name = NSStringToString(nsString);

                if (!string.IsNullOrWhiteSpace(name) && seen.Add(name))
                {
                    results.Add(name);
                }
            }

            return results;
        }

        private static string? NSStringToString(IntPtr nsString)
        {
            if (nsString == IntPtr.Zero)
            {
                return null;
            }

            IntPtr utf8Sel = sel_registerName("UTF8String");
            IntPtr utf8Ptr = objc_msgSend_IntPtr(nsString, utf8Sel);
            return utf8Ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8Ptr);
        }

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern nuint objc_msgSend_nuint(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr_nuint(IntPtr receiver, IntPtr selector, nuint arg1);
        */

        // ----------------------------
        // Linux
        // ----------------------------

        private static IReadOnlyList<string> GetLinuxDisplayNames()
        {
            var results = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            const string drmRoot = "/sys/class/drm";
            if (!Directory.Exists(drmRoot))
            {
                return results;
            }

            foreach (var dir in Directory.EnumerateDirectories(drmRoot))
            {
                var dirName = Path.GetFileName(dir);

                // Connector dirs usually look like: card0-HDMI-A-1, card1-DP-2, etc.
                if (string.IsNullOrEmpty(dirName) || !dirName.Contains('-', StringComparison.InvariantCulture))
                {
                    continue;
                }

                string statusPath = Path.Combine(dir, "status");
                if (!File.Exists(statusPath))
                {
                    continue;
                }

                string status;
                try
                {
                    status = File.ReadAllText(statusPath).Trim();
                }
                catch
                {
                    continue;
                }

                if (!status.Equals("connected", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string connectorName = dirName.Substring(dirName.IndexOf('-', StringComparison.InvariantCulture) + 1);
                string? monitorName = null;

                string edidPath = Path.Combine(dir, "edid");
                if (File.Exists(edidPath))
                {
                    try
                    {
                        byte[] edid = File.ReadAllBytes(edidPath);
                        monitorName = TryParseMonitorNameFromEdid(edid);
                    }
                    catch
                    {
                        // Ignore and fall back to connector name.
                    }
                }

                string finalName = string.IsNullOrWhiteSpace(monitorName)
                    ? connectorName
                    : monitorName!;

                if (seen.Add(finalName))
                {
                    results.Add(finalName);
                }
            }

            return results;
        }

        // Minimal EDID parser: looks for the monitor-name descriptor.
        private static string? TryParseMonitorNameFromEdid(byte[] edid)
        {
            if (edid == null || edid.Length < 128)
            {
                return null;
            }

            // Standard EDID descriptor blocks start at byte 54.
            for (int offset = 54; offset + 18 <= Math.Min(edid.Length, 126); offset += 18)
            {
                if (edid[offset + 0] == 0x00 &&
                    edid[offset + 1] == 0x00 &&
                    edid[offset + 2] == 0x00 &&
                    edid[offset + 3] == 0xFC)
                {
                    var raw = new byte[13];
                    Buffer.BlockCopy(edid, offset + 5, raw, 0, 13);

                    string name = Encoding.ASCII.GetString(raw)
                        .Replace("\0", string.Empty, StringComparison.InvariantCulture)
                        .Replace("\r", string.Empty, StringComparison.InvariantCulture)
                        .Replace("\n", string.Empty, StringComparison.InvariantCulture)
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
                }
            }

            return null;
        }
    }
}
