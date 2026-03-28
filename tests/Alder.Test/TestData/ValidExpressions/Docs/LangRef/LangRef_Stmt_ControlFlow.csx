var sum = 0;
for (var i = 0; i < 5; i++) { sum += i; }
var items = new List<int>();
foreach (var n in new[] { 1, 2, 3 }) { items.Add(n * 10); }
return sum == 10 && items.Count == 3 && items[2] == 30;
