using System;
using System.IO;
using System.Text;
using Nomad.FileSystem.Private.FileStreams;
using NUnit.Framework;

namespace Nomad.FileSystem.Tests
{
	[TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Streams.File")]
    [Category("Unit")]
	public sealed class FileReaderTests
	{
		private sealed class LargeLengthStream : Stream
		{
			public override bool CanRead => true;
			public override bool CanSeek => true;
			public override bool CanWrite => false;

			public override long Length => (long)int.MaxValue + 1L;

			public override long Position { get; set; }

			public override void Flush()
			{
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return 0;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				Position = origin switch
				{
					SeekOrigin.Begin => offset,
					SeekOrigin.Current => Position + offset,
					SeekOrigin.End => Length + offset,
					_ => throw new ArgumentOutOfRangeException(nameof(origin))
				};

				return Position;
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class ShortReadStream : Stream
		{
			private readonly byte[] _data;
			private readonly long _reportedLength;

			public ShortReadStream(byte[] data, long reportedLength)
			{
				_data = data;
				_reportedLength = reportedLength;
			}

			public override bool CanRead => true;
			public override bool CanSeek => true;
			public override bool CanWrite => false;
			public override long Length => _reportedLength;
			public override long Position { get; set; }

			public override void Flush()
			{
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (Position >= _data.Length)
				{
					return 0;
				}

				int remainingActualBytes = _data.Length - (int)Position;
				int bytesToCopy = Math.Min(count, remainingActualBytes);

				Array.Copy(_data, (int)Position, buffer, offset, bytesToCopy);
				Position += bytesToCopy;

				return bytesToCopy;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				Position = origin switch
				{
					SeekOrigin.Begin => offset,
					SeekOrigin.Current => Position + offset,
					SeekOrigin.End => Length + offset,
					_ => throw new ArgumentOutOfRangeException(nameof(origin))
				};

				return Position;
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
		}

		private static FileReader CreateTextFileReader(string text, out MemoryStream stream, out StreamReader streamReader)
		{
			stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

			streamReader = new StreamReader(
				stream,
				Encoding.UTF8,
				detectEncodingFromByteOrderMarks: false,
				bufferSize: 1024,
				leaveOpen: true
			);

			return new FileReader(streamReader);
		}

		private static FileReader CreateBinaryFileReader(
			Action<BinaryWriter> write,
			out MemoryStream stream,
			out BinaryReader binaryReader)
		{
			stream = new MemoryStream();

			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
			{
				write(writer);
			}

			stream.Position = 0;

			binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

			return new FileReader(binaryReader);
		}

		private static byte[] GetCurrentTextReadToEndBytes(string value)
		{
			char[] chars = value.ToCharArray();
			byte[] buffer = new byte[value.Length];

			Buffer.BlockCopy(chars, 0, buffer, 0, buffer.Length);

			return buffer;
		}

		[Test]
		public void Constructor_WithStreamReader_CreatesTextReader()
		{
			FileReader reader = CreateTextFileReader(
				"hello",
				out _,
				out StreamReader streamReader
			);

			Assert.Multiple(() =>
			{
				Assert.That(reader.IsText, Is.True);
				Assert.That(reader.GetStream(), Is.SameAs(streamReader));
			});
		}

		[Test]
		public void Constructor_WithBinaryReader_CreatesBinaryReader()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(123),
				out _,
				out BinaryReader binaryReader
			);

			Assert.Multiple(() =>
			{
				Assert.That(reader.IsText, Is.False);
				Assert.That(reader.GetStream(), Is.SameAs(binaryReader));
			});
		}

		[Test]
		public void Position_GetAndSet_UsesTextReaderBaseStream()
		{
			FileReader reader = CreateTextFileReader(
				"abcdef",
				out MemoryStream stream,
				out _
			);

			reader.Position = 3;

			Assert.Multiple(() =>
			{
				Assert.That(stream.Position, Is.EqualTo(3));
				Assert.That(reader.Position, Is.EqualTo(3));
			});
		}

		[Test]
		public void Position_GetAndSet_UsesBinaryReaderBaseStream()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer =>
				{
					writer.Write((byte)1);
					writer.Write((byte)2);
					writer.Write((byte)3);
					writer.Write((byte)4);
				},
				out MemoryStream stream,
				out _
			);

			reader.Position = 2;

			Assert.Multiple(() =>
			{
				Assert.That(stream.Position, Is.EqualTo(2));
				Assert.That(reader.Position, Is.EqualTo(2));
			});
		}

		[Test]
		public void ReadByte_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write((byte)0xAB),
				out _,
				out _
			);

