var items = new[] { 5, 3, 8, 1, 9, 2 };
var filtered = items.Where(x => x > 3).OrderBy(x => x).ToList();
var sum = items.Sum();
var avg = items.Average();
var count = items.Count();
return filtered.Count == 3 && filtered[0] == 5 && sum == 28 && count == 6;
