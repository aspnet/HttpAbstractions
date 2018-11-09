// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Http.Tests
{
    public abstract class PipeTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;

        protected const int MaximumSizeLow = 6;

        private readonly TestMemoryPool _pool;

        protected TestAdaptedPipe Pipe;

        public MemoryStream MemoryStream { get; set; }

        protected PipeTest(int pauseWriterThreshold = MaximumSizeHigh, int resumeWriterThreshold = MaximumSizeLow)
        {
            _pool = new TestMemoryPool();
            MemoryStream = new MemoryStream();
            Pipe = new TestAdaptedPipe(new PipeReaderAdapter(MemoryStream), new PipeWriterAdapter(MemoryStream));
        }

        public void Dispose()
        {
            Pipe.Writer.Complete();
            Pipe.Reader.Complete();
            _pool.Dispose();
        }
    }
}