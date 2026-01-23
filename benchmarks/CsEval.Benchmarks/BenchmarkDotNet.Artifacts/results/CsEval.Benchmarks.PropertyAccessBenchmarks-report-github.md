```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.2 (24G325) [Darwin 24.6.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 8.0.204
  [Host]   : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method                  | iterations | Mean         | Error       | StdDev      | Gen0     | Gen1   | Allocated |
|------------------------ |----------- |-------------:|------------:|------------:|---------:|-------:|----------:|
| **PropertyAccess_Single**   | **?**          |     **156.6 ns** |     **1.00 ns** |     **0.89 ns** |   **0.0410** |      **-** |     **344 B** |
| PropertyAccess_Multiple | ?          |  14,327.4 ns |   271.01 ns |   253.51 ns |   0.1373 |      - |    1256 B |
| ObjectMerge_TypedObject | ?          |     630.9 ns |     4.37 ns |     3.65 ns |   0.1554 |      - |    1304 B |
| Spread_TypedObject      | ?          |     493.4 ns |     3.75 ns |     3.51 ns |   0.0982 |      - |     824 B |
| Linq_OnTypedObjects     | ?          |  17,787.0 ns |   119.43 ns |   105.87 ns |   6.8970 | 0.0610 |   57928 B |
| **RepeatedPropertyAccess**  | **100**        |  **14,883.4 ns** |   **111.77 ns** |    **99.08 ns** |   **4.1046** |      **-** |   **34400 B** |
| RepeatedObjectMerge     | 100        |  65,780.4 ns |   898.30 ns |   840.27 ns |  15.5029 |      - |  130400 B |
| **RepeatedPropertyAccess**  | **1000**       | **154,296.3 ns** | **2,024.58 ns** | **1,893.79 ns** |  **41.0156** |      **-** |  **344000 B** |
| RepeatedObjectMerge     | 1000       | 638,464.2 ns | 6,512.34 ns | 5,773.02 ns | 155.2734 |      - | 1304001 B |
