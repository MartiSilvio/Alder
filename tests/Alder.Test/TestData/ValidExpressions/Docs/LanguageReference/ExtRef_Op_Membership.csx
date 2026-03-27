var a = 3 in new[] { 1, 2, 3, 4 };
var b = 5 not in new[] { 1, 2, 3, 4 };
return a and b;
