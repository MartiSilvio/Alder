var nums = new[] { 1, 2, 3, 4, 5 };
var sum = await Task.FromResult(nums.Where(x => x > 2).Sum());
return sum;
