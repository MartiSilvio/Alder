var nums = new[] { 1, 2, 3, 4 };
return nums.Aggregate(100, (acc, x) => acc + x);
