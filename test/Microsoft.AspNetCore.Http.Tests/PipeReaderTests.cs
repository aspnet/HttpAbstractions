// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public partial class PipelineReaderWriterFacts
    {
        [Fact]
        public async Task CanRead()
        {
            var pipeReaderAdapter = new PipeReaderAdapter(new MemoryStream(Encoding.ASCII.GetBytes("Hello World")));
            var readResult = await pipeReaderAdapter.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));
            pipeReaderAdapter.AdvanceTo(buffer.End);
        }
    }
}