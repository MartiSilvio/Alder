// §6.4.4: 'select' as lambda parameter name
var nums = new[] { 1, 2, 3 };
return nums.Aggregate(0, (acc, select) => acc + select * select);
