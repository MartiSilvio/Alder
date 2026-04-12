// §8.4.5: class constraint violated — int is a value type
T F<T>() where T : class => default(T);
return F<int>();
