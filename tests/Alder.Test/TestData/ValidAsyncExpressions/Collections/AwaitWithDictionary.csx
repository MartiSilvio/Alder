var dict = new Dictionary<string, int>();
dict["a"] = await Task.FromResult(1);
dict["b"] = await Task.FromResult(2);
return dict["a"] + dict["b"];
