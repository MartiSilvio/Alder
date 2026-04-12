// §8.4.2: GetGenericArguments returns the supplied type arguments
return typeof(List<int>).GetGenericArguments()[0] == typeof(int);
