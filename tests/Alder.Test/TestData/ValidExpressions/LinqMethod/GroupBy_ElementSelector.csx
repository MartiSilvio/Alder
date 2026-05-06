var nums = new[] { 1, 2, 3, 4, 5, 6 };
return nums.GroupBy(x => x % 2, x => x * 10).First().Sum();
