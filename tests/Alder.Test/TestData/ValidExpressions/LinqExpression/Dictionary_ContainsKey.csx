// §12.8.9.2: method invocation — Dictionary.ContainsKey
var dict = new Dictionary<string, int> { { "x", 1 } };
return dict.ContainsKey("x") && !dict.ContainsKey("y");
