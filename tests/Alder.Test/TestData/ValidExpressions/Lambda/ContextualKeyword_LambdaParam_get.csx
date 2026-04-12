// §6.4.4: 'get' as lambda parameter name
var nums = new[] { 10, 20, 30 };
return nums.Select(get => get * 2).First();
