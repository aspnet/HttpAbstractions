using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class TestAdaptedPipe
    {
        public TestAdaptedPipe(PipeReader reader, PipeWriter writer)
        {
            Reader = reader;
            Writer = writer;
        }

        public PipeReader Reader { get; private set; }

        public PipeWriter Writer { get; private set; }

        public void Reset()
        {
            // We won't be exposing an adapted pipe directly so making Reset noop
        }
    }
}
