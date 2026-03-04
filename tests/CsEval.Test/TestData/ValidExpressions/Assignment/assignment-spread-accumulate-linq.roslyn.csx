var items = new[] { 1, 2, 3 };
items = items.Concat(new[] { 4, 5 }).ToArray();
var filtered = items.Where(x => x > 2).ToList();
return filtered;
