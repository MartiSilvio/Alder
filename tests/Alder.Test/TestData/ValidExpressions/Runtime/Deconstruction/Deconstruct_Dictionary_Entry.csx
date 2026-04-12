// §12.21.2: foreach dictionary with KeyValuePair deconstruction
var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
int total = 0;
foreach (var (k, v) in dict)
    total += k.Length + v;
return total;
