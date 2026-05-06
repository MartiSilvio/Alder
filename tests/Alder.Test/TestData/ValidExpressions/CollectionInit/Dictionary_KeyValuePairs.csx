// §12.8.16.4: dictionary collection initializer via Add-style pairs
var d = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
return d["a"] + d["b"];
