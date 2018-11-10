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
        private PipeWriterAdapterWithListBacking _pipeWriterWithList;
        private byte[] _helloWorldBytes;
        private byte[] _largeWrite;

        [IterationSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _pipeWriter = new PipeWriterAdapter(_memoryStream);
            _pipeWriterWithList = new PipeWriterAdapterWithListBacking(_memoryStream);
            _helloWorldBytes = Encoding.ASCII.GetBytes("Hello World");
            _largeWrite = Encoding.ASCII.GetBytes(new string('a', 50000));
        }

        [Benchmark]
        public async Task WriteHelloWorld()
        {
            await _pipeWriter.WriteAsync(_helloWorldBytes);
        }

        [Benchmark]
        public async Task WriteHelloWorldLargeNumberOfWrites()
        {
            for (var i = 0; i < 1000; i++)
            {
                await _pipeWriter.WriteAsync(_helloWorldBytes);
            }
        }

        [Benchmark]
        public async Task WriteHelloWorldLargeWrite()
        {
            await _pipeWriter.WriteAsync(_largeWrite);
        }

        [Benchmark]
        public async Task WriteListHelloWorld()
        {
            await _pipeWriterWithList.WriteAsync(_helloWorldBytes);
        }

        [Benchmark]
        public async Task WriteListHelloWorldLargeNumberOfWrites()
        {
            for (var i = 0; i < 1000; i++)
            {
                await _pipeWriterWithList.WriteAsync(_helloWorldBytes);
            }
        }

        [Benchmark]
        public async Task WriteListHelloWorldLargeWrite()
        {
            await _pipeWriterWithList.WriteAsync(_largeWrite);
        }
    }
}
