{ Func<int, int> dbl = x => x * 2; Func<int, int> inc = x => x + 1; return inc(dbl(5)); }
