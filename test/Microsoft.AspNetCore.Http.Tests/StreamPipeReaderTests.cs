// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public partial class PipelineReaderWriterFacts : PipeTest
    {
        [Fact]
        public async Task CanRead()
        {
            var pipeReaderAdapter = new StreamPipeReader(new MemoryStream(Encoding.ASCII.GetBytes("Hello World")));
            var readResult = await pipeReaderAdapter.ReadAsync();
            var buffer = readResult.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));
            pipeReaderAdapter.AdvanceTo(buffer.End);
        }

        private void Write()
        {

        }
    }
}