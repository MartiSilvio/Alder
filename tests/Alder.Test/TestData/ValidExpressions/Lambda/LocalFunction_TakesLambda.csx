// §13.6.4: local function accepting a Func<int,int> and applying it
int Apply(int x, Func<int, int> f) => f(x);
return Apply(7, n => n + 3);
