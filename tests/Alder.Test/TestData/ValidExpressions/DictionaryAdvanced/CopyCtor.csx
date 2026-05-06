var src = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
var copy = new Dictionary<string, int>(src);
return copy.Count;
