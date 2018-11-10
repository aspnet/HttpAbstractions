``` ini

BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=2.2.100-preview2-009404
  [Host] : .NET Core 2.2.0-preview2-26905-02 (CoreCLR 4.6.26905.03, CoreFX 4.6.26905.02), 64bit RyuJIT
  Core   : .NET Core 2.2.0-preview2-26905-02 (CoreCLR 4.6.26905.03, CoreFX 4.6.26905.02), 64bit RyuJIT

Job=Core  Runtime=Core  Server=True  
Toolchain=.NET Core 2.2  InvocationCount=1  RunStrategy=Throughput  
UnrollFactor=1  

```
|                                 Method |       Mean |      Error |     StdDev |     Median |      Op/s | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------------- |-----------:|-----------:|-----------:|-----------:|----------:|------------:|------------:|------------:|--------------------:|
|                        WriteHelloWorld |   8.703 us |  0.3123 us |  0.8601 us |   8.345 us | 114,903.5 |           - |           - |           - |              2712 B |
|     WriteHelloWorldLargeNumberOfWrites | 645.751 us | 13.0963 us | 35.8508 us | 644.530 us |   1,548.6 |           - |           - |           - |            222032 B |
|              WriteHelloWorldLargeWrite |  55.306 us | 11.8659 us | 34.6135 us |  36.660 us |  18,081.2 |           - |           - |           - |            170216 B |
|                    WriteListHelloWorld |   6.991 us |  0.3372 us |  0.9174 us |   6.875 us | 143,035.3 |           - |           - |           - |               424 B |
| WriteListHelloWorldLargeNumberOfWrites | 395.641 us | 11.6011 us |  9.6875 us | 392.010 us |   2,527.5 |           - |           - |           - |            112744 B |
|          WriteListHelloWorldLargeWrite |  28.264 us |  5.1681 us | 15.1572 us |  19.640 us |  35,380.4 |           - |           - |           - |             54416 B |
