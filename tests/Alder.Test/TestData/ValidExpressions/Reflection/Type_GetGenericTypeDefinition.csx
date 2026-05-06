// §8.4.4: GetGenericTypeDefinition yields the unbound generic type
return typeof(List<int>).GetGenericTypeDefinition() == typeof(List<>);
