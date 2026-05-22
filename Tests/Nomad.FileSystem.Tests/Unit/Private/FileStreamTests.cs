using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nomad.FileSystem.Private.FileStreams;
using NUnit.Framework;

namespace Nomad.FileSystem.Tests
{
	[TestFixture]
    [Category("Nomad.FileSystem")]
    [Category("Streams.File")]
    [Category("Unit")]
	public sealed class FileStreamBaseTests
	{
		private sealed class TestFileStreamBase : FileStreamBase
		{
			public TestFileStreamBase(string filepath, FileMode fileMode, FileAccess fileAccess)
				: base(filepath, fileMode, fileAccess)
			{
			}

			public override bool CanRead =>
				!isDisposed
					? fileStream.CanRead
					: throw new ObjectDisposedException(nameof(FileStreamBase));

			public override bool CanWrite =>
				!isDisposed
					? fileStream.CanWrite
					: throw new ObjectDisposedException(nameof(FileStreamBase));

			public void WriteByteToUnderlyingStream(byte value)
			{
				fileStream.WriteByte(value);
			}

			public void InvokeDisposeFalseForCoverage()
			{
				Dispose(false);
			}

			public async ValueTask InvokeDisposeAsyncCoreForCoverage()
			{
				await DisposeAsyncCore();
			}
		}

		private sealed class TempFile : IDisposable
		{
			public string FilePath { get; }

			public TempFile(byte[]? initialBytes = null)
			{
				FilePath = Path.Combine(
					TestContext.CurrentContext.WorkDirectory,
					$"{Guid.NewGuid():N}.tmp"
				);

				File.WriteAllBytes(FilePath, initialBytes ?? Array.Empty<byte>());
			}

			public void Dispose()
			{
				try
				{
					if (File.Exists(FilePath))
					{
						File.Delete(FilePath);
					}
				}
				catch
				{
					// Test cleanup should not hide the original test failure.
				}
			}
		}

		[Test]
		public void Constructor_WithValidPath_OpensFileStream()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.Multiple(() =>
			{
				Assert.That(stream.IsOpen, Is.True);
				Assert.That(stream.FilePath, Is.EqualTo(tempFile.FilePath));
				Assert.That(stream.CanSeek, Is.True);
				Assert.That(stream.CanRead, Is.True);
				Assert.That(stream.CanWrite, Is.True);
			});
		}

