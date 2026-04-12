// §6.4.4: 'from' as lambda parameter name
var nums = new[] { 5, 10, 15, 20 };
return nums.Where(from => from >= 10).Count();
