// §6.4.4: 'when' as lambda parameter name
var nums = new[] { 3, 1, 4, 1, 5 };
return nums.OrderBy(when => when).First();
