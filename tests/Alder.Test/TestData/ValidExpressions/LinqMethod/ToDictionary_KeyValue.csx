var source = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
var dict = source.ToDictionary(x => x.Key, x => x.Value * 10);
return dict["b"];
