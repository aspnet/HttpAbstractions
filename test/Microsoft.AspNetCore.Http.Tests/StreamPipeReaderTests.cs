// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public partial class StreamPipeReaderTests : PipeTest
    {
        [Fact]
        public async Task CanRead()
        {
            Write(Encoding.ASCII.GetBytes("Hello World"));
            var readResult = await Reader.ReadAsync();
            var buffer = readResult.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));
            Reader.AdvanceTo(buffer.End);
        }

        [Fact]
        public async Task CanReadMultipleTimes()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));
            var readResult = await Reader.ReadAsync();

            Assert.Equal(MinimumReadSize, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(MinimumReadSize * 2, readResult.Buffer.Length);
            Assert.False(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(10000, readResult.Buffer.Length);
            Assert.False(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.End);
        }

        [Fact]
        public async Task ReadWithAdvance()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            var readResult = await Reader.ReadAsync();
            Assert.Equal(MinimumReadSize, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(MinimumReadSize, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);
        }
    }
}