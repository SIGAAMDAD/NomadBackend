using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nomad.Core.ECS;
using Nomad.Core.Scene.GameObjects;

namespace Nomad.EngineUtils.Private.Godot
{
	/// <summary>
	/// Heterogeneous component store for a single IGameObject.
	/// One component per exact runtime type.
	/// Fast iteration, O(1) lookup, O(1) swap-back removal.
	/// </summary>
	internal sealed class ComponentCollection : IDisposable
	{
		private struct Entry
		{
			public NomadBehaviour? Component;
			public byte Initialized;
		};

		private readonly IGameObject _owner;
		private readonly Dictionary<Type, int> _typeToIndex;
		private Entry[] _entries;
		private int _count;
		private bool _disposed;

		public int Count => _count;

		public ComponentCollection( IGameObject owner, int initialCapacity = 8 )
		{
			if ( initialCapacity < 1 ) {
				initialCapacity = 1;
			}

			_owner = owner ?? throw new ArgumentNullException( nameof( owner ) );
			_typeToIndex = new Dictionary<Type, int>( initialCapacity );
			_entries = ArrayPool<Entry>.Shared.Rent( initialCapacity );
		}

		public void Dispose()
		{
			if ( _disposed ) {
				return;
			}

			for ( int i = 0; i < _count; ++i ) {
				_entries[i] = default;
			}

			ArrayPool<Entry>.Shared.Return( _entries, clearArray: true );
			_entries = Array.Empty<Entry>();
			_typeToIndex.Clear();
			_count = 0;
			_disposed = true;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Has<T>()
			where T : class, IComponent
		{
			return _typeToIndex.ContainsKey( typeof( T ) );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryGet<T>( out T? component )
			where T : class, IComponent
		{
			if ( _typeToIndex.TryGetValue( typeof( T ), out int index ) ) {
				component = (T)(IComponent?)_entries[index].Component;
				return component is not null;
			}

			component = null;
			return false;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public T? Get<T>()
			where T : class, IComponent
		{
			return TryGet( out T? component ) ? component : null;
		}

		/// <summary>
		/// Creates, stores, initializes, then invokes the optional callback.
		/// Callback runs after OnInit.
		/// </summary>
		public T Add<T>( Action<T>? afterInit = null )
			where T : IComponent, new()
		{
			ThrowIfDisposed();

			Type type = typeof( T );
			if ( _typeToIndex.ContainsKey( type ) ) {
				throw new InvalidOperationException(
					$"Component of type '{type.FullName}' already exists on this object." );
			}

			EnsureCapacity( _count + 1 );

			T component = new T {
				Object = _owner
			};
			afterInit?.Invoke( component );

			int index = _count++;
			_entries[index] = new Entry {
				Component = component as NomadBehaviour,
				Initialized = 0
			};

			_typeToIndex.Add( type, index );

			InitializeAt( index );
			return component;
		}

		/// <summary>
		/// Adds an already-created component instance.
		/// Uses the component's exact runtime type as the lookup key.
		/// </summary>
		public NomadBehaviour AddExisting( NomadBehaviour component, bool initialize = true )
		{
			if ( component is null ) {
				throw new ArgumentNullException( nameof( component ) );
			}

			ThrowIfDisposed();

			Type type = component.GetType();
			if ( _typeToIndex.ContainsKey( type ) ) {
				throw new InvalidOperationException(
					$"Component of type '{type.FullName}' already exists on this object." );
			}

			EnsureCapacity( _count + 1 );

			component.Object = _owner;

			int index = _count++;
			_entries[index] = new Entry {
				Component = component,
				Initialized = 0
			};

			_typeToIndex.Add( type, index );

			if ( initialize ) {
				InitializeAt( index );
			}

			return component;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Remove<T>()
			where T : IComponent
		{
			return Remove( typeof( T ) );
		}

		public bool Remove( Type componentType )
		{
			if ( componentType is null ) {
				throw new ArgumentNullException( nameof( componentType ) );
			}

			ThrowIfDisposed();

			if ( !_typeToIndex.TryGetValue( componentType, out int index ) ) {
				return false;
			}

			ShutdownAt( index );
			RemoveAt( index );
			return true;
		}

		/// <summary>
		/// Initializes any components that were inserted deferred.
		/// Safe to call during owner OnInit.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveOptimization )]
		public void InitializePending()
		{
			ThrowIfDisposed();

			ref Entry start = ref MemoryMarshal.GetArrayDataReference( _entries );

			for ( nint i = 0, count = _count; i < count; ++i ) {
				ref Entry entry = ref Unsafe.Add( ref start, i );
				if ( entry.Component is not null && entry.Initialized == 0 ) {
					entry.Component.OnInit();
					entry.Initialized = 1;
				}
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveOptimization )]
		public void ShutdownAll()
		{
			ThrowIfDisposed();

			ref Entry start = ref MemoryMarshal.GetArrayDataReference( _entries );

			for ( nint i = 0, count = _count; i < count; ++i ) {
				ref Entry entry = ref Unsafe.Add( ref start, i );
				if ( entry.Component is not null && entry.Initialized != 0 ) {
					entry.Component.OnShutdown();
					entry.Initialized = 0;
				}
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveOptimization )]
		public void UpdateAll( float delta )
		{
			ThrowIfDisposed();

			ref Entry start = ref MemoryMarshal.GetArrayDataReference( _entries );

			for ( nint i = 0, count = _count; i < count; ++i ) {
				ref Entry entry = ref Unsafe.Add( ref start, i );
				if ( entry.Initialized != 0 ) {
					entry.Component!.OnUpdate( delta );
				}
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveOptimization )]
		public void PhysicsUpdateAll( float delta )
		{
			ThrowIfDisposed();

			ref Entry start = ref MemoryMarshal.GetArrayDataReference( _entries );

			for ( nint i = 0, count = _count; i < count; ++i ) {
				ref Entry entry = ref Unsafe.Add( ref start, i );
				if ( entry.Initialized != 0 ) {
					entry.Component!.OnPhysicsUpdate( delta );
				}
			}
		}

		public void Clear()
		{
			ThrowIfDisposed();

			for ( int i = _count - 1; i >= 0; --i ) {
				ShutdownAt( i );
				_entries[i] = default;
			}

			_typeToIndex.Clear();
			_count = 0;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void InitializeAt( int index )
		{
			ref Entry entry = ref _entries[index];
			if ( entry.Component is not null && entry.Initialized == 0 ) {
				entry.Component.OnInit();
				entry.Initialized = 1;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void ShutdownAt( int index )
		{
			ref Entry entry = ref _entries[index];
			if ( entry.Component is not null && entry.Initialized != 0 ) {
				entry.Component.OnShutdown();
				entry.Initialized = 0;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void RemoveAt( int index )
		{
			Type removedType = _entries[index].Component!.GetType();

			int last = _count - 1;
			if ( index != last ) {
				Entry moved = _entries[last];
				_entries[index] = moved;

				Type movedType = moved.Component!.GetType();
				_typeToIndex[movedType] = index;
			}

			_entries[last] = default;
			_typeToIndex.Remove( removedType );
			_count = last;
		}

		private void EnsureCapacity( int needed )
		{
			if ( needed <= _entries.Length ) {
				return;
			}

			int newCapacity = _entries.Length < 4 ? 4 : _entries.Length * 2;
			if ( newCapacity < needed ) {
				newCapacity = needed;
			}

			Entry[] newEntries = ArrayPool<Entry>.Shared.Rent( newCapacity );
			Array.Copy( _entries, 0, newEntries, 0, _count );

			ArrayPool<Entry>.Shared.Return( _entries, clearArray: true );
			_entries = newEntries;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void ThrowIfDisposed()
		{
			if ( _disposed ) {
				throw new ObjectDisposedException( nameof( ComponentCollection ) );
			}
		}
	};
};
