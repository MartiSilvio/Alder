```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.2 (24G325) [Darwin 24.6.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD
  Job-JMXUSA : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD

Runtime=.NET 8.0  IterationCount=12  WarmupCount=4  
Categories=Lifecycle  

```
| Method                            | Mean         | Error      | StdDev     | Median       | Min          | Max          | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|--------:|-------:|----------:|------------:|
| NCalc_CreateExpression            |     52.96 ns |   0.179 ns |   0.130 ns |     53.02 ns |     52.75 ns |     53.10 ns |  0.04 |    0.00 |  0.0488 |      - |     408 B |        0.05 |
| CsEval_CreateCompiledEngine       |  1,449.59 ns |   4.377 ns |   3.417 ns |  1,449.77 ns |  1,444.32 ns |  1,455.78 ns |  1.00 |    0.00 |  1.0777 | 0.0305 |    9024 B |        1.00 |
| CsEval_CreateInterpretedEngine    |  1,450.32 ns |   3.740 ns |   2.920 ns |  1,450.04 ns |  1,446.73 ns |  1,455.38 ns |  1.00 |    0.00 |  1.0777 | 0.0305 |    9024 B |        1.00 |
| DynamicExpresso_CreateInterpreter | 86,857.46 ns | 164.449 ns | 108.773 ns | 86,867.89 ns | 86,688.35 ns | 87,005.21 ns | 59.89 |    0.14 |  3.6621 |      - |   31564 B |        3.50 |
| Flee_CreateContext                | 91,240.04 ns | 625.420 ns | 488.287 ns | 91,180.55 ns | 90,454.14 ns | 92,106.13 ns | 62.91 |    0.35 | 16.8457 | 3.4180 |  142133 B |       15.75 |
