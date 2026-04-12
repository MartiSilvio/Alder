var nums = new[] { 1, 2, 3, 4, 1, 2 };
return nums.SkipWhile(x => x < 4).Sum();
