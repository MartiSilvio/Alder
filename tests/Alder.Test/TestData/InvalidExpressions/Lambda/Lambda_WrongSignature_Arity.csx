// §10.7.1: 2-param lambda cannot convert to Func<int,int> (parameter count mismatch)
Func<int, int> f = (x, y) => x + y;
return f(1);
