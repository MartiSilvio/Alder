Func<int, Func<int, int>> adder = x => y => x + y;
var add5 = adder(5);
return add5(3);
