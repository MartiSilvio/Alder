// §12.21.2: KeyValuePair<,>.Deconstruct used directly
var kvp = new KeyValuePair<string, int>("hello", 42);
var (key, value) = kvp;
return key.Length + value;
