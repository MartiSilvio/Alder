var items = new List<int> { 1, 2, 3, 4, 5, 6 };
var groups = from x in items group x by x % 2 into g select g.Count();
return groups.Sum();
