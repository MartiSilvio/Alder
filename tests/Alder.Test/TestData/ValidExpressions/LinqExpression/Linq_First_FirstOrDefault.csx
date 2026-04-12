var list = new List<int> { 10, 20, 30 };
var empty = new List<int>();
return list.First() + empty.FirstOrDefault();
