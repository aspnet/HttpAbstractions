// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class FlushAsyncCancellationTests : PipeTest
    {
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

        [Fact]
        public async Task FlushAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            var cancellationTokenSource1 = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(10);
            await buffer.FlushAsync(cancellationTokenSource1.Token);

            cancellationTokenSource1.Cancel();

            var cancellationTokenSource2 = new CancellationTokenSource();
            buffer = Pipe.Writer.WriteEmpty(10);
            // Verifying that ReadAsync does not throw
            await buffer.FlushAsync(cancellationTokenSource2.Token);
        }
    }

    public static class TestWriterExtensions
    {
        public static PipeWriter WriteEmpty(this PipeWriter writer, int count)
        {
            writer.GetSpan(count).Slice(0, count).Fill(0);
            writer.Advance(count);
            return writer;
        }
    }
}