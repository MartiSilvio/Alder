var list = new List<int> { 1, 2, 2, 3, 3, 3 };
var set = list.ToHashSet();
return set.Count;
