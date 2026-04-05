var dict = new Dictionary<string, int>();
dict["a"] = 1;
dict["b"] = 2;
var sum = 0;
foreach (var kvp in dict)
    sum += kvp.Value;
return sum;
