``` ini

BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=2.2.100-preview2-009404
  [Host] : .NET Core 3.0.0-preview1-26907-05 (CoreCLR 4.6.26907.04, CoreFX 4.6.26907.04), 64bit RyuJIT

Job=Core  Runtime=Core  Server=True  
Toolchain=.NET Core 2.2  InvocationCount=1  RunStrategy=Throughput  
UnrollFactor=1  

```
|                           Method | Mean | Error | Op/s |
|--------------------------------- |-----:|------:|-----:|
|                    AddSingleItem |   NA |    NA |   NA |
|                    AddThreeItems |   NA |    NA |   NA |
|    ConditionalAdd_ContainsKeyAdd |   NA |    NA |   NA |
|            ConditionalAdd_TryAdd |   NA |    NA |   NA |
|          ForEachThreeItems_Array |   NA |    NA |   NA |
|     ForEachThreeItems_Properties |   NA |    NA |   NA |
|              GetThreeItems_Array |   NA |    NA |   NA |
|         GetThreeItems_Properties |   NA |    NA |   NA |
|                    SetSingleItem |   NA |    NA |   NA |
|                  SetExistingItem |   NA |    NA |   NA |
|                    SetThreeItems |   NA |    NA |   NA |
|      TryGetValueThreeItems_Array |   NA |    NA |   NA |
| TryGetValueThreeItems_Properties |   NA |    NA |   NA |

Benchmarks with issues:
  RouteValueDictionaryBenchmark.AddSingleItem: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.AddThreeItems: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.ConditionalAdd_ContainsKeyAdd: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.ConditionalAdd_TryAdd: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.ForEachThreeItems_Array: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.ForEachThreeItems_Properties: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.GetThreeItems_Array: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.GetThreeItems_Properties: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.SetSingleItem: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.SetExistingItem: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.SetThreeItems: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.TryGetValueThreeItems_Array: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
  RouteValueDictionaryBenchmark.TryGetValueThreeItems_Properties: Core(Runtime=Core, Server=True, Toolchain=.NET Core 2.2, InvocationCount=1, RunStrategy=Throughput, UnrollFactor=1)
