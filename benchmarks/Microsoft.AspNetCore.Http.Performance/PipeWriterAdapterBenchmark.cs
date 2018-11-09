// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http
{
    public class PipeWriterAdapterBenchmark
    {
        private MemoryStream _memoryStream;
        private PipeWriterAdapter _pipeWriter;
        private byte[] _helloWorldBytes;

        [IterationSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _pipeWriter = new PipeWriterAdapter(_memoryStream);
            _helloWorldBytes = Encoding.ASCII.GetBytes("Hello World");
        }

        [Benchmark]
        public async Task WriteHelloWorld()
        {
            await _pipeWriter.WriteAsync(_helloWorldBytes);
        }
    }
}
