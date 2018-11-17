using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class PipeReaderTests : PipeTest
    {
        [Fact]
        public async Task AdvanceToInvalidCursorThrows()
        {
            Write(new byte[100]);

            ReadResult result = await Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            Reader.AdvanceTo(buffer.End);

            Reader.CancelPendingRead();
            result = await Reader.ReadAsync();
            Assert.Throws<InvalidOperationException>(() => Reader.AdvanceTo(buffer.End));
            Reader.AdvanceTo(result.Buffer.End);
        }
    }
}
