var list = new List<int> { 1, 2, 3, 4, 5 };
var empty = new List<int>();
return list.Last() + empty.LastOrDefault();
