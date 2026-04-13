// Known limitation: generic type parameters on local functions are not supported.
T Id<T>(T x) => x;
return Id(42);
