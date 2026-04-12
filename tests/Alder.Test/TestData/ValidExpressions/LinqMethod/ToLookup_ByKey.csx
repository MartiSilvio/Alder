var nums = new[] { 1, 2, 3, 4, 5 };
var lookup = nums.ToLookup(x => x % 2);
return lookup[1].Count();
