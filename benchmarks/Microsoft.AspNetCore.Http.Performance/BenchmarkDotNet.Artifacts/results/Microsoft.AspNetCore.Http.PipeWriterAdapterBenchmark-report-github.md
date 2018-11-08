``` ini

BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=2.2.100-preview2-009404
  [Host] : .NET Core 2.2.0-preview2-26905-02 (CoreCLR 4.6.26905.03, CoreFX 4.6.26905.02), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  InvocationCount=1  
IterationCount=1000  RunStrategy=Throughput  UnrollFactor=1  

```
|                                  Method |         Mean |      Error |      StdDev |       Median |      Op/s | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|---------------------------------------- |-------------:|-----------:|------------:|-------------:|----------:|------------:|------------:|------------:|--------------------:|
|                         WriteHelloWorld |     6.211 us |  0.0979 us |   0.8960 us |     5.910 us | 160,999.4 |           - |           - |           - |               712 B |
|      WriteHelloWorldLargeNumberOfWrites |   217.037 us |  1.6641 us |  14.6406 us |   211.680 us |   4,607.5 |           - |           - |           - |            192952 B |
|               WriteHelloWorldLargeWrite |    20.161 us |  0.3705 us |   3.4453 us |    18.985 us |  49,601.2 |           - |           - |           - |             54704 B |
| WriteHelloWorldLargeNumberOfLargeWrites | 8,631.575 us | 73.7181 us | 693.0832 us | 8,436.435 us |     115.9 |   1000.0000 |   1000.0000 |   1000.0000 |          12770712 B |
