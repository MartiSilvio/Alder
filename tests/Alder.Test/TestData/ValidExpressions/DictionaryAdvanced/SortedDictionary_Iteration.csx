var d = new SortedDictionary<string, int> { ["c"] = 3, ["a"] = 1, ["b"] = 2 };
var keys = new List<string>();
foreach (var kvp in d)
    keys.Add(kvp.Key);
return keys;
