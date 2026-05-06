var nums = new List<int> { 1, 2, 3, 4, 5 };
return nums.Where(x => x % 2 == 0).Sum();
