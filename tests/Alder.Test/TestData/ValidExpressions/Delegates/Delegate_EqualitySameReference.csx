// §20.5: Delegate equality - same lambda reference
Func<int, int> f = x => x + 1;
Func<int, int> g = f;
return f == g;
