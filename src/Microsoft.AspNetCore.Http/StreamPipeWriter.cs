// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Implements PipeWriter using a base stream implementation. 
    /// </summary>
    public class StreamPipeWriter : PipeWriter, IDisposable
    {
        private readonly int _minimumSegmentSize;
        private readonly Stream _writingStream;
        private int _bytesWritten;

        private List<CompletedBuffer> _completedSegments;
        private byte[] _currentSegment;
        private int _position;
        private int _previousSegmentPosition;

        private CancellationTokenSource _internalTokenSource;
        private bool _isCompleted;
        private ExceptionDispatchInfo _exceptionInfo;

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                if (_internalTokenSource == null)
                {
                    _internalTokenSource = new CancellationTokenSource();
                }
                return _internalTokenSource;
            }
            set { _internalTokenSource = value; }
        }

        public StreamPipeWriter(Stream writingStream) : this(writingStream, 4096)
        {
        }

        public StreamPipeWriter(Stream writingStream, int minimumSegmentSize)
        {
            _minimumSegmentSize = minimumSegmentSize;
            _writingStream = writingStream;
        }

        /// <inheritdoc />
        public override void Advance(int count)
        {
            if (_currentSegment == null)
            {
                throw new InvalidOperationException("No writing operation. Make sure GetMemory() was called.");
            }

            if (count >= 0)
            {
                if (_currentSegment.Length < _position + count)
                {
                    throw new InvalidOperationException("Can't advance past buffer size.");
                }
                _bytesWritten += count;
                _position += count;
            }
        }

        /// <inheritdoc />
        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.AsMemory(_position, _currentSegment.Length - _position);
        }

        /// <inheritdoc />
        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.AsSpan(_position, _currentSegment.Length - _position);
        }

        /// <inheritdoc />
        public override void CancelPendingFlush()
        {
            InternalTokenSource?.Cancel();
        }

        /// <inheritdoc />
        public override void Complete(Exception exception = null)
        {
            _isCompleted = true;
            if (exception != null)
            {
                _exceptionInfo = ExceptionDispatchInfo.Capture(exception);
            }
        }

        /// <inheritdoc />
        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            // Note this currently doesn't do anything.
            // Implementing completions/callbacks would require creating a PipeReaderAdapter.
        }

        /// <inheritdoc />
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            return FlushAsyncInternal(cancellationToken);
        }

        private async ValueTask<FlushResult> FlushAsyncInternal(CancellationToken cancellationToken = default)
        {
            if (_bytesWritten == 0)
            {
                return new FlushResult(isCanceled: false, IsCompletedOrThrow());
            }

            // Wrap the provided cancellationToken with an internal one
            // to allow CancelPendingFlush to cancel writes and flushes
            if (InternalTokenSource.IsCancellationRequested)
            {
                // If CancelPendingFlush was already called, we need to return a FlushResult with
                // Canceled. At this point, we create a fresh CTS that isn't canceled.
                // PERF: we only create a new CTS if the previous one wasn't canceled.
                // Otherwise, we would need to create a new CTS for every call to FlushAsync.
                var result = new FlushResult(isCanceled: true, IsCompletedOrThrow());
                InternalTokenSource = new CancellationTokenSource();
                return result;
            }

            var token = InternalTokenSource.Token;
            if (cancellationToken != CancellationToken.None)
            {
                // PERF: only link token sources if FlushAsync was provided a cancellation token.
                token = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    InternalTokenSource.Token).Token;
            }

            try
            {
                // Write all completed segments and whatever remains in the current segment
                // and flush the result.
                if (_completedSegments != null && _completedSegments.Count > 0)
                {
                    // We need to treat the first completed segment uniquely because we allow 
                    // partial consumption of the first CompletedSegment.
                    // Example:
                    // A user calls GetMemory(11) to move the _currentSegment _position to 11 and then call FlushAsync().
                    // Afterwards, they call GetMemory(1000), which would return Memory in _currentSegment.
                    // Afterwards, they call GetMemory(8000), which would return a new segment, which moves _currentSegment to _completedSegments
                    // Then the user calls FlushAsync().
                    // At this point, the firstSegment needs to be treated differently because it was only partially written to.
                    // To fix this, we use _previousSegmentPosition to store how far we were into the first _completedSegment.
                    var firstSegment = _completedSegments[0];
                    await _writingStream.WriteAsync(firstSegment.Buffer, 
                        _previousSegmentPosition, 
                        firstSegment.Length - _previousSegmentPosition, 
                        token);

                    // We also need to keep track of how many bytes we have written so far,
                    // s.t. the final write knows how many bytes have been consumed.
                    _bytesWritten -= firstSegment.Length - _previousSegmentPosition;

                    var count = _completedSegments.Count;
                    for (var i = 1; i < count; i++)
                    {
                        var segment = _completedSegments[i];
                        await _writingStream.WriteAsync(segment.Buffer, 0, segment.Length, token);
                        _bytesWritten -= segment.Length;
                        segment.Return();
                    }

                    _completedSegments.Clear();
                }

                if (_currentSegment != null)
                {
                    await _writingStream.WriteAsync(_currentSegment, _position - _bytesWritten, _bytesWritten, token);
                }

                await _writingStream.FlushAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Catch any cancellation and translate it into setting isCanceled = true
                return new FlushResult(isCanceled: true, IsCompletedOrThrow());
            }

            // After writing to the stream, we can return all ArrayPool segments that are completed.
            _bytesWritten = 0;
            _previousSegmentPosition = _position;

            return new FlushResult(isCanceled: false, IsCompletedOrThrow());
        }

        private void EnsureCapacity(int sizeHint)
        {
            // This does the Right Thing. It only subtracts _position from the current segment length if it's non-null.
            // If _currentSegment is null, it returns 0.
            var remainingSize = _currentSegment?.Length - _position ?? 0;

            // If the sizeHint is 0, any capacity will do
            // Otherwise, the buffer must have enough space for the entire size hint, or we need to add a segment.
            if ((sizeHint == 0 && remainingSize > 0) || (sizeHint > 0 && remainingSize >= sizeHint))
            {
                // We have capacity in the current segment
                return;
            }

            AddSegment(sizeHint);
        }

        private void AddSegment(int sizeHint = 0)
        {
            if (_currentSegment != null)
            {
                // We're adding a segment to the list
                if (_completedSegments == null)
                {
                    _completedSegments = new List<CompletedBuffer>();
                }

                // Position might be less than the segment length if there wasn't enough space to satisfy the sizeHint when
                // GetMemory was called. In that case we'll take the current segment and call it "completed", but need to
                // ignore any empty space in it.
                _completedSegments.Add(new CompletedBuffer(_currentSegment, _position));
            }

            // Get a new buffer using the minimum segment size, unless the size hint is larger than a single segment.
            _currentSegment = ArrayPool<byte>.Shared.Rent(Math.Max(_minimumSegmentSize, sizeHint));
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCompletedOrThrow()
        {
            if (!_isCompleted)
            {
                return false;
            }

            if (_exceptionInfo != null)
            {
                ThrowLatchedException();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowLatchedException()
        {
            _exceptionInfo.Throw();
        }

        public void Dispose()
        {
            _internalTokenSource?.Dispose();
        }

        /// <summary>
        /// Holds a byte[] from the pool and a size value. Basically a Memory but guaranteed to be backed by an ArrayPool byte[], so that we know we can return it.
        /// </summary>
        private readonly struct CompletedBuffer
        {
            public byte[] Buffer { get; }
            public int Length { get; }

            public ReadOnlySpan<byte> Span => Buffer.AsSpan(0, Length);

            public CompletedBuffer(byte[] buffer, int length)
            {
                Buffer = buffer;
                Length = length;
            }

            public void Return()
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }
    }
}