			Assert.That(reader.ReadByte(), Is.EqualTo(0xAB));
		}

		[Test]
		public void ReadSByte_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write((sbyte)-12),
				out _,
				out _
			);

			Assert.That(reader.ReadSByte(), Is.EqualTo(-12));
		}

		[Test]
		public void ReadChar_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write('N'),
				out _,
				out _
			);

			Assert.That(reader.ReadChar(), Is.EqualTo('N'));
		}

		[Test]
		public void ReadShort_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write((short)-12345),
				out _,
				out _
			);

			Assert.That(reader.ReadShort(), Is.EqualTo(-12345));
		}

		[Test]
		public void ReadUShort_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write((ushort)54321),
				out _,
				out _
			);

			Assert.That(reader.ReadUShort(), Is.EqualTo(54321));
		}

		[Test]
		public void ReadInt_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(-123456789),
				out _,
				out _
			);

			Assert.That(reader.ReadInt(), Is.EqualTo(-123456789));
		}

		[Test]
		public void ReadUInt_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(3456789012U),
				out _,
				out _
			);

			Assert.That(reader.ReadUInt(), Is.EqualTo(3456789012U));
		}

		[Test]
		public void ReadLong_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(-987654321012345678L),
				out _,
				out _
			);

			Assert.That(reader.ReadLong(), Is.EqualTo(-987654321012345678L));
		}

		[Test]
		public void ReadULong_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(9876543210123456789UL),
				out _,
				out _
			);

			Assert.That(reader.ReadULong(), Is.EqualTo(9876543210123456789UL));
		}

		[Test]
		public void ReadFloat_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(123.5f),
				out _,
				out _
			);

			Assert.That(reader.ReadFloat(), Is.EqualTo(123.5f));
		}

		[Test]
		public void ReadDouble_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(-9876.125d),
				out _,
				out _
			);

			Assert.That(reader.ReadDouble(), Is.EqualTo(-9876.125d));
		}

		[Test]
		public void ReadDecimal_BinaryMode_ReturnsExpectedValue()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(123456.789m),
				out _,
				out _
			);

			Assert.That(reader.ReadDecimal(), Is.EqualTo(123456.789m));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void ReadBoolean_BinaryMode_ReturnsExpectedValue(bool expected)
		{
			FileReader reader = CreateBinaryFileReader(
				writer => writer.Write(expected),
				out _,
				out _
			);

			Assert.That(reader.ReadBoolean(), Is.EqualTo(expected));
		}

		[Test]
		public void ReadString_BinaryMode_ReturnsBinaryReaderString()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write("Nomad Framework"),
				out _,
				out _
			);

			Assert.That(reader.ReadString(), Is.EqualTo("Nomad Framework"));
		}

		[Test]
		public void ReadString_TextMode_ReturnsNextLine()
		{
			FileReader reader = CreateTextFileReader(
				"first line\nsecond line",
				out _,
				out _
			);

			Assert.Multiple(() =>
			{
				Assert.That(reader.ReadString(), Is.EqualTo("first line"));
				Assert.That(reader.ReadString(), Is.EqualTo("second line"));
			});
		}

		[Test]
		public void ReadString_TextMode_AtEndOfStream_ReturnsNull()
		{
			FileReader reader = CreateTextFileReader(
				string.Empty,
				out _,
				out _
			);

			Assert.That(reader.ReadString(), Is.Null);
		}

		[Test]
		public void ReadToEnd_BinaryMode_ReturnsAllRemainingBytes()
		{
			byte[] expected =
			{
				1, 2, 3, 4, 5, 6
			};

			FileReader reader = CreateBinaryFileReader(
				writer => writer.Write(expected),
				out _,
				out _
			);

			byte[] actual = reader.ReadToEnd();

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void ReadToEnd_BinaryMode_ReturnsBytesFromCurrentPositionOnly()
		{
			byte[] input =
			{
				10, 20, 30, 40, 50
			};

			FileReader reader = CreateBinaryFileReader(
				writer => writer.Write(input),
				out _,
				out _
			);

			reader.Position = 2;

			byte[] actual = reader.ReadToEnd();

			Assert.That(actual, Is.EqualTo(new byte[] { 30, 40, 50 }));
		}

		[Test]
		public void ReadToEnd_BinaryMode_WhenNoBytesRemain_ReturnsEmptyArray()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(new byte[] { 1, 2, 3 }),
				out MemoryStream stream,
				out _
			);

			reader.Position = stream.Length;

			byte[] actual = reader.ReadToEnd();

			Assert.That(actual, Is.Empty);
		}

		[Test]
		public void ReadToEnd_BinaryMode_WhenRemainingBytesExceedIntMaxValue_ThrowsInvalidOperationException()
		{
			using var stream = new LargeLengthStream();
			using var binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

			var reader = new FileReader(binaryReader);

			var ex = Assert.Throws<InvalidOperationException>(() => reader.ReadToEnd());

			Assert.That(ex!.Message, Is.EqualTo("File is too large to read into a single array."));
		}

		[Test]
		public void ReadToEnd_BinaryMode_WhenReadReturnsFewerBytesThanRemaining_ThrowsIOException()
		{
			byte[] actualData =
			{
				1, 2, 3
			};

			using var stream = new ShortReadStream(actualData, reportedLength: 5);
			using var binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

			var reader = new FileReader(binaryReader);

			var ex = Assert.Throws<IOException>(() => reader.ReadToEnd());

			Assert.That(
				ex!.Message,
				Is.EqualTo("FileStream failed to read exactly 5 bytes!")
			);
		}

		[Test]
		public void ReadToEnd_TextMode_ReturnsCurrentRawCharByteCopyBehavior()
		{
			const string text = "ABCD";

			FileReader reader = CreateTextFileReader(
				text,
				out _,
				out _
			);

			byte[] actual = reader.ReadToEnd();
			byte[] expected = GetCurrentTextReadToEndBytes(text);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void ReadToEnd_TextMode_WhenNoTextRemains_ReturnsEmptyArray()
		{
			FileReader reader = CreateTextFileReader(
				string.Empty,
				out _,
				out _
			);

			byte[] actual = reader.ReadToEnd();

			Assert.That(actual, Is.Empty);
		}

		[Test]
		public void ReadToEnd_TextMode_ReturnsRemainingTextOnly()
		{
			const string firstLine = "first";
			const string remaining = "second";

			FileReader reader = CreateTextFileReader(
				firstLine + "\n" + remaining,
				out _,
				out _
			);

			Assert.That(reader.ReadString(), Is.EqualTo(firstLine));

			byte[] actual = reader.ReadToEnd();
			byte[] expected = GetCurrentTextReadToEndBytes(remaining);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void BinaryOnlyReadMethods_TextMode_ThrowNotSupportedException()
		{
			FileReader reader = CreateTextFileReader(
				"123",
				out _,
				out _
			);

			Assert.Multiple(() =>
			{
				Assert.Throws<NotSupportedException>(() => reader.ReadByte());
				Assert.Throws<NotSupportedException>(() => reader.ReadSByte());
				Assert.Throws<NotSupportedException>(() => reader.ReadChar());
				Assert.Throws<NotSupportedException>(() => reader.ReadShort());
				Assert.Throws<NotSupportedException>(() => reader.ReadUShort());
				Assert.Throws<NotSupportedException>(() => reader.ReadInt());
				Assert.Throws<NotSupportedException>(() => reader.ReadUInt());
				Assert.Throws<NotSupportedException>(() => reader.ReadLong());
				Assert.Throws<NotSupportedException>(() => reader.ReadULong());
				Assert.Throws<NotSupportedException>(() => reader.ReadFloat());
				Assert.Throws<NotSupportedException>(() => reader.ReadDouble());
				Assert.Throws<NotSupportedException>(() => reader.ReadDecimal());
				Assert.Throws<NotSupportedException>(() => reader.ReadBoolean());
			});
		}

		[Test]
		public void GetStream_TextMode_ReturnsDisposableStreamReader()
		{
			FileReader reader = CreateTextFileReader(
				"text",
				out _,
				out StreamReader streamReader
			);

			IDisposable stream = reader.GetStream();

			Assert.Multiple(() =>
			{
				Assert.That(stream, Is.Not.Null);
				Assert.That(stream, Is.SameAs(streamReader));
				Assert.That(stream, Is.TypeOf<StreamReader>());
			});
		}

		[Test]
		public void GetStream_BinaryMode_ReturnsDisposableBinaryReader()
		{
			FileReader reader = CreateBinaryFileReader(
				static writer => writer.Write(1),
				out _,
				out BinaryReader binaryReader
			);

			IDisposable stream = reader.GetStream();

			Assert.Multiple(() =>
			{
				Assert.That(stream, Is.Not.Null);
				Assert.That(stream, Is.SameAs(binaryReader));
				Assert.That(stream, Is.TypeOf<BinaryReader>());
			});
		}
	}
}