// §8.4.5: int does not derive from Exception
T F<T>() where T : Exception => default(T);
return F<int>();
