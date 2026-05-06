// §15.15: await dictionary then index lookup
var dict = await Task.FromResult(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
return dict["b"];
