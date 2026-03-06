```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.2 (24G325) [Darwin 24.6.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD
  Job-JMXUSA : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD

Runtime=.NET 8.0  IterationCount=12  WarmupCount=4  
Categories=AdvancedLanguage  

```
| Method                      | Scenario             | Mean        | Error     | StdDev    | Median      | Min         | Max         | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| Flee                        | Advan(...)rties [29] |    23.46 ns |  0.499 ns |  0.389 ns |    23.39 ns |    23.00 ns |    24.21 ns |  0.34 |    0.01 | 0.0029 |      - |      24 B |        0.08 |
| CsEval_Compiled             | Advan(...)rties [29] |    24.02 ns |  0.182 ns |  0.131 ns |    24.03 ns |    23.79 ns |    24.22 ns |  0.34 |    0.00 | 0.0029 |      - |      24 B |        0.08 |
| DynamicExpresso             | Advan(...)rties [29] |    36.84 ns |  0.277 ns |  0.183 ns |    36.82 ns |    36.60 ns |    37.10 ns |  0.53 |    0.00 | 0.0057 |      - |      48 B |        0.16 |
| Roslyn_ScriptCompiledRunner | Advan(...)rties [29] |    69.71 ns |  0.223 ns |  0.161 ns |    69.69 ns |    69.50 ns |    69.99 ns |  1.00 |    0.00 | 0.0362 |      - |     304 B |        1.00 |
| CsEval_Interpreted          | Advan(...)rties [29] |   697.18 ns |  5.091 ns |  3.975 ns |   696.35 ns |   692.28 ns |   704.33 ns | 10.00 |    0.06 | 0.2079 |      - |    1744 B |        5.74 |
|                             |                      |             |           |           |             |             |             |       |         |        |        |           |             |
| DynamicExpresso             | Advan(...)ional [26] |    37.39 ns |  0.262 ns |  0.173 ns |    37.38 ns |    37.19 ns |    37.64 ns |  0.50 |    0.01 | 0.0057 |      - |      48 B |        0.16 |
| CsEval_Compiled             | Advan(...)ional [26] |    46.62 ns |  0.690 ns |  0.539 ns |    46.65 ns |    45.81 ns |    47.45 ns |  0.62 |    0.01 | 0.0057 |      - |      48 B |        0.16 |
| Flee                        | Advan(...)ional [26] |    47.28 ns |  0.792 ns |  0.618 ns |    47.11 ns |    46.43 ns |    48.48 ns |  0.63 |    0.01 | 0.0029 |      - |      24 B |        0.08 |
| Roslyn_ScriptCompiledRunner | Advan(...)ional [26] |    75.41 ns |  1.704 ns |  1.330 ns |    75.58 ns |    72.60 ns |    77.12 ns |  1.00 |    0.02 | 0.0362 |      - |     304 B |        1.00 |
| CsEval_Interpreted          | Advan(...)ional [26] |   588.07 ns |  7.663 ns |  5.068 ns |   587.93 ns |   580.29 ns |   594.77 ns |  7.80 |    0.15 | 0.1688 |      - |    1416 B |        4.66 |
|                             |                      |             |           |           |             |             |             |       |         |        |        |           |             |
| DynamicExpresso             | Advanced/NestedMath  |    35.29 ns |  0.437 ns |  0.342 ns |    35.23 ns |    34.80 ns |    35.93 ns |  0.47 |    0.01 | 0.0057 | 0.0001 |      48 B |        0.16 |
| Flee                        | Advanced/NestedMath  |    46.43 ns |  0.569 ns |  0.444 ns |    46.48 ns |    45.56 ns |    47.07 ns |  0.62 |    0.01 | 0.0029 |      - |      24 B |        0.08 |
| Roslyn_ScriptCompiledRunner | Advanced/NestedMath  |    74.34 ns |  1.179 ns |  0.920 ns |    74.09 ns |    72.90 ns |    76.12 ns |  1.00 |    0.02 | 0.0362 |      - |     304 B |        1.00 |
| CsEval_Compiled             | Advanced/NestedMath  | 3,503.10 ns | 52.105 ns | 40.681 ns | 3,498.28 ns | 3,453.28 ns | 3,591.26 ns | 47.13 |    0.77 | 0.7706 |      - |    6456 B |       21.24 |
| CsEval_Interpreted          | Advanced/NestedMath  | 4,344.62 ns | 25.119 ns | 19.611 ns | 4,344.19 ns | 4,318.77 ns | 4,377.31 ns | 58.45 |    0.74 | 1.0071 |      - |    8432 B |       27.74 |
|                             |                      |             |           |           |             |             |             |       |         |        |        |           |             |
| Flee                        | Advan(...)ccess [26] |    30.32 ns |  0.039 ns |  0.028 ns |    30.33 ns |    30.27 ns |    30.36 ns |  0.42 |    0.01 | 0.0029 |      - |      24 B |        0.08 |
| DynamicExpresso             | Advan(...)ccess [26] |    35.82 ns |  0.092 ns |  0.072 ns |    35.81 ns |    35.71 ns |    35.95 ns |  0.49 |    0.01 | 0.0057 |      - |      48 B |        0.16 |
| Roslyn_ScriptCompiledRunner | Advan(...)ccess [26] |    73.03 ns |  1.433 ns |  1.119 ns |    72.75 ns |    71.85 ns |    75.03 ns |  1.00 |    0.02 | 0.0362 |      - |     304 B |        1.00 |
| CsEval_Compiled             | Advan(...)ccess [26] |   375.04 ns | 10.596 ns |  8.272 ns |   371.34 ns |   367.20 ns |   386.51 ns |  5.14 |    0.13 | 0.0772 |      - |     648 B |        2.13 |
| CsEval_Interpreted          | Advan(...)ccess [26] |   967.81 ns | 22.763 ns | 17.772 ns |   961.89 ns |   945.05 ns | 1,001.09 ns | 13.26 |    0.30 | 0.2403 |      - |    2024 B |        6.66 |
|                             |                      |             |           |           |             |             |             |       |         |        |        |           |             |
| Flee                        | Advan(...)icate [24] |    27.39 ns |  0.338 ns |  0.264 ns |    27.32 ns |    27.09 ns |    27.71 ns |  0.36 |    0.00 | 0.0029 | 0.0000 |      24 B |        0.08 |
| CsEval_Compiled             | Advan(...)icate [24] |    31.90 ns |  0.116 ns |  0.084 ns |    31.93 ns |    31.76 ns |    31.99 ns |  0.42 |    0.00 | 0.0029 |      - |      24 B |        0.08 |
| DynamicExpresso             | Advan(...)icate [24] |    41.01 ns |  0.143 ns |  0.111 ns |    41.00 ns |    40.83 ns |    41.17 ns |  0.55 |    0.00 | 0.0057 |      - |      48 B |        0.16 |
| Roslyn_ScriptCompiledRunner | Advan(...)icate [24] |    75.16 ns |  0.612 ns |  0.442 ns |    75.12 ns |    74.43 ns |    76.04 ns |  1.00 |    0.01 | 0.0362 |      - |     304 B |        1.00 |
| CsEval_Interpreted          | Advan(...)icate [24] | 1,011.67 ns | 12.550 ns |  9.798 ns | 1,010.10 ns |   996.89 ns | 1,026.99 ns | 13.46 |    0.15 | 0.3185 |      - |    2672 B |        8.79 |
