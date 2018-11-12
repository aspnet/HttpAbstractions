// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Http.Tests
{
    public abstract class PipeTest : IDisposable
    {
        private readonly TestMemoryPool _pool;

        protected TestAdaptedPipe Pipe;

        public MemoryStream MemoryStream { get; set; }

        protected PipeTest()
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