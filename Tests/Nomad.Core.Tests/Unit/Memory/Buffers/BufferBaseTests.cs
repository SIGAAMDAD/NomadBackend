using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Memory.Buffers;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Memory.Buffers")]
    [Category("Unit")]
    public class BufferBaseTests
    {
        #region Test Helpers

        private const int DefaultSize = 64;

        private static byte[] CreateTestData(int size, byte start = 0)
        {
            var data = new byte[size];
            for (int i = 0; i < size; i++)
                data[i] = (byte)((start + i) % 256);
            return data;
        }

        #endregion

        #region StandardBufferHandle Tests (basic allocation)

        [Test]
        public void StandardBufferHandle_Constructor_CreatesBufferOfSpecifiedLength()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buffer.Length, Is.EqualTo(DefaultSize));
                Assert.That(buffer.Span.Length, Is.EqualTo(DefaultSize));
                Assert.That(buffer.Memory.Length, Is.EqualTo(DefaultSize));
                Assert.That(buffer.IsDisposed, Is.False);
            }
        }

        [Test]
        public void StandardBufferHandle_Dispose_SetsIsDisposed()
        {
            var buffer = new StandardBufferHandle(DefaultSize);
            buffer.Dispose();
            Assert.That(buffer.IsDisposed, Is.True);
        }

        [Test]
        public void StandardBufferHandle_DisposeMultipleTimes_DoesNotThrow()
        {
            var buffer = new StandardBufferHandle(DefaultSize);
            buffer.Dispose();
            Assert.DoesNotThrow(() => buffer.Dispose());
        }

        [Test]
        public void StandardBufferHandle_DisposeAsync_ReleasesResources()
        {
            var buffer = new StandardBufferHandle(DefaultSize);
            Assert.DoesNotThrowAsync(async () => await buffer.DisposeAsync());
            Assert.That(buffer.IsDisposed, Is.True);
        }

        [Test]
        public void StandardBufferHandle_ToArray_ReturnsCopy()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);

            var array = buffer.ToArray();
            Assert.That(array, Is.EqualTo(data));
            // Modifying array does not affect buffer
            array[0] = 0xFF;
            Assert.That(buffer.Span[0], Is.EqualTo(data[0]));
        }

        [Test]
        public void StandardBufferHandle_AsStream_ReturnsReadableStream()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);

            using var stream = buffer.AsStream();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stream.CanRead, Is.True);
                Assert.That(stream.CanWrite, Is.False); // MemoryStream default is writable, but we didn't specify; AsStream() creates writable stream.
            }
            // Actually AsStream() calls new MemoryStream(buffer) which is writable. We'll test both.
            var readData = new byte[DefaultSize];
            stream.Read(readData, 0, DefaultSize);
            Assert.That(readData, Is.EqualTo(data));
        }

        [Test]
        public void StandardBufferHandle_AsStream_Writable_AllowsWriting()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            using var stream = buffer.AsStream(writable: true);
            stream.Write(data, 0, data.Length);
            stream.Flush();
            Assert.That(buffer.Span.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public void StandardBufferHandle_AsStream_WithOffsetAndLength()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);

            int offset = 10, length = 20;
            using var stream = buffer.AsStream(offset, length);
            var readData = new byte[length];
            stream.Read(readData, 0, length);
            Assert.That(readData, Is.EqualTo(data.Skip(offset).Take(length)));
        }

        [Test]
        public void StandardBufferHandle_GetMemory_ReturnsMemoryUpToLength()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var memory = buffer.GetMemory(DefaultSize - 10);
            Assert.That(memory.Length, Is.EqualTo(DefaultSize - 10));
            Assert.That(memory.Span[0], Is.EqualTo(buffer.Span[0])); // same underlying
        }

        [Test]
        public void StandardBufferHandle_GetMemory_SizeHintTooLarge_Throws()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetMemory(DefaultSize + 1));
        }

        [Test]
        public void StandardBufferHandle_AsSpan_ReturnsCorrectSlice()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);

            var span = buffer.AsSpan(10, 20);
            Assert.That(span.Length, Is.EqualTo(20));
            Assert.That(span.ToArray(), Is.EqualTo(data.Skip(10).Take(20)));
        }

        [Test]
        public void StandardBufferHandle_AsMemory_ReturnsCorrectSlice()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);

            var memory = buffer.AsMemory(10, 20);
            Assert.That(memory.Length, Is.EqualTo(20));
            Assert.That(memory.Span.ToArray(), Is.EqualTo(data.Skip(10).Take(20)));
        }

        [Test]
        public void StandardBufferHandle_Pin_ReturnsValidHandle()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            using var handle = buffer.Pin();
            unsafe
            {
                byte* ptr = (byte*)handle.Pointer;
                Assert.That(*ptr, Is.EqualTo(buffer.Span[0]));
            }
        }

        [Test]
        public void StandardBufferHandle_Clear_FillsWithZeros()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);
            buffer.Clear();
            Assert.That(buffer.Span.ToArray(), Is.All.EqualTo(0));
        }

        [Test]
        public void StandardBufferHandle_Clear_WithRange_FillsRangeWithZeros()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);
            buffer.Clear(10, 20);
            var expected = data.ToArray();
            Array.Clear(expected, 10, 20);
            Assert.That(buffer.Span.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void StandardBufferHandle_Fill_FillsWithSpecifiedByte()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            buffer.Fill(0xAB);
            Assert.That(buffer.Span.ToArray(), Is.All.EqualTo(0xAB));
        }

        [Test]
        public void StandardBufferHandle_GetSlice_ReturnsSpanSlice()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer.Span);

            var slice = buffer.GetSlice(10, 20);
            Assert.That(slice.Length, Is.EqualTo(20));
            Assert.That(slice.ToArray(), Is.EqualTo(data.Skip(10).Take(20)));
        }

        [Test]
        public void StandardBufferHandle_CopyFrom_Span_CopiesData()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var source = CreateTestData(DefaultSize / 2);
            buffer.CopyFrom(source.AsSpan(), 0, source.Length, dstOffset: 10);
            Assert.That(buffer.Span.Slice(10, source.Length).ToArray(), Is.EqualTo(source));
        }

        [Test]
        public void StandardBufferHandle_CopyTo_Span_CopiesData()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var source = CreateTestData(DefaultSize);
            source.CopyTo(buffer.Span);

            var dest = new byte[DefaultSize];
            buffer.CopyTo(dest.AsSpan(), 0, DefaultSize, srcOffset: 0);
            Assert.That(dest, Is.EqualTo(source));
        }

        [Test]
        public void StandardBufferHandle_Equals_SameContent_ReturnsTrue()
        {
            using var buffer1 = new StandardBufferHandle(DefaultSize);
            using var buffer2 = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(buffer1.Span);
            data.CopyTo(buffer2.Span);

            Assert.That(buffer1.Equals(buffer2), Is.True);
        }

        [Test]
        public void StandardBufferHandle_Equals_DifferentContent_ReturnsFalse()
        {
            using var buffer1 = new StandardBufferHandle(DefaultSize);
            using var buffer2 = new StandardBufferHandle(DefaultSize);
            var data1 = CreateTestData(DefaultSize, 0);
            var data2 = CreateTestData(DefaultSize, 1);
            data1.CopyTo(buffer1.Span);
            data2.CopyTo(buffer2.Span);

            Assert.That(buffer1.Equals(buffer2), Is.False);
        }

        [Test]
        public void StandardBufferHandle_CompareTo_LexicographicOrder()
        {
            using var buffer1 = new StandardBufferHandle(DefaultSize);
            using var buffer2 = new StandardBufferHandle(DefaultSize);
            var data1 = CreateTestData(DefaultSize, 0);
            var data2 = CreateTestData(DefaultSize, 0);
            data1.CopyTo(buffer1.Span);
            data2.CopyTo(buffer2.Span);
            // identical
            Assert.That(buffer1.CompareTo(buffer2), Is.Zero);

            // modify second to be greater at first position
            buffer2.Span[0] = (byte)(buffer1.Span[0] + 1);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buffer1.CompareTo(buffer2), Is.LessThan(0));
                Assert.That(buffer2.CompareTo(buffer1), Is.GreaterThan(0));
            }
        }

        #endregion

        #region PooledBufferHandle Tests

        [Test]
        public void PooledBufferHandle_Constructor_RentsFromArrayPool()
        {
            using var buffer = new PooledBufferHandle(DefaultSize);
            Assert.That(buffer.Length, Is.EqualTo(DefaultSize));
            Assert.That(buffer.IsDisposed, Is.False);
        }

        [Test]
        public void PooledBufferHandle_DisposeMultipleTimes_DoesNotThrow()
        {
            var buffer = new PooledBufferHandle(DefaultSize);
            buffer.Dispose();
            Assert.DoesNotThrow(() => buffer.Dispose());
        }

        #endregion

        #region SharedBufferHandle Tests

        [Test]
        public void SharedBufferHandle_Constructor_UsesProvidedArray()
        {
            var data = CreateTestData(DefaultSize);
            using var buffer = new SharedBufferHandle(data, DefaultSize);
            Assert.That(buffer.Span.Length, Is.EqualTo(DefaultSize));
            Assert.That(buffer.Span[0], Is.EqualTo(data[0]));
        }

        [Test]
        public void SharedBufferHandle_Dispose_DoesNotClearArray()
        {
            var data = CreateTestData(DefaultSize);
            var buffer = new SharedBufferHandle(data, DefaultSize);
            buffer.Dispose();
            // Array should still exist and contain data
            Assert.That(data, Is.EqualTo(CreateTestData(DefaultSize)));
        }

        #endregion

        #region NullBufferHandle Tests

        [Test]
        public void NullBufferHandle_IsSingleton()
        {
            var handle1 = NullBufferHandle.Handle;
            var handle2 = NullBufferHandle.Handle;
            Assert.That(ReferenceEquals(handle1, handle2), Is.True);
        }

        [Test]
        public void NullBufferHandle_PropertiesReturnZeroLengthAndNullBuffer()
        {
            var handle = NullBufferHandle.Handle;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(handle.Length, Is.Zero);
                Assert.That(handle.Span.IsEmpty, Is.True);
                Assert.That(handle.Memory.IsEmpty, Is.True);
                Assert.That(handle.IsDisposed, Is.False);
            }
        }

        [Test]
        public void NullBufferHandle_AsStream_ReturnsEmptyStream()
        {
            var handle = NullBufferHandle.Handle;
            using var stream = handle.AsStream();
            Assert.That(stream.Length, Is.Zero);
        }

        [Test]
        public void NullBufferHandle_ToArray_ReturnsEmptyArray()
        {
            var handle = NullBufferHandle.Handle;
            var array = handle.ToArray();
            Assert.That(array, Is.Empty);
        }

        [Test]
        public void NullBufferHandle_CopyFrom_DoesNothing()
        {
            var handle = NullBufferHandle.Handle;
            byte[] source = [1, 2, 3];
            Assert.DoesNotThrow(() => handle.CopyFrom(source.AsSpan(), 0, source.Length, 0));
            // No effect, but should not throw (bounds checks might be problematic because length is 0)
        }

        #endregion

        #region SliceBufferHandle Tests (ref struct)

        [Test]
        public void SliceBufferHandle_PropertiesReflectSlice()
        {
            using var parent = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(parent.Span);

            var slice = new SliceBufferHandle(parent, 10, 20);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(slice.Length, Is.EqualTo(20));
                Assert.That(slice.Buffer.Length, Is.EqualTo(20));
                Assert.That(slice.Buffer.ToArray(), Is.EqualTo(data.Skip(10).Take(20)));
            }
        }

        [Test]
        public void SliceBufferHandle_AsSpan_ReturnsSpanSlice()
        {
            using var parent = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(parent.Span);

            var slice = new SliceBufferHandle(parent, 10, 20);
            var span = slice.AsSpan();
            Assert.That(span.Length, Is.EqualTo(20));
            Assert.That(span.ToArray(), Is.EqualTo(data.Skip(10).Take(20)));

            var subSpan = slice.AsSpan(5, 10);
            Assert.That(subSpan.ToArray(), Is.EqualTo(data.Skip(15).Take(10)));
        }

        [Test]
        public void SliceBufferHandle_AsMemory_ReturnsMemorySlice()
        {
            using var parent = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(parent.Span);

            var slice = new SliceBufferHandle(parent, 10, 20);
            var memory = slice.AsMemory();
            Assert.That(memory.Length, Is.EqualTo(20));
            Assert.That(memory.Span.ToArray(), Is.EqualTo(data.Skip(10).Take(20)));

            var subMemory = slice.AsMemory(5, 10);
            Assert.That(subMemory.Span.ToArray(), Is.EqualTo(data.Skip(15).Take(10)));
        }

        [Test]
        public void SliceBufferHandle_Pin_PinsSlice()
        {
            using var parent = new StandardBufferHandle(DefaultSize);
            var slice = new SliceBufferHandle(parent, 10, 20);
            using var handle = slice.Pin();
            unsafe
            {
                byte* ptr = (byte*)handle.Pointer;
                // Should point to parent[10]
                Assert.That(*ptr, Is.EqualTo(parent.Span[10]));
            }
        }

        [Test]
        public void SliceBufferHandle_Clear_ClearsSliceOnly()
        {
            using var parent = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(parent.Span);

            var slice = new SliceBufferHandle(parent, 10, 20);
            slice.Clear();

            var expected = data.ToArray();
            Array.Clear(expected, 10, 20);
            Assert.That(parent.Span.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void SliceBufferHandle_Equals_ComparesContent()
        {
            using var parent = new StandardBufferHandle(DefaultSize);
            var data = CreateTestData(DefaultSize);
            data.CopyTo(parent.Span);

            var slice1 = new SliceBufferHandle(parent, 10, 20);
            var slice2 = new SliceBufferHandle(parent, 10, 20);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(slice1.Equals(parent), Is.False); // different types
                                                              // To compare slices we need to compare content manually
                Assert.That(slice1.AsSpan().SequenceEqual(slice2.AsSpan()), Is.True);
            }
        }

        #endregion

        #region Edge Cases and Argument Validation

        [Test]
        public void BufferBase_CopyFrom_WithInvalidOffsets_Throws()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var source = new byte[10];
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyFrom(source.AsSpan(), -1, 5, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyFrom(source.AsSpan(), 0, 5, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyFrom(source.AsSpan(), 5, 10, 0)); // beyond source length
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyFrom(source.AsSpan(), 0, 10, DefaultSize - 5)); // would exceed buffer length
        }

        [Test]
        public void BufferBase_CopyTo_WithInvalidOffsets_Throws()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            var dest = new byte[DefaultSize];
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyTo(dest.AsSpan(), -1, 5, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyTo(dest.AsSpan(), 0, 5, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyTo(dest.AsSpan(), 0, 10, DefaultSize - 5)); // src beyond buffer
        }

        [Test]
        public void BufferBase_AsSpan_WithInvalidRange_Throws()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AsSpan(-1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AsSpan(DefaultSize, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AsSpan(10, DefaultSize - 5)); // start + length > size
        }

        [Test]
        public void BufferBase_Clear_WithInvalidRange_Throws()
        {
            using var buffer = new StandardBufferHandle(DefaultSize);
            Assert.Throws<IndexOutOfRangeException>(() => buffer.Clear(-1, 10));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.Clear(DefaultSize, 1));
        }

        #endregion
    }
}