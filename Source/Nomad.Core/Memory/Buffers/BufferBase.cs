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
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;

namespace Nomad.Core.Memory.Buffers
{
    /// <summary>
    /// The base buffer owner class.
    /// </summary>
    public abstract class BufferBase : IBufferHandle
    {
        /// <summary>
        /// The size of the owned buffer. This might not be the actual allocated size, but it is the maximum bytes we're allowed to work with.
        /// </summary>
        public int Length => _length;
        private readonly int _length;

        /// <summary>
        /// The owned chunk of memory represented as <see cref="Span{byte}"/>.
        /// </summary>
        public Span<byte> Span => buffer.AsSpan(0, _length);

        /// <summary>
        /// The owned chunk of memory represented as <see cref="Memory{byte}"/>.
        /// </summary>
        public Memory<byte> Memory => buffer.AsMemory(0, _length);

        protected readonly byte[] buffer;

        /// <summary>
        /// Whether the buffer owner has been killed.
        /// </summary>
        public bool IsDisposed => isDisposed;
        protected bool isDisposed = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        protected BufferBase(byte[] buffer, int length)
        {
            this.buffer = buffer;
            _length = length;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
            isDisposed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual async ValueTask DisposeAsyncCore()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool Equals(IBufferHandle? other)
        {
            ArgumentGuard.ThrowIfNull(other);
            return _length == other.Length && Span.SequenceEqual(other.Span);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int CompareTo(IBufferHandle? other)
        {
            ArgumentGuard.ThrowIfNull(other);
            return Span.SequenceCompareTo(other.Span);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] ToArray()
        {
            byte[] copy = new byte[_length];
            MemCpy(buffer, 0, copy, 0, _length);
            return copy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Stream AsStream()
            => new MemoryStream(buffer, 0, _length, writable: false, publiclyVisible: false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writable"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Stream AsStream(bool writable)
            => new MemoryStream(buffer, 0, _length, writable, publiclyVisible: false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Stream AsStream(int offset, int length)
        {
            ValidateRange(offset, length);
            return new MemoryStream(buffer, offset, length, writable: false, publiclyVisible: false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sizeHint"></param>
        /// <returns></returns>
        public virtual Memory<byte> GetMemory(int sizeHint = 0)
        {
            RangeGuard.ThrowIfGreaterThan(sizeHint, _length, nameof(sizeHint));
            return AsMemory(0, sizeHint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Span<byte> AsSpan(int start)
        {
            ValidateRange(start, _length - start);
            return buffer.AsSpan(start, _length - start);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Span<byte> AsSpan(int start, int length)
        {
            ValidateRange(start, length);
            return buffer.AsSpan(start, length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Memory<byte> AsMemory(int start)
        {
            ValidateRange(start, _length - start);
            return buffer.AsMemory(start, _length - start);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Memory<byte> AsMemory(int start, int length)
        {
            ValidateRange(start, length);
            return buffer.AsMemory(start, length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MemoryHandle Pin()
            => Memory.Pin();

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Clear()
            => Array.Clear(buffer, 0, _length);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Clear(int start, int length)
        {
            ValidateRange(start, length);
            Array.Clear(buffer, start, length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Fill(byte f)
            => Array.Fill(buffer, f, 0, _length);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Span<byte> GetSlice(int start, int length)
        {
            ValidateRange(start, length);
            return buffer.AsSpan(start, length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateRange(int start, int length)
        {
            RangeGuard.ThrowIfLessThan(start, 0, nameof(start));
            RangeGuard.ThrowIfLessThan(length, 0, nameof(length));
            RangeGuard.ThrowIfGreaterThan(start + length, _length, nameof(length));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dstOffset"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CopyFrom(ReadOnlySpan<byte> source, int offset, int length, int dstOffset = 0)
            => MemCpy(source, offset, Span, dstOffset, length);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="srcOffset"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CopyTo(Span<byte> dest, int offset, int length, int srcOffset = 0)
            => MemCpy(Span, srcOffset, dest, offset, length);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="srcOffset"></param>
        /// <param name="dst"></param>
        /// <param name="dstOffset"></param>
        /// <param name="length"></param>
        private unsafe void MemCpy(ReadOnlySpan<byte> src, int srcOffset, Span<byte> dst, int dstOffset, int length)
        {
            if (src.Length == 0 || dst.Length == 0)
            {
                return;
            }

            // Validate all indices and lengths
            RangeGuard.ThrowIfLessThan(srcOffset, 0, nameof(srcOffset));
            RangeGuard.ThrowIfLessThan(dstOffset, 0, nameof(dstOffset));
            RangeGuard.ThrowIfLessThan(length, 0, nameof(length));

            // Ensure the source region is entirely within the source span
            RangeGuard.ThrowIfGreaterThan(srcOffset + length, src.Length, nameof(srcOffset));

            // Ensure the destination region is entirely within the destination span
            RangeGuard.ThrowIfGreaterThan(dstOffset + length, dst.Length, nameof(dstOffset));

            // Compute the remaining space in destination (optional, but used in MemoryCopy call)
            long destSize = dst.Length - dstOffset;

            fixed (byte* srcPtr = &src[srcOffset])
            fixed (byte* dstPtr = &dst[dstOffset])
            {
                System.Buffer.MemoryCopy(srcPtr, dstPtr, destSize, length);
            }
        }
    }
}
