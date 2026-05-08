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
using Godot;
using Nomad.Core.ECS;
using Nomad.Core.Scene.GameObjects;
using Nomad.EngineUtils.Private.Godot;

namespace Nomad.EngineUtils
{
    /// <summary>
    ///
    /// </summary>
    public sealed class GodotGameObject : IGameObject
    {
        /// <summary>
        ///
        /// </summary>
        public string Name
        {
            get => _node.Name;
            set => _node.Name = value;
        }

        /// <summary>
        ///
        /// </summary>
        public bool Enabled
        {
            get => _node.ProcessMode != Node.ProcessModeEnum.Disabled;
            set => _node.ProcessMode = value ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        }

        /// <summary>
        ///
        /// </summary>
        public IGameObject? Parent
        {
            get => _parent;
            set => _parent = value;
        }
        private IGameObject? _parent;

        /// <summary>
        ///
        /// </summary>
        public Node Node => _node;
        private readonly Node _node;

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<IGameObject> Children => _children;
        private readonly List<IGameObject> _children = new();

        private readonly ComponentCollection _components;

        private bool _isDisposed = false;

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        public GodotGameObject(Node node)
        {
            _node = node;
            _components = new ComponentCollection(this);
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _components.Clear();
            }
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? CastAs<T>()
            where T : class, IGameObject
        {
            if (_node is T castedType)
            {
                return castedType;
            }
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="initializer"></param>
        public T AddComponent<T>(Action<T>? initializer = null)
            where T : IComponent, new()
        {
            return _components.Add(initializer);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetComponent<T>()
            where T : class, IComponent
        {
            return _components.Get<T>();
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasComponent<T>()
            where T : class, IComponent
        {
            return _components.Has<T>();
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveComponent<T>()
            where T : class, IComponent
        {
            _components.Remove(typeof(T));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="childName"></param>
        /// <returns></returns>
        public T? FindChild<T>(string childName)
            where T : class, IGameObject
        {
            if (string.IsNullOrEmpty(childName))
            {
                return null;
            }

            // TODO: write a zero-allocation version of this
            ReadOnlySpan<string> segments = childName.Split('/');
            var current = _node;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }
                bool found = false;
                foreach (var child in current.GetChildren())
                {
                    if (child.Name == segment)
                    {
                        current = child;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return null;
                }
            }
            return (T)WrapNode(current);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static IGameObject WrapNode(Node node)
        {
            if (node is IGameObject gameObject)
            {
                return gameObject;
            }
            return new GodotGameObject(node);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(IGameObject child)
        {
            if (child is Node node)
            {
                _node.AddChild(node);
            }
            else if (child is GodotGameObject gameObject)
            {
                _node.AddChild(gameObject._node);
            }
            _children.Add(child);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="child"></param>
        public void RemoveChild(IGameObject child)
        {
            if (child is Node node)
            {
                _node.RemoveChild(node);
            }
            else if (child is GodotGameObject gameObject)
            {
                _node.RemoveChild(gameObject._node);
            }
            _children.Remove(child);
        }

        /// <summary>
        ///
        /// </summary>
        public void OnInit()
        {
            _components.InitializePending();
        }

        /// <summary>
        ///
        /// </summary>
        public void OnShutdown()
        {
            _components.ShutdownAll();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="delta"></param>
        public void OnUpdate(float delta)
        {
            _components.UpdateAll(delta);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="delta"></param>
        public void OnPhysicsUpdate(float delta)
        {
            _components.PhysicsUpdateAll(delta);
        }
    }
}
