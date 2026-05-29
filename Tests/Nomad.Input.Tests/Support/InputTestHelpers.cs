using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Input;
using Nomad.Core.Memory.Buffers;
using Nomad.Events;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests
{
    internal sealed class InputFileSystemFixture
    {
        private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _searchDirectories = new();

        public Mock<IFileSystem> Mock { get; }
        public IFileSystem Object => Mock.Object;

        public InputFileSystemFixture(params (string Path, string Content)[] files)
        {
            Mock = new Mock<IFileSystem>(MockBehavior.Strict);

            for (int i = 0; i < files.Length; i++)
            {
                SetFile(files[i].Path, files[i].Content);
            }

            Mock.Setup(fileSystem => fileSystem.Dispose());
            Mock.Setup(fileSystem => fileSystem.AddSearchDirectory(It.IsAny<string>()))
                .Callback((string path) => AddSearchDirectory(path));
            Mock.Setup(fileSystem => fileSystem.FileExists(It.IsAny<string>()))
                .Returns((string path) => FileExists(path));
            Mock.Setup(fileSystem => fileSystem.LoadFile(It.IsAny<string>()))
                .Returns((string path) => LoadBuffer(path));
            Mock.Setup(fileSystem => fileSystem.DirectoryExists(It.IsAny<string>()))
                .Returns((string path) => DirectoryExists(path));
            Mock.Setup(fileSystem => fileSystem.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string path, string pattern, bool recursive) => GetFiles(path, pattern, recursive));
        }

        public void SetFile(string path, string content)
        {
            string normalizedPath = NormalizePath(path);
            _files[normalizedPath] = content;
            RegisterParentDirectories(normalizedPath);
        }

        public void SetDirectory(string path)
        {
            RegisterDirectoryHierarchy(path);
        }

        private void AddSearchDirectory(string path)
        {
            string normalizedDirectory = NormalizeDirectory(path);
            if (string.IsNullOrEmpty(normalizedDirectory))
            {
                return;
            }

            RegisterDirectoryHierarchy(normalizedDirectory);
            if (!_searchDirectories.Contains(normalizedDirectory, StringComparer.OrdinalIgnoreCase))
            {
                _searchDirectories.Add(normalizedDirectory);
            }
        }

        private IBufferHandle? LoadBuffer(string path)
        {
            if (!_files.TryGetValue(ResolvePath(path), out string? content))
            {
                return null;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(content);
            var buffer = new StandardBufferHandle(bytes.Length);
            bytes.CopyTo(buffer.Span);
            return buffer;
        }

        private bool FileExists(string path)
        {
            return _files.ContainsKey(ResolvePath(path));
        }

        private bool DirectoryExists(string path)
        {
            string normalizedDirectory = NormalizeDirectory(path);
            return !string.IsNullOrEmpty(normalizedDirectory) && _directories.Contains(normalizedDirectory);
        }

        private IReadOnlyList<string> GetFiles(string path, string pattern, bool recursive)
        {
            string normalizedDirectory = NormalizeDirectory(path);

            return _files.Keys
                .Where(file => IsFileInDirectory(file, normalizedDirectory, recursive))
                .Where(file => MatchesPattern(Path.GetFileName(file), pattern))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private void RegisterParentDirectories(string filePath)
        {
            string? directoryPath = GetContainingDirectory(filePath);
            if (directoryPath == null)
            {
                return;
            }

            RegisterDirectoryHierarchy(directoryPath);
        }

        private void RegisterDirectoryHierarchy(string directoryPath)
        {
            for (string? current = NormalizeDirectory(directoryPath); !string.IsNullOrEmpty(current); current = GetParentDirectory(current))
            {
                _directories.Add(current);
            }
        }

        private static bool IsFileInDirectory(string filePath, string directoryPath, bool recursive)
        {
            if (string.IsNullOrEmpty(directoryPath) || !filePath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (recursive)
            {
                return true;
            }

            return string.Equals(GetContainingDirectory(filePath), directoryPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesPattern(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
            {
                return true;
            }

            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                return fileName.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
            }

            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetContainingDirectory(string path)
        {
            string normalizedPath = NormalizePath(path);
            int separatorIndex = normalizedPath.LastIndexOf('/');
            return separatorIndex < 0 ? null : normalizedPath[..(separatorIndex + 1)];
        }

        private static string? GetParentDirectory(string directoryPath)
        {
            string trimmedDirectory = directoryPath.TrimEnd('/');
            if (string.IsNullOrEmpty(trimmedDirectory))
            {
                return null;
            }

            int separatorIndex = trimmedDirectory.LastIndexOf('/');
            return separatorIndex < 0 ? null : trimmedDirectory[..(separatorIndex + 1)];
        }

        private static string NormalizeDirectory(string path)
        {
            string normalized = NormalizePath(path).TrimEnd('/');
            return string.IsNullOrEmpty(normalized) ? string.Empty : normalized + "/";
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }

        private string ResolvePath(string path)
        {
            string normalizedPath = NormalizePath(path);
            if (_files.ContainsKey(normalizedPath))
            {
                return normalizedPath;
            }

            foreach (string searchDirectory in _searchDirectories)
            {
                string candidate = NormalizePath(Path.Combine(searchDirectory, normalizedPath));
                if (_files.ContainsKey(candidate))
                {
                    return candidate;
                }
            }

            return normalizedPath;
        }
    }

    internal static class InputTestHelpers
    {
        public static GameEventRegistry CreateEventRegistry(out MockLogger logger)
        {
            logger = new MockLogger();
            return new GameEventRegistry(logger);
        }

        public static MockCVarSystem CreateCVarSystem(IGameEventRegistryService eventRegistry, string defaultsPath)
        {
            var cvarSystem = new MockCVarSystem(eventRegistry);
            cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.DEFAULTS_PATH, defaultsPath);
            cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.KEYBOARD_MOUSE_MAPPING, "KeyboardAndMouse");
            cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.GAMEPAD_MAPPING, "Gamepad");
            cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.INPUT_DELAY_MS, 0);
            return cvarSystem;
        }

        public static InputActionDefinition Action(string name, InputValueType valueType, params InputBindingDefinition[] bindings)
        {
            return Action(name, name, valueType, bindings);
        }

        public static InputActionDefinition Action(string name, string id, InputValueType valueType, params InputBindingDefinition[] bindings)
        {
            return new InputActionDefinition(
                name,
                id,
                valueType,
                bindings.ToImmutableArray()
            );
        }

        public static InputBindingDefinition Button(InputScheme scheme, InputDeviceSlot device, InputControlId control, params InputControlId[] modifiers)
        {
            return new InputBindingDefinition
            {
                Scheme = scheme,
                Kind = InputBindingKind.Button,
                Button = new ButtonBinding(device, control, modifiers.ToImmutableArray())
            };
        }

        public static InputBindingDefinition Axis1D(InputScheme scheme, InputDeviceSlot device, InputControlId control, float deadzone = 0.0f, float sensitivity = 1.0f, float scale = 1.0f, bool invert = false)
        {
            return new InputBindingDefinition
            {
                Scheme = scheme,
                Kind = InputBindingKind.Axis1D,
                Axis1D = new Axis1DBinding(device, control, deadzone, sensitivity, scale, invert)
            };
        }

        public static InputBindingDefinition Axis2D(InputScheme scheme, InputDeviceSlot device, InputControlId control, float deadzone = 0.0f, float sensitivity = 1.0f, float scaleX = 1.0f, float scaleY = 1.0f, bool invertX = false, bool invertY = false)
        {
            return new InputBindingDefinition
            {
                Scheme = scheme,
                Kind = InputBindingKind.Axis2D,
                Axis2D = new Axis2DBinding(device, control, deadzone, sensitivity, scaleX, scaleY, invertX, invertY)
            };
        }

        public static InputBindingDefinition Delta2D(InputScheme scheme, InputDeviceSlot device, InputControlId control, float sensitivity = 1.0f, float scaleX = 1.0f, float scaleY = 1.0f, bool invertX = false, bool invertY = false)
        {
            return new InputBindingDefinition
            {
                Scheme = scheme,
                Kind = InputBindingKind.Delta2D,
                Delta2D = new Delta2DBinding(device, control, sensitivity, scaleX, scaleY, invertX, invertY)
            };
        }

        public static InputBindingDefinition Composite1D(InputScheme scheme, InputControlId negative, InputControlId positive, float scale = 1.0f, bool normalize = true)
        {
            return new InputBindingDefinition
            {
                Scheme = scheme,
                Kind = InputBindingKind.Axis1DComposite,
                Axis1DComposite = new Axis1DCompositeBinding(negative, positive, scale, normalize)
            };
        }

        public static InputBindingDefinition Composite2D(InputScheme scheme, InputControlId up, InputControlId down, InputControlId left, InputControlId right, float scaleX = 1.0f, float scaleY = 1.0f, bool normalize = true)
        {
            return new InputBindingDefinition
            {
                Scheme = scheme,
                Kind = InputBindingKind.Axis2DComposite,
                Axis2DComposite = new Axis2DCompositeBinding(up, down, left, right, normalize, scaleX, scaleY)
            };
        }

        public static CompiledBindingRepository CompileToRepository(params InputActionDefinition[] actions)
        {
            var repository = new CompiledBindingRepository();
            var compiler = new BindingCompilerService(repository);
            compiler.CompileIntoRepository(actions.ToImmutableArray());
            return repository;
        }
    }
}
