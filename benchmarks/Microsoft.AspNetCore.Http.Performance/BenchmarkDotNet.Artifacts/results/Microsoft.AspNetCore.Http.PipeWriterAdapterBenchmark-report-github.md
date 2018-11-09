``` ini

BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=2.2.100-preview2-009404
  [Host] : .NET Core 3.0.0-preview1-26907-05 (CoreCLR 4.6.26907.04, CoreFX 4.6.26907.04), 64bit RyuJIT

Job=Core  Runtime=Core  Server=True  
Toolchain=.NET Core 2.2  InvocationCount=1  RunStrategy=Throughput  
UnrollFactor=1  

```
|          Method | Mean | Error | Op/s |
|---------------- |-----:|------:|-----:|
| WriteHelloWorld |   NA |    NA |   NA |

Benchmarks with issues:
  PipeWriterAdapterBenchmark.WriteHelloWorld: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
