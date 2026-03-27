var list = new List<int> { 1, 2, 3 };
var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
var arr = new[] { 10, 20, 30 };
var sized = new int[5];
return list.Count + dict.Count + arr.Length + sized.Length;
