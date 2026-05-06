// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
// §8.4.5: int does not derive from Exception
T F<T>() where T : Exception => default(T);
return F<int>();
