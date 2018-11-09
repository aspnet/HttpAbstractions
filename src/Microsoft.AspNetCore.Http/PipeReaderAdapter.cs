using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public class PipeReaderAdapter : PipeReader
    {
        public Stream _readingStream;
        public Pipe _pipe;
        public PipeReaderAdapter(Stream readingStream)
        {
            _readingStream = readingStream;
            _pipe = new Pipe();
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            _pipe.Reader.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _pipe.Reader.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            _pipe.Reader.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            _pipe.Reader.Complete(exception);
        }

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            _pipe.Reader.OnWriterCompleted(callback, state);
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            // if data is available, read from the pipe directly,
            // otherwise write into the pipe
            if (_pipe.Reader.TryRead(out var readResult))
            {
                return new ValueTask<ReadResult>(readResult);
            }
            else
            {
                // This feels wrong.
                ReadFromStreamWriteToPipe().GetAwaiter().GetResult();
                return _pipe.Reader.ReadAsync(cancellationToken);
            }
        }

        public async Task ReadFromStreamWriteToPipe()
        {
            // TODO think about memory size.
            var memory = new byte[4096];
            // TODO this needs to do something with cancellation
            var dataRead = await _readingStream.ReadAsync(memory, 0, 4096);

            // TODO make this so it doesn't copy twice
            await _pipe.Writer.WriteAsync(new Memory<byte>(memory, 0, dataRead));
        }

        public override bool TryRead(out ReadResult result)
        {
            return _pipe.Reader.TryRead(out result);
        }
    }
}
