using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public class PipeWriterAdapter : PipeWriter
    {
        internal const int SegmentPoolSize = 16;

        private Stream _writingStream;
        private MemoryPool<byte> _memoryPool;
        private BufferSegment _writingHead;
        private BufferSegment _commitHead;
        private int _commitHeadIndex;
        private int _pooledSegmentCount;
        private readonly int _minimumSegmentSize;
        private readonly BufferSegment[] _bufferSegmentPool;
        private int _currentWriteLength;
        private int _length;

        public PipeWriterAdapter(Stream writingStream)
        {
            var options = PipeOptions.Default;
            _bufferSegmentPool = new BufferSegment[SegmentPoolSize];
            _writingStream = writingStream;
            _memoryPool = options.Pool;
            _minimumSegmentSize = options.MinimumSegmentSize;
        }

        public PipeWriterAdapter(Stream writingStream, MemoryPool<byte> memoryPool)
        {
            var options = PipeOptions.Default;
            _bufferSegmentPool = new BufferSegment[SegmentPoolSize];
            _writingStream = writingStream;
            _memoryPool = memoryPool;
            _minimumSegmentSize = options.MinimumSegmentSize;
        }

        public override void Advance(int bytesWritten)
        {
            if (_writingHead == null)
            {
                throw new InvalidOperationException("No writing operation. Make sure GetMemory() was called.");
            }

            if (bytesWritten >= 0)
            {
                Debug.Assert(!_writingHead.ReadOnly);
                Debug.Assert(_writingHead.Next == null);

                Memory<byte> buffer = _writingHead.AvailableMemory;

                if (_writingHead.End > buffer.Length - bytesWritten)
                {
                    throw new InvalidOperationException("Can't advance past buffer size.");
                }

                // if bytesWritten is zero, these do nothing
                _writingHead.End += bytesWritten;
                _currentWriteLength += bytesWritten;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(bytesWritten));
            }
        }

        public override void CancelPendingFlush()
        {
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            CommitUnsynchronized().GetAwaiter().GetResult();
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            // Take whatever is between start and committed and write it
            return CommitUnsynchronized(cancellationToken);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            AllocateWriteHead(sizeHint);

            // Slice the AvailableMemory to the WritableBytes size
            int end = _writingHead.End;
            Memory<byte> availableMemory = _writingHead.AvailableMemory;
            availableMemory = availableMemory.Slice(end);
            return availableMemory;
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            AllocateWriteHead(sizeHint);

            // Slice the AvailableMemory to the WritableBytes size
            int end = _writingHead.End;
            Span<byte> availableSpan = _writingHead.AvailableMemory.Span;
            availableSpan = availableSpan.Slice(end);
            return availableSpan;
        }

        private void AllocateWriteHead(int sizeHint)
        {
            BufferSegment segment = null;
            if (_writingHead != null)
            {
                segment = _writingHead;

                int bytesLeftInBuffer = segment.WritableBytes;

                // If inadequate bytes left or if the segment is readonly
                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint || segment.ReadOnly)
                {
                    BufferSegment nextSegment = CreateSegmentUnsynchronized();
                    nextSegment.SetMemory(_memoryPool.Rent(GetSegmentSize(sizeHint)));

                    segment.SetNext(nextSegment);

                    _writingHead = nextSegment;
                }
            }
            else
            {
                if (_commitHead != null && !_commitHead.ReadOnly)
                {
                    // Try to return the tail so the calling code can append to it
                    int remaining = _commitHead.WritableBytes;

                    if (sizeHint <= remaining && remaining > 0)
                    {
                        // Free tail space of the right amount, use that
                        segment = _commitHead;

                        // Set write head to assigned segment
                        _writingHead = segment;
                        return;
                    }
                }

                // No free tail space, allocate a new segment
                segment = CreateSegmentUnsynchronized();
                segment.SetMemory(_memoryPool.Rent(GetSegmentSize(sizeHint)));

                if (_commitHead == null)
                {
                    // No previous writes have occurred
                    _commitHead = segment;
                }
                else if (segment != _commitHead && _commitHead.Next == null)
                {
                    // Append the segment to the commit head if writes have been committed
                    // and it isn't the same segment (unused tail space)
                    _commitHead.SetNext(segment);
                }

                // Set write head to assigned segment
                _writingHead = segment;
            }
        }

        private int GetSegmentSize(int sizeHint)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(_minimumSegmentSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(_memoryPool.MaxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (_pooledSegmentCount > 0)
            {
                _pooledSegmentCount--;
                return _bufferSegmentPool[_pooledSegmentCount];
            }

            return new BufferSegment();
        }

        private void ReturnSegmentUnsynchronized(BufferSegment segment)
        {
            if (_pooledSegmentCount < _bufferSegmentPool.Length)
            {
                _bufferSegmentPool[_pooledSegmentCount] = segment;
                _pooledSegmentCount++;
            }
        }

        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotImplementedException();
        }

        internal async ValueTask<FlushResult> CommitUnsynchronized(CancellationToken token = default)
        {
            if (_writingHead == null)
            {
                // Nothing written to commit
                // How do we know isCompleted?
                return new FlushResult(false, true);
            }

            // Whatever is between the commitHead and writingHead should be written
            var ros = new ReadOnlySequence<byte>(_commitHead, _commitHeadIndex, _writingHead, _writingHead.End);
            var data = ros.ToArray();
            try
            {
                await _writingStream.WriteAsync(data, 0, data.Length, token);
                await _writingStream.FlushAsync(token);
            }
            catch (Exception)
            {
                return new FlushResult(true, false);
            }

            // Always move the commit head to the write head
            var bytesWritten = _currentWriteLength;
            _commitHead = _writingHead;
            _commitHeadIndex = _writingHead.End;
            _length += bytesWritten;

            // Clear the writing state
            _writingHead = null;
            _currentWriteLength = 0;

            return new FlushResult(false, false);
        }
    }
}
