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
    public readonly struct SaveDictionaryEntryWriter
    {
        private readonly ISaveSectionWriter _section;
        private readonly string _path;

        public SaveDictionaryEntryWriter(
            ISaveSectionWriter section,
            string path)
        {
            _section = section;
            _path = path;
        }

        public void Write<T>(
            string name,
            T value)
        {
            _section.Write($"{_path}.{name}", value);
        }

        public SaveObjectNodeWriter Object(
            string name)
        {
            return new SaveObjectNodeWriter(
                _section,
                $"{_path}.{name}");
        }
    }

    public readonly struct SaveObjectNodeWriter
    {
        private readonly ISaveSectionWriter _section;
        private readonly string _path;

        public SaveObjectNodeWriter(ISaveSectionWriter section, string path)
        {
            _section = section;
            _path = path;
        }

        public void Write<T>(string name, T value)
        {
            _section.Write($"{_path}.{name}", value);
        }
    }

    public static class SaveSectionWriterExtensions
    {
        public static void Write<T>(this ISaveSectionWriter writer, string name, T value)
        {
            ArgumentGuard.ThrowIfNull(writer);
            ArgumentGuard.ThrowIfNull(name);

            if (typeof(T).IsEnum)
            {
                WriteEnum(writer, name, typeof(T), value);
                return;
            }

            writer.AddField(name, value);
        }

        public static void WriteBool(this ISaveSectionWriter writer, string name, bool value)
        {
            Write(writer, name, value);
        }

        public static void WriteByte(this ISaveSectionWriter writer, string name, byte value)
        {
            Write(writer, name, value);
        }

        public static void WriteSByte(this ISaveSectionWriter writer, string name, sbyte value)
        {
            Write(writer, name, value);
        }

        public static void WriteInt16(this ISaveSectionWriter writer, string name, short value)
        {
            Write(writer, name, value);
        }

        public static void WriteUInt16(this ISaveSectionWriter writer, string name, ushort value)
        {
            Write(writer, name, value);
        }

        public static void WriteInt32(this ISaveSectionWriter writer, string name, int value)
        {
            Write(writer, name, value);
        }

        public static void WriteUInt32(this ISaveSectionWriter writer, string name, uint value)
        {
            Write(writer, name, value);
        }

        public static void WriteInt64(this ISaveSectionWriter writer, string name, long value)
        {
            Write(writer, name, value);
        }

        public static void WriteUInt64(this ISaveSectionWriter writer, string name, ulong value)
        {
            Write(writer, name, value);
        }

        public static void WriteSingle(this ISaveSectionWriter writer, string name, float value)
        {
            Write(writer, name, value);
        }

        public static void WriteDouble(this ISaveSectionWriter writer, string name, double value)
        {
            Write(writer, name, value);
        }

        public static void WriteString(this ISaveSectionWriter writer, string name, string value)
        {
            Write(writer, name, value);
        }

        public static void WriteEnum<TEnum>(this ISaveSectionWriter writer, string name, TEnum value)
            where TEnum : unmanaged, Enum
        {
            ArgumentGuard.ThrowIfNull(writer);
            ArgumentGuard.ThrowIfNull(name);

            WriteEnum(writer, name, typeof(TEnum), value);
        }

        private static void WriteEnum<T>(ISaveSectionWriter writer, string name, Type enumType, T value)
        {
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            object converted = Convert.ChangeType(value, underlyingType);

            if (underlyingType == typeof(byte))
            {
                writer.WriteByte(name, (byte)converted);
                return;
            }

            if (underlyingType == typeof(sbyte))
            {
                writer.WriteSByte(name, (sbyte)converted);
                return;
            }

            if (underlyingType == typeof(short))
            {
                writer.WriteInt16(name, (short)converted);
                return;
            }

            if (underlyingType == typeof(ushort))
            {
                writer.WriteUInt16(name, (ushort)converted);
                return;
            }

            if (underlyingType == typeof(int))
            {
                writer.WriteInt32(name, (int)converted);
                return;
            }

            if (underlyingType == typeof(uint))
            {
                writer.WriteUInt32(name, (uint)converted);
                return;
            }

            if (underlyingType == typeof(long))
            {
                writer.WriteInt64(name, (long)converted);
                return;
            }

            if (underlyingType == typeof(ulong))
            {
                writer.WriteUInt64(name, (ulong)converted);
                return;
            }

            throw new InvalidOperationException($"Enum '{enumType.FullName}' has unsupported underlying type '{underlyingType.FullName}'.");
        }

        public static void WriteList<T>(this ISaveSectionWriter section, string name, IReadOnlyList<T> values)
            where T : unmanaged
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(values);

            section.WriteInt32($"{name}.Count", values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                section.Write($"{name}[{index}]", values[index]);
            }
        }

        public static void WriteList<T>(this ISaveSectionWriter section, string name, IReadOnlyList<T> values, Action<ISaveSectionWriter, string, T> write)
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(values);
            ArgumentGuard.ThrowIfNull(write);

            section.WriteInt32($"{name}.Count", values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                write(section, $"{name}[{index}]", values[index]);
            }
        }

        public static void WriteStringList(this ISaveSectionWriter section, string name, IReadOnlyList<string> values)
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(values);

            WriteList(section, name, values, static (writer, path, value) => writer.WriteString(path, value));
        }

        public static void WriteArray<T>(this ISaveSectionWriter section, string name, T[] values)
            where T : unmanaged
        {
            WriteList(section, name, values);
        }

        public static void WriteArray<T>(this ISaveSectionWriter section, string name, T[] values, Action<ISaveSectionWriter, string, T> write)
        {
            WriteList(section, name, values, write);
        }

        public static void WriteStringArray(this ISaveSectionWriter section, string name, string[] values)
        {
            WriteStringList(section, name, values);
        }

        public static void WriteDictionary<TKey, TValue>(
            this ISaveSectionWriter section,
            string name,
            IReadOnlyDictionary<TKey, TValue> values
        )
            where TKey : notnull
            where TValue : unmanaged
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(values);

            section.WriteInt32($"{name}.Count", values.Count);

            int index = 0;

            foreach (var pair in values)
            {
                string entryPath = $"{name}[{index}]";

                section.Write($"{entryPath}.Key", pair.Key);
                section.Write($"{entryPath}.Value", pair.Value);

                index++;
            }
        }

        public static void WriteDictionary<TKey, TValue>(
            this ISaveSectionWriter section,
            string name,
            IReadOnlyDictionary<TKey, TValue> values,
            Action<SaveDictionaryEntryWriter, TValue> writeValue
        )
            where TKey : notnull
        {
            ArgumentGuard.ThrowIfNull(section);
            ArgumentGuard.ThrowIfNull(name);
            ArgumentGuard.ThrowIfNull(values);
            ArgumentGuard.ThrowIfNull(writeValue);

            section.AddField($"{name}.Count", values.Count);

            int index = 0;

            foreach (var pair in values)
            {
                string entryPath = $"{name}[{index}]";

                section.Write($"{entryPath}.Key", pair.Key);

                var entryWriter = new SaveDictionaryEntryWriter(section, $"{entryPath}.Value");

                writeValue(entryWriter, pair.Value);

                index++;
            }
        }
    }
}
