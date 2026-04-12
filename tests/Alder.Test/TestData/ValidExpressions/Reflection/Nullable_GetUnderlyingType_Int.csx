// §8.3.12: Nullable.GetUnderlyingType returns the wrapped value type
return Nullable.GetUnderlyingType(typeof(int?)) == typeof(int);
