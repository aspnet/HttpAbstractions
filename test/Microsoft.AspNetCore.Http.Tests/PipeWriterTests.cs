// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class PipeWriterTests : PipeTest
    {
        [Theory]
        [InlineData(3, -1, 0)]
        [InlineData(3, 0, -1)]
        [InlineData(3, 0, 4)]
        [InlineData(3, 4, 0)]
        [InlineData(3, -1, -1)]
        [InlineData(3, 4, 4)]
        public void ThrowsForInvalidParameters(int arrayLength, int offset, int length)
        {
            PipeWriter writer = Pipe.Writer;
            var array = new byte[arrayLength];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = (byte)(i + 1);
            }

            writer.Write(new Span<byte>(array, 0, 0));
            writer.Write(new Span<byte>(array, array.Length, 0));

            try
            {
                writer.Write(new Span<byte>(array, offset, length));
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }

            writer.Write(new Span<byte>(array, 0, array.Length));
            Assert.Equal(array, Read());
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(1, 1)]
        public void CanWriteWithOffsetAndLength(int offset, int length)
        {
            PipeWriter writer = Pipe.Writer;
            var array = new byte[] { 1, 2, 3 };

            writer.Write(new Span<byte>(array, offset, length));

            Assert.Equal(array.Skip(offset).Take(length).ToArray(), Read());
        }

        [Fact]
        public void CanWriteIntoHeadlessBuffer()
        {
            PipeWriter writer = Pipe.Writer;

            writer.Write(new byte[] { 1, 2, 3 });
            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Fact]
        public void CanWriteMultipleTimes()
        {
            PipeWriter writer = Pipe.Writer;

            writer.Write(new byte[] { 1 });
            writer.Write(new byte[] { 2 });
            writer.Write(new byte[] { 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Fact]
        public async Task CanWriteAsyncMultipleTimesIntoSameBlock()
        {
            PipeWriter writer = Pipe.Writer;

            await writer.WriteAsync(new byte[] { 1 });
            await writer.WriteAsync(new byte[] { 2 });
            await writer.WriteAsync(new byte[] { 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Theory]
        [InlineData(100, 1000)]
        [InlineData(100, 8000)]
        [InlineData(100, 10000)]
        [InlineData(8000, 100)]
        [InlineData(8000, 8000)]
        public async Task CanAdvanceWithPartialConsumptionOfFirstSegment(int firstWriteLength, int secondWriteLength)
        {
            PipeWriter writer = Pipe.Writer;
            await writer.WriteAsync(Encoding.ASCII.GetBytes("a"));

            var expectedLength = firstWriteLength + secondWriteLength + 1;

            var memory = writer.GetMemory(firstWriteLength);
            writer.Advance(firstWriteLength);

            memory = writer.GetMemory(secondWriteLength);
            writer.Advance(secondWriteLength);

            await writer.FlushAsync();

            Assert.Equal(expectedLength, Read().Length);
        }

        [Fact]
        public void CanWriteOverTheBlockLength()
        {
            Memory<byte> memory = Pipe.Writer.GetMemory();
            PipeWriter writer = Pipe.Writer;

            IEnumerable<byte> source = Enumerable.Range(0, memory.Length).Select(i => (byte)i);
            byte[] expectedBytes = source.Concat(source).Concat(source).ToArray();

            writer.Write(expectedBytes);

            Assert.Equal(expectedBytes, Read());
        }

        [Fact]
        public void EnsureAllocatesSpan()
        {
            PipeWriter writer = Pipe.Writer;
            var span = writer.GetSpan(10);

            Assert.True(span.Length >= 10);
            // 0 byte Flush would not complete the reader so we complete.
            Pipe.Writer.Complete();
            Assert.Equal(new byte[] { }, Read());
        }

        [Fact]
        public void SlicesSpanAndAdvancesAfterWrite()
        {
            int initialLength = Pipe.Writer.GetSpan(3).Length;

            PipeWriter writer = Pipe.Writer;

            writer.Write(new byte[] { 1, 2, 3 });
            Span<byte> span = Pipe.Writer.GetSpan();

            Assert.Equal(initialLength - 3, span.Length);
            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(5000)]
        [InlineData(50000)]
        public async Task WriteLargeDataBinary(int length)
        {
            var data = new byte[length];
            new Random(length).NextBytes(data);
            PipeWriter output = Pipe.Writer;
            output.Write(data);
            await output.FlushAsync();

            var input = Read();
            Assert.Equal(data, input.ToArray());
        }

        [Fact]
        public async Task CanWriteNothingToBuffer()
        {
            PipeWriter buffer = Pipe.Writer;
            buffer.GetMemory(0);
            buffer.Advance(0); // doing nothing, the hard way
            await buffer.FlushAsync();
        }

        [Fact]
        public void EmptyWriteDoesNotThrow()
        {
            Pipe.Writer.Write(new byte[0]);
        }

        [Fact]
        public void ThrowsOnAdvanceOverMemorySize()
        {
            Memory<byte> buffer = Pipe.Writer.GetMemory(1);
            var exception = Assert.Throws<InvalidOperationException>(() => Pipe.Writer.Advance(buffer.Length + 1));
            Assert.Equal("Can't advance past buffer size.", exception.Message);
        }

        [Fact]
        public void ThrowsOnAdvanceWithNoMemory()
        {
            PipeWriter buffer = Pipe.Writer;
            var exception = Assert.Throws<InvalidOperationException>(() => buffer.Advance(1));
            Assert.Equal("No writing operation. Make sure GetMemory() was called.", exception.Message);
        }

        [Fact]
        public async Task ThrowsOnCompleteAndWrite()
        {
            PipeWriter buffer = Pipe.Writer;
            buffer.Complete(new InvalidOperationException("Whoops"));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await buffer.FlushAsync());

            Assert.Equal("Whoops", exception.Message);
        }

        [Fact]
        public async Task WriteCanBeCancelledViaProvidedCancellationToken()
        {
            var pipeWriter = new StreamPipeWriter(new HangingStream());
            var cts = new CancellationTokenSource(1);
            var flushResult = await pipeWriter.WriteAsync(Encoding.ASCII.GetBytes("data"), cts.Token);
            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public async Task WriteCanBeCanceledViaCancelPendingFlushWhenFlushIsAsync()
        {
            var pipeWriter = new StreamPipeWriter(new HangingStream());
            FlushResult flushResult = new FlushResult();

            var e = new ManualResetEventSlim();

            var task = Task.Run(async () =>
            {
                try
                {
                    var writingTask = pipeWriter.WriteAsync(Encoding.ASCII.GetBytes("data"));
                    e.Set();
                    flushResult = await writingTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            });

            e.Wait();

            pipeWriter.CancelPendingFlush();

            await task;

            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public void FlushAsyncCancellationDeadlock()
        {
            var cts = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();

            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            var e = new ManualResetEventSlim();

            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync(cts.Token).GetAwaiter();
            awaiter.OnCompleted(
                () => {
                    // We are on cancellation thread and need to wait until another FlushAsync call
                    // takes pipe state lock
                    e.Wait();

                    // Make sure we had enough time to reach _cancellationTokenRegistration.Dispose
                    Thread.Sleep(100);

                    // Try to take pipe state lock
                    buffer.FlushAsync();
                });

            // Start a thread that would run cancellation callbacks
            Task cancellationTask = Task.Run(() => cts.Cancel());
            // Start a thread that would call FlushAsync with different token
            // and block on _cancellationTokenRegistration.Dispose
            Task blockingTask = Task.Run(
                () => {
                    e.Set();
                    buffer.FlushAsync(cts2.Token);
                });

            bool completed = Task.WhenAll(cancellationTask, blockingTask).Wait(TimeSpan.FromSeconds(10));
            Assert.True(completed);
        }

        [Fact]
        public void FlushAsyncCompletedAfterPreCancellation()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(1);

            Pipe.Writer.CancelPendingFlush();

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);

            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();

            Assert.True(flushResult.IsCanceled);

            flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfCanceledBeforeFlush()
        {
            CheckCanceledFlush();
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfCanceledBeforeFlushMultipleTimes()
        {
            for (var i = 0; i < 10; i++)
            {
                CheckCanceledFlush();
            }
        }

        [Fact]
        public async Task FlushAsyncReturnsCanceledInterleaved()
        {
            for (var i = 0; i < 5; i++)
            {
                CheckCanceledFlush();
                await CheckWriteIsNotCanceled();
            }
        }

        [Fact]
        public async Task FlushAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            var cancellationTokenSource1 = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(10);
            await buffer.FlushAsync(cancellationTokenSource1.Token);

            cancellationTokenSource1.Cancel();

            var cancellationTokenSource2 = new CancellationTokenSource();
            buffer = Pipe.Writer.WriteEmpty(10);

            await buffer.FlushAsync(cancellationTokenSource2.Token);
        }

        private byte[] Read()
        {
            Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            MemoryStream.Position = 0;
            var buffer = new byte[MemoryStream.Length];
            var result = MemoryStream.Read(buffer, 0, (int)MemoryStream.Length);
            return buffer;
        }

        private async Task CheckWriteIsNotCanceled()
        {
            var flushResult = await Pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes("data"));
            Assert.False(flushResult.IsCanceled);
        }

        private void CheckCanceledFlush()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            Pipe.Writer.CancelPendingFlush();

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();
            Assert.True(flushResult.IsCanceled);
        }
    }

    internal class HangingStream : Stream
    {
        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
            return 0;
        }
    }

    internal static class TestWriterExtensions
    {
        public static PipeWriter WriteEmpty(this PipeWriter writer, int count)
        {
            writer.GetSpan(count).Slice(0, count).Fill(0);
            writer.Advance(count);
            return writer;
        }
    }
}
