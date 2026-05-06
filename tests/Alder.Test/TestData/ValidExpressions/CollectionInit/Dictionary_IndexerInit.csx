// §12.8.16.3: indexer-style object initializer on dictionary
var d = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
return d["a"] + d["b"];
