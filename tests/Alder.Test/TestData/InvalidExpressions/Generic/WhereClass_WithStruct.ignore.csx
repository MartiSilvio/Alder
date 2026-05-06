// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
// §8.4.5: class constraint violated — int is a value type
T F<T>() where T : class => default(T);
return F<int>();
