// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
// §8.4.5: object does not implement IComparable<object>
T F<T>() where T : IComparable<T> => default(T);
return F<object>();
