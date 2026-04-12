// §12.8.9.2: method invocation — Dictionary.TryGetValue with out var
var dict = new Dictionary<string, int> { { "key", 42 } };
if (dict.TryGetValue("key", out var val))
    return val;
return -1;
