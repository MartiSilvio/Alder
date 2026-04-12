// §8.4.5: object does not implement IComparable<object>
T F<T>() where T : IComparable<T> => default(T);
return F<object>();
