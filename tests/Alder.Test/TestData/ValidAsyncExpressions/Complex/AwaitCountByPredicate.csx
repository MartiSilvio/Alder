// §15.15: Count LINQ on an awaited list with predicate
var nums = await Task.FromResult(new List<int> { 1, 2, 3, 4, 5, 6 });
return nums.Count(n => n % 2 == 0);
