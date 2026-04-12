// §20.5: Function composition via lambda-instantiated delegates
Func<int, int> doubler = x => x * 2;
Func<int, int> incr = x => x + 1;
Func<int, int> composed = x => incr(doubler(x));
return composed(5);
