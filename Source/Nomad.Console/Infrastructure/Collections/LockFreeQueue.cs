/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/


using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure.Collections {
	/*
	===================================================================================
	
	LockFreeQueue
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	/// <summary>
	/// Lock-free queue implementation similar to boost::lockfree::queue
	/// Supports single-producer/single-consumer (SPSC) or 
	/// multi-producer/multi-consumer (MPMC) modes
	/// </summary>
	/// <typeparam name="T">Type of elements in queue</typeparam>
	public class LockFreeQueue<T> : IProducerConsumerCollection<T> {
		private class Node( T value = default ) {
			public T Value = value;
			public Node Next = null;
		};

		public object SyncRoot => throw new NotSupportedException( "Lock-free collections don't support SyncRoot" );
		public bool IsSynchronized => true;

		/// <summary>
		/// Check if queue is empty (approximate)
		/// </summary>
		public bool IsEmpty => _head.Next == null;

		/// <summary>
		/// Get approximate count (for monitoring only, not for synchronization)
		/// </summary>
		public int Count {
			get {
				// Volatile read for best effort
				return Volatile.Read( ref _count );
			}
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// Clear all items from queue
		/// </summary>
		public void Clear() {
			while ( TryTake( out _ ) ) { }
		}

		/*
		===============
		CopyTo
		===============
		*/
		public void CopyTo( T[] array, int index ) {
			if ( array == null ) throw new ArgumentNullException( nameof( array ) );
			if ( index < 0 || index >= array.Length )
				throw new ArgumentOutOfRangeException( nameof( index ) );

			int i = index;
			Node current = _head.Next;
			while ( current != null && i < array.Length ) {
				array[ i++ ] = current.Value;
				current = current.Next;
			}
		}

		/*
		===============
		CopyTo
		===============
		*/
		public void CopyTo( Array array, int index ) {
			CopyTo( (T[])array, index );
		}

		/*
		===============
		ToArray
		===============
		*/
		public T[] ToArray() {
			var list = new List<T>();
			Node current = _head.Next;
			while ( current != null ) {
				list.Add( current.Value );
				current = current.Next;
			}
			return list.ToArray();
		}

		/*
		===============
		GetEnumerator
		===============
		*/
		public IEnumerator<T> GetEnumerator() {
			Node current = _head.Next;
			while ( current != null ) {
				yield return current.Value;
				current = current.Next;
			}
		}

		/*
		===============
		GetEnumerator
		===============
		*/
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private volatile Node _head;
		private volatile Node _tail;
		private readonly bool _allowMultipleProducers;
		private readonly bool _allowMultipleConsumers;
		private int _count;
		private readonly object _countLock = new();

		/*
		===============
		LockFreeQueue
		===============
		*/
		/// <summary>
		/// Creates a lock-free queue
		/// </summary>
		/// <param name="capacity">Initial capacity (hint for pre-allocation)</param>
		/// <param name="allowMultipleProducers">True for MPMC, false for SPSC</param>
		/// <param name="allowMultipleConsumers">True for MPMC, false for SPSC</param>
		public LockFreeQueue( int capacity = 128, bool allowMultipleProducers = true, bool allowMultipleConsumers = true ) {
			var dummy = new Node();
			_head = dummy;
			_tail = dummy;
			_allowMultipleProducers = allowMultipleProducers;
			_allowMultipleConsumers = allowMultipleConsumers;
			_count = 0;
		}

		/*
		===============
		TryAdd
		===============
		*/
		/// <summary>
		/// Enqueue an item (thread-safe)
		/// </summary>
		public bool TryAdd( T item ) {
			var node = new Node( item );

			if ( _allowMultipleProducers ) {
				// Multi-producer: use interlocked operations
				Node oldTail, oldNext;

				while ( true ) {
					oldTail = _tail;
					oldNext = oldTail.Next;

					// Check if tail is still consistent
					if ( oldTail == _tail ) {
						if ( oldNext == null ) {
							// Try to link new node at end
							if ( Interlocked.CompareExchange( ref oldTail.Next, node, null ) == null ) {
								// Try to move tail forward
								Interlocked.CompareExchange( ref _tail, node, oldTail );
								IncrementCount();
								return true;
							}
						} else {
							// Help other thread by moving tail forward
							Interlocked.CompareExchange( ref _tail, oldNext, oldTail );
						}
					}
				}
			} else {
				// Single-producer: simpler algorithm
				var tail = _tail;
				tail.Next = node;
				_tail = node;
				IncrementCount();
				return true;
			}
		}

		/*
		===============
		TryTake
		===============
		*/
		/// <summary>
		/// Dequeue an item (thread-safe)
		/// </summary>
		public bool TryTake( out T item ) {
			if ( _allowMultipleConsumers ) {
				// Multi-consumer: use interlocked operations
				while ( true ) {
					var head = _head;
					var tail = _tail;
					var next = head.Next;

					if ( head == _head ) { // Check consistency
						if ( head == tail ) {
							// Queue might be empty or tail needs updating
							if ( next == null ) {
								item = default;
								return false; // Queue is empty
							}

							// Help by moving tail forward
							Interlocked.CompareExchange( ref _tail, next, tail );
						} else {
							// Read value before CAS (important!)
							item = next.Value;

							// Try to move head forward
							if ( Interlocked.CompareExchange( ref _head, next, head ) == head ) {
								DecrementCount();
								// Optional: set old head's value to default for GC
								head.Value = default;
								return true;
							}
						}
					}
				}
			} else {
				// Single-consumer: simpler algorithm
				var head = _head;
				var next = head.Next;

				if ( next == null ) {
					item = default;
					return false;
				}

				item = next.Value;
				_head = next;
				DecrementCount();
				head.Value = default; // Help GC
				return true;
			}
		}

		/*
		===============
		TryPeek
		===============
		*/
		/// <summary>
		/// Try to peek at the front item without removing it
		/// </summary>
		public bool TryPeek( out T item ) {
			var next = _head.Next;
			if ( next == null ) {
				item = default;
				return false;
			}

			item = next.Value;
			return true;
		}

		/*
		===============
		IncrementCount
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		private void IncrementCount() {
			if ( _allowMultipleProducers || _allowMultipleConsumers ) {
				Interlocked.Increment( ref _count );
			} else {
				// Single-threaded: simple increment
				_count++;
			}
		}

		/*
		===============
		DecrementCount
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		private void DecrementCount() {
			if ( _allowMultipleProducers || _allowMultipleConsumers ) {
				Interlocked.Decrement( ref _count );
			} else {
				_count--;
			}
		}
	};
}