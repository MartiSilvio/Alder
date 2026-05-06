Func<int> f = () => 42;
Func<int> g = f;
return f == g;