		[Test]
		public void Constructor_UsesFileShareReadWrite()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.DoesNotThrow(() =>
			{
				using var secondHandle = new FileStream(
					tempFile.FilePath,
					FileMode.Open,
					FileAccess.ReadWrite,
					FileShare.ReadWrite
				);

				Assert.That(secondHandle.CanRead, Is.True);
				Assert.That(secondHandle.CanWrite, Is.True);
			});
		}

		[Test]
		public void Constructor_WithNullPath_ThrowsArgumentException()
		{
			Assert.Catch<ArgumentException>(() =>
			{
				using var _ = new TestFileStreamBase(
					null!,
					FileMode.OpenOrCreate,
					FileAccess.ReadWrite
				);
			});
		}

		[Test]
		public void Constructor_WithEmptyPath_ThrowsArgumentException()
		{
			Assert.Catch<ArgumentException>(() =>
			{
				using var _ = new TestFileStreamBase(
					string.Empty,
					FileMode.OpenOrCreate,
					FileAccess.ReadWrite
				);
			});
		}

		[Test]
		public void Constructor_WithWhitespacePath_ThrowsArgumentExceptionOrFileException()
		{
			Assert.Catch<Exception>(() =>
			{
				using var _ = new TestFileStreamBase(
					"   ",
					FileMode.OpenOrCreate,
					FileAccess.ReadWrite
				);
			});
		}

		[Test]
		public void CanRead_ReflectsFileAccessRead()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.Read
			);

			Assert.Multiple(() =>
			{
				Assert.That(stream.CanRead, Is.True);
				Assert.That(stream.CanWrite, Is.False);
			});
		}

		[Test]
		public void CanWrite_ReflectsFileAccessWrite()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.Write
			);

			Assert.Multiple(() =>
			{
				Assert.That(stream.CanRead, Is.False);
				Assert.That(stream.CanWrite, Is.True);
			});
		}

		[Test]
		public void CanSeek_WhenOpen_ReturnsUnderlyingFileStreamCanSeek()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.That(stream.CanSeek, Is.True);
		}

		[Test]
		public void Position_Get_WhenOpen_ReturnsUnderlyingFileStreamPosition()
		{
			using var tempFile = new TempFile(new byte[] { 10, 20, 30, 40 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Seek(2, SeekOrigin.Begin);

			Assert.That(stream.Position, Is.EqualTo(2));
		}

		[Test]
		public void Position_Set_WhenOpen_SeeksFromBeginning()
		{
			using var tempFile = new TempFile(new byte[] { 10, 20, 30, 40 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Position = 3;

			Assert.That(stream.Position, Is.EqualTo(3));
		}

		[Test]
		public void Position_Set_ToZero_SeeksToBeginning()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Position = 4;
			stream.Position = 0;

			Assert.That(stream.Position, Is.EqualTo(0));
		}

		[Test]
		public void Length_Get_WhenOpen_ReturnsUnderlyingFileLength()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4, 5 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.That(stream.Length, Is.EqualTo(5));
		}

		[Test]
		public void Length_Set_WhenOpen_ChangesUnderlyingFileLength()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4, 5 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Length = 2;

			Assert.Multiple(() =>
			{
				Assert.That(stream.Length, Is.EqualTo(2));
				Assert.That(new FileInfo(tempFile.FilePath).Length, Is.EqualTo(2));
			});
		}

		[Test]
		public void FilePath_WhenOpen_ReturnsUnderlyingFileStreamName()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.That(stream.FilePath, Is.EqualTo(tempFile.FilePath));
		}

		[Test]
		public void IsOpen_WhenConstructed_ReturnsTrue()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.That(stream.IsOpen, Is.True);
		}

		[Test]
		public void IsOpen_AfterDispose_ReturnsFalse()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.That(stream.IsOpen, Is.False);
		}

		[Test]
		public void Flush_WhenOpen_DoesNotThrow()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.WriteByteToUnderlyingStream(123);

			Assert.DoesNotThrow(() => stream.Flush());
		}

		[Test]
		public async Task FlushAsync_WhenOpen_DoesNotThrow()
		{
			using var tempFile = new TempFile();

			await using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.WriteByteToUnderlyingStream(123);

			Assert.DoesNotThrowAsync(async () =>
			{
				await stream.FlushAsync().AsTask();
			});
		}

		[Test]
		public void FlushAsync_WithCanceledToken_ThrowsOperationCanceledException()
		{
			using var tempFile = new TempFile();

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				await stream.FlushAsync(cts.Token).AsTask();
			});
		}

		[Test]
		public void Seek_FromBegin_ChangesPositionAndReturnsNewPosition()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4, 5 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			long result = stream.Seek(3, SeekOrigin.Begin);

			Assert.Multiple(() =>
			{
				Assert.That(result, Is.EqualTo(3));
				Assert.That(stream.Position, Is.EqualTo(3));
			});
		}

		[Test]
		public void Seek_FromCurrent_ChangesPositionAndReturnsNewPosition()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4, 5 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Position = 1;

			long result = stream.Seek(2, SeekOrigin.Current);

			Assert.Multiple(() =>
			{
				Assert.That(result, Is.EqualTo(3));
				Assert.That(stream.Position, Is.EqualTo(3));
			});
		}

		[Test]
		public void Seek_FromEnd_ChangesPositionAndReturnsNewPosition()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4, 5 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			long result = stream.Seek(-2, SeekOrigin.End);

			Assert.Multiple(() =>
			{
				Assert.That(result, Is.EqualTo(3));
				Assert.That(stream.Position, Is.EqualTo(3));
			});
		}

		[Test]
		public void SetLength_WhenOpen_ChangesLength()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3, 4, 5 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.SetLength(3);

			Assert.Multiple(() =>
			{
				Assert.That(stream.Length, Is.EqualTo(3));
				Assert.That(new FileInfo(tempFile.FilePath).Length, Is.EqualTo(3));
			});
		}

		[Test]
		public void SetLength_WhenIncreasingLength_ExtendsFile()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3 });

			using var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.SetLength(8);

			Assert.Multiple(() =>
			{
				Assert.That(stream.Length, Is.EqualTo(8));
				Assert.That(new FileInfo(tempFile.FilePath).Length, Is.EqualTo(8));
			});
		}

		[Test]
		public void Close_DisposesStream()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Close();

			Assert.Multiple(() =>
			{
				Assert.That(stream.IsOpen, Is.False);
				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.CanSeek;
				});
			});
		}

		[Test]
		public void Dispose_DisposesStreamAndSetsIsOpenFalse()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.Multiple(() =>
			{
				Assert.That(stream.IsOpen, Is.False);
				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.Position;
				});
			});
		}

		[Test]
		public void Dispose_CanBeCalledMultipleTimes()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.DoesNotThrow(() =>
			{
				stream.Dispose();
				stream.Dispose();
				stream.Dispose();
			});

			Assert.That(stream.IsOpen, Is.False);
		}

		[Test]
		public async Task DisposeAsync_DisposesStream()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			await stream.DisposeAsync();

			Assert.Multiple(() =>
			{
				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.Position;
				});

				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.Length;
				});

				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.CanSeek;
				});
			});
		}

		[Test]
		public async Task DisposeAsync_CanBeCalledMultipleTimes()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			Assert.DoesNotThrowAsync(async () =>
			{
				await stream.DisposeAsync();
				await stream.DisposeAsync();
				await stream.DisposeAsync();
			});
		}

		[Test]
		public void DisposeFalseForCoverage_MarksStreamDisposedButDoesNotNullUnderlyingFileStream()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.InvokeDisposeFalseForCoverage();

			Assert.Multiple(() =>
			{
				Assert.That(stream.IsOpen, Is.True);

				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.Position;
				});

				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.Length;
				});

				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.FilePath;
				});

				Assert.Throws<ObjectDisposedException>(() =>
				{
					_ = stream.CanSeek;
				});
			});

			stream.Dispose();
		}

		[Test]
		public void DisposeFalseForCoverage_WhenAlreadyDisposed_DoesNotThrow()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.DoesNotThrow(() => stream.InvokeDisposeFalseForCoverage());
		}

		[Test]
		public async Task DisposeAsyncCoreForCoverage_WhenAlreadyDisposed_DoesNotThrow()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.DoesNotThrowAsync(async () =>
			{
				await stream.InvokeDisposeAsyncCoreForCoverage();
			});
		}

		[Test]
		public void CanSeek_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			var ex = Assert.Throws<ObjectDisposedException>(() =>
			{
				_ = stream.CanSeek;
			});

			Assert.That(ex!.ObjectName, Is.EqualTo(nameof(FileStreamBase)));
		}

		[Test]
		public void Position_Get_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			var ex = Assert.Throws<ObjectDisposedException>(() =>
			{
				_ = stream.Position;
			});

			Assert.That(ex!.ObjectName, Is.EqualTo(nameof(FileStreamBase)));
		}

		[Test]
		public void Position_Set_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.Throws<ObjectDisposedException>(() =>
			{
				stream.Position = 0;
			});
		}

		[Test]
		public void Length_Get_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			var ex = Assert.Throws<ObjectDisposedException>(() =>
			{
				_ = stream.Length;
			});

			Assert.That(ex!.ObjectName, Is.EqualTo(nameof(FileStreamBase)));
		}

		[Test]
		public void Length_Set_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.Throws<ObjectDisposedException>(() =>
			{
				stream.Length = 0;
			});
		}

		[Test]
		public void FilePath_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			var ex = Assert.Throws<ObjectDisposedException>(() =>
			{
				_ = stream.FilePath;
			});

			Assert.That(ex!.ObjectName, Is.EqualTo(nameof(FileStreamBase)));
		}

		[Test]
		public void Flush_AfterDispose_ThrowsArgumentNullException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.Throws<ArgumentNullException>(() => stream.Flush());
		}

		[Test]
		public void FlushAsync_AfterDispose_ThrowsArgumentNullException()
		{
			using var tempFile = new TempFile();

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.ThrowsAsync<ArgumentNullException>(async () =>
			{
				await stream.FlushAsync().AsTask();
			});
		}

		[Test]
		public void Seek_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3 });

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.Throws<ObjectDisposedException>(() =>
			{
				stream.Seek(0, SeekOrigin.Begin);
			});
		}

		[Test]
		public void SetLength_AfterDispose_ThrowsObjectDisposedException()
		{
			using var tempFile = new TempFile(new byte[] { 1, 2, 3 });

			var stream = new TestFileStreamBase(
				tempFile.FilePath,
				FileMode.Open,
				FileAccess.ReadWrite
			);

			stream.Dispose();

			Assert.Throws<ObjectDisposedException>(() =>
			{
				stream.SetLength(0);
			});
		}
	}
}