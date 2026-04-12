// §12.12.12: is pattern — int can never be string (always false, but should compile)
// Actually this compiles in C# — it just returns false. Let me test something that truly fails.
// Testing: declaration pattern with nullable type is a compile error per §11.2.2
int x = 5;
if (x is int? y)
    return y;
return 0;
