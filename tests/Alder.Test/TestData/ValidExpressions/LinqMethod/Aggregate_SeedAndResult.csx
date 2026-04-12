var nums = new[] { 1, 2, 3, 4 };
return nums.Aggregate(0, (acc, x) => acc + x, result => result * 2);
