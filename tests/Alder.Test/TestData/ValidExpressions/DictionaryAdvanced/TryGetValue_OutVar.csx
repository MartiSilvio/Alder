// §12.17: declaration expression — out var in TryGetValue call
var d = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
return d.TryGetValue("a", out var v) ? v : -1;
