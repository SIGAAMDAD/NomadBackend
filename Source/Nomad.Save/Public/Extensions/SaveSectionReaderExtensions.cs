/*
===========================================================================
The Nomad MPLv2 Source Code
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
using Nomad.Core.Compatibility.Guards;
using Nomad.Save.Interfaces;

namespace Nomad.Save.Extensions
{
    public static class SaveSectionReaderExtensions
    {
        public static T Read<T>(this ISaveSectionReader reader, string name)
            where T : unmanaged
        {
            ArgumentGuard.ThrowIfNull(reader);
            ArgumentGuard.ThrowIfNull(name);

            if (typeof(T).IsEnum)
            {
                return (T)ReadEnum(reader, name, typeof(T));
            }

            return reader.GetField<T>(name);
        }

        public static bool ReadBool(this ISaveSectionReader reader, string name)
        {
            return Read<bool>(reader, name);
        }

        public static byte ReadByte(this ISaveSectionReader reader, string name)
        {
            return Read<byte>(reader, name);
        }

        public static sbyte ReadSByte(this ISaveSectionReader reader, string name)
        {
            return Read<sbyte>(reader, name);
        }

        public static short ReadInt16(this ISaveSectionReader reader, string name)
        {
            return Read<short>(reader, name);
        }

        public static ushort ReadUInt16(this ISaveSectionReader reader, string name)
        {
            return Read<ushort>(reader, name);
        }

        public static int ReadInt32(this ISaveSectionReader reader, string name)
        {
            return Read<int>(reader, name);
        }

        public static uint ReadUInt32(this ISaveSectionReader reader, string name)
        {
            return Read<uint>(reader, name);
        }

        public static long ReadInt64(this ISaveSectionReader reader, string name)
        {
            return Read<long>(reader, name);
        }

        public static ulong ReadUInt64(this ISaveSectionReader reader, string name)
        {
            return Read<ulong>(reader, name);
        }

        public static float ReadSingle(this ISaveSectionReader reader, string name)
        {
            return Read<float>(reader, name);
        }

        public static double ReadDouble(this ISaveSectionReader reader, string name)
        {
            return Read<double>(reader, name);
        }

        public static string ReadString(this ISaveSectionReader reader, string name)
        {
            ArgumentGuard.ThrowIfNull(reader);
            ArgumentGuard.ThrowIfNull(name);

            return reader.GetString(name);
        }

        public static TEnum ReadEnum<TEnum>(this ISaveSectionReader reader, string name)
            where TEnum : unmanaged, Enum
        {
            ArgumentGuard.ThrowIfNull(reader);
            ArgumentGuard.ThrowIfNull(name);

            Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            return (TEnum)ReadEnum(reader, name, typeof(TEnum), underlyingType);
        }

        private static object ReadEnum(ISaveSectionReader reader, string name, Type enumType)
        {
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            return ReadEnum(reader, name, enumType, underlyingType);
        }

        private static object ReadEnum(ISaveSectionReader reader, string name, Type enumType, Type underlyingType)
        {
            if (underlyingType == typeof(byte))
            {
                return Enum.ToObject(enumType, reader.ReadByte(name));
            }

            if (underlyingType == typeof(sbyte))
            {
                return Enum.ToObject(enumType, reader.ReadSByte(name));
            }

            if (underlyingType == typeof(short))
            {
                return Enum.ToObject(enumType, reader.ReadInt16(name));
            }

            if (underlyingType == typeof(ushort))
            {
                return Enum.ToObject(enumType, reader.ReadUInt16(name));
            }

            if (underlyingType == typeof(int))
            {
                return Enum.ToObject(enumType, reader.ReadInt32(name));
            }

            if (underlyingType == typeof(uint))
            {
                return Enum.ToObject(enumType, reader.ReadUInt32(name));
            }

            if (underlyingType == typeof(long))
            {
                return Enum.ToObject(enumType, reader.ReadInt64(name));
            }

            if (underlyingType == typeof(ulong))
            {
                return Enum.ToObject(enumType, reader.ReadUInt64(name));
            }

            throw new InvalidOperationException($"Enum '{enumType.FullName}' has unsupported underlying type '{underlyingType.FullName}'.");
        }

        public static List<T> ReadList<T>(this ISaveSectionReader section, string name)
            where T : unmanaged
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);

            return ReadList(section, name, static (reader, path) => reader.Read<T>(path));
        }

        public static List<T> ReadList<T>(this ISaveSectionReader section, string name, Func<ISaveSectionReader, string, T> read)
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(read);

            int count = section.ReadInt32($"{name}.Count");
            var values = new List<T>(count);

            for (int index = 0; index < count; index++)
            {
                values.Add(read(section, $"{name}[{index}]"));
            }

            return values;
        }

        public static List<string> ReadStringList(this ISaveSectionReader section, string name)
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);

            return ReadList(section, name, static (reader, path) => reader.ReadString(path));
        }

        public static T[] ReadArray<T>(this ISaveSectionReader section, string name)
            where T : unmanaged
        {
            return ReadList<T>(section, name).ToArray();
        }

        public static T[] ReadArray<T>(this ISaveSectionReader section, string name, Func<ISaveSectionReader, string, T> read)
        {
            return ReadList(section, name, read).ToArray();
        }

        public static string[] ReadStringArray(this ISaveSectionReader section, string name)
        {
            return ReadStringList(section, name).ToArray();
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this ISaveSectionReader section, string name, Func<ISaveSectionReader, TValue> read)
            where TKey : unmanaged
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(read);

            int count = section.ReadInt32($"{name}.Count");
            var values = new Dictionary<TKey, TValue>(count);

            for (int index = 0; index < count; index++)
            {
                string entryPath = $"{name}[{index}]";

                TKey key = section.Read<TKey>($"{entryPath}.Key");
                TValue value = read(section);

                values[key] = value;
            }

            return values;
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this ISaveSectionReader section, string name, Func<ISaveSectionReader, string, TValue> read)
            where TKey : unmanaged
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(read);

            int count = section.ReadInt32($"{name}.Count");
            var values = new Dictionary<TKey, TValue>(count);

            for (int index = 0; index < count; index++)
            {
                string entryPath = $"{name}[{index}]";

                TKey key = section.Read<TKey>($"{entryPath}.Key");
                TValue value = read(section, $"{entryPath}.Value");

                values[key] = value;
            }

            return values;
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this ISaveSectionReader section, string name)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);

            int count = section.ReadInt32($"{name}.Count");
            var values = new Dictionary<TKey, TValue>(count);

            for (int index = 0; index < count; index++)
            {
                string entryPath = $"{name}[{index}]";

                TKey key = section.Read<TKey>($"{entryPath}.Key");
                TValue value = section.Read<TValue>($"{entryPath}.Value");

                values[key] = value;
            }

            return values;
        }
    }
}
