``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 10.0.17134
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical cores and 6 physical cores
.NET Core SDK=2.2.100-preview2-009404
  [Host] : .NET Core 2.2.0-preview2-26905-02 (CoreCLR 4.6.26905.03, CoreFX 4.6.26905.02), 64bit RyuJIT

Runtime=Core  Server=True  Toolchain=.NET Core 3.0  
RunStrategy=Throughput  

```
|                           Method | Mean | Error | Op/s | Allocated |
|--------------------------------- |-----:|------:|-----:|----------:|
|                    AddSingleItem |   NA |    NA |   NA |       N/A |
|                    AddThreeItems |   NA |    NA |   NA |       N/A |
|    ConditionalAdd_ContainsKeyAdd |   NA |    NA |   NA |       N/A |
|            ConditionalAdd_TryAdd |   NA |    NA |   NA |       N/A |
|          ForEachThreeItems_Array |   NA |    NA |   NA |       N/A |
|     ForEachThreeItems_Properties |   NA |    NA |   NA |       N/A |
|              GetThreeItems_Array |   NA |    NA |   NA |       N/A |
|         GetThreeItems_Properties |   NA |    NA |   NA |       N/A |
|                    SetSingleItem |   NA |    NA |   NA |       N/A |
|                  SetExistingItem |   NA |    NA |   NA |       N/A |
|                    SetThreeItems |   NA |    NA |   NA |       N/A |
|      TryGetValueThreeItems_Array |   NA |    NA |   NA |       N/A |
| TryGetValueThreeItems_Properties |   NA |    NA |   NA |       N/A |

Benchmarks with issues:
  RouteValueDictionaryBenchmark.AddSingleItem: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.AddThreeItems: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.ConditionalAdd_ContainsKeyAdd: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.ConditionalAdd_TryAdd: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.ForEachThreeItems_Array: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.ForEachThreeItems_Properties: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.GetThreeItems_Array: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.GetThreeItems_Properties: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.SetSingleItem: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.SetExistingItem: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.SetThreeItems: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.TryGetValueThreeItems_Array: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
  RouteValueDictionaryBenchmark.TryGetValueThreeItems_Properties: Job-RAWIYE(Runtime=Core, Server=True, Toolchain=.NET Core 3.0, RunStrategy=Throughput)
