// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public class StreamPipeReader : PipeReader
    {
        private readonly int _minimumSegmentSize;
        private readonly int _minimumReadSize;
        private readonly Stream _readingStream;
        private readonly MemoryPool<byte> _pool;

        private CancellationTokenSource _internalTokenSource;
        private bool _isCompleted;
        private ExceptionDispatchInfo _exceptionInfo;
        private object lockObject = new object();

        private BufferSegment _readHead;
        private int _readIndex;

        private BufferSegment _commitHead;
        private int _commitIndex;
        private long _consumedLength;
        private long _examinedLength;

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                lock (lockObject)
                {
                    if (_internalTokenSource == null)
                    {
                        _internalTokenSource = new CancellationTokenSource();
                    }
                    return _internalTokenSource;
                }
            }
        }

        public StreamPipeReader(Stream readingStream) : this(readingStream, 4096)
        {
        }

        public StreamPipeReader(Stream readingStream, int minimumSegmentSize, MemoryPool<byte> pool = null)
        {
            _minimumSegmentSize = minimumSegmentSize;
            _minimumReadSize = _minimumSegmentSize / 4;
            _readingStream = readingStream;
            _pool = pool ?? MemoryPool<byte>.Shared;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            AdvanceTo((BufferSegment)consumed.GetObject(), consumed.GetInteger(), (BufferSegment)examined.GetObject(), examined.GetInteger());
        }

        private void AdvanceTo(BufferSegment consumedSegment, int consumedIndex, BufferSegment examinedSegment, int examinedIndex)
        {
            BufferSegment returnStart = null;
            BufferSegment returnEnd = null;

            if (consumedSegment != null)
            {
                if (_readHead == null)
                {
                    throw new InvalidOperationException("");
                }
                // TODO use examinedSegment/index to allow readasync to continue.

                returnStart = _readHead;
                returnEnd = consumedSegment;

                var consumedBytes = new ReadOnlySequence<byte>(returnStart, _readIndex, consumedSegment, consumedIndex).Length;
                _consumedLength -= consumedBytes;

                var examinedBytes = new ReadOnlySequence<byte>(returnStart, _readIndex, examinedSegment, examinedIndex).Length;
                _examinedLength -= examinedBytes;

                if (consumedIndex == returnEnd.Length) // _commitHead != returnEnd
                {
                    var nextBlock = returnEnd.NextSegment;
                    if (_readHead == returnEnd)
                    {
                        _readHead = nextBlock;
                        _readIndex = 0;
                        // This check is bad lol
                        if (_readHead == null)
                        {
                            _commitHead = null;
                        }
                    }

                    returnEnd = nextBlock;
                }
                else
                {
                    _readHead = consumedSegment;
                    _readIndex = consumedIndex;
                }
            }

            while (returnStart != null && returnStart != returnEnd)
            {
                returnStart.ResetMemory();
                returnStart = returnStart.NextSegment;
            }
        }

        public override void CancelPendingRead()
        {
            InternalTokenSource.Cancel();
        }

        public override void Complete(Exception exception = null)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            if (exception != null)
            {
                _exceptionInfo = ExceptionDispatchInfo.Capture(exception);
            }

            var segment = _readHead;
            while (segment != null)
            {
                segment.ResetMemory();
                segment = segment.NextSegment;
            }
        }

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotSupportedException();
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (TryRead(out var readResult))
            {
                return readResult;
            }
            try
            {
                AllocateCommitHead();
#if NETCOREAPP2_2
                var length = await _readingStream.ReadAsync(_commitHead.AvailableMemory, InternalTokenSource.Token);
#elif NETSTANDARD2_0
                MemoryMarshal.TryGetArray<byte>(_commitHead.AvailableMemory, out var arraySegment);
                var length = await _readingStream.ReadAsync(arraySegment.Array, 0, arraySegment.Count, InternalTokenSource.Token);
#else
#error Target frameworks need to be updated.
#endif
                _commitHead.End += length;
                _commitIndex = _commitHead.End;
                _consumedLength += length;
                _examinedLength += length;

                var ros = new ReadOnlySequence<byte>(_readHead, _readIndex, _commitHead, _commitIndex - _commitHead.Start);
                return new ReadResult(ros, isCanceled: false, IsCompletedOrThrow());
            }
            catch (OperationCanceledException)
            {
                // Remove the cancellation token such that the next time Flush is called
                // A new CTS is created.
                lock (lockObject)
                {
                    _internalTokenSource = null;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                // Catch any cancellation and translate it into setting isCanceled = true
                var ros = new ReadOnlySequence<byte>(_readHead, _readIndex, _commitHead, _commitIndex - _commitHead.Start);
                return new ReadResult(ros, isCanceled: true, IsCompletedOrThrow());
            }
        }

        private void AllocateCommitHead()
        {
            BufferSegment segment;
            if (_commitHead != null)
            {
                segment = _commitHead;
                var bytesLeftInBuffer = segment.WritableBytes;
                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < _minimumReadSize || segment.ReadOnly)
                {
                    var nextSegment = CreateSegmentUnsynchronized();
                    nextSegment.SetMemory(_pool.Rent(GetSegmentSize()));
                    segment.SetNext(nextSegment);
                    _commitHead = nextSegment;
                }
            }
            else
            {
                if (_readHead != null && !_commitHead.ReadOnly)
                {
                    // Don't believe this can be hit.
                    var remaining = _commitHead.WritableBytes;
                    if (_minimumReadSize <= remaining && remaining > 0)
                    {
                        segment = _readHead;
                        _commitHead = segment;
                        return;
                    }
                }

                segment = CreateSegmentUnsynchronized();
                segment.SetMemory(_pool.Rent(GetSegmentSize()));
                if (_readHead == null)
                {
                    _readHead = segment;
                }
                else if (segment != _readHead && _readHead.Next == null)
                {
                    _readHead.SetNext(segment);
                }

                _commitHead = segment;
            }
        }

        private int GetSegmentSize()
        {
            var adjustedToMaximumSize = Math.Min(_pool.MaxBufferSize, _minimumSegmentSize);
            return adjustedToMaximumSize;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            return new BufferSegment();
        }
            
        public override bool TryRead(out ReadResult result)
        {
            if (_consumedLength > 0 && _examinedLength > 0)
            {
                var ros = new ReadOnlySequence<byte>(_readHead, _readIndex, _commitHead, _commitIndex - _commitHead.Start);
                result = new ReadResult(ros, isCanceled: false, IsCompletedOrThrow());
                return true;
            }

            return false;
        }

        private void Cancel()
        {
            InternalTokenSource.Cancel();
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
            Complete();
        }

        private class BufferSegment : ReadOnlySequenceSegment<byte>
        {
            private IMemoryOwner<byte> _memoryOwner;
            private BufferSegment _next;
            private int _end;

            public int Start { get; private set; }

            public int End
            {
                get => _end;
                set
                {
                    _end = value;
                    Memory = AvailableMemory.Slice(Start, _end - Start);
                }
            }

            public BufferSegment NextSegment
            {
                get => _next;
                set
                {
                    _next = value;
                    Next = value;
                }
            }

            public void SetMemory(IMemoryOwner<byte> memoryOwner)
            {
                SetMemory(memoryOwner, 0, 0);
            }

            public void SetMemory(IMemoryOwner<byte> memoryOwner, int start, int end, bool readOnly = false)
            {
                _memoryOwner = memoryOwner;

                AvailableMemory = _memoryOwner.Memory;

                ReadOnly = readOnly;
                RunningIndex = 0;
                Start = start;
                End = end;
                NextSegment = null;
            }

            public void ResetMemory()
            {
                _memoryOwner.Dispose();
                _memoryOwner = null;
                AvailableMemory = default;
            }

            internal IMemoryOwner<byte> MemoryOwner => _memoryOwner;

            public Memory<byte> AvailableMemory { get; private set; }

            public int Length => End - Start;

            public bool ReadOnly { get; private set; }

            public int WritableBytes
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => AvailableMemory.Length - End;
            }

            public void SetNext(BufferSegment segment)
            {
                NextSegment = segment;

                segment = this;

                while (segment.Next != null)
                {
                    segment.NextSegment.RunningIndex = segment.RunningIndex + segment.Length;
                    segment = segment.NextSegment;
                }
            }
        }
    }
}
