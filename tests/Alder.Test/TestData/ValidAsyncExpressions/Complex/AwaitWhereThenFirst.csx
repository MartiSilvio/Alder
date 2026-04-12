// §15.15: LINQ Where + First on an awaited list
var nums = await Task.FromResult(new List<int> { 1, 2, 3, 4, 5 });
return nums.Where(n => n > 2).First();
