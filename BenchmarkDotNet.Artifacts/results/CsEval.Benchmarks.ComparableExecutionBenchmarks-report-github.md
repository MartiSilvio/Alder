```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.2 (24G325) [Darwin 24.6.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD
  Job-JMXUSA : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD

Runtime=.NET 8.0  IterationCount=12  WarmupCount=4  
Categories=WarmExecution  

```
| Method                      | Scenario             | Mean          | Error       | StdDev      | Median        | Min           | Max           | Ratio    | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |--------------:|------------:|------------:|--------------:|--------------:|--------------:|---------:|--------:|--------:|-------:|----------:|------------:|
| NativeDelegate_Baseline     | Arith(...)ality [25] |      2.942 ns |   0.0156 ns |   0.0113 ns |      2.937 ns |      2.929 ns |      2.959 ns |     1.00 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Arith(...)ality [25] |     19.500 ns |   0.0591 ns |   0.0461 ns |     19.479 ns |     19.459 ns |     19.582 ns |     6.63 |    0.03 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Arith(...)ality [25] |     20.381 ns |   0.2492 ns |   0.1945 ns |     20.357 ns |     20.130 ns |     20.820 ns |     6.93 |    0.07 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Arith(...)ality [25] |     35.121 ns |   0.2551 ns |   0.1991 ns |     35.126 ns |     34.826 ns |     35.454 ns |    11.94 |    0.08 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Arith(...)ality [25] |     69.471 ns |   0.7714 ns |   0.5577 ns |     69.277 ns |     68.918 ns |     70.496 ns |    23.62 |    0.20 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Arith(...)ality [25] |    164.585 ns |   0.3661 ns |   0.2647 ns |    164.572 ns |    164.224 ns |    165.125 ns |    55.95 |    0.22 |  0.0908 |      - |     760 B |       31.67 |
| CsEval_Interpreted          | Arith(...)ality [25] |    308.711 ns |   1.4152 ns |   1.1049 ns |    308.700 ns |    306.972 ns |    310.476 ns |   104.94 |    0.53 |  0.1001 |      - |     840 B |       35.00 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Arith(...)dence [21] |      2.145 ns |   0.0274 ns |   0.0214 ns |      2.144 ns |      2.121 ns |      2.193 ns |     1.00 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Arith(...)dence [21] |      3.451 ns |   0.0167 ns |   0.0120 ns |      3.450 ns |      3.427 ns |      3.475 ns |     1.61 |    0.02 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Arith(...)dence [21] |      3.633 ns |   0.0117 ns |   0.0091 ns |      3.633 ns |      3.616 ns |      3.648 ns |     1.69 |    0.02 |       - |      - |         - |        0.00 |
| DynamicExpresso             | Arith(...)dence [21] |     34.711 ns |   0.1453 ns |   0.1134 ns |     34.683 ns |     34.585 ns |     34.927 ns |    16.19 |    0.16 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Arith(...)dence [21] |     66.111 ns |   0.2410 ns |   0.1743 ns |     66.127 ns |     65.883 ns |     66.451 ns |    30.83 |    0.30 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Arith(...)dence [21] |    284.220 ns |   1.0086 ns |   0.7875 ns |    284.154 ns |    283.098 ns |    285.736 ns |   132.53 |    1.31 |  0.1411 |      - |    1184 B |       49.33 |
| CsEval_Interpreted          | Arith(...)dence [21] |    560.199 ns |   3.2617 ns |   2.5465 ns |    559.487 ns |    556.577 ns |    565.594 ns |   261.21 |    2.74 |  0.2069 |      - |    1736 B |       72.33 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Arith(...)ables [24] |      2.667 ns |   0.0170 ns |   0.0113 ns |      2.665 ns |      2.652 ns |      2.686 ns |     1.00 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Arith(...)ables [24] |     32.783 ns |   0.4905 ns |   0.3829 ns |     32.582 ns |     32.484 ns |     33.631 ns |    12.29 |    0.15 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Arith(...)ables [24] |     34.854 ns |   0.0722 ns |   0.0564 ns |     34.864 ns |     34.756 ns |     34.925 ns |    13.07 |    0.06 |  0.0057 | 0.0001 |      48 B |        2.00 |
| Flee                        | Arith(...)ables [24] |     37.265 ns |   0.0817 ns |   0.0591 ns |     37.245 ns |     37.202 ns |     37.358 ns |    13.97 |    0.06 |  0.0029 |      - |      24 B |        1.00 |
| Roslyn_ScriptCompiledRunner | Arith(...)ables [24] |     70.703 ns |   0.1771 ns |   0.1383 ns |     70.688 ns |     70.499 ns |     70.901 ns |    26.51 |    0.12 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Arith(...)ables [24] |    340.862 ns |   1.1705 ns |   0.9139 ns |    340.977 ns |    339.159 ns |    342.155 ns |   127.81 |    0.61 |  0.1683 |      - |    1408 B |       58.67 |
| CsEval_Interpreted          | Arith(...)ables [24] |    688.493 ns |   4.4111 ns |   3.4439 ns |    687.558 ns |    683.956 ns |    693.884 ns |   258.17 |    1.62 |  0.2069 |      - |    1736 B |       72.33 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Boolean/Composite    |      2.685 ns |   0.0053 ns |   0.0035 ns |      2.684 ns |      2.681 ns |      2.690 ns |     1.00 |    0.00 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Boolean/Composite    |     33.491 ns |   0.0648 ns |   0.0429 ns |     33.489 ns |     33.439 ns |     33.571 ns |    12.47 |    0.02 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Boolean/Composite    |     34.728 ns |   0.0848 ns |   0.0662 ns |     34.724 ns |     34.627 ns |     34.879 ns |    12.93 |    0.03 |  0.0057 |      - |      48 B |        2.00 |
| Flee                        | Boolean/Composite    |     37.949 ns |   0.1696 ns |   0.1324 ns |     37.905 ns |     37.763 ns |     38.163 ns |    14.13 |    0.05 |  0.0029 |      - |      24 B |        1.00 |
| Roslyn_ScriptCompiledRunner | Boolean/Composite    |     72.212 ns |   0.1948 ns |   0.1408 ns |     72.148 ns |     72.004 ns |     72.510 ns |    26.90 |    0.06 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Boolean/Composite    |    353.249 ns |   2.1066 ns |   1.3934 ns |    352.818 ns |    351.955 ns |    356.763 ns |   131.57 |    0.52 |  0.1788 |      - |    1496 B |       62.33 |
| CsEval_Interpreted          | Boolean/Composite    |    509.271 ns |   3.0406 ns |   2.0112 ns |    508.615 ns |    506.768 ns |    512.634 ns |   189.68 |    0.75 |  0.1745 |      - |    1464 B |       61.00 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| DynamicExpresso             | Compe(...)tress [32] |     36.123 ns |   0.2802 ns |   0.2026 ns |     36.141 ns |     35.608 ns |     36.353 ns |     0.55 |    0.00 |  0.0057 | 0.0001 |      48 B |        2.00 |
| NativeDelegate_Baseline     | Compe(...)tress [32] |     65.600 ns |   0.1067 ns |   0.0772 ns |     65.628 ns |     65.424 ns |     65.676 ns |     1.00 |    0.00 |  0.0029 |      - |      24 B |        1.00 |
| Roslyn_ScriptCompiledRunner | Compe(...)tress [32] |    193.383 ns |   6.1241 ns |   4.7813 ns |    196.257 ns |    185.102 ns |    197.095 ns |     2.95 |    0.07 |  0.0362 |      - |     304 B |       12.67 |
| CsEval_Compiled             | Compe(...)tress [32] |    578.968 ns |   1.4382 ns |   1.0399 ns |    579.067 ns |    576.787 ns |    580.315 ns |     8.83 |    0.02 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Compe(...)tress [32] |    684.997 ns |   3.0272 ns |   2.3634 ns |    685.575 ns |    680.858 ns |    687.779 ns |    10.44 |    0.04 |  0.0029 |      - |      24 B |        1.00 |
| NCalc                       | Compe(...)tress [32] | 13,642.499 ns |  19.7478 ns |  15.4178 ns | 13,644.395 ns | 13,610.867 ns | 13,660.025 ns |   207.97 |    0.33 |  7.1106 | 0.1831 |   59497 B |    2,479.04 |
| CsEval_Interpreted          | Compe(...)tress [32] | 42,080.056 ns | 164.9480 ns | 128.7806 ns | 42,130.933 ns | 41,826.027 ns | 42,230.245 ns |   641.47 |    2.02 | 13.2446 | 0.0610 |  111178 B |    4,632.42 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Compe(...)ition [38] |      2.693 ns |   0.0193 ns |   0.0128 ns |      2.690 ns |      2.675 ns |      2.718 ns |     1.00 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Compe(...)ition [38] |     19.206 ns |   0.0388 ns |   0.0281 ns |     19.208 ns |     19.135 ns |     19.246 ns |     7.13 |    0.03 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Compe(...)ition [38] |     19.258 ns |   0.0386 ns |   0.0301 ns |     19.257 ns |     19.215 ns |     19.318 ns |     7.15 |    0.03 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Compe(...)ition [38] |     35.180 ns |   0.2148 ns |   0.1677 ns |     35.178 ns |     34.935 ns |     35.465 ns |    13.06 |    0.08 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Compe(...)ition [38] |     67.843 ns |   0.1489 ns |   0.1077 ns |     67.822 ns |     67.733 ns |     68.040 ns |    25.19 |    0.12 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Compe(...)ition [38] |     89.406 ns |   0.2261 ns |   0.1765 ns |     89.385 ns |     89.052 ns |     89.655 ns |    33.20 |    0.16 |  0.0488 |      - |     408 B |       17.00 |
| CsEval_Interpreted          | Compe(...)ition [38] |    210.182 ns |   0.9430 ns |   0.6237 ns |    210.041 ns |    209.706 ns |    211.796 ns |    78.05 |    0.42 |  0.0677 |      - |     568 B |       23.67 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Compe(...)ching [30] |      2.763 ns |   0.0243 ns |   0.0176 ns |      2.764 ns |      2.739 ns |      2.793 ns |     1.00 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Compe(...)ching [30] |     19.540 ns |   0.0505 ns |   0.0394 ns |     19.548 ns |     19.469 ns |     19.597 ns |     7.07 |    0.05 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Compe(...)ching [30] |     32.465 ns |   0.5127 ns |   0.4002 ns |     32.382 ns |     31.834 ns |     33.250 ns |    11.75 |    0.16 |  0.0057 |      - |      48 B |        2.00 |
| DynamicExpresso             | Compe(...)ching [30] |     34.718 ns |   0.2129 ns |   0.1540 ns |     34.653 ns |     34.583 ns |     35.011 ns |    12.57 |    0.09 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Compe(...)ching [30] |     69.416 ns |   0.6837 ns |   0.5338 ns |     69.192 ns |     68.861 ns |     70.634 ns |    25.12 |    0.24 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Compe(...)ching [30] |  1,061.860 ns |   5.6052 ns |   4.0529 ns |  1,061.940 ns |  1,056.697 ns |  1,068.699 ns |   384.32 |    2.73 |  0.5150 |      - |    4312 B |      179.67 |
| CsEval_Interpreted          | Compe(...)ching [30] |  1,795.867 ns |  19.5550 ns |  15.2673 ns |  1,791.262 ns |  1,777.939 ns |  1,825.193 ns |   649.98 |    6.63 |  0.7000 | 0.0057 |    5856 B |      244.00 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Compe(...)ality [42] |      2.922 ns |   0.0204 ns |   0.0135 ns |      2.920 ns |      2.899 ns |      2.947 ns |     1.00 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Compe(...)ality [42] |     30.935 ns |   1.1253 ns |   0.8785 ns |     30.717 ns |     29.967 ns |     32.463 ns |    10.59 |    0.29 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Compe(...)ality [42] |     34.745 ns |   0.0873 ns |   0.0631 ns |     34.746 ns |     34.635 ns |     34.841 ns |    11.89 |    0.06 |  0.0057 |      - |      48 B |        2.00 |
| CsEval_Compiled             | Compe(...)ality [42] |     40.099 ns |   0.0430 ns |   0.0256 ns |     40.103 ns |     40.069 ns |     40.144 ns |    13.73 |    0.06 |  0.0086 |      - |      72 B |        3.00 |
| Roslyn_ScriptCompiledRunner | Compe(...)ality [42] |     69.230 ns |   0.1959 ns |   0.1417 ns |     69.235 ns |     68.989 ns |     69.435 ns |    23.70 |    0.11 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Compe(...)ality [42] |    427.080 ns |   1.0370 ns |   0.7498 ns |    427.197 ns |    425.795 ns |    428.087 ns |   146.19 |    0.69 |  0.2141 |      - |    1792 B |       74.67 |
| CsEval_Interpreted          | Compe(...)ality [42] |    648.195 ns |   2.7251 ns |   1.9704 ns |    647.655 ns |    646.166 ns |    651.290 ns |   221.87 |    1.17 |  0.1831 |      - |    1536 B |       64.00 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Compe(...)ation [33] |      2.109 ns |   0.0096 ns |   0.0075 ns |      2.111 ns |      2.090 ns |      2.120 ns |     1.00 |    0.00 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Compe(...)ation [33] |      3.461 ns |   0.0167 ns |   0.0131 ns |      3.459 ns |      3.444 ns |      3.484 ns |     1.64 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Compe(...)ation [33] |      7.437 ns |   0.0176 ns |   0.0137 ns |      7.437 ns |      7.407 ns |      7.460 ns |     3.53 |    0.01 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Compe(...)ation [33] |     34.930 ns |   0.0444 ns |   0.0294 ns |     34.924 ns |     34.891 ns |     34.992 ns |    16.57 |    0.06 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Compe(...)ation [33] |     66.693 ns |   0.0437 ns |   0.0289 ns |     66.701 ns |     66.646 ns |     66.727 ns |    31.63 |    0.11 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Compe(...)ation [33] |    121.795 ns |   0.2783 ns |   0.2173 ns |    121.734 ns |    121.511 ns |    122.122 ns |    57.76 |    0.22 |  0.0772 |      - |     648 B |       27.00 |
| CsEval_Interpreted          | Compe(...)ation [33] |    255.371 ns |   1.0307 ns |   0.7453 ns |    255.245 ns |    254.269 ns |    256.565 ns |   121.11 |    0.54 |  0.0944 |      - |     792 B |       33.00 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Conditional/Ternary  |      2.691 ns |   0.0065 ns |   0.0047 ns |      2.691 ns |      2.685 ns |      2.702 ns |     1.00 |    0.00 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Conditional/Ternary  |     28.837 ns |   0.1255 ns |   0.0980 ns |     28.820 ns |     28.726 ns |     29.025 ns |    10.71 |    0.04 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Conditional/Ternary  |     29.046 ns |   1.3558 ns |   1.0585 ns |     29.181 ns |     27.482 ns |     30.210 ns |    10.79 |    0.38 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Conditional/Ternary  |     35.368 ns |   0.1705 ns |   0.1331 ns |     35.381 ns |     35.119 ns |     35.578 ns |    13.14 |    0.05 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Conditional/Ternary  |     70.550 ns |   0.2264 ns |   0.1768 ns |     70.519 ns |     70.314 ns |     70.818 ns |    26.21 |    0.08 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Conditional/Ternary  |    222.402 ns |   1.1360 ns |   0.8869 ns |    222.232 ns |    221.483 ns |    224.093 ns |    82.63 |    0.35 |  0.1185 |      - |     992 B |       41.33 |
| CsEval_Interpreted          | Conditional/Ternary  |    331.461 ns |   1.9371 ns |   1.5123 ns |    331.738 ns |    328.257 ns |    333.216 ns |   123.15 |    0.58 |  0.0973 |      - |     816 B |       34.00 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Functions/MathMix    |      2.674 ns |   0.0074 ns |   0.0058 ns |      2.675 ns |      2.662 ns |      2.683 ns |     1.00 |    0.00 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Functions/MathMix    |     34.665 ns |   0.0877 ns |   0.0580 ns |     34.672 ns |     34.569 ns |     34.743 ns |    12.96 |    0.03 |  0.0057 |      - |      48 B |        2.00 |
| Flee                        | Functions/MathMix    |     37.704 ns |   0.1980 ns |   0.1545 ns |     37.755 ns |     37.446 ns |     37.899 ns |    14.10 |    0.06 |  0.0029 |      - |      24 B |        1.00 |
| Roslyn_ScriptCompiledRunner | Functions/MathMix    |     71.468 ns |   0.1594 ns |   0.1055 ns |     71.479 ns |     71.255 ns |     71.655 ns |    26.72 |    0.07 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Functions/MathMix    |    344.978 ns |   2.9703 ns |   2.1477 ns |    344.538 ns |    342.689 ns |    348.990 ns |   128.99 |    0.81 |  0.1702 |      - |    1424 B |       59.33 |
| CsEval_Compiled             | Functions/MathMix    |  3,485.758 ns |  10.7179 ns |   8.3678 ns |  3,484.364 ns |  3,474.945 ns |  3,502.758 ns | 1,303.40 |    4.04 |  0.7706 |      - |    6456 B |      269.00 |
| CsEval_Interpreted          | Functions/MathMix    |  4,080.559 ns |  15.9965 ns |  12.4890 ns |  4,078.124 ns |  4,064.771 ns |  4,103.617 ns | 1,525.81 |    5.49 |  0.9766 |      - |    8200 B |      341.67 |
|                             |                      |               |             |             |               |               |               |          |         |         |        |           |             |
| NativeDelegate_Baseline     | Mix/N(...)icate [23] |      2.733 ns |   0.0092 ns |   0.0066 ns |      2.732 ns |      2.726 ns |      2.748 ns |     1.00 |    0.00 |  0.0029 |      - |      24 B |        1.00 |
| CsEval_Compiled             | Mix/N(...)icate [23] |     25.802 ns |   0.0159 ns |   0.0105 ns |     25.802 ns |     25.785 ns |     25.817 ns |     9.44 |    0.02 |  0.0029 |      - |      24 B |        1.00 |
| Flee                        | Mix/N(...)icate [23] |     28.472 ns |   0.0799 ns |   0.0624 ns |     28.479 ns |     28.375 ns |     28.578 ns |    10.42 |    0.03 |  0.0029 |      - |      24 B |        1.00 |
| DynamicExpresso             | Mix/N(...)icate [23] |     35.538 ns |   0.0938 ns |   0.0678 ns |     35.518 ns |     35.467 ns |     35.671 ns |    13.00 |    0.04 |  0.0057 |      - |      48 B |        2.00 |
| Roslyn_ScriptCompiledRunner | Mix/N(...)icate [23] |     69.101 ns |   0.1788 ns |   0.1293 ns |     69.087 ns |     68.983 ns |     69.394 ns |    25.28 |    0.07 |  0.0362 |      - |     304 B |       12.67 |
| NCalc                       | Mix/N(...)icate [23] |    307.727 ns |   3.7907 ns |   2.9595 ns |    309.098 ns |    303.507 ns |    311.132 ns |   112.58 |    1.07 |  0.1626 |      - |    1360 B |       56.67 |
| CsEval_Interpreted          | Mix/N(...)icate [23] |    614.024 ns |   2.8506 ns |   2.0611 ns |    613.399 ns |    612.255 ns |    618.113 ns |   224.65 |    0.89 |  0.2069 |      - |    1736 B |       72.33 |
