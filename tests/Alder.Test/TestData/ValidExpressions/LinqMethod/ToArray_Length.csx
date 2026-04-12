var nums = new List<int> { 1, 2, 3 };
return nums.Where(x => x > 1).ToArray().Length;
