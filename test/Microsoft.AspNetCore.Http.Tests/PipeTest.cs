// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Tests
{
    public abstract class PipeTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;

        protected TestAdaptedPipe Pipe;

        public MemoryStream MemoryStream { get; set; }

        protected PipeTest()
        {
            MemoryStream = new MemoryStream();
            Pipe = new TestAdaptedPipe(null /* TODO add PipeReaderAdapter here */ , new StreamPipeWriter(MemoryStream));
        }

        public void Dispose()
        {
            Pipe.Writer.Complete();
        }
    }
}
