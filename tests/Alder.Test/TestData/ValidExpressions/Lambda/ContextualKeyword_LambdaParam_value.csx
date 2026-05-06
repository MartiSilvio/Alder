// §6.4.4: 'value' as lambda parameter name
var nums = new[] { 1, 2, 3, 4, 5 };
return nums.Where(value => value > 3).Count();
